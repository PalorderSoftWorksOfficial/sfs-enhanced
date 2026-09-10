using System;
using System.Threading;
using System.Threading.Tasks;

namespace SFSEnhanced.Shared.Protocol
{
    /// <summary>A raw frame as received from the transport: plain wire frame or sealed packet.</summary>
    public sealed class RawFrame
    {
        public const byte PlainTag = 0x01;
        public const byte SealedTag = 0x02;

        public bool IsSealed;
        public byte[] Data;
    }

    public sealed class SecureHandshakeResult
    {
        public bool Accepted;
        public string RejectReason;
        public string PlayerId;
        public string IssuedToken;
        public SecureChannel Channel;
    }

    public interface ISecureTransport
    {
        Task SendPlainAsync(PacketType type, object payload, CancellationToken ct);
        Task SendSealedAsync(byte[] sealedBytes, CancellationToken ct);
        Task<RawFrame> ReceiveRawAsync(CancellationToken ct);
    }

    public static class SecureFrame
    {
        public static byte[] Wrap(RawFrame frame)
        {
            var data = new byte[frame.Data.Length + 1];
            data[0] = frame.IsSealed ? RawFrame.SealedTag : RawFrame.PlainTag;
            Buffer.BlockCopy(frame.Data, 0, data, 1, frame.Data.Length);
            return data;
        }

        public static RawFrame Unwrap(byte[] data)
        {
            if (data == null || data.Length < 1) return null;
            byte tag = data[0];
            if (tag != RawFrame.PlainTag && tag != RawFrame.SealedTag) return null;
            var frame = new RawFrame { IsSealed = tag == RawFrame.SealedTag, Data = new byte[data.Length - 1] };
            Buffer.BlockCopy(data, 1, frame.Data, 0, frame.Data.Length);
            return frame;
        }
    }

    public sealed class SecureHandshakeClient
    {
        private readonly ISecureTransport _transport;

        public SecureHandshakeClient(ISecureTransport transport)
        {
            _transport = transport;
        }

