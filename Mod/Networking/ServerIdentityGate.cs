using System;
using SFSEnhanced.Shared.Networking;
using SFSEnhanced.Shared.Protocol;

namespace SFSEnhanced.Mod.Networking
{
    internal sealed class PendingIdentityApproval
    {
        public string EndpointKey;
        public string Fingerprint;
        public ServerIdentity Identity;
        public string Origin;
        public bool IsReplacement;
    }

    internal sealed class ServerIdentityGate : IServerIdentityGate
    {
        private readonly ServerIdentityPinStore _store;
        private readonly PendingIdentityApproval _approval;

        public string EndpointKey { get; }
        public string HostInput { get; }
        public int? DeclaredPort { get; }
        public string ResolvedHost { get; }
        public int? ResolvedPort { get; }

        public bool ApprovalUsed { get; private set; }
        public bool HadPin { get; private set; }
        public string PinnedFingerprint { get; private set; }
        public PendingIdentityApproval Approval => _approval;

        public ServerIdentityGate(ServerIdentityPinStore store, string endpointKey, string hostInput, int? declaredPort, string resolvedHost, int? resolvedPort, PendingIdentityApproval approval)
        {
            _store = store;
            EndpointKey = endpointKey;
            HostInput = hostInput;
            DeclaredPort = declaredPort;
            ResolvedHost = resolvedHost;
            ResolvedPort = resolvedPort;
            _approval = approval;
        }

        public ServerIdentityDecision Evaluate(ServerIdentity identity, out string pinnedFingerprint)
        {
            pinnedFingerprint = null;
            if (identity == null || !identity.Valid) return ServerIdentityDecision.FirstUse;

            PinLookup lookup = _store.Lookup(EndpointKey, identity.Fingerprint);
            pinnedFingerprint = lookup.PinnedFingerprint;
            PinnedFingerprint = lookup.PinnedFingerprint;

            if (lookup.StoreUnreadable) return ServerIdentityDecision.Unreadable;
            if (lookup.Match == PinMatch.Match) return ServerIdentityDecision.Trusted;

            if (ApprovalMatches(identity))
            {
                ApprovalUsed = true;
                HadPin = lookup.Match == PinMatch.Changed;
                return ServerIdentityDecision.Trusted;
            }

            return lookup.Match == PinMatch.Changed ? ServerIdentityDecision.Changed : ServerIdentityDecision.FirstUse;
        }

        public ServerIdentityPin BuildPin(ServerIdentity identity, string serverName, DateTime now)
        {
            if (identity == null || !identity.Valid || string.IsNullOrWhiteSpace(EndpointKey)) return null;
            return new ServerIdentityPin
            {
                EndpointKey = EndpointKey,
                HostInput = HostInput,
                DeclaredPort = DeclaredPort,
                ResolvedHost = ResolvedHost,
                ResolvedPort = ResolvedPort,
                Fingerprint = identity.Fingerprint,
                ShortFingerprint = identity.ShortFingerprint,
                ServerName = serverName,
                Trust = "pinned",
                Origin = _approval?.Origin ?? "user_verified",
                FirstSeenUtc = now,
                LastVerifiedUtc = now
            };
        }

        public void Verify(ServerIdentity identity, string serverName)
        {
            if (identity == null || !identity.Valid || string.IsNullOrWhiteSpace(EndpointKey)) return;
            _store.Touch(EndpointKey, ResolvedHost, ResolvedPort, serverName, DateTime.UtcNow);
        }

        public bool Forget() => _store.Forget(EndpointKey);

        private bool ApprovalMatches(ServerIdentity identity)
        {
            return _approval != null &&
                   string.Equals(_approval.EndpointKey, EndpointKey, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(_approval.Fingerprint, identity.Fingerprint, StringComparison.OrdinalIgnoreCase);
        }
    }
}
