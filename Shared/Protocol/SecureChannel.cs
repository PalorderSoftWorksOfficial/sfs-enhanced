using System;
using System.Security.Cryptography;
using System.Text;

namespace SFSEnhanced.Shared.Protocol
{
    public static class SessionAuth
    {
        public const int ChallengeBytes = 32;
        public const int NonceBytes = 16;
        public const int SessionIdBytes = 16;
        public const int CoordinateBytes = 32;

        private const string AuthLabel = "SFSEnhanced-Auth-v2";
        private const string PreMasterLabel = "SFSEnhanced-PreMaster-v2";
        private const string TranscriptLabel = "SFSEnhanced-Transcript-v2";
        private const string SessionIdLabel = "SFSEnhanced-SessionId-v2";
        private const string IdentityLabel = "SFSEnhanced-ServerIdentity-v1";
        private const string RegistrationLabel = "SFSEnhanced-Registration-v2";

        public static byte[] RandomBytes(int count)
        {
            var bytes = new byte[count];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return bytes;
        }

        public static byte[] ComputeTokenKey(string authToken) => Sha256(Encoding.UTF8.GetBytes(authToken ?? string.Empty));

        public static byte[] ComputePasswordHash(string registrationPassword)
        {
            byte[] salt = Encoding.UTF8.GetBytes("SFSEnhanced-Registration-Salt-v1");
            byte[] data = Encoding.UTF8.GetBytes(registrationPassword ?? string.Empty);
            byte[] current = Concat(salt, data);
            for (int round = 0; round < 600000; round++)
            {
                current = Sha256(Concat(current, data));
            }
            return current;
        }

        public static byte[] ComputeRegistrationProof(byte[] passwordHash, byte[] transcriptHash)
        {
            using var hmac = new HMACSHA256(passwordHash);
            return hmac.ComputeHash(Concat(Encoding.UTF8.GetBytes(RegistrationLabel), transcriptHash));
        }

        public static byte[] ComputeTranscriptHash(byte[] clientNonce, byte[] serverChallenge, byte[] clientEphemeralX, byte[] clientEphemeralY, byte[] serverEphemeralX, byte[] serverEphemeralY, byte[] serverStaticX, byte[] serverStaticY, string playerName)
        {
            return Sha256(Concat(
                Encoding.UTF8.GetBytes(TranscriptLabel),
                clientNonce,
                serverChallenge,
                clientEphemeralX,
                clientEphemeralY,
                serverEphemeralX,
                serverEphemeralY,
                serverStaticX,
                serverStaticY,
                Encoding.UTF8.GetBytes(playerName ?? string.Empty)));
        }

        public static byte[] ComputeTokenProof(byte[] tokenKey, byte[] transcriptHash)
        {
            using var hmac = new HMACSHA256(tokenKey);
            return hmac.ComputeHash(Concat(Encoding.UTF8.GetBytes(AuthLabel), transcriptHash));
        }

        public static byte[] ComputePreMaster(byte[] ephemeralSharedSecret, byte[] staticSharedSecret, byte[] transcriptHash, byte[] exchangeIdentifier, byte[] clientNonce)
        {
            using var hmac = new HMACSHA256(Sha256(Concat(ephemeralSharedSecret, staticSharedSecret)));
            return hmac.ComputeHash(Concat(Encoding.UTF8.GetBytes(PreMasterLabel), transcriptHash, exchangeIdentifier, clientNonce));
        }

        public static byte[] DeriveSessionId(byte[] preMaster)
        {
            byte[] hash = Sha256(Concat(Encoding.UTF8.GetBytes(SessionIdLabel), preMaster));
            var sessionId = new byte[SessionIdBytes];
            Buffer.BlockCopy(hash, 0, sessionId, 0, SessionIdBytes);
            return sessionId;
        }

        public static byte[] ComputeIdentityFingerprint(byte[] staticX, byte[] staticY)
        {
            var hex = new StringBuilder(64);
            foreach (byte b in Sha256(Concat(Encoding.UTF8.GetBytes(IdentityLabel), staticX, staticY)))
                hex.Append(b.ToString("x2"));
            return Encoding.UTF8.GetBytes(hex.ToString());
        }

        public static bool FixedTimeEquals(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
            return diff == 0;
        }

        internal static byte[] Sha256(byte[] data)
        {
            using var sha = SHA256.Create();
            return sha.ComputeHash(data);
        }

