# QA / Security Review — SFS Enhanced

**Reviewer role:** independent QA + security reviewer (read-only audit).
**Scope:** `PRODUCT_VISION.md`, solution/project files, `Shared/`, `Server/`, `Directory/`, `Auth/`, `Mod/`, `scripts/`, `.github/workflows/`, docs.
**Date:** 2026-11-14
**Source changes by reviewer:** none. Only this file was created. No commits, no pushes, no deletions.

### Audit baseline (important)

All findings were assessed against **HEAD `78b25b0` plus the working-tree state observed at audit start**. While this audit was running, other agents changed the tree concurrently (`git status`):

- Modified: `Mod/Networking/NetClient.cs`, `Server/Networking/NetServer.cs`, `Server/Social/FriendsService.cs`, `Shared/Models/WorldModels.cs`, `Shared/Protocol/PacketType.cs`, `Shared/Protocol/Packets.cs`
- Deleted: `Mod/UI/MultiplayerMenu.Packets.cs`, `Mod/UI/MultiplayerMenu.Panels.cs`
- Added (untracked): `Mod/UI/App/`, `Mod/UI/Kit/`, `Server/Networking/NetServer.Profile.cs`, `Server/Social/ProfileService.cs`, `TEAM_REPORT_PLANNER.md`

Consequences for the manager:

1. Findings that cite a deleted file (**V4**, **Q-30**, and the `MultiplayerMenu.Packets.cs` reference under **V4**) describe the baseline code; the UI packet/panel logic may now live under `Mod/UI/App/` and must be re-checked against that code, not against this report alone.
2. Findings that cite a modified file (**V3/V14/V17/V18/V22**, **Q-13/Q-14/Q-16/Q-19**) must be re-verified against the current working tree before assignment, because the modification may already address part of them.
3. An in-progress profile slice (`NetServer.Profile.cs`, `ProfileService.cs`, `Mod/UI/App/`, `Mod/UI/Kit/`) exists in the working tree. Section 7's acceptance criteria were written without reading it; treat them as a checklist for reviewing that slice rather than as a green-field spec.
4. No build or test result in this report covers those uncommitted files: the successful clean-copy build (1.1) ran against the tree as copied at that moment, before those additions were present.

**Evidence labels used below**

- **[VERIFIED]** — reproduced or measured in this session; command/output quoted.
- **[INSPECTED]** — read from source and traced by hand; no runtime reproduction attempted in this pass.
- **[DOC]** — claim made in project docs that the code does not support.

---

## 1. Baseline environment

| Item | Value |
| --- | --- |
| OS | Ubuntu 26.04.1 LTS |
| .NET SDK | `8.0.428` at `/home/administrator/.dotnet` (SDK-only install; **not on `PATH`**) |
| Other toolchains | `javac` only. No `mono`, no `msbuild`, no `nuget`. |
| Game assemblies | `Dependencies/` present (Assembly-CSharp.dll, UnityEngine modules, Newtonsoft, 0Harmony) |
| NuGet | repo-root `nuget.config` clears offline feeds; `nuget.org` reachable; required packages already in `~/.nuget/packages` |
| Repo state | branch `main`, HEAD `78b25b0`, untracked `PRODUCT_VISION.md` |
| `/tmp` mount | `noexec` — `dotnet run` of built apphosts fails with `Permission denied`; run built apps as `dotnet <app>.dll` |

### 1.1 Build — [VERIFIED]

In-place build is **blocked by pre-existing root-owned build artifacts**, not by the code:

```
$ dotnet build SFSEnhanced.sln
error : Error writing to source link file
  'obj/Debug/netstandard2.0/SFSEnhanced.Shared.sourcelink.json':
  Access to the path
  '/home/administrator/sfs-enhanced/Shared/obj/Debug/netstandard2.0/SFSEnhanced.Shared.sourcelink.json'
  is denied.  [Shared/SFSEnhanced.Shared.csproj]
Build FAILED.  1 Error(s)
```

Root-owned artifacts found (`find . -maxdepth 3 -user root`):

```
./Shared/obj/Release            (root:root)
./scripts/multiplayer-integration/bin   (root:root)
./scripts/multiplayer-integration/obj   (root:root)
```

Any CI agent or developer running as `administrator` cannot build in place. This must be fixed by the environment owner (delete/`chown` those directories); it is an environment defect, **not** a source defect, and it masks real build results.

Same tree copied to a writable location builds cleanly:

```
$ cp -a . /tmp/sfs-qa/repo && cd /tmp/sfs-qa/repo && dotnet build SFSEnhanced.sln
SFSEnhanced.Shared    -> Shared/bin/Debug/netstandard2.0/SFSEnhanced.Shared.dll
BuildSfsPack          -> tools/BuildSfsPack/bin/Debug/net8.0/BuildSfsPack.dll
SFSEnhanced.Directory -> Directory/bin/Debug/net8.0/SFSEnhanced.Directory.dll
SFSEnhanced.Mod       -> Mod/bin/Debug/net472/SFSEnhanced.dll
SFSEnhanced.Server    -> Server/bin/Debug/net8.0/SFSEnhanced.Server.dll
Build succeeded.  0 Warning(s)  0 Error(s)
```

**Canonical commands for the team**

```bash
export DOTNET_ROOT=/home/administrator/.dotnet
export PATH=$DOTNET_ROOT:$PATH
dotnet build SFSEnhanced.sln                       # requires writable obj/ (see 1.1)
dotnet build Auth/SFSEnhanced.Auth.csproj          # NOT in the solution
dotnet build scripts/multiplayer-integration/MultiplayerIntegration.csproj
```

### 1.2 Solution coverage — [VERIFIED]

`SFSEnhanced.sln` contains 5 projects: `Shared`, `Server`, `Mod`, `Directory`, `tools/BuildSfsPack`.

Not in the solution, not built by `dotnet build SFSEnhanced.sln`:

