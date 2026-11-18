using System;
using System.Threading;
using System.Threading.Tasks;
using SFSEnhanced.Shared.Models;

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

    public enum ServerIdentityDecision
    {
        Trusted,
        FirstUse,
        Changed,
        Unreadable
    }

    public interface IServerIdentityGate
    {
        string EndpointKey { get; }
        ServerIdentityDecision Evaluate(ServerIdentity identity, out string pinnedFingerprint);
    }

    public sealed class SecureHandshakeResult
    {
        public bool Accepted;
        public string RejectReason;
        public string ErrorCode;
        public bool IdentityProofVerified;
        public string PlayerId;
        public string IssuedToken;
        public SecureChannel Channel;
        public ServerIdentity Identity;
        public ServerIdentityStatus IdentityStatus;
        public ServerIdentityDecision IdentityDecision;
        public string CanonicalEndpointKey;
        public string PresentedFingerprint;
        public string PresentedShortFingerprint;
        public string PinnedFingerprint;
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

        public async Task<SecureHandshakeResult> RunAsync(string playerName, string authToken, string clientModVersion, string registrationPassword, CancellationToken ct, IServerIdentityGate identityGate = null, bool createAccount = false)
        {
            if (!PlayerNameRules.TryCanonicalize(playerName, out string canonicalPlayerName, out string nameError))
                return Reject(nameError, AuthErrorCode.NameInvalid);
            playerName = canonicalPlayerName;
            bool registerMode = createAccount;
            var clientNonce = SessionAuth.RandomBytes(SessionAuth.NonceBytes);
            await _transport.SendPlainAsync(PacketType.Hello, new HelloPacket
            {
                ProtocolVersion = HelloPacket.ProtocolVersion3,
                PlayerName = playerName,
                ClientModVersion = clientModVersion,
                ClientNonce = clientNonce,
                Mode = registerMode ? HelloPacket.ModeRegister : HelloPacket.ModeToken
            }, ct).ConfigureAwait(false);

            (bool ok, PacketType type, string json) ack = await ReceivePlainAsync(ct).ConfigureAwait(false);
            if (!ack.ok) return Reject("Connection closed during handshake.");
            if (ack.type == PacketType.AuthError)
                return Reject(ExtractMessage(ack.json) ?? "Authentication failed.", ExtractCode(ack.json));
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
            if (registerMode && ackPacket.RegistrationPasswordRequired && string.IsNullOrEmpty(registrationPassword))
                return Reject("This server requires a registration password to create an account. Enter it to continue.", AuthErrorCode.RegistrationPasswordRequired);

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
                return Reject(ExtractMessage(keysJson) ?? "Authentication failed.", ExtractCode(keysJson));
            if (keysType != PacketType.AuthChallenge)
                return Reject("Expected the server ephemeral key.");

            var challenge = Deserialize<AuthChallengePacket>(keysJson);
            if (challenge == null) return Reject("Malformed server ephemeral key.");
            if (challenge.EphemeralX == null || challenge.EphemeralX.Length != SessionAuth.CoordinateBytes ||
                challenge.EphemeralY == null || challenge.EphemeralY.Length != SessionAuth.CoordinateBytes)
                return Reject("Server ephemeral key was invalid.");
            if (!SessionAuth.FixedTimeEquals(challenge.ExchangeIdentifier, ackPacket.ExchangeIdentifier))
                return Reject("Server exchange identifier did not match.");

            ServerIdentity identity = ServerIdentityCalculator.FromStaticPoint(challenge.StaticX, challenge.StaticY);
            if (!identity.Valid)
                return IdentityReject(identity, ServerIdentityDecision.Trusted, null, null, identity.Error);

            ServerIdentityDecision decision = ServerIdentityDecision.Trusted;
            string pinnedFingerprint = null;
            if (identityGate != null)
            {
                decision = identityGate.Evaluate(identity, out pinnedFingerprint);
                if (decision == ServerIdentityDecision.FirstUse)
                    return IdentityReject(identity, decision, identityGate.EndpointKey, pinnedFingerprint,
                        "This server's identity has not been trusted on this device. Verify its fingerprint out of band before connecting.");
                if (decision == ServerIdentityDecision.Changed)
                    return IdentityReject(identity, decision, identityGate.EndpointKey, pinnedFingerprint,
                        "This server's identity does not match the fingerprint trusted on this device. Refusing to connect.");
                if (decision == ServerIdentityDecision.Unreadable)
                    return IdentityReject(identity, decision, identityGate.EndpointKey, pinnedFingerprint,
                        "The trusted server fingerprints stored on this device could not be read, so this server cannot be verified. Repair or clear the stored fingerprints before connecting.");
            }

            byte[] ephemeralSecret = P256.DeriveSharedSecret(privateKey, challenge.EphemeralX, challenge.EphemeralY);
            if (ephemeralSecret == null)
                return Reject("Server ephemeral key was not on the agreed curve.");

            byte[] staticSecret = P256.DeriveSharedSecret(privateKey, challenge.StaticX, challenge.StaticY);
            if (staticSecret == null)
                return Reject("The server identity key could not be used for the key exchange.");

            byte[] transcriptHash = SessionAuth.ComputeTranscriptHash(clientNonce, ackPacket.ServerChallenge, clientX, clientY, challenge.EphemeralX, challenge.EphemeralY, challenge.StaticX, challenge.StaticY, playerName);
            byte[] tokenKey = SessionAuth.ComputeTokenKey(authToken);
            byte[] proof = SessionAuth.ComputeTokenProof(tokenKey, transcriptHash);
            byte[] preMaster = SessionAuth.ComputePreMaster(ephemeralSecret, staticSecret, transcriptHash, ackPacket.ExchangeIdentifier, clientNonce);
            byte[] sessionId = SessionAuth.DeriveSessionId(preMaster);
            var channel = SecureChannel.FromPreMaster(preMaster, sessionId, isServer: false);

            SecureHandshakeResult proofFailure = await VerifyIdentityProofAsync(channel, staticSecret, transcriptHash, ct).ConfigureAwait(false);
            if (proofFailure != null) return proofFailure;

            await _transport.SendPlainAsync(PacketType.AuthResponse, new AuthResponsePacket
            {
                Proof = proof
            }, ct).ConfigureAwait(false);

            for (int round = 0; round < 4; round++)
            {
                (bool resultOk, PacketType resultType, string resultJson) = await ReceiveAnyAsync(channel, ct).ConfigureAwait(false);
                if (!resultOk) return Reject("Connection closed before authentication completed.");
                if (resultType == PacketType.AuthError)
                    return Reject(ExtractMessage(resultJson) ?? "Authentication failed.", ExtractCode(resultJson));
                if (resultType == PacketType.RegistrationPrompt)
                {
                    if (!registerMode)
                        return Reject("The server asked for a registration password while signing in to an existing account. Refusing.", AuthErrorCode.HandshakeInvalid);
                    if (!await AnswerRegistrationPromptAsync(channel, resultJson, registrationPassword, transcriptHash, ct).ConfigureAwait(false))
                        return Reject("This server requires a registration password to create an account. Enter it to continue.", AuthErrorCode.RegistrationPasswordRequired);
                    continue;
                }
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
                    IdentityProofVerified = true,
                    Channel = channel,
                    Identity = identity,
                    IdentityStatus = identity.Status,
                    IdentityDecision = decision,
                    CanonicalEndpointKey = identityGate?.EndpointKey,
                    PresentedFingerprint = identity.Fingerprint,
                    PresentedShortFingerprint = identity.ShortFingerprint,
                    PinnedFingerprint = pinnedFingerprint
                };
            }

            return Reject("The server asked for the registration password repeatedly.", AuthErrorCode.HandshakeInvalid);
        }

        private async Task<SecureHandshakeResult> VerifyIdentityProofAsync(SecureChannel channel, byte[] staticSharedSecret, byte[] transcriptHash, CancellationToken ct)
        {
            (bool ok, PacketType type, string json) received;
            try
            {
                received = await ReceiveAnyAsync(channel, ct).ConfigureAwait(false);
            }
            catch (InvalidOperationException)
            {
                return Reject("The server failed to prove it holds the identity key it presented, so no credential was sent. Refusing to connect.", AuthErrorCode.IdentityProofInvalid);
            }
            if (!received.ok)
                return Reject("The connection closed before the server proved its identity, so no credential was sent.", AuthErrorCode.IdentityProofMissing);
            if (received.type == PacketType.AuthError)
                return Reject(ExtractMessage(received.json) ?? "Authentication failed.", ExtractCode(received.json));
            if (received.type == PacketType.Error)
                return Reject(ExtractMessage(received.json) ?? "The server rejected the handshake.", AuthErrorCode.HandshakeInvalid);
            if (received.type != PacketType.IdentityProof)
                return Reject("The server did not prove it holds its identity key before authentication, so no credential was sent. Refusing to connect.", AuthErrorCode.IdentityProofMissing);

            IdentityProofPacket packet = Deserialize<IdentityProofPacket>(received.json);
            if (packet == null || packet.Version != IdentityProofPacket.CurrentVersion || packet.Mac == null || packet.Mac.Length != 32)
                return Reject("The server sent a malformed identity proof, so no credential was sent. Refusing to connect.", AuthErrorCode.IdentityProofInvalid);

            byte[] expected = SessionAuth.ComputeIdentityProof(staticSharedSecret, transcriptHash);
            if (!SessionAuth.FixedTimeEquals(expected, packet.Mac))
                return Reject("The server failed to prove it holds the identity key it presented, so no credential was sent. Refusing to connect.", AuthErrorCode.IdentityProofInvalid);
            return null;
        }

        private async Task<bool> AnswerRegistrationPromptAsync(SecureChannel channel, string json, string registrationPassword, byte[] transcriptHash, CancellationToken ct)
        {
            RegistrationPromptPacket prompt = Deserialize<RegistrationPromptPacket>(json);
            if (prompt == null || !prompt.Required) return false;
            if (string.IsNullOrEmpty(registrationPassword)) return false;
            if (prompt.Salt == null || prompt.Salt.Length != SessionAuth.RegistrationSaltBytes) return false;

            byte[] passwordHash = SessionAuth.ComputePasswordHashV2(registrationPassword, prompt.Salt);
            byte[] registrationProof = SessionAuth.ComputeRegistrationProofV2(passwordHash, transcriptHash);
            await _transport.SendSealedAsync(channel.Seal(PacketType.RegistrationAuth, new RegistrationAuthPacket { Proof = registrationProof }), ct).ConfigureAwait(false);
            return true;
        }

        private static SecureHandshakeResult IdentityReject(ServerIdentity identity, ServerIdentityDecision decision, string endpointKey, string pinnedFingerprint, string reason)
        {
            return new SecureHandshakeResult
            {
                Accepted = false,
                RejectReason = reason,
                ErrorCode = decision == ServerIdentityDecision.Unreadable ? AuthErrorCode.IdentityStoreUnreadable : AuthErrorCode.IdentityUntrusted,
                Identity = identity,
                IdentityStatus = identity?.Status ?? ServerIdentityStatus.MissingKey,
                IdentityDecision = decision,
                CanonicalEndpointKey = endpointKey,
                PresentedFingerprint = identity?.Fingerprint,
                PresentedShortFingerprint = identity?.ShortFingerprint,
                PinnedFingerprint = pinnedFingerprint
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

        private static SecureHandshakeResult Reject(string reason, string code) => new SecureHandshakeResult { RejectReason = reason, ErrorCode = code };

        private static string ExtractCode(string json)
        {
            try
            {
                return Newtonsoft.Json.JsonConvert.DeserializeObject<AuthErrorPacket>(json)?.Code;
            }
            catch
            {
                return null;
            }
        }

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
