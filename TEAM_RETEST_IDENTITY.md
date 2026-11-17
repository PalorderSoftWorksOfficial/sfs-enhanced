# Retest request — Gate 2 server identity pinning

**From:** manager. **To:** independent QA and the `TEAM_SECURITY_IDENTITY.md` reviewer.
**Design record:** `IDENTITY_GATE2_DESIGN.md` (supersedes the earlier `docs/SERVER_IDENTITY_PINNING.md` draft, which was removed to avoid a competing document).
**Rule:** reviewers are read-only. Report defects; do not fix them in place.

## 1. What changed

| Area | File | Change |
| --- | --- | --- |
| Identity calculation | `Shared/Protocol/ServerIdentity.cs` (new) | Validates 32-byte on-curve static point; derives 64-hex fingerprint and a grouped 16-char short form from one digest |
| Identity label | `Shared/Protocol/SecureChannel.cs` | Added `ComputeIdentityDigest`; `ComputeIdentityFingerprint` output is byte-identical to before |
| Handshake gate | `Shared/Protocol/SecureHandshake.cs` | Missing/malformed/off-curve static key is a hard failure; identity decided before the token proof; zero-static-secret fallback removed; player name trimmed once |
| Canonical authority | `Shared/Networking/EndpointKey.cs` (new) | `host:port` vs `host#auto`, IDNA, trailing dot, IPv6 bracketing, label and delimiter rejection, port conflicts, `LastSuccessfulKey` |
| Pin store | `Shared/Networking/ServerIdentityPinStore.cs` (new) | Per-endpoint pins, explicit-accept writes, previous-fingerprint audit on re-pin, corrupt-store backup, cap 64 |
| Token store | `Shared/Networking/EndpointTokenStore.cs` (new) | Per-endpoint tokens plus conservative legacy migration result codes |
| Client wiring | `Mod/Networking/NetClient.cs` | Gate before proof, per-endpoint token, migration notice, structured `ServerIdentityPrompt` |
| Gate/prompt | `Mod/Networking/ServerIdentityGate.cs` (new) | Pin lookup, trust, verify, forget |
| Migration | `Mod/Networking/TokenMigration.cs` (new) | Builds candidates from `ServerHistory` and defers selection to the shared helper |
| Storage | `Mod/Networking/PlayerPrefsEndpointStorage.cs` (new) | PlayerPrefs `IEndpointStorage`; separate keys for pins and tokens |
| Resolver | `Mod/Networking/ServerEndpointResolver.cs` | Preserves the canonical key; SRV target is informational; embedded port is no longer dropped; `.` target rejected |
| UI | `Mod/UI/App/AppState.cs`, `IAppActions.cs`, `MultiplayerMenu.cs`, `Pages/ServersPage.cs`, `Pages/SettingsPage.cs` | First-use and changed-identity panels, explicit trust, two-step forget, trusted-server list, migration notice |
| Server | `Server/Program.cs`, `Server/Networking/SecureSessionHandshake.cs` | Fingerprint printed at startup and in `status`; registration warning; player name trimmed once before lookup, validation and registration |
| Docs | `README.md`, `PRODUCT_ARCHITECTURE.md`, `IMPLEMENTATION_PLAN.md`, `IDENTITY_GATE2_DESIGN.md` | TLS and certificate-pinning claims removed; Gate 2 recorded; Gate 2.5 queued |

## 2. Focus for the security reviewer

1. **Confirm V3** from `TEAM_SECURITY_IDENTITY.md` §3 independently: `SecureSessionHandshake.cs` only length-checks `proof.Proof`, and the `registerMode` / `existing == null` paths never call `ValidateProof`. We did **not** change that, because a genuinely new client has no shared secret; the compensating control is that the client now sends nothing after its ephemeral key until the endpoint is trusted. Decide whether that is adequate or whether register mode should refuse entirely against an untrusted endpoint.
2. **Ordering.** Verify by reading `SecureHandshake.RunAsync` that no path reaches `ComputeTokenProof`, `ComputeRegistrationProof`, or the second `AuthResponse` send while the gate returns `FirstUse`/`Changed`, and that an invalid static key returns before key derivation.
3. **Tier 2 decision.** `IDENTITY_GATE2_DESIGN.md` §8 defers `IdentityProof`. Challenge the reasoning if you disagree.
4. **Pin write timing.** The pin is written by the explicit *Trust and connect* action and the client then retries. Confirm this cannot be abused: no proof on the refused attempt, no silent resume, no overwrite on mismatch.
5. **Token migration.** `TokenMigration.LastSuccessfulEndpointKey` uses the newest `LastConnectedUtc` entry and refuses to guess when it cannot derive a key. Confirm the failure path leaves `sfs_enhanced.token` intact and attaches it to nothing.
6. **Known limitations to verify are stated honestly:** global fixed registration salt (not changed), server-side unvalidated proof on account creation (not changed), plaintext PlayerPrefs pins (accepted, local attackers out of scope), and first-contact TOFU being insufficient against an on-path attacker — the UI says "trusted on this device", never "verified".

## 3. Commands (the in-repo `obj/` is root-owned)

```bash
export DOTNET_ROOT=/home/administrator/.dotnet; export PATH=$DOTNET_ROOT:$PATH
rsync -a --exclude .git --exclude bin --exclude obj ./ /tmp/retest-identity/repo/
cd /tmp/retest-identity/repo
dotnet build SFSEnhanced.sln                                   # expect 0 warnings / 0 errors
dotnet build scripts/multiplayer-integration/MultiplayerIntegration.csproj
# keep stdin open, otherwise the console loop treats EOF as shutdown
tail -f /dev/null | dotnet Server/bin/Debug/net8.0/SFSEnhanced.Server.dll --port 18942 --data /tmp/retest-identity/data &
for i in 1 2 3 4 5; do
  SFS_INTEGRATION_PORT=18942 dotnet scripts/multiplayer-integration/bin/Debug/net8.0/MultiplayerIntegration.dll
  echo "RUN$i_EXIT=$?"
done
```

Expect seven `[assert]` lines — including `identity is decided before the auth proof is sent`, `endpoint keys canonicalize without aliasing`, `endpoint tokens migrate once and never cross endpoints`, and `player names are canonicalized before lookup` — then `MULTI_CLIENT_INTEGRATION_PASSED` and exit 0 for all five runs.

## 4. Manager evidence so far

Clean copy `/tmp/g2final/repo`: solution build and harness build both `0 Warning(s) / 0 Error(s)`. Five consecutive runs against one server on port 18941 each printed all seven assertions and `MULTI_CLIENT_INTEGRATION_PASSED` with exit 0; a sixth run of the exact final tree also passed. The server log shows the intended `[auth-failed] handshake rejected` cycle and three `Bob` connections (plain, space-padded, lowercased) resolving to the same `PlayerId`.

## 5. Still unverified

- **The Gate 0 in-game gate remains open.** No SFS runtime here, so the D2 canvas mount, window layout, the new identity panel's geometry, and every visual claim are unproven.
- Resolver SRV behaviour against real DNS is untested; the pin-key invariance is tested, the lookup is not.
- Live `NetClient` behaviour against a hostile server (as opposed to the scripted transport in the harness) is untested.
- `Mod/Networking/ServerEndpointResolver.cs` and a `PlayerPrefsStorage.cs` were later assigned to an endpoint specialist who has not reported. The current resolver returns `CanonicalEndpoint`, which `NetClient` depends on; that API must be preserved or reconciled when the specialist's report lands.