- `Auth/SFSEnhanced.Auth.csproj` — Steam ticket service.
- `scripts/multiplayer-integration/MultiplayerIntegration.csproj` — the only end-to-end test.
- `TestClient/` — referenced by the smoke scripts but **contains no `.cs` and no `.csproj`**, only `bin/` and `obj/` folders.

Consequence: the primary documented build command (`dotnet build SFSEnhanced.sln`) does not compile the test harness or the auth service, so the "all projects build" claim in `agent.md` is false as stated.

### 1.3 Test execution status — [VERIFIED, NOT PASSED]

| Check | Result |
| --- | --- |
| `dotnet build SFSEnhanced.sln` (clean copy) | **PASS** — 0 warnings, 0 errors |
| `dotnet build SFSEnhanced.sln` (in place) | **FAIL** — permission denied on root-owned `obj` |
| `scripts/fac-smoke-test.sh` | **CANNOT RUN** — builds `TestClient/SFSEnhanced.TestClient.csproj`, which does not exist |
| `scripts/multiplayer-integration` | **NOT PASSED — did not execute.** Attempt: started `SFSEnhanced.Server.dll --port 18777`, harness aborted with `System.InvalidOperationException: Transport connection failed`. The background server did not survive the command's process cleanup, so no handshake happened. No pass may be claimed for this harness. |
| Unit/integration tests (xunit/NUnit/MSTest) | **NONE EXIST.** `code_search` for `xunit|NUnit|MSTest|[Fact]|[Test]` returned zero test-framework references outside `Assembly-CSharp`. `PRODUCT_VISION`/`agent.md` calls this "the multiplayer integration harness"; it is a hand-rolled console program, not a test framework, and it fails by throwing. |

The harness must be re-run by whoever owns a long-lived server process before any release claim. A correct invocation is:

```bash
dotnet Server/bin/Debug/net8.0/SFSEnhanced.Server.dll --port 18777 --data /tmp/sfs-data &
SFS_INTEGRATION_PORT=18777 dotnet scripts/multiplayer-integration/bin/Debug/net8.0/MultiplayerIntegration.dll
# expect: MULTI_CLIENT_INTEGRATION_PASSED
```

### 1.4 Measured crypto/handshake costs — [VERIFIED]

Benchmark against the built `Shared` assembly (`Release`, same host):

```
ComputePasswordHash single call ms=1484
P256 keygen  avg ms=0
P256 PublicPoint avg ms=76
```

`SessionAuth.ComputePasswordHash` costs **~1.5 s of CPU per call**, and every handshake performs several P256 scalar multiplications (~76 ms each). This is the basis of finding **S3**.

---

## 2. Verified defects

### V1 — Idle dedicated server spins one core at ~118% CPU **[VERIFIED, HIGH]**

`Server/Program.cs`, `ConsoleCommandLoop`:

```csharp
while (!cts.IsCancellationRequested)
{
    string line = Console.ReadLine();
    if (line == null) continue;      // EOF -> null forever -> tight loop
```

Run with stdin closed (systemd, nohup, container, CI redirect — the normal deployment shapes):

```
$ dotnet Server/bin/Debug/net8.0/SFSEnhanced.Server.dll --port 18777 --data /tmp/sfs-qa/data </dev/null
$ ps -eo pid,pcpu,args | grep SFSEnhanced.Server
1735721  118  00:04  dotnet Server/bin/Debug/net8.0/SFSEnhanced.Server.dll --port 18777 ...
```

**Impact:** every production Linux host burns a full core doing nothing; combined with `WorldTimeLoopAsync` this makes hosting cost and thermal/CPU complaints inevitable.
**Fix direction (manager to assign):** break the loop when `ReadLine()` returns `null`, or move console commands behind a TTY check / timed read.

### V2 — CI smoke-test job cannot pass **[VERIFIED, HIGH]**

`.github/workflows/server-cross-platform.yml` runs `./scripts/fac-smoke-test.sh` (and the PowerShell twin) on both runners. Those scripts execute `dotnet build TestClient/SFSEnhanced.TestClient.csproj`; `TestClient/` contains only `bin/` and `obj/`. The job fails before exercising the server, so the only CI coverage of the server transport is dead.
**Note:** the same workflow's integration step starts the server with stdout/stderr redirected to a file while stdin is inherited from the runner — combined with V1 this can also pin a runner core.

### V3 — Multiplayer integration harness reports "pass" only by exception-free exit, and it is fragile **[INSPECTED, MEDIUM]**

- `scripts/multiplayer-integration/Program.cs` has no assertions framework and no test isolation: two peers, one shared world, sequential awaits with 4 s timeouts. Any latency spike on a loaded runner becomes a failure with no diagnostics.
- `PeerTransport.ReceiveRawAsync` spawns a **new** pump `Task` per call and completes/discards its buffer in `finally`. Frames that arrive just after the awaited one (e.g. `AuthResult` immediately after `AuthChallenge`/`AuthResponse`) are read by a pump whose buffer is already completed and dropped. This is a genuine cause of intermittent handshake failures.
- The harness never covers: private-world join rejection, world download authorization, region claims, build-control grants, upload limits, disconnect/`ControllingPlayerId` cleanup, rate limits, directory service, auth service, or any `Mod/` code.

### V4 — Protocol enum advertises messages nobody implements **[VERIFIED by search, MEDIUM]**

- `PacketType.ClaimListRequest` (62) / `ClaimListResponse` (63): declared in `Shared/Protocol/PacketType.cs`; **no handler in `Server/Networking/*`** and no sender in `Mod/`. A client sending `ClaimListRequest` gets `Error: "Unhandled packet ClaimListRequest"`.
- `TimeWarpStatePacket` and `PlayerPresencePacket` are deserialized by `Mod/UI/MultiplayerMenu.Packets.cs` (baseline file, deleted in the working tree during this audit — see §0) but **never sent by the server** (only `WorldTimeState` / `TimeWarpResult` are). Dead contracts.
- `PacketType.WorldUpload` (21) is declared but unused; upload works only via `WorldUploadChunk`.
- `Mod/Social/FriendsUI.cs` still carries `TODO(game-hook)` comments although `Mod/UI/MultiplayerMenu.Panels.cs` renders the list — stale comments that misrepresent the state.

