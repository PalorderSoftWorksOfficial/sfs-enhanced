# BUGS

Verified defects only. Every entry was reproduced or read at the cited source. Status reflects the
current worktree, not a plan.

Severity: **S1** data loss / auth bypass / crash, **S2** wrong behaviour with a workaround,
**S3** cosmetic or honesty.

## Open

### B-1 (S1) In-game D2 mount is unverified
Source: `Mod/UI/MultiplayerMenu.cs` `CreateScreenHolder`, `MultiplayerScreen`.
Repro: open the multiplayer shell in a real Spaceflight Simulator client.
Expected: the window mounts on the `Base_PC` canvas, renders, Escape walks the history stack.
Actual: never observed. No SFS runtime is available in this environment, so every visual and mount
claim is unverified.
Owner: manager + user (requires the game). Regression test: manual in-game probe.

### B-3 (S2) Chat text is not filtered server-side
Source: `Server/Networking/NetServer` chat handler.
Rotation: client renders with `richText = false`, which is the only defence. Abuse filtering is queued
in `IMPLEMENTATION_PLAN.md` Gate 1.



### B-6 (S3) Region overlap policy undefined
Source: `Server/Social/ClaimsService.cs`.
Region claims are bounded in size and count but may overlap arbitrarily, so two players can both
claim the same space. Needs an overlap rule before region claims are advertised in UI.

### B-7 (S3) Upload expiry has no operator surface
Source: `NetServer.Limits.cs`, `NetServer.Worlds.cs`.
Buffers expire after `--upload-idle-seconds`, but a client sees only a later "chunks must arrive in
order" error rather than an explicit "upload expired" code.

### B-8 (S3) Directory errors are indistinguishable from an empty list
Source: `Mod/Networking/ServerDirectoryClient.cs`.
A failed fetch renders as "no public servers listed" rather than a load error.

## Closed in this slice

### B-9 (S2) Non-current-screen shell teardown left a dead screen in the stack
Reported from an earlier trace of `Hide()`. Re-examined against `ScreenManager.SetStack`
(`Assembly-CSharp/SFS.Input/ScreenManager.cs:47`): `SetStack` replaces the entire stack and opens the
previous screen, so the buried multiplayer lambda cannot be reopened, and `Hide()` now returns without
destroying anything when the restore fails or when the shell is still current. Closed as code-verified;
the remaining gap is the in-game mount gate (B-1), not the teardown path.

### B-10 (S1) World download was not authorized
Source: `NetServer.Worlds.cs` `HandleWorldDownloadRequest` (pre-change).
Any authenticated client could download any world save by id, including private worlds it was not
allowed to join.
Fixed: one `CanAccessWorld` helper (owner, whitelist, friend, public) now gates both joining and
downloading, plus a per-player download rate limit and an 8 MiB size cap. Verified by the data-plane
assertion (stranger refused on a private world, owner and public downloads succeed).

### B-11 (S1) Upload and chunk transfer were unbounded
Source: `NetServer.Worlds.cs` `HandleWorldUploadChunk` (pre-change).
No total size cap, no ordering or duplicate enforcement, one buffer per player without limit, and a
malformed payload could throw out of the handler (`Convert.FromBase64String` outside a try/catch)
while leaving the reservation stuck.
Fixed: chunk and total caps, strictly increasing chunk index, duplicate rejection, one active upload
per player, a global concurrent cap, idle expiry, guarded decode/parse/validate, and `AbortUpload` on
every terminal path. Verified by ordered failure cases plus a successful retry after each one.

### B-12 (S1) Imported saves could smuggle ownership and trust
Source: `HandleWorldUploadChunk` (pre-change).
An uploaded `WorldRecord` kept its `OwnerPlayerId`, `ControllingPlayerId`, `TrustedPlayerIds` and
arbitrary build ids, so a save import could grant interaction rights or hijack controllers.
Fixed: `SfsWorldSanitizer` rewrites owner and controller ids to the uploader, clears claim trust
lists, rejects duplicate/oversized ids, non-finite state and dangling claims. Verified by the
smuggling assertion.

### B-13 (S2) A dropped client could hold a build forever
Source: `NetServer.Worlds.cs` (pre-change).
`ControllingPlayerId` was only cleared by another player's control request, so a disconnect could
leave a rocket locked if the build was claim-protected.
Fixed: control is released on leave/disconnect. Verified by the rejoin assertion.

### B-14 (S1) A token login silently created an account
Source: `Server/Networking/SecureSessionHandshake.cs` (pre-change) `else if (existing == null)` branch.
Repro: connect with a name the server has never seen, in token mode. The server created the account
and returned a token, so a stale or foreign credential produced a new account on someone else's
server, and a client could not tell "wrong token" from "name not registered".
Fixed: token mode authenticates existing accounts only and returns `unknown_account`; account creation
requires explicit `ModeRegister`. Verified by `[assert] account authentication is explicit and
failures are distinct`.

### B-15 (S1) Registration password was an offline verifier sent in the clear
Source: `Shared/Protocol/SecureChannel.cs` `ComputePasswordHash` (fixed global salt) and
`SecureHandshake`/`SecureSessionHandshake` (plaintext `RegistrationSecret` in the second
`AuthResponse`, sent before the client could verify the peer).
Repro: capture the second `AuthResponse`; the value plus the observable transcript is a verifier for
`passwordHash` at 600k SHA-256 per guess, transferable to every server using the same global salt.
Fixed: the proof is no longer computed or sent until the client has validated the pinned static key,
the session channel is derived, and the server has proven key possession by sending a sealed
`RegistrationPrompt`. The client answers with a sealed `RegistrationAuth`. The salt is per server
instance. A plaintext `RegistrationSecret` is refused. Documented limit: this removes passive capture
and cross-server reuse; it is not a PAKE, so a server that knows the password can still test guesses.
Verified by the password-server assertions in the harness.

### B-16 (S2) A failed write could corrupt the endpoint store
Source: `Mod/Networking/PlayerPrefsEndpointStorage.cs` (pre-change).
`Write` reported success unconditionally and `PlayerPrefs.Save()` failures are silent, so a failed
flush could leave a truncated pin or token document with no way to tell.
Fixed: `Shared/Networking/StagedEndpointStorage` writes to a `…pending` key, verifies the readback,
commits, verifies again, and clears the staged key. A dropped write is reported as failure and the
previous value is left intact. Verified by `[assert] staged storage never promotes a dropped write`.

### B-17 (S2) Whitespace and case variants created duplicate accounts
Source: `Server/Networking/SecureSessionHandshake.cs` (pre-change).
Lookup used the untrimmed name while registration used the trimmed name, so `"Bob "` created a second
account also displayed as `Bob`.
Fixed: `PlayerNameRules` canonicalizes once and the canonical value is used for lookup, validation,
registration and the transcript. Verified by
`[assert] player names are canonicalized before lookup` and the case/whitespace assertions in the
account-mode suite.

### B-18 (S1) README claimed TLS and certificate fingerprint pinning
Source: `README.md` (pre-change).
Fixed: the security section states there is no TLS, describes the P-256 sealed channel, per-endpoint
pinning, and the trust-on-first-use limit.
