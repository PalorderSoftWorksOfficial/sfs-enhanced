using System;
using System.Threading;
using System.Threading.Tasks;
using SFSEnhanced.Server.Persistence;
using SFSEnhanced.Shared.Models;
using SFSEnhanced.Shared.Protocol;
using ServerConfig = SFSEnhanced.Server.ServerConfig;

namespace SFSEnhanced.Server.Networking
{
    public sealed class SecureSessionHandshake
    {
        private const string StaticKeyFile = "server_static_key";
        private static readonly TimeSpan HandshakeTimeout = TimeSpan.FromSeconds(12);

        private static byte[] _staticPrivate;
        private static byte[] _staticX;
        private static byte[] _staticY;
        private static string _identityFingerprint;
        private static readonly object StaticLock = new object();

        public static string IdentityFingerprint => _identityFingerprint;

        public static void InitializeStaticKey(FileStore store)
        {
            lock (StaticLock)
            {
                if (_staticPrivate != null) return;
                StaticKeyRecord record = store.Load<StaticKeyRecord>("system", StaticKeyFile);
                if (record != null && !string.IsNullOrEmpty(record.PrivateKeyBase64))
                {
                    _staticPrivate = Convert.FromBase64String(record.PrivateKeyBase64);
                }
                else
                {
                    _staticPrivate = P256.GeneratePrivateKey();
                    store.EnsureFolder("system");
                    store.Save("system", StaticKeyFile, new StaticKeyRecord { PrivateKeyBase64 = Convert.ToBase64String(_staticPrivate) });
                }
                P256.PublicPoint(_staticPrivate, out _staticX, out _staticY);
                _identityFingerprint = System.Text.Encoding.UTF8.GetString(SessionAuth.ComputeIdentityFingerprint(_staticX, _staticY));
            }
        }