This is the "unimplemented API contracts / fake functionality" class the vision explicitly forbids.

### V5 — No client verification of the server identity despite documented pinning **[DOC + INSPECTED, HIGH]**

`Shared/Protocol/SecureSessionHandshake.cs` sends `IdentityFingerprint` in `AuthChallengePacket`. `Shared/Protocol/SecureHandshake.cs` (`SecureHandshakeClient.RunAsync`) **never reads `challenge.IdentityFingerprint`**, and `Mod/Networking/NetClient.cs` has no pin store, no first-use confirmation, and no UI to display or accept a fingerprint.

`README.md` states: "Multiplayer traffic is not sent as plaintext TCP. The server creates or loads a TLS certificate…" and "The client supports certificate fingerprint pinning." `agent.md` and `docs/ARCHITECTURE.md` say TCP/TLS was **removed** in favour of Lidgren UDP + app-layer crypto. Both cannot be true; the code has **no TLS and no pinning**.

**Impact:** a client cannot distinguish the intended server from a rogue endpoint at the same address (hostile LAN, poisoned DNS/SRV, compromised community host). Because `Server/Networking/SecureSessionHandshake.cs` auto-registers unknown names in token mode, a rogue server simply accepts the client and issues its own token, then observes chat, blueprints and invites. The handshake's static-key mixing prevents a passive MITM from reading the real session, but it does **not** let the client authenticate the peer.
**Needed:** persist and compare the fingerprint (TOFU + explicit change prompt), surface it in the connect UI, and correct `README.md` to describe the actual transport.

### V6 — The stored "token hash" is the authentication key **[INSPECTED, HIGH]**

`Server/Persistence/AccountService.cs`:

```csharp
account.AuthTokenKey = Convert.ToBase64String(SessionAuth.ComputeTokenKey(token)); // SHA-256(token)
...
byte[] expected = SessionAuth.ComputeTokenProof(tokenKey, transcriptHash);        // HMAC(tokenKey, ...)
```

The persisted value is used directly as the HMAC key. Anyone who reads `data/accounts/*.json` (backup, misconfigured share, path traversal via V9, compromised host) can authenticate as **every** account without cracking anything — no offline work is required. `README.md`'s "tokens are stored as SHA-256 hashes" implies one-way protection that does not exist.
**Needed:** either document this explicitly as "database is the credential" or move to a proof-of-possession design (per-account keypair, client signs the transcript; the server stores only the public key).

### V7 — Long-lived bearer token in plaintext Unity `PlayerPrefs` **[INSPECTED, HIGH]**

`Mod/ModSettings.cs`: `AuthToken` → `PlayerPrefs` (`sfs_enhanced.token`). On Windows that is the registry under `HKCU\Software\<company>\<product>`; on Linux `~/.config/unity3d/...`. It is not encrypted, never expires, and there is no rotation or revoke path (`AuthResultPacket.AuthToken` is issued once; nothing invalidates it). Any local process or user on a shared machine can copy the token for permanent takeover. There is also no way for a user to log out other than deleting prefs.

### V8 — Registration password is expensive on the unauthenticated path, globally salted, and unusable from the UI **[VERIFIED cost + INSPECTED design, HIGH]**

`Shared/Protocol/SecureChannel.cs`:

```csharp
public static byte[] ComputePasswordHash(string registrationPassword)
{
    byte[] salt = Encoding.UTF8.GetBytes("SFSEnhanced-Registration-Salt-v1"); // fixed, global
    ... 600000 rounds of SHA-256 ...
```

`Server/Networking/SecureSessionHandshake.cs` computes `required = ComputeRegistrationProof(ComputePasswordHash(config.RegistrationPassword), transcriptHash)` for **every** incoming handshake when `RegistrationPassword` is set — before any proof is validated and before `MaxPlayers` is enforced:
`_connections.Count >= _config.MaxPlayers` is checked **after** `AuthenticateAsync` returns.

Measured cost: **1484 ms CPU per call** (section 1.4). Floating a few dozen handshakes in parallel saturates every core on a typical host; the attacker needs no account and no valid token. Related weaknesses: single fixed salt (identical passwords hash identically across servers), `RegistrationPassword` stored in plaintext in `server.json`, and the client never sends one (`NetClient.ConnectAsync(host, port, playerName)` → `registrationPassword: null`, and `Mod/UI/MultiplayerMenu*.cs` has no field for it), so **the mod cannot connect to any server that sets a registration password** — a supported configuration is unusable from the game.

### V9 — Client-controlled identifiers reach the filesystem unchecked **[INSPECTED, MEDIUM-HIGH]**

`Server/Networking/NetServer.Social.cs` → `HandleFriendResponse` passes `resp.FromPlayerId` (client-supplied) to `_friends.RespondToRequest` → `AccountService.FindById(fromPlayerId)` → `FileStore.Load("accounts", id)`:

```csharp
string path = Path.Combine(_root, folder, id + ".json");
```

Nothing validates that `id` is a 32-char GUID. `FromPlayerId = "../../../etc/passwd"` yields a traversal read (with `.json` appended); a crafted record on disk whose `PlayerId` is attacker-chosen would then be re-saved through `AccountService.Save` into a path outside `accounts/` (write traversal). `WorldManager.Get(worldId)` is cache-only, so world IDs are not exposed the same way, but the pattern is shared.
**Needed:** validate every client-supplied identifier (`Guid.TryParseExact(..., "N")`), and add a `Path.GetFullPath` containment assertion inside `FileStore` so no future caller can escape the root.

### V10 — World download has no authorization check **[INSPECTED, MEDIUM-HIGH]**