        internal static byte[] Concat(params byte[][] parts)
        {
            int total = 0;
            foreach (var part in parts) total += part?.Length ?? 0;
            var result = new byte[total];
            int offset = 0;
            foreach (var part in parts)
            {
                if (part == null) continue;
                Buffer.BlockCopy(part, 0, result, offset, part.Length);
                offset += part.Length;
            }
            return result;
        }
    }

    public sealed class SecureChannel
    {
        private const byte ProtocolVersion = 2;
        private const int HeaderBytes = 6;
        private const int NonceBytes = 12;
        private const int TagBytes = 16;
        private const int ChannelCount = 2;
        private const int ReplayWindow = 1024;

        private readonly byte[] _sessionId;
        private readonly byte[][] _sendEncKeys = new byte[ChannelCount][];
        private readonly byte[][] _sendMacKeys = new byte[ChannelCount][];
        private readonly byte[][] _recvEncKeys = new byte[ChannelCount][];
        private readonly byte[][] _recvMacKeys = new byte[ChannelCount][];
        private readonly Aes _aes;
        private readonly object _receiveLock = new object();
        private readonly object _sendLock = new object();
        private readonly bool[][] _replayWindows = { new bool[ReplayWindow], new bool[ReplayWindow] };
        private readonly uint[] _highestSequence = new uint[ChannelCount];
        private readonly uint[] _sendSequence = new uint[ChannelCount];
        private bool _wrapped;

        public long PacketsSealed { get; private set; }
        public long PacketsOpened { get; private set; }

        private SecureChannel(byte[] preMaster, byte[] sessionId, bool isServer)
        {
            _sessionId = sessionId;
            byte[] firstSeed = SessionAuth.Sha256(SessionAuth.Concat(preMaster, new[] { (byte)1 }));
            byte[] secondSeed = SessionAuth.Sha256(SessionAuth.Concat(preMaster, new[] { (byte)2 }));
            byte[] outgoingSeed = isServer ? secondSeed : firstSeed;
            byte[] incomingSeed = isServer ? firstSeed : secondSeed;
            for (byte channel = 0; channel < ChannelCount; channel++)
            {
                (_sendEncKeys[channel], _sendMacKeys[channel]) = SplitKeys(outgoingSeed, channel);
                (_recvEncKeys[channel], _recvMacKeys[channel]) = SplitKeys(incomingSeed, channel);
            }
            _aes = Aes.Create();
            _aes.Mode = CipherMode.ECB;
            _aes.Padding = PaddingMode.None;
        }

        public static SecureChannel FromPreMaster(byte[] preMaster, byte[] sessionId, bool isServer)
        {
            if (preMaster == null || preMaster.Length != 32) throw new ArgumentException("Invalid pre-master key.");
            if (sessionId == null || sessionId.Length != SessionAuth.SessionIdBytes) throw new ArgumentException("Invalid session id.");
            return new SecureChannel(preMaster, sessionId, isServer);
        }

        public byte[] Seal(PacketType type, object payload)
        {
            if (_wrapped) throw new InvalidOperationException("Secure channel has been closed.");
            byte channel = (byte)LidgrenWire.GetChannel(type);
            lock (_sendLock)
            {
                uint sequence = _sendSequence[channel]++;
                byte[] envelope = LidgrenWire.Encode(type, payload);
                var nonce = BuildNonce(sequence);
                var header = BuildHeader(sequence, channel, nonce);
                ApplyCtr(_sendEncKeys[channel], nonce, envelope);
                byte[] tag = ComputeTag(_sendMacKeys[channel], header, envelope);
                PacketsSealed++;
                return SessionAuth.Concat(header, envelope, tag);
            }
        }

