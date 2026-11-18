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
        private const string RegistrationSaltFile = "registration_salt";
        private static readonly TimeSpan HandshakeTimeout = TimeSpan.FromSeconds(12);

        private static byte[] _staticPrivate;
        private static byte[] _staticX;
        private static byte[] _staticY;
        private static string _identityFingerprint;
        private static byte[] _registrationSalt;
        private static byte[] _registrationPasswordHash;
        private static string _registrationPasswordSource;
        private static readonly object StaticLock = new object();

        public static string IdentityFingerprint => _identityFingerprint;

        public static byte[] RegistrationPasswordHash(string password)
        {
            lock (StaticLock)
            {
                if (_registrationPasswordHash != null && string.Equals(_registrationPasswordSource, password, StringComparison.Ordinal))
                    return _registrationPasswordHash;
                if (_registrationSalt == null)
                    throw new InvalidOperationException("The registration salt is not initialized.");
                _registrationPasswordHash = SessionAuth.ComputePasswordHashV2(password, _registrationSalt);
                _registrationPasswordSource = password;
                return _registrationPasswordHash;
            }
        }

        public static void InitializeStaticKey(FileStore store)
        {
            lock (StaticLock)
            {
                if (_staticPrivate != null) return;
                if (store.Exists("system", StaticKeyFile))
                {
                    StaticKeyRecord record;
                    try
                    {
                        record = store.Load<StaticKeyRecord>("system", StaticKeyFile);
                    }
                    catch (Exception e)
                    {
                        throw new InvalidOperationException("The server identity key file could not be read. Restore data/system/server_static_key.json from backup; the server will not silently generate a new identity. (" + e.Message + ")", e);
                    }

                    byte[] loaded = null;
                    if (record != null && !string.IsNullOrEmpty(record.PrivateKeyBase64))
                    {
                        try { loaded = Convert.FromBase64String(record.PrivateKeyBase64); }
                        catch (FormatException) { loaded = null; }
                    }
                    if (loaded == null || !P256.IsValidPrivateKey(loaded))
                    {
                        throw new InvalidOperationException("The server identity key file is present but does not contain a usable key. Restore data/system/server_static_key.json from backup, or delete it deliberately to create a new identity; every client that trusted the old key will then report a changed identity.");
                    }
                    _staticPrivate = loaded;
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

        public static void InitializeRegistrationSalt(FileStore store)
        {
            lock (StaticLock)
            {
                if (_registrationSalt != null) return;
                RegistrationSaltRecord record = null;
                try
                {
                    record = store.Load<RegistrationSaltRecord>("system", RegistrationSaltFile);
                }
                catch
                {
                    record = null;
                }

                if (record != null && !string.IsNullOrEmpty(record.SaltBase64))
                {
                    byte[] decoded = null;
                    try { decoded = Convert.FromBase64String(record.SaltBase64); }
                    catch { decoded = null; }
                    if (decoded != null && decoded.Length == SessionAuth.RegistrationSaltBytes)
                    {
                        _registrationSalt = decoded;
                        return;
                    }
                }

                _registrationSalt = SessionAuth.RandomBytes(SessionAuth.RegistrationSaltBytes);
                store.EnsureFolder("system");
                store.Save("system", RegistrationSaltFile, new RegistrationSaltRecord { SaltBase64 = Convert.ToBase64String(_registrationSalt) });
            }
        }

        public async Task<PlayerAccount> AuthenticateAsync(ClientConnection conn, AccountService accounts, ServerConfig config, CancellationToken ct)
        {
            try
            {
                return await AuthenticateCoreAsync(conn, accounts, config, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return null;
            }
            catch (Exception e)
            {
                Console.WriteLine($"[handshake-error] {e.GetType().Name}: {e.Message}");
                await SendFailureAsync(conn, ct, AuthErrorCode.HandshakeInvalid, "The handshake failed on the server. Try again.").ConfigureAwait(false);
                return null;
            }
        }

        private async Task SendFailureAsync(ClientConnection conn, CancellationToken ct, string code, string message)
        {
            try
            {
                if (conn.IsSecure)
                    await conn.SendSealedAsync(PacketType.AuthError, new AuthErrorPacket { Code = code, Message = message }, ct).ConfigureAwait(false);
                else
                    await conn.SendPlainAsync(PacketType.AuthError, new AuthErrorPacket { Code = code, Message = message }, ct).ConfigureAwait(false);
            }
            catch
            {
            }
        }

        private async Task<PlayerAccount> AuthenticateCoreAsync(ClientConnection conn, AccountService accounts, ServerConfig config, CancellationToken ct)
        {
            HelloPacket hello = await ReceivePlainAsync(conn, ct).ConfigureAwait(false);
            if (hello == null || !string.Equals(hello.ProtocolVersion, HelloPacket.ProtocolVersion3, StringComparison.Ordinal))
            {
                await RejectAsync(conn, ct, AuthErrorCode.ProtocolVersion, "Unsupported protocol version.").ConfigureAwait(false);
                return null;
            }

            if (!PlayerNameRules.TryCanonicalize(hello.PlayerName, out string playerName, out string nameError))
            {
                await RejectAsync(conn, ct, AuthErrorCode.NameInvalid, nameError).ConfigureAwait(false);
                return null;
            }
            if (hello.ClientNonce == null || hello.ClientNonce.Length != SessionAuth.NonceBytes)
            {
                await RejectAsync(conn, ct, AuthErrorCode.HandshakeInvalid, "Client handshake nonce was invalid.").ConfigureAwait(false);
                return null;
            }

            bool registerMode = string.Equals(hello.Mode, HelloPacket.ModeRegister, StringComparison.Ordinal);
            if (!registerMode && !string.Equals(hello.Mode, HelloPacket.ModeToken, StringComparison.Ordinal))
            {
                await RejectAsync(conn, ct, AuthErrorCode.HandshakeInvalid, "Unknown handshake mode.").ConfigureAwait(false);
                return null;
            }

            bool passwordRequired = !string.IsNullOrEmpty(config.RegistrationPassword);
            bool registrationAllowed = config.AllowRegistration;
            PlayerAccount existing = accounts.FindByName(playerName);

            byte[] serverChallenge = SessionAuth.RandomBytes(SessionAuth.ChallengeBytes);
            byte[] exchangeIdentifier = SessionAuth.RandomBytes(SessionAuth.SessionIdBytes);
            DateTime issuedUtc = DateTime.UtcNow;

            await conn.SendPlainAsync(PacketType.HelloAck, new HelloAckPacket
            {
                Accepted = true,
                ServerChallenge = serverChallenge,
                ClientNonce = hello.ClientNonce,
                ExchangeIdentifier = exchangeIdentifier,
                RegistrationAllowed = registrationAllowed,
                RegistrationPasswordRequired = passwordRequired
            }, ct).ConfigureAwait(false);

            AuthResponsePacket ephemeral = await ReceivePlainAsync<AuthResponsePacket>(conn, ct).ConfigureAwait(false);
            if (ephemeral == null || ephemeral.EphemeralX == null || ephemeral.EphemeralX.Length != SessionAuth.CoordinateBytes ||
                ephemeral.EphemeralY == null || ephemeral.EphemeralY.Length != SessionAuth.CoordinateBytes)
            {
                await RejectAsync(conn, ct, AuthErrorCode.HandshakeInvalid, "Client ephemeral key was invalid.").ConfigureAwait(false);
                return null;
            }
            if (!P256.IsOnCurve(ephemeral.EphemeralX, ephemeral.EphemeralY))
            {
                await RejectAsync(conn, ct, AuthErrorCode.HandshakeInvalid, "Client ephemeral key was not on the agreed curve.").ConfigureAwait(false);
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

            byte[] ephemeralSecret = P256.DeriveSharedSecret(ephemeralPrivate, clientX, clientY);
            if (ephemeralSecret == null)
            {
                await RejectAsync(conn, ct, AuthErrorCode.HandshakeInvalid, "Client ephemeral key was not on the agreed curve.").ConfigureAwait(false);
                return null;
            }
            byte[] staticSecret = P256.DeriveSharedSecret(_staticPrivate, clientX, clientY);
            if (staticSecret == null)
            {
                await RejectAsync(conn, ct, AuthErrorCode.HandshakeInvalid, "The client key could not be combined with the server identity key.").ConfigureAwait(false);
                return null;
            }

            byte[] transcriptHash = SessionAuth.ComputeTranscriptHash(hello.ClientNonce, serverChallenge, clientX, clientY, serverX, serverY, _staticX, _staticY, playerName);
            byte[] preMaster = SessionAuth.ComputePreMaster(ephemeralSecret, staticSecret, transcriptHash, exchangeIdentifier, hello.ClientNonce);
            byte[] sessionId = SessionAuth.DeriveSessionId(preMaster);

            var channel = SecureChannel.FromPreMaster(preMaster, sessionId, isServer: true);
            conn.EstablishSecureChannel(channel);
            await conn.SendSealedAsync(PacketType.IdentityProof, new IdentityProofPacket
            {
                Version = IdentityProofPacket.CurrentVersion,
                Mac = SessionAuth.ComputeIdentityProof(staticSecret, transcriptHash)
            }, ct).ConfigureAwait(false);

            AuthResponsePacket proof = await ReceivePlainAsync<AuthResponsePacket>(conn, ct).ConfigureAwait(false);
            if (proof == null || proof.Proof == null || proof.Proof.Length != 32)
            {
                await RejectAsync(conn, ct, AuthErrorCode.HandshakeInvalid, "Authentication proof was invalid.").ConfigureAwait(false);
                return null;
            }
            if (DateTime.UtcNow - issuedUtc > HandshakeTimeout)
            {
                await RejectAsync(conn, ct, AuthErrorCode.HandshakeInvalid, "The handshake timed out.").ConfigureAwait(false);
                return null;
            }

            if (proof.RegistrationSecret != null && proof.RegistrationSecret.Length > 0)
            {
                await RejectAsync(conn, ct, AuthErrorCode.RegistrationPasswordRequired,
                    "This server no longer accepts a registration password in the clear. Update SFS Enhanced and enter the password again.").ConfigureAwait(false);
                return null;
            }

            if (passwordRequired && registerMode)
            {
                await conn.SendSealedAsync(PacketType.RegistrationPrompt, new RegistrationPromptPacket
                {
                    Required = true,
                    Version = 3,
                    Salt = _registrationSalt
                }, ct).ConfigureAwait(false);

                Task<(bool ok, PacketType type, string json)> promptTask = conn.ReceiveSecureAsync(ct);
                Task finished = await Task.WhenAny(promptTask, Task.Delay(TimeSpan.FromSeconds(10), ct)).ConfigureAwait(false);
                if (finished != promptTask)
                {
                    await SendFailureAsync(conn, ct, AuthErrorCode.RegistrationTimeout,
                        "The registration password challenge timed out. Try again.").ConfigureAwait(false);
                    return null;
                }
                (bool promptOk, PacketType promptType, string promptJson) = await promptTask.ConfigureAwait(false);
                if (!promptOk)
                {
                    return null;
                }
                if (promptType != PacketType.RegistrationAuth)
                {
                    await conn.SendSealedAsync(PacketType.AuthError, new AuthErrorPacket
                    {
                        Code = AuthErrorCode.HandshakeInvalid,
                        Message = "The registration password proof arrived in the wrong order."
                    }, ct).ConfigureAwait(false);
                    return null;
                }

                RegistrationAuthPacket registration = Deserialize<RegistrationAuthPacket>(promptJson);
                byte[] requiredProof = SessionAuth.ComputeRegistrationProofV2(RegistrationPasswordHash(config.RegistrationPassword), transcriptHash);
                if (registration == null || registration.Proof == null || !SessionAuth.FixedTimeEquals(requiredProof, registration.Proof))
                {
                    await conn.SendSealedAsync(PacketType.AuthError, new AuthErrorPacket
                    {
                        Code = AuthErrorCode.BadRegistrationPassword,
                        Message = "The registration password was incorrect."
                    }, ct).ConfigureAwait(false);
                    return null;
                }
            }

            PlayerAccount account;
            string issuedToken = null;

            if (registerMode)
            {
                if (existing != null)
                {
                    await RejectAsync(conn, ct, AuthErrorCode.NameTaken, "That name is already registered on this server. Sign in with your existing account instead.").ConfigureAwait(false);
                    return null;
                }
                if (!registrationAllowed)
                {
                    await RejectAsync(conn, ct, AuthErrorCode.RegistrationDisabled, "This server is not accepting new registrations.").ConfigureAwait(false);
                    return null;
                }
                var created = accounts.CreateAccount(playerName);
                account = created.account;
                issuedToken = created.plainToken;
            }
            else
            {
                if (existing == null)
                {
                    await RejectAsync(conn, ct, AuthErrorCode.UnknownAccount, registrationAllowed
                        ? "No account exists for that name on this server. Create an account to continue."
                        : "No account exists for that name on this server, and this server is not accepting new registrations.").ConfigureAwait(false);
                    return null;
                }
                account = existing;
                if (!accounts.ValidateProof(account, proof.Proof, transcriptHash))
                {
                    await RejectAsync(conn, ct, AuthErrorCode.BadToken, "The saved auth token for that account was not accepted. Sign in again or use a different name.").ConfigureAwait(false);
                    return null;
                }
            }

            conn.AttachAccount(account);
            await conn.SendSealedAsync(PacketType.AuthResult, new AuthResultPacket
            {
                Accepted = true,
                PlayerId = account.PlayerId,
                AuthToken = issuedToken,
                SessionId = sessionId
            }, ct).ConfigureAwait(false);
            return account;
        }

        private static async Task RejectAsync(ClientConnection conn, CancellationToken ct, string code, string reason)
        {
            try { await conn.SendPlainAsync(PacketType.AuthError, new AuthErrorPacket { Message = reason, Code = code }, ct).ConfigureAwait(false); }
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

    public sealed class RegistrationSaltRecord
    {
        public string SaltBase64;
    }
}