`Server/Networking/NetServer.Worlds.cs`, `HandleWorldDownloadRequest` looks up the world and immediately streams the full `WorldRecord` — builds (including `PartsBlueprintJson`), claims, whitelist and owner IDs. `HandleWorldJoin` in the same file **does** enforce `!IsPublic && owner != me && !AreFriends && !Whitelist`. The download path has no equivalent check, so any authenticated player who knows or is given a private world id can extract the whole private world. Guids are unguessable, but ids leak through invites, logs, chat, and former members.
**Repro (once a server is running):** account A creates a private world; account B (no friendship, not whitelisted) sends `WorldDownloadRequest{WorldId=<A's id>}` and receives `WorldDownloadChunk` frames with the full JSON.
**Same file:** `HandleWorldUploadChunk` also never checks that the chunk author owns anything, and uploaded `Claims`/`Builds` are re-owned to the uploader without validation.

### V11 — Upload path has no resource bounds; a single packet can request a multi-gigabyte allocation **[INSPECTED, HIGH]**

```csharp
var buffer = _pendingUploads.GetOrAdd(uploadKey, _ => new List<string>(new string[chunk.TotalChunks]));
```

`TotalChunks` is attacker-controlled and only checked for `> 0`. `new string[int.MaxValue]` is a ~17 GB allocation; a smaller value (e.g. 200,000,000 → 1.6 GB) succeeds and is retained until the connection closes. There is no cap on `TotalChunks`, no cap on total upload bytes, no cap on concurrent `UploadId`s per account, and `Convert.FromBase64String(string.Concat(buffer))` then materialises the whole payload again. Uploaded worlds are written to disk with no quota (`_worlds.Create` + `Persist`), so disk exhaustion is one looping client away.
**Needed:** hard caps (e.g. 4 MB/world upload, ≤ 64 chunks, ≤ 2 concurrent uploads), reject out-of-range `TotalChunks` before allocating, enforce a per-account world/storage quota.

### V12 — Region claims let any player freeze other players' builds **[INSPECTED, HIGH]**

`Server/Social/ClaimsService.cs`:

```csharp
bool covers = (claim.Shape == ClaimShape.Build && claim.BuildId == buildId)
    || (claim.Shape == ClaimShape.Region && build != null && WithinRadius(claim, build));
if (!covers) continue;
if (claim.OwnerPlayerId == playerId) return true;
if (claim.TrustedPlayerIds.Contains(playerId)) return true;
return false;                       // first covering claim wins, order = insertion order
```

`HandleClaimCreate` (`NetServer.Social.cs`) validates ownership **only** for `ClaimShape.Build`; `Region` claims accept any `CenterX/CenterY/RadiusMeters` — including `double.PositiveInfinity`, which covers every build in the world. Claims are never bounded by count, per-player limit, or world bounds. Once such a claim exists and precedes a player's own build claim in `world.Claims`, that player's `BuildStateUpdate` and `BuildRemove` are denied (`ClaimDenied`, "This build is claimed by another player") — including for builds they spawned afterwards, because spawning appends a claim **after** the region claim. `HandleBuildSpawn` also never checks `_claims.CanInteract`, so builds can be pushed into a claimed region freely.
**Repro:** Alice creates a public world and a build; Bob joins and sends `ClaimCreate{Shape=Region, CenterX=0, CenterY=0, RadiusMeters=1e12}`; Alice spawns a new rocket → her `RocketPrimaryState` for it is rejected. Bob has soft-locked the world without touching a build.
**Needed:** reject non-finite/negative/oversized radii, cap claims per player per world, evaluate build-claims before region-claims (or grant owners of the build precedence), and check claims on spawn.

### V13 — `ControllingPlayerId` is never released **[INSPECTED, MEDIUM]**

`HandleBuildControlRequest` only grants when `build.ControllingPlayerId == null || == playerId`. Nothing clears it on `WorldLeave`, disconnect (`HandleClientAsync` finally), or world unload. A player who disconnects while piloting leaves every build they controlled permanently in "Another player is already piloting this" state; the build can never be taken over, and `BuildRemove` still works only for the owner, so the world accumulates dead builds forever.
**Repro:** Alice spawns and controls build-1, then disconnects; Bob's `BuildControlRequest` for build-1 is denied.

### V14 — Untrusted blueprint JSON and unbounded build spam reach every client **[INSPECTED, MEDIUM-HIGH]**

`HandleBuildSpawn` trusts a client `BuildSnapshot` wholesale: arbitrary `BuildId`, arbitrary `PartsBlueprintJson` up to the 8 MB packet cap, no count limit, no validation of `PlanetAddress`/`Kind`. Clients feed it to `Mod/World/MultiBuildManager.cs` → `JsonWrapper.FromJson<RocketSave>(rocketSaveJson)` → `RocketManager.LoadRocket`. A malformed or hostile blueprint is therefore deserialized and instantiated inside the game process of every player in the world (crash, part-count explosion, or worse depending on `RocketSave` handling). There is also no rate limit: `BuildSpawn` persists the whole world file on each call.
**Needed:** validate blueprint size and shape, cap builds per player per world, rate-limit spawn/state packets, and wrap the client-side `LoadRocket` in a budget.

### V15 — Numeric "validation" does not constrain physics **[INSPECTED, MEDIUM]**

`NetServer.cs` `IsFinite` rejects NaN/Infinity only. `PosX = 1e308`, `VelX = 1e308` are accepted and relayed; remote clients interpolate toward them (`MultiBuildManager.ApplyTransform` → `physics.SetLocationAndState`), producing absurd positions that can break rendering or throw inside the game. `docs/ARCHITECTURE.md` promises "server-side numeric validation" and a v2 item for "impossible accelerations" — neither exists.

### V16 — No rate limiting anywhere; every write path is a disk amplifier **[INSPECTED, MEDIUM-HIGH]**

Neither `NetServer` nor any service has per-player or per-IP throttling. Every one of these writes the **entire** world or account JSON under a single global `FileStore._lock`:

| Packet | Write cost |
| --- | --- |
| `ClaimCreate` / `ClaimRemove` | full `worlds/<id>.json` rewrite |
| `BuildSpawn` / `BuildRemove` / `BuildControlRequest` | full world rewrite |
| `WorldCreate` | new world file, unbounded count per player |
| `FriendRequest` / `FriendRequestResponse` | two full `accounts/*.json` rewrites |
| `ChatMessage` | fan-out to all connections, no throttle |

A single client can drive continuous whole-file rewrites and world-list/disk growth. `AccountService.FindByName` additionally reads **every** account file on every handshake and on every friend request (no index, no cache), so cost grows linearly with the user base.

### V17 — Concurrent account writes lose data **[INSPECTED, MEDIUM]**

`FriendsService` loads an account, mutates it, and `_accounts.Save(...)` writes it back — with no per-account lock and no server-side cache. `RequestFriend` writes both requester and target; `RespondToRequest` writes responder and requester; `AccountService.Touch` writes on every connect. Two players accepting/requesting concurrently (or a connect racing a friend accept) silently clobber each other's friend lists. Same pattern for `WorldRecord.Builds`/`Claims` lists mutated from multiple `HandleClientAsync` tasks without locks (`List<T>` is not thread-safe; `HandleBuildSpawn`/`HandleBuildRemove`/`RemoveAll` on shared lists can throw or corrupt).

### V18 — `NetClient` disconnect races **[INSPECTED, MEDIUM]**

`Mod/Networking/NetClient.cs`: `Disconnect()` sets `_cts = null`, `_peer = null`, `_pendingFrames.CompleteAdding()` while `PumpAsync`, `ConsumeFrames`, and the handshake transport may still be using them; `_pendingFrames` is replaced on reconnect while old consumers block on the old instance. Expected failure modes are `ObjectDisposedException` / `NullReferenceException` during connect-disconnect-reconnect, which the UI exposes ("DISCONNECT" button plus a panel back-navigation path). The connect path also awaits on the Unity main thread (`ModMain.ConnectToServer` is `async void`), so a hung connect freezes the menu.

### V19 — Server does not print the identity fingerprint it documents **[VERIFIED]**

`Server/Program.cs` prints name/port/data/advertise only. `README.md`: "The generated certificate fingerprint is printed to the server console and can be published with the server listing." `SecureSessionHandshake.IdentityFingerprint` exists but is never surfaced — so V5's pinning cannot even be performed manually by a host.

### V20 — Directory service is unauthenticated, unbounded, and in-memory **[INSPECTED, MEDIUM]**

`Directory/Program.cs`:

- `POST /api/v1/servers/register` accepts unlimited registrations from anyone; `servers` is a `ConcurrentDictionary` that never evicts → unbounded memory growth and listing spam (the only expiry is a 45 s filter on the *read* path).
- Heartbeat token comparison `existing.Token != request.HeartbeatToken` is not fixed-time (low practical risk for 32 random bytes, but trivially fixable).
- No rate limits, no proof of ownership of the advertised host, no cap on `Motd`/`Name` length, no TLS termination guidance.
- Listings are lost on restart (acceptable, but should be documented).
- Clients display `Host` from the directory and connect without any fingerprint (V5), so a forged listing is a direct path to a rogue server.

### V21 — Steam authentication is unwired dead code **[VERIFIED + INSPECTED, MEDIUM]**

`Auth/Program.cs` + `Auth/SteamTicketVerifier.cs` expose a single endpoint that verifies a ticket and returns a SteamID. Nothing calls it: no `ProjectReference` from `Mod`/`Server` (grep for `SFSEnhanced.Auth` outside `Auth/` finds only an unrelated crypto label), no session-credential issuance, no server-side validation, and the project is not in the solution. `docs/SECURE_STEAM_AUTH.md` / `docs/STEAM_AUTH_NEXT.md` therefore describe an unimplemented flow. Also: the Steam **Web API key is placed in the query string** of the outbound URL (line 20 of `SteamTicketVerifier.cs`), which leaks into proxy/`HttpClient` logs and diagnostics; and the endpoint has no rate limiting, so it can be used to exhaust the Steam API quota.

### V22 — Documentation asserts removed/missing security properties **[DOC, HIGH for trust]**

- `README.md`: "TLS transport: Implemented", "TLS certificate fingerprint pinning", "Persistent authentication: Implemented".
- `docs/ARCHITECTURE.md`: "The game client connects over the same TCP protocol regardless of the host OS", "voice chat: `ChatMessagePacket` already exists", "time warp enforcement is a `// TODO(game-hook)`" — all stale; the code is UDP-only, has no voice, and does implement timewarp voting.
- `agent.md`: "Mod targets net472 … Run integration against it" is accurate, but "TLS-protected" references and "no Harmony" conflict with `Mod/SFSEnhanced.Mod.csproj`, which still has a `Lib.Harmony` PackageReference (unused by code).
- `PRODUCT_VISION.md` requires `QA_PLAN.md`, `BUGS.md`, `DESIGN_SYSTEM.md`, `UX_FLOWS.md`, `IMPLEMENTATION_PLAN.md`, `AGENT_ROLES.md`; **only `ARCHITECTURE.md`, `ROADMAP.md` and research notes exist**. This report is the only QA artefact.

---

## 3. Test coverage gaps

Current automated coverage is limited to one console program that exercises a happy path. Nothing guards the failure modes above.