        public bool TryOpen(byte[] data, out PacketType type, out string json, out string error, out bool replayed)
        {
            type = 0;
            json = null;
            error = null;
            replayed = false;
            if (_wrapped)
            {
                error = "Secure channel has been closed.";
                return false;
            }
            if (data == null || data.Length < HeaderBytes + NonceBytes + TagBytes + LidgrenWire.HeaderBytes)
            {
                error = "Sealed packet is too short.";
                return false;
            }
            if (data[0] != ProtocolVersion)
            {
                error = "Unknown sealed packet version.";
                return false;
            }
            byte channel = data[5];
            if (channel >= ChannelCount)
            {
                error = "Unknown sealed packet channel.";
                return false;
            }
            lock (_receiveLock)
            {
                uint sequence = ((uint)data[1] << 24) | ((uint)data[2] << 16) | ((uint)data[3] << 8) | data[4];
                var header = new byte[HeaderBytes + NonceBytes];
                Buffer.BlockCopy(data, 0, header, 0, header.Length);
                int bodyLength = data.Length - header.Length - TagBytes;
                var body = new byte[bodyLength];
                Buffer.BlockCopy(data, header.Length, body, 0, bodyLength);
                var expectedTag = new byte[TagBytes];
                Buffer.BlockCopy(data, header.Length + bodyLength, expectedTag, 0, TagBytes);
                byte[] actualTag = ComputeTag(_recvMacKeys[channel], header, body);
                if (!SessionAuth.FixedTimeEquals(expectedTag, actualTag))
                {
                    error = "Packet authentication failed.";
                    return false;
                }
                if (IsReplay(channel, sequence))
                {
                    replayed = true;
                    error = "Replayed or stale packet rejected.";
                    return false;
                }
                var nonce = new byte[NonceBytes];
                Buffer.BlockCopy(header, HeaderBytes, nonce, 0, NonceBytes);
                ApplyCtr(_recvEncKeys[channel], nonce, body);
                try
                {
                    (type, json) = LidgrenWire.Decode(body);
                }
                catch (Exception e)
                {
                    error = "Sealed packet framing was invalid: " + e.Message;
                    return false;
                }
                PacketsOpened++;
                return true;
            }
        }

        public void Wrap()
        {
            _wrapped = true;
        }

        private static (byte[] encKey, byte[] macKey) SplitKeys(byte[] seed, byte channel)
        {
            return (SessionAuth.Sha256(SessionAuth.Concat(seed, new[] { channel, (byte)3 })), SessionAuth.Sha256(SessionAuth.Concat(seed, new[] { channel, (byte)4 })));
        }

        private byte[] BuildNonce(uint sequence)
        {
            var nonce = new byte[NonceBytes];
            Buffer.BlockCopy(_sessionId, 0, nonce, 0, 8);
            nonce[8] = (byte)(sequence >> 24);
            nonce[9] = (byte)(sequence >> 16);
            nonce[10] = (byte)(sequence >> 8);
            nonce[11] = (byte)sequence;
            return nonce;
        }

        private static byte[] BuildHeader(uint sequence, byte channel, byte[] nonce)
        {
            var header = new byte[HeaderBytes + NonceBytes];
            header[0] = ProtocolVersion;
            header[1] = (byte)(sequence >> 24);
            header[2] = (byte)(sequence >> 16);
            header[3] = (byte)(sequence >> 8);
            header[4] = (byte)sequence;
            header[5] = channel;
            Buffer.BlockCopy(nonce, 0, header, HeaderBytes, NonceBytes);
            return header;
        }

        private bool IsReplay(byte channel, uint sequence)
        {
            var window = _replayWindows[channel];
            if (sequence > _highestSequence[channel])
            {
                uint advance = sequence - _highestSequence[channel];
                if (advance >= ReplayWindow)
                {
                    Array.Clear(window, 0, window.Length);
                }
                else
                {
                    for (uint i = 1; i <= advance; i++)
                        window[(_highestSequence[channel] + i) % ReplayWindow] = false;
                }
                _highestSequence[channel] = sequence;
                window[sequence % ReplayWindow] = true;
                return false;
            }
            uint offset = _highestSequence[channel] - sequence;
            if (offset >= ReplayWindow) return true;
            uint slot = sequence % ReplayWindow;
            if (window[slot]) return true;
            window[slot] = true;
            return false;
        }

        private byte[] ComputeTag(byte[] macKey, byte[] header, byte[] ciphertext)
        {
            using var hmac = new HMACSHA256(macKey);
            byte[] tag = hmac.ComputeHash(SessionAuth.Concat(_sessionId, header, ciphertext));
            var truncated = new byte[TagBytes];
            Buffer.BlockCopy(tag, 0, truncated, 0, TagBytes);
            return truncated;
        }

        private void ApplyCtr(byte[] key, byte[] nonce, byte[] buffer)
        {
            var counterBlock = new byte[16];
            Buffer.BlockCopy(nonce, 0, counterBlock, 0, NonceBytes);
            using (ICryptoTransform encryptor = _aes.CreateEncryptor(key, new byte[16]))
            {
                var keystream = new byte[16];
                for (int position = 0; position < buffer.Length; position += 16)
                {
                    encryptor.TransformBlock(counterBlock, 0, 16, keystream, 0);
                    int chunk = Math.Min(16, buffer.Length - position);
                    for (int i = 0; i < chunk; i++)
                        buffer[position + i] ^= keystream[i];
                    for (int i = 15; i >= 0; i--)
                    {
                        if (++counterBlock[i] != 0) break;
                    }
                }
            }
        }
    }
}
