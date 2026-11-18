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

### B-19 (S3) Rooms do not survive a server restart
Source: `Server/Rooms/RoomManager.cs` (in-memory by design).
Repro: open a room, restart the server. The world still exists on disk, the room listing is gone.
Expected: this is the documented lifecycle (a room is a live session, the world is the save), but the
product vision asks for persistent rooms. Fixing it needs a room record store, a liveness/heartbeat
rule so stale rooms expire, and a migration test. It is not silently persisted today.
Owner: manager. Regression test: restart test in `scripts/multiplayer-integration` (not written).

### B-20 (S3) A host who leaves keeps the room open with no ownership transfer
Source: `NetServer.Rooms.cs` `HandleWorldLeave` / `CloseRoomIfEmpty`.
Repro: host opens a friends-only room, leaves, never returns. The room stays listed (if populated)
and no one can close it because only the host may close.
Expected: rooms close when empty (implemented), but a populated room with an absent host has no
recovery path other than the host returning or the server restarting.
Needed: host transfer or a host-absent close policy. Today the close button is host-only and the
server refuses other callers with an explicit reason.
Owner: manager. Regression test: pending the lifecycle decision.

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

### B-3 (S2) Chat text is not filtered server-side
Source: `Server/Networking/NetServer.Social.cs` `HandleChat`, pre-change relayed text verbatim.
Fixed: `Server/Social/ChatTextFilter.cs` refuses `<`/`>` with `chat_rejected`, strips control,
zero-width and bidi characters, collapses whitespace, caps at 512 characters and refuses text that is
empty after filtering. Rate-limited refusals now return `chat_rate_limited` instead of being dropped
silently. Verified by `[assert] chat text is filtered and validated server-side`, which also proves a
refused message is not echoed to other players.

### B-6 (S3) Region overlap policy undefined
Source: `Server/Social/ClaimsService.cs`.
Fixed policy: a region claim may not overlap a region claim owned by another player (circle test), and
may not contain another player's build. Re-claiming the identical region returns the existing claim
instead of creating a duplicate. Refusals carry a precise reason and `claim_rejected`. Verified by
`[assert] region claims cannot overlap another player's region or build`.

### B-7 (S3) Upload expiry has no operator surface
Source: `NetServer.Limits.cs`, `NetServer.Worlds.cs`.
Fixed: an expired buffer leaves a short-lived tombstone, so a late chunk gets
`upload_expired` ("…expired after N seconds of inactivity…") instead of "chunks must arrive in order".
Sending chunk 0 again starts a fresh upload with the same id. The server logs
`[upload] expired player=… upload=… after N s idle` and `status` reports
`uploads pending=N expired=M`. Verified by the `upload_expired` assertion in the data-plane test.

### B-8 (S3) Directory errors are indistinguishable from an empty list
Source: `Mod/Networking/ServerDirectoryClient.cs`.
Fixed: the client returns `ServerDirectoryResult { Servers, Error }` and distinguishes no URL,
timeout, unreachable host, non-success HTTP status and unreadable JSON. The Servers page renders
"COULD NOT LOAD THE DIRECTORY", the reason and a RETRY action, independent of the empty-list state.
Code-verified only: no directory failure is exercised by the headless harness, and the page itself
needs the in-game mount gate (B-1).

### B-15 (S1) Registration password was an offline verifier sent in the clear
Source: `Shared/Protocol/SecureChannel.cs` `ComputePasswordHash` (fixed global salt) and
`SecureHandshake`/`SecureSessionHandshake` (plaintext `RegistrationSecret` in the second
`AuthResponse`, sent before the client could verify the peer).
Repro: capture the second `AuthResponse`; the value plus the observable transcript is a verifier for
`passwordHash` at 600k SHA-256 per guess, transferable to every server using the same global salt.
Fixed: the registration-password proof is now answered only inside the sealed channel, and the token
MAC is no longer sent before the server proves key possession. Exact ordering: the trust decision is
the local pin (or one-time user acceptance); the client sends its ephemeral key; the server returns
`AuthChallenge` with its static key; the server sends a sealed `IdentityProof` (HMAC bound to the
static-key shared secret and the transcript) and only then does the client send the token MAC; a
password server next sends a sealed `RegistrationPrompt` whose production requires the static private
key, and the client answers with a sealed `RegistrationAuth`. Neither the password proof nor the
token MAC can be harvested from a peer that cannot complete the channel. The salt is per server
instance and the v3 KDF cost is 100k rounds (measured 1.4s at 600k was a blocking stall on the
interactive path). A plaintext `RegistrationSecret` is refused.
Documented limit: this removes passive capture and cross-server reuse; it is not a PAKE, so a server
that knows the password can still test guesses offline, and trust-on-first-use still cannot detect an
attacker on the very first connection. Verified by the password-server assertions (20 back-to-back
registrations and 20 token sign-ins on the gated server) and by
`[assert] identity is decided and proven before the auth proof is sent`, which includes a server that
omits the proof and a server that sends a forged one.

### B-21 (S2) A corrupt identity key silently became a new identity
Source: `Server/Networking/SecureSessionHandshake.cs` `InitializeStaticKey` (pre-change).
Repro: leave `data/system/server_static_key.json` present but empty or unreadable and start the
server. The old code generated a new key and overwrote the file, so every client that had pinned the
server saw an identity change with no operator explanation, and the real key was lost.
Fixed: only a genuinely absent key file creates a key. A present file that cannot be read, is empty,
or is not a valid P-256 scalar now fails startup with an actionable message, sets exit code 1 and
leaves the file untouched. Verified manually: two consecutive starts printed the same fingerprint, and
a corrupt file produced `Startup failed: …` with exit 1 and no rewrite.

### B-22 (S3) Two simultaneous joins could exceed room capacity
Source: `NetServer.Rooms.cs` `HandleRoomJoin` (pre-change read the population, then set membership in
a later step).
Repro: two clients join the last free slot at the same instant; both read `players < capacity` and
both are admitted.
Fixed: the capacity check and membership assignment happen under `_roomJoinGate`, and room creation
under `_roomCreateGate` so two rooms cannot be opened for one world. Verified by the existing room
assertions and code review; no race-specific harness test exists (deterministic concurrency is not
reproducible in the current harness).

### B-23 (S2) Linux release publish failed on the Windows-only pack step
Source: `Server/SFSEnhanced.Server.csproj` `BuildSfsPack` target.
Repro: `dotnet publish Server/SFSEnhanced.Server.csproj -r linux-x64 -c Release` on Linux. The target
used backslash separators and assumed `tools/BuildSfsPack/bin/Release/net8.0/BuildSfsPack.dll` already
existed, so publish exited with MSB3073 and produced nothing.
Fixed: the target uses forward slashes, builds `BuildSfsPack` for the current configuration before
invoking it, and packages the build output. Verified: Release build produces
`artifacts/SFSEnhanced-Server-1.0.0.pack`, and linux-x64 plus win-x64 publishes both succeed.

### B-24 (S3) Documented EOF shutdown did not stop the server
Source: `Server/Program.cs` console loop (pre-change broke out of the loop on EOF without cancelling).
Repro: `SFSEnhanced.Server --data /tmp/x < /dev/null` hung forever, contradicting the documented
non-interactive run.
Fixed: EOF cancels the server token, so the process stops cleanly. Verified against the published
linux-x64 build: startup prints the identity, then `Server stopped.` and exit 0.

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