| Area | Coverage | Gap |
| --- | --- | --- |
| Handshake happy path + issued token | harness (unverified this session) | no test for expired/rotated tokens (feature absent), registration password, `AllowRegistration=false`, handshake timeout |
| Wrong-token rejection | harness | no brute-force/rate-limit test; no test that account files are not readable via crafted ids |
| Replay + tamper rejection | harness (echo-count heuristic) | the test itself is timing-dependent (`DrainUntilQuietAsync`, 400 ms) and can pass vacuously if the probe packets are dropped |
| Privacy/authorization | **none** | private-world join is untested; world download authorization untested; region claims untested; build control/ownership transfer untested |
| Upload/download | **none** | chunk ordering, duplicate chunks, `TotalChunks` mismatch, oversized upload, invalid JSON, quota |
| Persistence/migration | **none** | no test loads a pre-existing `worlds/` or `accounts/` fixture; no schema-version field exists to test |
| Concurrency | **none** | no parallel-connect, parallel-friend-accept, or parallel-claim tests |
| Directory | **none** | no HTTP tests at all |
| Auth service | **none** | no tests, not built in CI |
| Mod/UI | **none** | no play-mode or even compile-level contract tests beyond `dotnet build` of `net472` |
| Multiplayer integration in CI | declared in workflow but not verified to pass (V2, V3) | — |

**Minimum test project to add** (owner: implementer, per manager assignment): a `SFSEnhanced.Tests` xunit project covering `SecureChannel` (replay window, MAC failure, sequence wrap, cross-channel isolation), `SessionAuth` (proof/transcript binding, `FixedTimeEquals`), `ClaimsService` (region/build precedence, non-finite radius), `FileStore` (id validation, concurrent writes), and `WorldManager` (persist/load round trip).

---

## 4. Integration test readiness

**Ready enough to be useful, not ready to be trusted.**

- Builds from source once `obj` ownership is fixed; harness builds separately.
- Requires a running server on a reachable port; `SFS_INTEGRATION_PORT` is supported. The server must not be started with stdin closed until V1 is fixed (it will spin a core and may starve the runner).
- Not usable as a gate: no framework, no per-case reporting, hand-rolled timing, and a known frame-loss bug in `PeerTransport` (V3).
- CI runs it only on `ubuntu-latest` for the server workflow, and only when paths under `Server/`, `Shared/`, `TestClient/`, `scripts/` change — so `Mod/`-only changes get no multiplayer regression signal at all.
- No environment fixture: the harness creates unique account names per run, so it never exercises account reuse, migrations, or cleanup. Data directory grows in CI without assertions.
- Recommended preconditions for accepting it as a gate: (1) fix `obj` ownership, (2) fix `PeerTransport` single-consumer pumping, (3) replace `TestPeer` polling loops with a single pump queue, (4) add the negative tests listed in section 3, (5) run it on both `ubuntu-latest` and `windows-latest`.

---

## 5. Concrete reproduction / attack scenarios

Only scenarios marked VERIFIED were executed. The rest are traced from source and are expected to reproduce on the current build; each must be confirmed by the assigned fixer.

| # | Scenario | Status | Expected result |
| --- | --- | --- | --- |
| R1 | Start server with `</dev/null` | **VERIFIED** | ~118% CPU idle (V1) |
| R2 | `./scripts/fac-smoke-test.sh` | **VERIFIED** (script content) | fails on missing `TestClient` project (V2) |
| R3 | Call `SessionAuth.ComputePasswordHash` once | **VERIFIED** | 1484 ms (V8) |
| R4 | `dotnet build SFSEnhanced.sln` in place | **VERIFIED** | access denied, root-owned `obj` (1.1) |
| R5 | B sends `WorldDownloadRequest` for A's private world | INSPECTED | full world JSON returned (V10) |
| R6 | B creates `Region` claim with `RadiusMeters=1e12`, A spawns a rocket and publishes state | INSPECTED | A gets `ClaimDenied` on their own build (V12) |
| R7 | B sends `WorldUploadChunk{TotalChunks=200000000, Base64Data="AAAA"}` | INSPECTED | server allocates a ~1.6 GB `string[]` and keeps it (V11) |
| R8 | A controls a build and disconnects; B requests control | INSPECTED | "Another player is already piloting this" forever (V13) |
| R9 | B sends `FriendRequestResponse{FromPlayerId="../../../../tmp/x"}` | INSPECTED | traversal-shaped path built in `FileStore.Load` (V9) |
| R10 | A connects to a rogue server at the intended address | INSPECTED | accepted, no fingerprint prompt (V5) |
| R11 | Flood `ClaimCreate`/`BuildSpawn` from one client | INSPECTED | unbounded whole-file rewrites, no throttle (V16) |
| R12 | Client sends `ClaimListRequest` | INSPECTED | `Error: Unhandled packet ClaimListRequest` (V4) |
| R13 | Mod connects to a server with `RegistrationPassword` set | INSPECTED | always rejected: no UI field, `registrationPassword: null` (V8) |

---

## 6. Protocol and data-migration risks

1. **No protocol version negotiation at the packet level.** `HelloPacket.ProtocolVersion` is a string (`"2.0"`) checked with exact equality; there is no minimum/maximum range and no capability list. Any future packet change is a hard flag day, and a `3.0` client gets "Unsupported protocol version" with no path to a friendly upgrade message.
2. **Sealed-packet version is a hard constant** (`SecureChannel.ProtocolVersion = 2`) with no fallback. A rolling upgrade of a self-hosted server will disconnect every client mid-session.
3. **No schema version in persisted records.** `WorldRecord` and `PlayerAccount` are raw Newtonsoft classes with public fields and no `Version` member; adding a field is tolerated, renaming or changing a type silently drops data. `BuildSnapshot.PartsBlueprintJson` embeds SFS `RocketSave` JSON that is versioned by the game, not by this project — a game update can make stored worlds unloadable with no detection.
4. **Token/identity migration is undefined.** `PlayerAccount.AuthTokenKey` is a SHA-256 of a bearer secret (V6). Any move to Steam or keypair auth has to migrate existing accounts with no way to re-issue tokens for offline players — they lose their identity. A migration plan is required before `Auth` becomes real.
5. **World IDs are the storage key** (`worlds/<WorldId>.json`) and appear in client-supplied packets. There is no ownership/version field guarding concurrent writes, and `HandleWorldUploadChunk` replaces builds/claims wholesale, so an upload can silently overwrite ownership metadata of anything the uploader could reference.
6. **`WorldJoinAck` carries the entire world in one packet.** `LidgrenWire.MaxPayloadBytes` is 8 MB; a world that exceeds it makes `LidgrenWire.Encode` throw inside `conn.SendAsync`, which is caught by the connection-level `catch`, so **joining a large world silently kills the connection**. Chunking exists for download but not for join, and no size is checked before sending.
7. **Hot-stream channel choice is a protocol-level decision** (`LidgrenWire.IsHotStream` → unreliable-sequenced). New state packets added to that set will silently lose data under loss, with no way for a client to detect it.
8. **No `Config` versioning**: `ServerConfig.Load` deserialises whatever JSON is present and silently uses defaults for missing fields (`AllowRegistration = true` is the default). A config file written before registration existed enables open registration on upgrade — a security-relevant default.