        /// <summary>Runs the handshake on a fresh connection. Returns the authenticated account, or null after a rejection was sent.</summary>
        public async Task<PlayerAccount> AuthenticateAsync(ClientConnection conn, AccountService accounts, ServerConfig config, CancellationToken ct)
        {
            HelloPacket hello = await ReceivePlainAsync(conn, ct).ConfigureAwait(false);
            if (hello == null || !string.Equals(hello.ProtocolVersion, HelloPacket.ProtocolVersion2, StringComparison.Ordinal))
            {
                await RejectAsync(conn, ct, "Unsupported protocol version.").ConfigureAwait(false);
                return null;
            }
            if (string.IsNullOrWhiteSpace(hello.PlayerName) || hello.PlayerName.Trim().Length > 32)
            {
                await RejectAsync(conn, ct, "A player name of 1-32 characters is required.").ConfigureAwait(false);
                return null;
            }
            if (hello.ClientNonce == null || hello.ClientNonce.Length != SessionAuth.NonceBytes)
            {
                await RejectAsync(conn, ct, "Client handshake nonce was invalid.").ConfigureAwait(false);
                return null;
            }
            bool registerMode = string.Equals(hello.Mode, HelloPacket.ModeRegister, StringComparison.Ordinal);
            if (!registerMode && !string.Equals(hello.Mode, HelloPacket.ModeToken, StringComparison.Ordinal))
            {
                await RejectAsync(conn, ct, "Unknown handshake mode.").ConfigureAwait(false);
                return null;
            }

            PlayerAccount existing = accounts.FindByName(hello.PlayerName);

            string playerName = hello.PlayerName.Trim();
            byte[] serverChallenge = SessionAuth.RandomBytes(SessionAuth.ChallengeBytes);
            byte[] exchangeIdentifier = SessionAuth.RandomBytes(SessionAuth.SessionIdBytes);
            DateTime issuedUtc = DateTime.UtcNow;

            await conn.SendPlainAsync(PacketType.HelloAck, new HelloAckPacket
            {
                Accepted = true,
                ServerChallenge = serverChallenge,
                ClientNonce = hello.ClientNonce,
                ExchangeIdentifier = exchangeIdentifier
            }, ct).ConfigureAwait(false);

            AuthResponsePacket ephemeral = await ReceivePlainAsync<AuthResponsePacket>(conn, ct).ConfigureAwait(false);
            if (ephemeral == null || ephemeral.EphemeralX == null || ephemeral.EphemeralX.Length != SessionAuth.CoordinateBytes ||
                ephemeral.EphemeralY == null || ephemeral.EphemeralY.Length != SessionAuth.CoordinateBytes)
            {
                await RejectAsync(conn, ct, "Client ephemeral key was invalid.").ConfigureAwait(false);
                return null;
            }
            if (!P256.IsOnCurve(ephemeral.EphemeralX, ephemeral.EphemeralY))
            {
                await RejectAsync(conn, ct, "Client ephemeral key was not on the agreed curve.").ConfigureAwait(false);
                return null;
            }
            byte[] clientX = ephemeral.EphemeralX;
            byte[] clientY = ephemeral.EphemeralY;

            byte[] ephemeralPrivate = P256.GeneratePrivateKey();
            P256.PublicPoint(ephemeralPrivate, out byte[] serverX, out byte[] serverY);

            await conn.SendPlainAsync(PacketType.AuthChallenge, new AuthChallengePacket
            {
                EphemeralX = serverX,
                EphemeralY = serverY,
                StaticX = _staticX,
                StaticY = _staticY,
                IdentityFingerprint = _identityFingerprint,
                ExchangeIdentifier = exchangeIdentifier,
                FinalFlag = true
            }, ct).ConfigureAwait(false);

            AuthResponsePacket proof = await ReceivePlainAsync<AuthResponsePacket>(conn, ct).ConfigureAwait(false);
            if (proof == null || proof.Proof == null || proof.Proof.Length != 32)
            {
                await RejectAsync(conn, ct, "Authentication proof was invalid.").ConfigureAwait(false);
                return null;
            }
            if (DateTime.UtcNow - issuedUtc > HandshakeTimeout)
            {
                await RejectAsync(conn, ct, "The handshake timed out.").ConfigureAwait(false);
                return null;
            }

            byte[] ephemeralSecret = P256.DeriveSharedSecret(ephemeralPrivate, clientX, clientY);
            byte[] staticSecret = P256.DeriveSharedSecret(_staticPrivate, clientX, clientY) ?? new byte[32];
            if (ephemeralSecret == null)
            {
                await RejectAsync(conn, ct, "Client ephemeral key was not on the agreed curve.").ConfigureAwait(false);
                return null;
            }

            byte[] transcriptHash = SessionAuth.ComputeTranscriptHash(hello.ClientNonce, serverChallenge, clientX, clientY, serverX, serverY, _staticX, _staticY, playerName);
            byte[] preMaster = SessionAuth.ComputePreMaster(ephemeralSecret, staticSecret, transcriptHash, exchangeIdentifier, hello.ClientNonce);
            byte[] sessionId = SessionAuth.DeriveSessionId(preMaster);

            PlayerAccount account;
            string issuedToken = null;
            if (!string.IsNullOrEmpty(config.RegistrationPassword))
            {
                if (proof.RegistrationSecret == null || proof.RegistrationSecret.Length != 32)
                {
                    await RejectAsync(conn, ct, "This server requires a registration password.").ConfigureAwait(false);
                    return null;
                }
                byte[] required = SessionAuth.ComputeRegistrationProof(SessionAuth.ComputePasswordHash(config.RegistrationPassword), transcriptHash);
                if (!SessionAuth.FixedTimeEquals(required, proof.RegistrationSecret))
                {
                    await RejectAsync(conn, ct, "The registration password was incorrect.").ConfigureAwait(false);
                    return null;
                }
            }
            if (registerMode)
            {
                if (existing != null)
                {
                    await RejectAsync(conn, ct, "That name is already taken.").ConfigureAwait(false);
                    return null;
                }
                if (!config.AllowRegistration)
                {
                    await RejectAsync(conn, ct, "This server does not accept new registrations.").ConfigureAwait(false);
                    return null;
                }
                var created = accounts.CreateAccount(playerName);
                account = created.account;
                issuedToken = created.plainToken;
            }
            else if (existing == null)
            {
                if (!config.AllowRegistration)
                {
                    await RejectAsync(conn, ct, "No account exists for this name and this server does not accept new registrations.").ConfigureAwait(false);
                    return null;
                }
                var created = accounts.CreateAccount(playerName);
                account = created.account;
                issuedToken = created.plainToken;
            }
            else
            {
                account = existing;
                if (!accounts.ValidateProof(account, proof.Proof, transcriptHash))
                {
                    await RejectAsync(conn, ct, "The auth token was not accepted for this name.").ConfigureAwait(false);
                    return null;
                }
            }

            var channel = SecureChannel.FromPreMaster(preMaster, sessionId, isServer: true);
            conn.EstablishSecureChannel(channel, account);
            await conn.SendSealedAsync(PacketType.AuthResult, new AuthResultPacket
            {
                Accepted = true,
                PlayerId = account.PlayerId,
                AuthToken = issuedToken,
                SessionId = sessionId
            }, ct).ConfigureAwait(false);
            return account;
        }

        private static async Task RejectAsync(ClientConnection conn, CancellationToken ct, string reason)
        {
            try { await conn.SendPlainAsync(PacketType.AuthError, new AuthErrorPacket { Message = reason }, ct).ConfigureAwait(false); }
            catch { }
        }

        private static async Task<HelloPacket> ReceivePlainAsync(ClientConnection conn, CancellationToken ct)
        {
            RawFrame frame = await conn.ReceiveRawAsync(ct).ConfigureAwait(false);
            if (frame == null || frame.IsSealed) return null;
            (PacketType type, string json) = LidgrenWire.Decode(frame.Data);
            if (type != PacketType.Hello) return null;
            return Deserialize<HelloPacket>(json);
        }

        private static async Task<AuthResponsePacket> ReceivePlainAsync<T>(ClientConnection conn, CancellationToken ct) where T : class
        {
            RawFrame frame = await conn.ReceiveRawAsync(ct).ConfigureAwait(false);
            if (frame == null || frame.IsSealed) return null;
            (PacketType type, string json) = LidgrenWire.Decode(frame.Data);
            if (type != PacketType.AuthResponse) return null;
            return Deserialize<AuthResponsePacket>(json);
        }

        private static T Deserialize<T>(string json) where T : class => json == null ? null : Newtonsoft.Json.JsonConvert.DeserializeObject<T>(json);
    }

    public sealed class StaticKeyRecord
    {
        public string PrivateKeyBase64;
    }
}