        public async Task<SecureHandshakeResult> RunAsync(string playerName, string authToken, string clientModVersion, string registrationPassword, CancellationToken ct)
        {
            bool registerMode = !string.IsNullOrEmpty(registrationPassword);
            var clientNonce = SessionAuth.RandomBytes(SessionAuth.NonceBytes);
            await _transport.SendPlainAsync(PacketType.Hello, new HelloPacket
            {
                ProtocolVersion = HelloPacket.ProtocolVersion2,
                PlayerName = playerName,
                ClientModVersion = clientModVersion,
                ClientNonce = clientNonce,
                Mode = registerMode ? HelloPacket.ModeRegister : HelloPacket.ModeToken
            }, ct).ConfigureAwait(false);

            (bool ok, PacketType type, string json) ack = await ReceivePlainAsync(ct).ConfigureAwait(false);
            if (!ack.ok) return Reject("Connection closed during handshake.");
            if (ack.type == PacketType.AuthError)
                return Reject(ExtractMessage(ack.json) ?? "Authentication failed.");
            if (ack.type != PacketType.HelloAck)
                return Reject("Expected a handshake challenge.");

            var ackPacket = Deserialize<HelloAckPacket>(ack.json);
            if (ackPacket == null) return Reject("Malformed handshake response.");
            if (!ackPacket.Accepted)
                return Reject(string.IsNullOrEmpty(ackPacket.RejectReason) ? "Authentication failed." : ackPacket.RejectReason);
            if (ackPacket.ServerChallenge == null || ackPacket.ServerChallenge.Length != SessionAuth.ChallengeBytes)
                return Reject("Server handshake challenge was invalid.");
            if (!SessionAuth.FixedTimeEquals(ackPacket.ClientNonce, clientNonce))
                return Reject("Server did not echo the client nonce.");
            if (ackPacket.ExchangeIdentifier == null || ackPacket.ExchangeIdentifier.Length != SessionAuth.SessionIdBytes)
                return Reject("Server exchange identifier was invalid.");

            byte[] privateKey = P256.GeneratePrivateKey();
            P256.PublicPoint(privateKey, out byte[] clientX, out byte[] clientY);

            await _transport.SendPlainAsync(PacketType.AuthResponse, new AuthResponsePacket
            {
                EphemeralX = clientX,
                EphemeralY = clientY
            }, ct).ConfigureAwait(false);

            (bool keysOk, PacketType keysType, string keysJson) = await ReceivePlainAsync(ct).ConfigureAwait(false);
            if (!keysOk) return Reject("Connection closed while deriving session keys.");
            if (keysType == PacketType.AuthError)
                return Reject(ExtractMessage(keysJson) ?? "Authentication failed.");
            if (keysType != PacketType.AuthChallenge)
                return Reject("Expected the server ephemeral key.");

            var challenge = Deserialize<AuthChallengePacket>(keysJson);
            if (challenge == null) return Reject("Malformed server ephemeral key.");
            if (challenge.EphemeralX == null || challenge.EphemeralX.Length != SessionAuth.CoordinateBytes ||
                challenge.EphemeralY == null || challenge.EphemeralY.Length != SessionAuth.CoordinateBytes)
                return Reject("Server ephemeral key was invalid.");
            if (!SessionAuth.FixedTimeEquals(challenge.ExchangeIdentifier, ackPacket.ExchangeIdentifier))
                return Reject("Server exchange identifier did not match.");

            byte[] ephemeralSecret = P256.DeriveSharedSecret(privateKey, challenge.EphemeralX, challenge.EphemeralY);
            if (ephemeralSecret == null)
                return Reject("Server ephemeral key was not on the agreed curve.");

            byte[] staticSecret = challenge.StaticX != null && challenge.StaticX.Length == SessionAuth.CoordinateBytes &&
                                  challenge.StaticY != null && challenge.StaticY.Length == SessionAuth.CoordinateBytes
                ? P256.DeriveSharedSecret(privateKey, challenge.StaticX, challenge.StaticY) ?? new byte[32]
                : new byte[32];

            byte[] transcriptHash = SessionAuth.ComputeTranscriptHash(clientNonce, ackPacket.ServerChallenge, clientX, clientY, challenge.EphemeralX, challenge.EphemeralY, challenge.StaticX, challenge.StaticY, playerName);
            byte[] tokenKey = SessionAuth.ComputeTokenKey(authToken);
            byte[] proof = SessionAuth.ComputeTokenProof(tokenKey, transcriptHash);
            byte[] preMaster = SessionAuth.ComputePreMaster(ephemeralSecret, staticSecret, transcriptHash, ackPacket.ExchangeIdentifier, clientNonce);
            byte[] sessionId = SessionAuth.DeriveSessionId(preMaster);
            var channel = SecureChannel.FromPreMaster(preMaster, sessionId, isServer: false);

            byte[] registrationSecret = registerMode
                ? SessionAuth.ComputeRegistrationProof(SessionAuth.ComputePasswordHash(registrationPassword), transcriptHash)
                : null;

            await _transport.SendPlainAsync(PacketType.AuthResponse, new AuthResponsePacket
            {
                Proof = proof,
                RegistrationSecret = registrationSecret
            }, ct).ConfigureAwait(false);

            (bool resultOk, PacketType resultType, string resultJson) = await ReceiveAnyAsync(channel, ct).ConfigureAwait(false);
            if (!resultOk) return Reject("Connection closed before authentication completed.");
            if (resultType == PacketType.AuthError)
                return Reject(ExtractMessage(resultJson) ?? "Authentication failed.");
            if (resultType != PacketType.AuthResult)
                return Reject("Expected an authentication result.");

            var authResult = Deserialize<AuthResultPacket>(resultJson);
            if (authResult == null || !authResult.Accepted)
                return Reject(authResult == null ? "Server returned an invalid authentication result." : authResult.RejectReason);

            return new SecureHandshakeResult
            {
                Accepted = true,
                PlayerId = authResult.PlayerId,
                IssuedToken = authResult.AuthToken,
                Channel = channel
            };
        }

        private async Task<(bool ok, PacketType type, string json)> ReceivePlainAsync(CancellationToken ct)
        {
            RawFrame frame = await _transport.ReceiveRawAsync(ct).ConfigureAwait(false);
            while (frame != null && frame.IsSealed) frame = await _transport.ReceiveRawAsync(ct).ConfigureAwait(false);
            if (frame == null) return (false, 0, null);
            (PacketType type, string json) = LidgrenWire.Decode(frame.Data);
            return (true, type, json);
            }

        private async Task<(bool ok, PacketType type, string json)> ReceiveAnyAsync(SecureChannel channel, CancellationToken ct)
        {
            while (true)
            {
                RawFrame frame = await _transport.ReceiveRawAsync(ct).ConfigureAwait(false);
                if (frame == null) return (false, 0, null);
                if (!frame.IsSealed)
                {
                    (PacketType plainType, string plainJson) = LidgrenWire.Decode(frame.Data);
                    if (plainType == PacketType.AuthError || plainType == PacketType.Error)
                        return (true, plainType, plainJson);
                    continue;
                }
                if (channel.TryOpen(frame.Data, out PacketType type, out string json, out string error, out bool replayed))
                    return (true, type, json);
                if (replayed) continue;
                throw new InvalidOperationException(error ?? "Sealed packet was rejected.");
            }
        }

        private static SecureHandshakeResult Reject(string reason) => new SecureHandshakeResult { RejectReason = reason };

        private static T Deserialize<T>(string json) => json == null ? default : Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);

        private static string ExtractMessage(string json)
        {
            try
            {
                var parsed = Newtonsoft.Json.JsonConvert.DeserializeObject<ErrorPacket>(json);
                return parsed?.Message ?? json;
            }
            catch
            {
                return json;
            }
        }
    }
}