---

## 7. Acceptance criteria — first UI/profile vertical slice

The vision's first slice is "coherent in-game application shell/navigation + persistent account/profile foundations" without breaking connect/world/friends/chat. Given the code as it is, the following are the minimum testable criteria.

**Foundation (server + shared)**

- AC1. `PlayerAccount` gains profile fields with defaults and a migration path: display name, bio, avatar/colour id, privacy flags, created date, stats counters. Loading an existing `accounts/*.json` without these fields must succeed and persist unchanged data.
- AC2. New packets: `ProfileGet`, `ProfileUpdate`, `ProfileResponse` added to `PacketType` with a documented handler on the server and a sender/receiver in `Mod/`; no type is added to the enum without a handler (fixes V4 class of defect).
- AC3. Profile updates are authorized (a player can only write their own profile), length-limited, and rate-limited; the server strips control characters and rejects empty/oversized display names.
- AC4. `ProfileResponse` never returns `AuthTokenKey`, and no packet returns another player's private fields (privacy flags respected).
- AC5. Directory-listed profiles require an explicit privacy setting; default is "friends only" for bio/avatar, "public" for display name and stats.

**UI shell**

- AC6. The multiplayer flow opens as a single `ScreenManager` screen with a persistent header (connection state, player name) and a back action that always returns to the previous page, not to the Play menu. No panel opens two windows at once (currently `ShowFriendsPanel`/`ShowChatPanel` create separate windows and toggle `_screenHolder`).
- AC7. Panels have loading, empty and error states — the world list, server list, friends list and chat already need these today.
- AC8. The connect screen exposes: host, port, registration password (needed for V8), and shows the server identity fingerprint on first connect with an explicit accept/abort choice (needed for V5).
- AC9. Profile page reachable from the shell in ≤ 2 actions, showing display name, avatar, bio, friends count, published creations count (0 with an empty state, not a fake number).
- AC10. No button is present without a working handler; no `TODO`-backed control is rendered (`Mod/Social/FriendsUI.cs` comments must be removed or the code moved).
- AC11. The whole flow is usable at the SFS play screen's real resolution (test at 1280×720 and 1920×1080 in-game) without clipped controls or overlapping windows.

**Verification for the slice**

- AC12. `dotnet build SFSEnhanced.sln` passes in place on a clean checkout (requires fixing the `obj` ownership blocker).
- AC13. `dotnet build Auth/SFSEnhanced.Auth.csproj` and `dotnet build scripts/multiplayer-integration/MultiplayerIntegration.csproj` are added to the solution/CI.
- AC14. The integration harness passes with `MULTI_CLIENT_INTEGRATION_PASSED` **on both** `ubuntu-latest` and `windows-latest`, with the harness's frame-loss bug fixed.
- AC15. A new xunit suite covers profile validation, privacy enforcement and the migration of an existing account record; it runs in CI.
- AC16. Manual in-game check recorded: connect → open profile → edit → reconnect → profile persists; connect to a server with `RegistrationPassword` succeeds.

---

## 8. Prioritised bug list

Severity: **S1** = release blocker / host or account compromise; **S2** = serious exploit or persistent corruption; **S3** = significant defect; **S4** = hygiene.

> Re-verify rows citing files changed after the audit baseline (§0) before assigning: `NetClient.cs`, `NetServer.cs`, `FriendsService.cs`, `WorldModels.cs`, `PacketType.cs`, `Packets.cs`, and the deleted `MultiplayerMenu.Packets.cs` / `MultiplayerMenu.Panels.cs`.

| ID | Sev | Area | Summary | File(s) | Status |
| --- | --- | --- | --- | --- | --- |
| Q-01 | S1 | Ops | Idle server burns ~118% CPU with stdin closed | `Server/Program.cs` (`ConsoleCommandLoop`) | VERIFIED |
| Q-02 | S1 | CI | Smoke test builds a non-existent `TestClient` project; CI job can never pass | `scripts/fac-smoke-test.sh`, `.ps1`, `.github/workflows/server-cross-platform.yml` | VERIFIED |
| Q-03 | S1 | Auth | Stored token "hash" is the HMAC key → account-file read = auth forgery for all accounts | `Server/Persistence/AccountService.cs` | INSPECTED |
| Q-04 | S1 | Auth | Registration-password path costs 1.5 s CPU per unauthenticated handshake; no pre-auth caps | `Shared/Protocol/SecureChannel.cs`, `Server/Networking/SecureSessionHandshake.cs` | VERIFIED cost |
| Q-05 | S1 | Auth | Client never verifies server identity; fingerprint ignored; docs claim pinning | `Shared/Protocol/SecureHandshake.cs`, `Mod/Networking/NetClient.cs`, `README.md` | INSPECTED |
| Q-06 | S2 | Auth | Long-lived bearer token in plaintext `PlayerPrefs`, no rotation/revocation | `Mod/ModSettings.cs` | INSPECTED |
| Q-07 | S2 | Authz | `WorldDownloadRequest` has no privacy/ownership check (private world exfiltration) | `Server/Networking/NetServer.Worlds.cs` | INSPECTED |
| Q-08 | S2 | Resource | Upload path allocates `new string[TotalChunks]` from a client value; no quotas anywhere | `Server/Networking/NetServer.Worlds.cs`, `Server/World/WorldManager.cs` | INSPECTED |
| Q-09 | S2 | Authz | Region claims are unbounded → any player can lock other players' builds world-wide | `Server/Social/ClaimsService.cs`, `NetServer.Social.cs` | INSPECTED |
| Q-10 | S2 | Filesystem | Client-supplied ids reach `FileStore` paths without validation (traversal read/write) | `NetServer.Social.cs`, `Server/Persistence/FileStore.cs` | INSPECTED |
| Q-11 | S2 | Content | Untrusted `PartsBlueprintJson` deserialized and spawned in every client | `NetServer.Worlds.cs`, `Mod/World/MultiBuildManager.cs` | INSPECTED |
| Q-12 | S2 | Concurrency | Account/world records mutated and re-saved without locks → lost updates and list corruption | `Server/Social/FriendsService.cs`, `Server/World/WorldManager.cs` | INSPECTED |
| Q-13 | S3 | Gameplay | `ControllingPlayerId` never released on leave/disconnect → permanently unclaimable builds | `Server/Networking/NetServer.Worlds.cs` | INSPECTED |
| Q-14 | S3 | Anti-cheat | Only NaN/Inf checked; teleport/velocity values accepted and relayed | `Server/Networking/NetServer.cs` | INSPECTED |
| Q-15 | S3 | Abuse | No rate limits on chat, claims, spawns, world creation, friend requests, handshakes | server-wide | INSPECTED |
| Q-16 | S3 | Protocol | Enum types with no handler (`ClaimList*`) and packets never sent (`TimeWarpState`, `PlayerPresence`) | `Shared/Protocol/PacketType.cs`, `Mod/UI/MultiplayerMenu.Packets.cs` | VERIFIED by search |
| Q-17 | S3 | Protocol | World join sends the whole world in one ≤8 MB packet; oversized worlds kill the connection | `NetServer.Worlds.cs`, `Shared/Protocol/LidgrenWire.cs` | INSPECTED |
| Q-18 | S3 | Mod | Cannot connect to servers requiring a registration password (no UI, `null` passed) | `Mod/Networking/NetClient.cs`, `Mod/UI/MultiplayerMenu.cs` | INSPECTED |
| Q-19 | S3 | Mod | Connect/disconnect/reconnect races dispose shared state; `async void` connect | `Mod/Networking/NetClient.cs`, `Mod/ModMain.cs` | INSPECTED |
| Q-20 | S3 | Directory | Unauthenticated, unbounded in-memory registration; no eviction; non-fixed-time token compare | `Directory/Program.cs` | INSPECTED |
| Q-21 | S3 | Auth svc | Steam service unwired, not in solution, API key in query string, no rate limit | `Auth/*`, `SFSEnhanced.sln` | VERIFIED (not in sln) |
| Q-22 | S3 | Docs | README/docs claim TLS, pinning, TCP and unimplemented features | `README.md`, `docs/ARCHITECTURE.md`, `agent.md` | VERIFIED |
| Q-23 | S3 | Test | Integration harness can drop handshake frames; no framework; no negative coverage | `scripts/multiplayer-integration/Program.cs` | INSPECTED |
| Q-24 | S4 | Build | Solution omits `Auth` and the integration project; `Lib.Harmony` referenced but unused | `SFSEnhanced.sln`, `Mod/SFSEnhanced.Mod.csproj` | VERIFIED |
| Q-25 | S4 | Build | `Shared/obj/Release` and integration `bin/obj` are root-owned → in-place build blocked | repo filesystem | VERIFIED |
| Q-26 | S4 | Ops | Identity fingerprint never printed (cannot be pinned manually) | `Server/Program.cs` | VERIFIED |
| Q-27 | S4 | Perf | `WorldTimeState` broadcast every 250 ms to every world; `AccountService.FindByName` scans all files; 3 arrays allocated per sealed packet | `NetServer.cs`, `AccountService.cs`, `SecureChannel.cs` | INSPECTED |
| Q-28 | S4 | Docs | `docs/` missing required artefacts (`BUGS.md`, `QA_PLAN.md`, `DESIGN_SYSTEM.md`, `UX_FLOWS.md`, `IMPLEMENTATION_PLAN.md`, `AGENT_ROLES.md`) | `docs/` | VERIFIED |
| Q-29 | S4 | Privacy | Every connect without a port leaks the server hostname to Cloudflare DoH | `Mod/Networking/ServerEndpointResolver.cs` | INSPECTED |
| Q-30 | S4 | UX | Panels create independent windows instead of replacing the front page (contradicts `README.md` UX section); duplicated `HideXPanel()` calls | `Mod/UI/MultiplayerMenu.Panels.cs` | INSPECTED |

---

## 9. What QA needs from the manager

1. **Environment:** clear the root-owned `obj`/`bin` directories (Q-25) so in-place builds are possible, and have the host keep one server running long enough to run the harness. Until then no build/test claim is trustworthy.
2. **Assign Q-01, Q-02, Q-04 first** — they are cheap, verified, and currently make the documented workflow (build → run server → smoke test) impossible or expensive.
3. **Freeze new `Mod/UI` work** until Q-05/Q-18 are resolved, because the first UI slice (section 7) must expose fingerprint confirmation and the registration password; building the shell without them creates rework.
4. **Require a test project** and add it, plus `Auth` and the integration project, to `SFSEnhanced.sln` (Q-24) before accepting the first vertical slice.
5. **Create `BUGS.md` and `QA_PLAN.md`**, seeded from section 8, so bugs have owners and regression tests (vision items 7 and 2).
6. **Have the implementer fix `README.md`/`docs/ARCHITECTURE.md` security claims** (Q-22); shipping documentation that overstates transport security is a product-risk issue, not a wording preference.

**Not fixed by this reviewer, by instruction.** No source file, project file, script or documentation file other than this report was changed.
