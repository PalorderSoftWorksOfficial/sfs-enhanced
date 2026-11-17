# TEAM_REPORT_PLANNER.md — Product Planner / Systems Architect Audit

**Author role:** Product planner / systems architect (read-only audit; no source or shared doc was modified).
**Date:** 2026-11-14
**Repository:** `/home/administrator/sfs-enhanced` @ `main`
**Audience:** Manager (primary Freebuff model), then UX, implementer, QA, security reviewer.

---

## 0. Method and confidence

Read directly: `PRODUCT_VISION.md`, `agent.md`, `README.md`, `docs/ARCHITECTURE.md`, `docs/ROADMAP.md`, `docs/COMMUNITY_PRIORITY.md`, `docs/FEATURE_RESEARCH.md`, `docs/STEAM_AUTH_NEXT.md`, `docs/SECURE_STEAM_AUTH.md`, `docs/SETUP_MOD_SIDE.md`, all of `Shared/**`, all of `Server/**`, `Directory/Program.cs`, `Auth/**`, all of `Mod/**` (networking, world, UI, social, packaging, integrated), `scripts/multiplayer-integration/Program.cs`, `scripts/fac-smoke-test.sh`, all `.csproj`, `SFSEnhanced.sln`, `.github/workflows/*`, and the committed sample data under `worlds/`.

Every capability claim below is tied to a file path. Where a document claims something the code does not do, I say so explicitly (section 12). I did not run the build, the integration harness, or the game — so all statements are static-code findings, not test results. Nothing here was verified at runtime.

**Concurrent-work caveat (read this before acting on any "MISSING" row).** The working tree changed while this audit was in progress. `git status` shows uncommitted work from another specialist: new `Server/Social/ProfileService.cs`, new `Server/Networking/NetServer.Profile.cs`, new `Shared/Models/ProfilePrivacy` / profile fields on `PlayerAccount`, new packets `ProfileRequest/Response/Update/UpdateResult` (ids 80–83), client plumbing in `Mod/Networking/NetClient.cs`, and a new uncommitted UI layer at `Mod/UI/App/` (`AppState.cs`, `IAppActions.cs`, `AppPage.cs`) and `Mod/UI/Kit/` (`UiKit.cs`, `Theme.cs`) that replaces the deleted `Mod/UI/MultiplayerMenu.Panels.cs` and `MultiplayerMenu.Packets.cs`. I read `ProfileService.cs` and `NetServer.Profile.cs` and diffed the model/protocol changes; I did **not** review the new UI layer. Where that work overlaps a finding below, the finding is marked **[superseded by uncommitted work]** and describes the committed baseline. The manager must re-verify those rows against the final merge before planning from them.

---

## 1. Executive summary

The multiplayer **transport, session security, world/build state, claims, friends, chat and time-warp arbitration are real, implemented, and covered by a genuine two-client integration harness**. That is a stronger foundation than the handoff documents imply, and it must not be rebuilt.

Everything the product vision calls a *platform* — global identity, discoverable rooms, a creator library, moderation — **does not exist in any form**. There is no `Room` type anywhere in `Server/` or `Shared/` (verified: zero matches for `room`), no published-creation entity or endpoint (zero matches outside `Assembly-CSharp/`'s built-in SFS sharing), and no roles/bans/mutes/reports/audit (zero matches in `Server/`). A **server-local profile slice does now exist as uncommitted work** (display name, bio, accent colour, two privacy flags, stats counters, rate-limited updates — `Server/Social/ProfileService.cs`, `Server/Networking/NetServer.Profile.cs`, packets 80–83); it is per-server, not global, and it was written after this audit's model reads, so treat §2.2 as the committed baseline plus that slice.

Three structural problems dominate the plan:

1. **Identity is scoped to one server, not one account.** `PlayerAccount` lives in that server's `data/accounts/` (`Shared/Models/WorldModels.cs`, `Server/Persistence/AccountService.cs`). The client stores exactly one token in one `PlayerPrefs` key (`Mod/ModSettings.cs`), so joining a second server overwrites the first server's credential (`Mod/Networking/NetClient.cs` writes `ModSettings.AuthToken`). "Persistent account identity independent of the server currently joined" (`PRODUCT_VISION.md`) is **not achievable without a new identity service**, and the current client bug actively destroys tokens.
2. **World == room.** `WorldRecord` is simultaneously the save file, the joinable space, and the discovery listing (`Shared/Models/WorldModels.cs`, `Server/World/WorldManager.cs`). The vision's four distinct concepts (server, room, world, published creation) collapse into one record, which is why there is no room metadata, no capacity, no roles, no lifecycle, and no way for a published creation to *not* be a live room.
3. **Persistence cannot carry the platform.** One JSON file per record, one global lock, whole-file rewrite on hot paths, no schema version on any record (`Server/Persistence/FileStore.cs`, `Server/World/WorldManager.cs`). Measured from code: ~1–2 full world-document disk writes per second **per player**, each containing the full rocket JSON.

The good news is that the migration surface is small and well-bounded: the wire protocol is versioned (`HelloPacket.ProtocolVersion2`), JSON payloads are additive-friendly, and `FileStore` is already isolated behind `Save/Load/ListIds` so `WorldManager`/`AccountService` do not care what stores them.

---

## 1.5 Key architecture decisions (the ones that are hard to reverse)

These are the decisions the rest of the plan depends on; detail and evidence follow in the sections named.

| # | Decision | Why it is load-bearing | Detail |
| --- | --- | --- | --- |
| D1 | **Keep the game server authoritative for game state; add a separate platform control plane** for identity, profiles, rooms registry and creations. The two talk over HTTPS, never over Lidgren. | Global identity and a searchable catalogue cannot live in a per-server JSON folder, and putting them in the game server would hold gameplay hostage to a central service. Preserves `docs/ROADMAP.md`'s self-hostability constraint. | §3 |
| D2 | **`PlayerId` stays per-server.** Bind it upward (`PlayerAccount.PlatformAccountId`) instead of making it global. | Every authorization check in `Server/Networking/*` and `Server/Social/*` already keys on `PlayerId`. Making it global invalidates all of them at once. | §4.1 |
| D3 | **Split the four concepts: Server → Room → World → Creation.** A Room *references* one World on one Server; a World is never discoverable on its own; a Creation is an immutable published snapshot that never implies a live room. | Today one `WorldRecord` plays all four roles, which is exactly why capacity, roles, lifecycle, tags and visibility do not exist. This split is the prerequisite for both rooms and the workshop. | §5 |
| D4 | **Platform is optional.** `PlatformUrl` empty ⇒ behaviour identical to today (local accounts, token auth, directory only). | Standalone mode is the regression baseline and the CI guarantee; it also keeps community servers viable if the platform is down or unfunded. | §3, §6.3 |
| D5 | **Metadata over HTTP, gameplay over the protocol.** Do not stream room catalogues or profiles through Lidgren. | Sidesteps the 8 MB payload ceiling, the shared per-channel sequence space, and the reliable-ordered hot path. | §7.1 |
| D6 | **Schema-version every persisted record before the first platform write,** with a forward upgrader and legacy field aliases. | Records already diverge (`AuthTokenHash` on disk vs `AuthTokenKey` in the model). A second silent rename without versions will strand live accounts. | §7.2 |
| D7 | **Separate live state from durable state.** Positions stay in memory; builds/claims persist on change with debounce, never on the 1 Hz `BuildSpawn` stream. | Whole-file rewrite on the hot path is the single most likely cause of instability misdiagnosed as a networking bug. This is a hard prerequisite for rooms and uploads. | §7.2 |
| D8 | **Uploads are validated, capped and non-path-derived.** Bounded chunk counts, cumulative size cap, content-type check, no partial artefacts on failure. | The current upload handler allocates from an attacker-controlled chunk count and has no cap; the packaging code (`Shared/Packaging/PackageInstaller.cs`) already demonstrates the traversal-rejection posture to reuse. | §2.3, §10 |

---

## 1.6 Priority risks (ranked)

Ordered by "probability × damage if left unfixed", not by effort. Each is evidenced in the sections named.

| # | Risk | Impact if ignored | Evidence | Fix gate |
| --- | --- | --- | --- | --- |
| R1 | **Single global client token slot.** One `PlayerPrefs` key; every token issue overwrites it. | Using a second server silently locks the player out of the first. Destroys the credibility of any "persistent identity" work that ships on top of it. | `Mod/ModSettings.cs`, `Mod/Networking/NetClient.cs` | Before any identity/profile slice is called done |
| R2 | **Persistence contention on hot paths.** ~1–2 full world-document writes/second/player, each containing full rocket JSON, behind one global lock. | Players, disk and latency degrade as concurrency rises; room and upload traffic will amplify it. Will be blamed on the network. | `NetServer.Worlds.cs` `HandleBuildSpawn` → `WorldManager.AddOrUpdateBuild` → `Persist`; `NetServer.AutosaveLoopAsync` | Before rooms (Phase 3) and before creations (Phase 4) |
| R3 | **Unauthorized world download + unbounded upload allocation.** Any connected player can download any world by id; `new string[chunk.TotalChunks]` is attacker-controlled with no size cap. | Private builds leak; a single client can OOM the server. Both become critical the moment a UI exposes them. | `NetServer.Worlds.cs` `HandleWorldDownloadRequest`, `HandleWorldUploadChunk` | Before upload/download is exposed in UI |
| R4 | **No schema version on any record, with one rename already in the wild.** | Silent account/world loss on the next model change; unrecoverable without forensics. | `Shared/Models/WorldModels.cs`, `worlds/accounts/*.json` | Phase 0 |
| R5 | **Identity verification is unproven.** Room and creation ownership, moderation, and ratings all rest on "who is this account". Today it is a per-server opaque token with no rotation, no revocation, and a Steam path that is unreachable skeleton code. | Every social and UGC feature inherits weak attribution; moderation cannot act on a durable identity. | `Server/Persistence/AccountService.cs`, `Auth/*` (not in `SFSEnhanced.sln`), `docs/SECURE_STEAM_AUTH.md` | Phase 2 |
| R6 | **Verification baseline is partly broken or absent.** No unit tests; solution doesn't include the integration harness; the CI smoke step builds a `TestClient` project that does not exist. | Regressions land silently and arguments about "is it broken" replace measurement. | `scripts/fac-smoke-test.sh`, `.github/workflows/*`, `SFSEnhanced.sln` | Phase 0, first |
| R7 | **Directory is unverified and non-durable.** In-memory state, no registration proof, no rate limits, non-constant-time token compare, no fingerprint field. | Spoofed/duplicate listings, blank list after restart, trivial spam — on a service meant to be the public front door. | `Directory/Program.cs`, `Server/Networking/ServerDirectoryPublisher.cs` | Phase 5 (harden earlier if it goes public) |
| R8 | **Concurrent uncommitted work overlapping this report.** Profile slice + new UI layer landed mid-audit; UI files deleted/replaced. | Two agents editing the same protocol/UI surface; findings and reviews go stale mid-flight. | `git status`, §0 caveat | Manager assigns file ownership before next slice |
| R9 | **Doc truth drift.** `README.md` advertises TLS, pinning and fingerprint publishing that do not exist; `agent.md`'s "no Harmony" is contradicted by the QoL patches. | Reviewers and new agents build on false constraints; security posture is overstated to users. | §12 | Each slice reconciles the docs it invalidates |
| R10 | **Claim trust list is a dead field.** `CanInteract` honours `TrustedPlayerIds`, nothing ever populates it. | "Share with friends" silently does not work, and looks like a permissions bug later. | `Server/Social/ClaimsService.cs`, `Shared/Protocol/Packets.cs` | Phase 3 or earlier, cheap |

---

## 2. Verified capabilities

Legend: **OK** = implemented and reachable; **PARTIAL** = code exists but is incomplete, unreachable from the client, or unsafe; **MISSING** = no implementation.

### 2.1 Transport and session security — mostly OK

| Capability | Status | Evidence |
| --- | --- | --- |
| Lidgren UDP transport, no Unity dependency in server | OK | `Server/Networking/LidgrenServerTransport.cs`; `Server/SFSEnhanced.Server.csproj` (net8.0, no `Assembly-CSharp` reference) |
| Reliable-ordered vs unreliable-sequenced channels | OK | `Shared/Protocol/LidgrenWire.cs` (`IsHotStream`, `GetChannel`); client send `Mod/Networking/NetClient.cs` |
| Ephemeral P256 ECDH + static-key ECDH, transcript-bound | OK | `Shared/Protocol/P256.cs`, `SecureChannel.cs` (`ComputePreMaster`, `ComputeTranscriptHash`), `SecureSessionHandshake.cs` |
| HMAC token proof; token never on the wire; server stores SHA-256 key only | OK | `AccountService.ValidateProof` / `TokenKey`; `AuthResponsePacket.Proof` |
| AES-CTR + HMAC-SHA256 sealed frames, per-channel sequence, 1024-entry replay window | OK | `SecureChannel.Seal` / `TryOpen` / `IsReplay` |
| Tamper and replay rejection | OK | 16-byte truncated tag, fixed-time compare; integration probes in `scripts/multiplayer-integration/Program.cs` |
| Bounded handshake (12 s) and abuse cutoff (32 bad packets) | OK | `SecureSessionHandshake.HandshakeTimeout`; `ClientConnection.ReceiveSecureAsync` |
| TLS transport | **MISSING** | No `SslStream`/`X509`/TLS anywhere. `README.md` claims TLS is implemented — false (see §12) |
| Certificate fingerprint pinning / first-connection trust | **MISSING** | `Shared/Models/ServerDirectoryModels.cs` has no fingerprint field; client never verifies `AuthChallengePacket.IdentityFingerprint` |
| Password-protected servers | **MISSING** | `ServerConfig.PasswordProtected` is only *advertised* (`ServerDirectoryPublisher.cs`) and never enforced in `SecureSessionHandshake` |
| Rate limiting (chat/friends/claims/uploads) | **MISSING** | No counters or throttles in `NetServer*.cs` |

### 2.2 Identity, accounts, profiles

| Capability | Status | Evidence |
| --- | --- | --- |
| Per-server account, opaque token, stable `PlayerId` | OK | `PlayerAccount` in `Shared/Models/WorldModels.cs`; `AccountService.CreateAccount` (24 random bytes → SHA-256 stored) |
| Registration / token modes, name uniqueness (case-insensitive) | OK | `HelloPacket.ModeToken` / `ModeRegister`; `SecureSessionHandshake.AuthenticateAsync` |
| Auto-registration in token mode for unknown names | OK | `SecureSessionHandshake` `else if (existing == null)` branch returns the issued token as the sealed first response |
| Server-wide registration password | PARTIAL | `ServerConfig.RegistrationPassword` + `SessionAuth.ComputePasswordHash` — hand-rolled 600k-round iterated SHA-256 over a **fixed salt**, not PBKDF2/Argon2 (`SecureChannel.cs:31`) |
| Global account identity across servers | **MISSING** | Accounts are files inside one server's `DataDir` (`Server/Program.cs`, `FileStore`) |
| Per-server credential storage on the client | **BROKEN** | single `PlayerPrefs` key `sfs_enhanced.token` (`ModSettings.cs`); overwritten on every token issue (`NetClient.cs`) |
| Profile: display name, bio, accent colour, privacy flags, counters | **PARTIAL — [superseded by uncommitted work]** | Committed baseline had only `PlayerName`, `FirstSeenUtc`, `LastSeenUtc`. Uncommitted: `Server/Social/ProfileService.cs` (validated `DisplayName` 32 / `Bio` 280, `#RRGGBB` accent, `ShowPresence`, `AllowFriendRequests`, `WorldsOwned`, `FriendsCount`, `SessionsCount`), `NetServer.Profile.cs` with a 5-burst / 1-per-second token bucket. Still **per-server**, no global account, no avatar image, no activity feed |
| Friends list / requests, persisted per account | OK | `Server/Social/FriendsService.cs`; `FriendPlayerIds`, `Incoming/OutgoingFriendRequests` |
| Presence (online + current world) | PARTIAL | In-memory only, dies on restart (`FriendsService._online`, `_currentWorld`); no presence packet is ever broadcast — see next row |
| `PlayerConnected` / `PlayerDisconnected` broadcast | **MISSING (declared only)** | `PacketType.cs:43-44` exist; **no server code sends them**; client handlers are dead branches (`MultiplayerMenu.Packets.cs`, `MultiBuildManager.cs`) |
| Privacy settings | **PARTIAL — [superseded]** | Only the two flags above (`ProfilePrivacy` in `Shared/Models/WorldModels.cs`); no visibility levels, no per-field control |
| Blocking, reporting | **MISSING** | No types, packets, or endpoints |
| Steam identity | **SKELETON** | `Auth/Program.cs` + `Auth/SteamTicketVerifier.cs` call `AuthenticateUserTicket` and return a bare `SteamId`. Not in `SFSEnhanced.sln`, no client caller, no session credential, no server-side consumer. Client-side ticket source *is* feasible: `Assembly-CSharp/SteamManager.cs`, `Assembly-CSharp/PlatformUtilities.cs` |

### 2.3 Worlds / multiplayer simulation — the strongest area

| Capability | Status | Evidence |
| --- | --- | --- |
| Persistent world with owner, public flag, planet-pack id | OK | `WorldRecord`; `WorldManager.Create` |
| Multi-build world (many independently owned builds) | OK | `WorldRecord.Builds`, `BuildSnapshot`, `AddOrUpdateBuild` |
| World create / list public / join / leave | OK | `NetServer.Worlds.cs` (`HandleWorldCreate`, `Join`, `Leave`) |
| Private-world access control (owner / friends / whitelist) | OK | `HandleWorldJoin` predicate |
| World upload (chunked base64) and download (chunked) | PARTIAL | `HandleWorldUploadChunk`, `HandleWorldDownloadRequest` — **no client UI calls either**; download performs **no visibility check** (any connected player can download any world by id); upload has **no size cap** and `new string[chunk.TotalChunks]` is attacker-controlled |
| Build spawn / remove / state update with numeric validation | OK | `HandleBuildSpawn`, `HandleBuildRemove`, `IsFinite` guards |
| Owner/controller authorization for state updates | OK | `build.OwnerPlayerId != playerId && build.ControllingPlayerId != playerId` |
| Build control arbitration | PARTIAL | `HandleBuildControlRequest` implemented; **no client UI** sends `BuildControlRequest` |
| Claims (per-build, circular region) | OK | `Server/Social/ClaimsService.cs`; `CanInteract` enforced on spawn/state/remove |
| Claim trust list (trusted friends) | **DEAD FIELD** | `ClaimInfo.TrustedPlayerIds` is read by `CanInteract` but **nothing ever populates it** — no packet carries it and `ClaimsService.Create` never sets it |
| Server-authoritative world clock | OK | `NetServer.AdvanceWorldTime`, 250 ms `WorldTimeLoopAsync`, client correction in `MultiBuildManager.TickInterpolation` |
| Time-warp vote (all-players, 15 s expiry, proximity lock) | OK | `NetServer.TimeWarp.cs` (`IsAnyPlayerNearby`, 50 km radius) |
| Ghost rockets (non-simulated, colliders off, interpolated) | OK | `Mod/World/RemoteRocketGhost.cs`; `MultiBuildManager.ApplyTransform` |
| Local fleet sync (multiple local vessels published) | OK | `Mod/World/LocalRocketFleetSync.cs` |
| Part module sync (parachute state, booster fuel) | PARTIAL | `Mod/World/PartModuleSync.cs` — only two module types |
| Docking / joint builds / debris | **MISSING** | No joint, docking, or debris concept in `BuildSnapshot` or packets |
| Full physics authority / anti-cheat on physics | **MISSING (by design)** | Documented trade-off in `docs/ARCHITECTURE.md`; only NaN/Infinity guards |

### 2.4 Rooms

**MISSING entirely.** Zero occurrences of `room` in `Server/` and `Shared/`. There is no room record, no visibility enum (public/private/invite-only/unlisted), no description, tags, category, thumbnail, capacity, ownership roles, invites as room objects, lifecycle, moderation, or reconnect semantics. `WorldRecord.IsPublic` + `Whitelist` is the whole access model.

### 2.5 Discovery, directory, server browser

| Capability | Status | Evidence |
| --- | --- | --- |
| Self-hostable directory with register/heartbeat/unregister/list | OK | `Directory/Program.cs` (minimal API, 45 s cutoff, 20 s publish interval in `ServerDirectoryPublisher.cs`) |
| Server-side heartbeat token | OK | 32 random bytes hex, compared with `!=` (not constant-time) |
| Directory persistence | **MISSING** | `ConcurrentDictionary` in memory — directory restart erases all registrations |
| Search / region filter / sort beyond player count / pagination | **MISSING** | `GET /api/v1/servers` returns everything sorted by `OnlinePlayers` then `Name` |
| Ping / latency / country / capacity filter / favorites / MOTD rendering | **MISSING** | `ServerListing` carries `Region` and counts only; no latency measurement; favorites do not exist |
| Verification / moderation of listings | **MISSING** | Any client can register any `Host`/`Port` with no proof of ownership |
| Client browser UI | PARTIAL | `Mod/UI/MultiplayerMenu.Panels.cs` `LoadPublicServersAsync` — hard cap of **7** servers, no states beyond one label |
| SRV discovery | PARTIAL | `Mod/Networking/ServerEndpointResolver.cs` uses **Cloudflare DoH over HTTPS** as the resolver, hard-coded; silently falls back to port 7777 on any failure |
| TLS to directory | **MISSING** | `server.example.json` uses `http://127.0.0.1:8787`; publisher has no TLS requirement or pinning |

### 2.6 Creator library / Workshop

**MISSING in SFS Enhanced.** No creation entity, no publish/list/download endpoint, no asset-type distinction, no versioning, no ratings/favorites/download counts, no reporting. `PartsBlueprintJson` exists only as a field inside a live `BuildSnapshot`.

Relevant prior art *inside the game* that the plan must account for: SFS ships its own blueprint sharing client — `Assembly-CSharp/SFS.Sharing/SharingRequester.cs` (`UploadRocket`, `UploadRocketLinked`, `GetRocket`, `IsModerator`, `ServerPageSize = 45`) against `https://sharing.spaceflightsimulator.app`, plus `Assembly-CSharp/SFS.UI/DownloadMenu.cs` and `Assembly-CSharp/SFS.Builds/Blueprint_Saving.cs`. The in-game save format for the build editor is **`Blueprint`**, whereas SFS Enhanced syncs **`RocketSave`** (`MultiBuildManager.SnapshotFromRocket` uses `JsonWrapper.ToJson(new RocketSave(rocket))`). A creator library must decide which asset format each type uses; they are not the same type.

### 2.7 Chat / social UI

| Capability | Status | Evidence |
| --- | --- | --- |
| Server-wide and per-world chat with server-set sender and 512-char cap | OK | `NetServer.Social.cs` `HandleChat` |
| Chat UI with 50-line log | PARTIAL | `MultiplayerMenu.Panels.cs` — in-memory only, cleared on close |
| Chat history / persistence / mute / report | **MISSING** | Nothing persisted; no moderation verbs |
| Friends UI | PARTIAL | `MultiplayerMenu.RenderFriendsList` (real) plus a duplicated, TODO-laden `Mod/Social/FriendsUI.cs` |
| Invites into a world | PARTIAL | `HandleFriendInvite` + `_pendingInviteWorldId`; invite targets a **world id**, not a room; expires on logout |
| Notifications | **MISSING** | Only a single status label (`_status`) |

### 2.8 In-game UI / application shell

**[superseded by uncommitted work]** — `Mod/UI/MultiplayerMenu.Panels.cs` and `MultiplayerMenu.Packets.cs` are deleted in the working tree and replaced by `Mod/UI/App/` + `Mod/UI/Kit/`, which I did not review. The baseline (committed) implementation is described here and remains the honest picture of what shipped.

Real, but not a platform shell. `Mod/UI/MultiplayerMenu.cs` builds one 900×620 window with 8 absolute-positioned buttons and 5 inputs; sub-panels are 520–560 px windows that hide each other via `_screenHolder.SetActive(false)` (`MultiplayerMenu.Panels.cs`). There is no navigation stack, no reusable card/list component, no pagination or scroll for lists (hard cap of 7 rows), no loading/empty/error states beyond one label, no localization (SFS has `SFS/SFS_Translation.cs`, unused), no settings screen (`ModSettings` is raw `PlayerPrefs`), and no profile or account screen.

`EnsurePlayButton` (`MultiplayerMenu.cs`) do**es** implement the vision's "first-class Multiplayer entry in the Play menu" by cloning `WorldsMenu.worldButtons[0]` and relabelling it `MULTIPLAYER` — verified, and it is retried for 5 s after the home scene loads (`ModMain.ModLoop`).

### 2.9 Testing and CI

| Item | Status | Evidence |
| --- | --- | --- |
| Two-client end-to-end harness over real transport + secure channel | **OK and substantial** | `scripts/multiplayer-integration/Program.cs`: token issue, wrong-token rejection, reconnect identity, world create/list/join, join broadcast, spawn, primary/secondary/part-module state, claim create/remove, friend request/accept, chat, time-warp vote + result, world time, replay probe (exactly 1 accepted), tamper probe (rejected), leave + player-count |
| Harness in the solution | **MISSING** | `SFSEnhanced.sln` contains only Shared, Server, Mod, Directory, BuildSfsPack |
| Unit tests of any kind | **MISSING** | No test project, no framework reference |
| CI builds the solution | **MISSING** | `server-cross-platform.yml` builds Server only; `mod-build.yml` builds Mod only |
| CI smoke test runnable | **BROKEN** | `scripts/fac-smoke-test.sh` runs `dotnet build TestClient/SFSEnhanced.TestClient.csproj`; `TestClient/` contains only `bin/` and `obj/` — **no project or source exists**, so the `Smoke test Linux/Windows` steps in `server-cross-platform.yml` cannot pass |
| Auth service built by CI | **MISSING** | `Auth/` is not referenced by any workflow |

---

## 3. Recommended platform architecture

Principle: **keep the authoritative game server exactly as it is, and add a separate platform control plane beside it.** The dedicated server must remain self-hostable and fully functional with zero central infrastructure (`docs/ROADMAP.md` "Technical direction" already demands this).

```
                    ┌──────────────────────────────────────────┐
   SFS + Mod  ─────▶│ PLATFORM API (new, HTTP/TLS, accounts)   │
   (in-game)        │ identity · profiles · rooms registry ·   │
                    │ creations · social · moderation          │
                    └───────────────┬──────────────────────────┘
                                    │ server-issued access token
                                    ▼
   SFS + Mod  ─────▶┌──────────────────────────────────────────┐
   (Lidgren UDP,    │ DEDICATED GAME SERVER (exists today)     │
    session-secured)│ worlds · builds · claims · chat · time   │
                    │ verifies platform token, owns game truth │
                    └──────────────────────────────────────────┘
```

Four planes, four different lifetimes:

| Plane | Owner | Lifetime | Persistence today |
| --- | --- | --- | --- |
| **Identity** | Platform API | permanent, global | none (accounts are per-server files) |
| **Room registry** | Platform API | permanent metadata, ephemeral runtime | none |
| **Game session (server)** | Dedicated server | process lifetime | JSON files |
| **World / save state** | Dedicated server | permanent, per world | JSON files |
| **Published creation** | Platform API | permanent, immutable versions | none |

**Why a separate control plane is unavoidable.** Global account identity, cross-server profiles, followers, ratings, and a searchable creation catalogue cannot live in a per-server JSON folder. Any design that tries (e.g. "each server syncs accounts") creates a distributed-consensus problem for a mod team and breaks self-hosting. The control plane must be *optional*: a server with `PlatformUrl` unset behaves exactly as today (local accounts, token auth), and the client degrades to server-local identity.

**Why the game server must not become the platform.** Physics-adjacent state must stay low-latency and server-local. Putting profiles/creations in the game server would force every server to hold global state it cannot validate, and would make the creator library hostage to whichever server is online.

**Client-side consequence (important for UX):** the mod needs an explicit *connection context* — "signed in as X via platform" vs "guest on this server as X" — because today these are indistinguishable and the token is a single global slot. This is a UX and data-model change, not a reskin.

**Concrete component plan** (new, additive; existing files stay):

| New project | Responsibility | Depends on |
| --- | --- | --- |
| `Platform/` (ASP.NET Core, net8.0) | identity, profiles, rooms registry, creations, social graph, moderation, moderation queue | `Shared/Models/*.cs` (new platform models) |
| `Platform.Client` (netstandard2.0, referenced by `Mod` and `Shared`) | typed HTTP client for platform API | nothing Unity-specific |
| `Server/Persistence/` extended | `IWorldStore`, `IAccountStore`, `ICreationStore` interfaces; JSON impl today, SQLite impl later | — |
| `Shared/Protocol/` extended | protocol version negotiation, packet family for room metadata delta | — |

---

## 4. Identity / account / profile model

### 4.1 The three identities that must be distinguished

1. **Platform account** (`AccountId`, global, permanent) — created by the platform API. Owns profiles, creations, friends, rooms. *New.*
2. **Game identity** (`PlayerId`, per dedicated server) — what `PlayerAccount` is today, and what every authorization check in `NetServer`/`ClaimsService` already uses. **Do not change its meaning**; instead bind it: `PlayerAccount` gains `PlatformAccountId` and `VerifiedAt`.
3. **Session** (ephemeral, per connection) — `SecureChannel` session id, already exists.

Keeping `PlayerId` as the per-server key is the single highest-value decision in this report: it means no authorization code in `Server/Networking/*` or `Server/Social/*` has to change for the platform to be added.

### 4.2 Proposed records (additive, versioned)

```
PlatformAccount
  AccountId            // guid-N, global
  Handle               // unique, immutable-ish, lowercase canon
  DisplayName          // mutable, unique per normalization rule
  Email?               // only if email auth chosen
  PasswordHash?        // only if password auth chosen (never plaintext)
  CreatedUtc, UpdatedUtc, Status(active/suspended/deleted)
  LinkedSteamId?       // from verified ticket, never client-supplied alone

Profile                     // public projection of an account
  AccountId
  Bio, AvatarId/AvatarUrl, AccentColor, Pronouns?, Region?
  Privacy: { ProfileVisibility, ShowActivity, AllowFriendRequests, AllowRoomInvites }
  Stats: { WorldsPublished, RoomsHosted, PlaySeconds, BuildsShared, LastSeenUtc }
  Badges[]             // optional, later

AccountCredential           // N per account; enables multi-server, multi-device
  CredentialId, AccountId, Kind(server-token|platform-session|steam), SecretHash, IssuedUtc, ExpiresUtc?, RevokedUtc?
```

`PlayerAccount` (existing, `Shared/Models/WorldModels.cs`) grows four fields only: `PlatformAccountId`, `DisplayNameSnapshot`, `ProfileSyncedUtc`, `Role`. It keeps `AuthTokenKey` and all friend/request lists so existing flows and the integration harness keep working.

### 4.3 Fixes the identity model must include (all verified defects)

1. **Per-server token storage.** `ModSettings.AuthToken` is one key. Replace with a keyed store (`sfs_enhanced.token.<host>:<port>` or a small JSON credential file under the mod folder). Until this is fixed, using more than one server silently locks the player out of the first.
2. **Legacy account field mismatch.** Committed data uses `"AuthTokenHash"` (`worlds/accounts/61b7c7400f4d4445aa0c8a007430171d.json`) but the model field is `AuthTokenKey` — an old record deserializes to a null key and can never authenticate. Any store migration must accept both names.
3. **`AccountService.FindByName` is O(n) full-file reads** on every handshake and every friend operation. Add an index file (handle → `PlayerId`) or move to SQLite before the platform adds profile traffic.
4. **`AuthToken` has no rotation or revocation path** — no packet, no endpoint. Add `CredentialId` + revoke; a stolen token is currently permanent.
5. **Do not treat the registration password as a user password.** `ComputePasswordHash` is a fixed-salt 600k-round SHA-256 loop shared by all servers (`Shared/Protocol/SecureChannel.cs:31`); it is acceptable only as a coarse gate. If real user passwords are chosen, use PBKDF2/Argon2 with per-user salts via `Rfc2898DeriveBytes` or a library, and never reuse this function.

---

## 5. Room vs world vs server vs published creation — boundaries

This is the decision that unblocks the entire roadmap. Current state: one type, `WorldRecord`, plays all four roles.

| Concept | Definition | Lifetime | Authoritative store | Owns |
| --- | --- | --- | --- | --- |
| **Server** (`ServerId`) | A running dedicated-server process reachable at host:port | process/job | directory (listing) + platform (ownership/verification) | players, capacity, region, MOTD, mod/game version, worlds it hosts |
| **Room** (`RoomId`) | The *advertised, joinable social space*: metadata, visibility, owners/roles, capacity, tags, lifecycle | permanent metadata, runtime may be offline | platform registry, with a **live binding** to one server + one world | title, description, visibility, roles, invites, capacity, category/tags, thumbnail, join identifier |
| **World** (`WorldId`) | The **save**: builds, claims, world clock, planet pack | permanent | dedicated server (`WorldRecord`) | `Builds`, `Claims`, `WorldTime`, `Whitelist` |
| **Creation** (`CreationId`) | An **immutable published version** of an asset (save/blueprint/planet pack/scenario) | permanent, versioned | platform (blob store + metadata) | title, description, screenshots, tags, creator, game/mod version, asset type, validation report, stats |

**Binding rules (must be written into the architecture doc):**

- A Room **references** exactly one World and one Server. Many Rooms may never share a World; one World serves at most one live Room at a time.
- A World **is not** joinable because it is listed publicly; only Rooms are discoverable. Today `WorldListResponse.Worlds` is the room browser — it must become the *room* list, and `World` stops being a user-facing discovery object.
- `WorldRecord.IsPublic` becomes an internal "allow this world to be bound to a public room" flag; visibility moves to the Room.
- **Publishing a creation never creates a room** and never publishes a live world. Publishing copies an immutable snapshot out of a world/server.
- **Creating a room from a creation** is an explicit action: install the creation on a server → import into a new World → create a Room bound to both. The reverse (publish a room's world) is also explicit.
- Deleted/archived room metadata must not delete the World. Deleting a creation version must not affect worlds already imported from it.
- **Never expose filesystem paths.** `WorldRecord` has no path fields today (good); the import/publish pipeline must keep it that way.

---

## 6. Backend / service requirements and hosting implications

### 6.1 What exists and its hosting cost

| Service | Depends on | State | Scale unit | Notes |
| --- | --- | --- | --- | --- |
| Dedicated game server | .NET 8, UDP port | JSON files in `DataDir` | one process per server | already self-hostable, single-file publish works (`scripts/publish-server.sh`, CI matrix win/linux) |
| Directory (`Directory/`) | ASP.NET Core | **in-memory only** | one tiny process | restart = empty list; needs persistence + rate limiting before it is a public service |
| Auth (`Auth/`) | ASP.NET Core + Steam Web API key | stateless | one process | unbuilt/unused; returns a bare SteamId |

### 6.2 What the platform must add

- **Postgres (or SQLite for the single-node case)** for accounts, profiles, friends, rooms, creations metadata, moderation. JSON files are not viable for cross-server queries, uniqueness constraints, ratings, or search.
- **Blob storage** for creation payloads, screenshots and thumbnails. Requirements: content-addressed or versioned keys, content-type validation, size caps, malware/quota policy, signed download URLs. **Never** trust a client-declared size, and never write to a path derived from user input (the existing `Shared/Packaging/PackageInstaller.cs` already proves the team knows how to reject traversal — reuse that posture).
- **Job/queue** for thumbnail rendering and validation (single small worker; Redis is unnecessary at current scale — do not add one until measured).
- **Search**: start with Postgres full-text (`tsvector`) on creations/rooms. Do not add a search service for a mod catalogue of this size.
- **Observability**: structured logging with request/room/account correlation ids, health endpoints, and per-endpoint latency/counter metrics. Today `Server/` logs via bare `Console.WriteLine` only.
- **Abuse control**: per-IP and per-account rate limits on register (directory currently accepts unlimited), heartbeat, upload, chat, friend requests, reports.

### 6.3 Hosting implications and honest costs

- The **game server must stay free to self-host** and must not require the platform. Introduce a config switch `PlatformUrl` (empty = standalone mode). Standalone mode is the regression baseline for CI.
- A central platform means **ongoing operational cost and a privacy obligation** (emails, IPs, SteamIds, chat reports). It also means the platform becomes a single point of failure for *discovery and login only* — never for gameplay. A dedicated server must keep accepting its existing token accounts with the platform unreachable.
- Directory and platform can share one deployment at small scale, but room listings need heartbeat freshness (45 s today) — keep that cheap and unauthenticated for reads.
- Verification of a server's claimed `Host`/`Port` is a real requirement before listings are trusted: add an optional challenge (directory calls back, or the server publishes a signed nonce) rather than trusting registration.

---

## 7. Protocol and persistence migration risks

### 7.1 Wire protocol

| Risk | Evidence | Mitigation |
| --- | --- | --- |
| Version is a single literal, no negotiation | `HelloPacket.ProtocolVersion2 = "2.0"`; server rejects any mismatch outright (`SecureSessionHandshake`) | Add `MinProtocol`/`MaxProtocol` in `HelloAck` so the server can serve two versions during rollout; keep `2.0` behaviour as the floor |
| `PacketType` is a flat `byte` enum with no capability flags | `Shared/Protocol/PacketType.cs` | Treat unknown packet types as ignorable (client already logs an error for the reverse direction); add a `Capabilities` bitmask in `AuthResultPacket`. Note ids **80–83 are already taken** by the uncommitted profile packets, so reserve ranges per family (rooms, creations, moderation) before parallel work starts |
| 8 MB payload ceiling with UDP fragmentation | `LidgrenWire.MaxPayloadBytes = 8 * 1024 * 1024` | Cap far lower for interactive packets; keep 32 KB chunking as in `HandleWorldDownloadRequest`; enforce a cumulative upload cap |
| Hot-stream packets share a sequence space per channel across all builds | `SecureChannel` per-channel `_sendSequence`, `LidgrenWire.GetChannel` | Fine while small; if build count grows, split per-build sequencing or accept only newest-per-build on receive |
| Two `BuildSpawn` producers with different IDs | `MultiBuildManager.TickLocalPublish` (controlled rocket) and `LocalRocketFleetSync.Tick` (all other local rockets) | Unify into one build-id authority before adding rooms, or duplicate/ghosting bugs will be blamed on rooms |
| Rooms/creations are a new packet family or HTTP? | — | Keep **gameplay in the protocol**, **metadata over HTTP**. Do not stream room catalogues through Lidgren |

### 7.2 Persistence

| Risk | Evidence | Mitigation |
| --- | --- | --- |
| No schema version on any record | `WorldRecord`, `PlayerAccount` have no schema field | Add `SchemaVersion` to every persisted record **before** the first platform write; write a one-way upgrader |
| Field rename already broke a committed record | `worlds/accounts/*.json` uses `AuthTokenHash`; model is `AuthTokenKey` | Deserialize with alias support; add a startup validator that logs and migrates legacy shapes |
| Whole-document rewrite on hot path | `HandleBuildSpawn` → `AddOrUpdateBuild` → `Persist`, plus `ClaimsService.Create` → `Persist`; client republishes `BuildSpawn` every 15 ticks (`MultiBuildManager`) | Split live state from durable state: keep positions in memory, persist builds/claims on change *with debounce*; never persist on a 1 Hz stream |
| Single global store lock + `File.Copy` per save | `FileStore.Save` (`lock (_lock)`, tmp + `File.Copy` + `Delete`) | Move to SQLite with WAL, or at minimum a per-file lock and an async writer queue |
| `PersistAll()` every 30 s over every world | `NetServer.AutosaveLoopAsync` | Persist dirty worlds only |
| Directory state is purely in-memory | `Directory/Program.cs` `ConcurrentDictionary` | Persist listings + heartbeat tokens, or accept and document "directory restart drops all lists" |
| In-memory presence lost on restart | `FriendsService._online`, `_currentWorld` | Acceptable for presence; must not be used for authorization or profile stats |
| Two data roots in the repo | `worlds/worlds/`, `worlds/accounts/` at the root are stale committed fixtures | Delete or move under a test fixture path; they are a misleading schema reference today |

### 7.3 Ordering constraint

**Do not start the creator library before persistence is fixed.** Creation blobs plus the existing whole-file world writes will contend on the same `FileStore` lock and the same disk. Persistence is a prerequisite, not a parallel task.

---

## 8. Dependencies

**Already used (no new vendor needed):** Newtonsoft.Json 13.0.3 (`Shared`, `Server`); AscensionGameDev.Lidgren.Network 1.8.0.13 (`Server`, `Mod`, harness); Lib.Harmony 2.3.3 (`Mod`, used by `Mod/Integrated/VanillaUpgradesIntegration.cs` — note this contradicts the "no Harmony" claim in `agent.md` for QoL patches); Microsoft.NETFramework.ReferenceAssemblies.net472; ASP.NET Core minimal API (`Directory`, `Auth`).

**External runtime dependencies that already exist and need decisions:**

| Dependency | Where | Risk |
| --- | --- | --- |
| Cloudflare DoH (`https://cloudflare-dns.com/dns-query`) | `Mod/Networking/ServerEndpointResolver.cs` | Third-party resolver, hard-coded, silent fallback to 7777; a privacy and availability dependency in the connect path |
| Steam Web API (`partner.steam-api.com`) | `Auth/SteamTicketVerifier.cs` | Requires a Web API key and app id; currently no consumer |
| Game assemblies in `Dependencies/` | `Mod/SFSEnhanced.Mod.csproj` | Cannot be redistributed; must stay out of release packs |

**New dependencies I recommend (and nothing more):**

- `Platform/`: ASP.NET Core + one relational database. I have not selected a provider; that is a decision for the manager (see §11) and, if an external service is chosen, it should be researched through the discovery tooling rather than picked from memory.
- A password KDF only **if** user passwords are chosen (`Rfc2898DeriveBytes` in the BCL is sufficient; avoid adding a crypto package).
- Nothing else. No Redis, no message broker, no orchestration framework, no second JSON library, no DI container in `Mod`.

**Do-not-add warning:** the mod targets `net472` and loads Unity 2018-era assemblies. Any new client dependency must be `netstandard2.0`, must not pull `System.Text.Json`, and must be verified to load inside Unity Mono.

---

## 9. Phased vertical-slice roadmap

Sequencing rule from `PRODUCT_VISION.md`: shell + account/profile foundations first, then rooms, then creator publishing, then deeper social. Each slice must be independently shippable, must not regress the multiplayer flows, and must be provable by an automated check.

### Phase 0 — Baseline honesty and guardrails (no product change)

Goal: make the current system verifiable so later slices cannot silently break it.

- Fix or remove `scripts/fac-smoke-test.sh`'s dependency on the nonexistent `TestClient/SFSEnhanced.TestClient.csproj` (currently guarantees CI smoke failure).
- Add `scripts/multiplayer-integration` to `SFSEnhanced.sln`; add a CI job that runs it on Linux (it already exists as a step inside `server-cross-platform.yml`, but the smoke step before it fails).
- Document protocol/data schema in one place; add `SchemaVersion` to `WorldRecord` and `PlayerAccount` and a startup validator for legacy field names.
- **Acceptance:** `dotnet build SFSEnhanced.sln` succeeds; integration harness prints `MULTI_CLIENT_INTEGRATION_PASSED` in CI; a world written by the current server loads after the change.

### Phase 1 — Credential and identity correctness (client-side, no new service)

- Per-server token storage (fixes the silent token-overwrite defect), plus a visible "signed in as / guest" account state.
- Rotate/revoke support for the local token; surface connection errors as real states, not one status label.
- `PlayerConnected`/`PlayerDisconnected` actually broadcast by `NetServer` (types already exist and the client already handles them), giving real presence.
- **Acceptance:** connecting to two servers in one session preserves both tokens; reconnect with a rotated token succeeds and a revoked token fails; presence appears and disappears on join/leave without any new packet type.

### Phase 2 — Platform identity and profiles (first control-plane slice)

- **Already in flight (uncommitted):** server-local profiles and rate-limited updates. Do not re-specify or duplicate this; review it, keep it, and build on it.
- Remaining: `Platform/` with register/login (or verified Steam ticket), `AccountId`, `PlayerAccount.PlatformAccountId` binding at handshake; **standalone mode unchanged** when `PlatformUrl` is empty.
- Profile screen in the mod: display name, avatar image, bio, stats, published creations (empty state honest), privacy toggles — extending the fields already added, not replacing them.
- **Acceptance:** the same account is recognizable on two different dedicated servers; a profile edit is visible on both; a server with no platform configured still accepts its local accounts and passes the integration harness; no secret is logged; tokens are never printed.

### Phase 3 — Rooms as first-class objects

- `Room` record + registry endpoints; room metadata (title, description, visibility public/unlisted/private/invite-only, capacity, tags, category, thumbnail), owner/roles, join identifier.
- Room ↔ world ↔ server binding; migrate the world browser into a room browser with pagination, search, sort, and real loading/empty/error states.
- Create-room flow: pick server → pick/create world → set metadata → join.
- **Acceptance:** a room is discoverable with accurate online count and version; a private room is invisible to non-members; join by shareable id works; leaving and rejoining restores the world exactly; a room's world is untouched if the room is archived; legacy public worlds still joinable via the migration path.

### Phase 4 — Creator library v1

- Creation entity with asset types (save/world, blueprint, planet pack, scenario), immutable versions, validation report, ownership/visibility, size limits.
- Publish from a world/blueprint; browse/search/detail with screenshots; download → import into local SFS saves.
- Explicit "create a room from this creation" action.
- **Acceptance:** a published save round-trips into a new world on another server; an over-limit or malformed upload is rejected with a specific error and no partial file; a private creation is not downloadable by URL guess; publishing does not create a live room.

### Phase 5 — Server browser and directory hardening

- Directory persistence, rate limiting, listing verification, pagination/filters/region; latency measured client-side and displayed honestly (or not displayed at all).
- **Acceptance:** directory restart keeps listings; a spoofed host registration is rejected or flagged; latency shown is measured, not fabricated.

### Phase 6 — Social depth and moderation

- Notifications, blocking, reporting, mute, invites tied to rooms; roles (owner/admin/moderator), kick/ban/whitelist, audit log; moderation queue in the platform.
- **Acceptance:** a banned account cannot join any room on that server; a report creates a reviewable record; every moderation action is in the audit log with actor, target, reason, time; blocked users cannot message or invite.

### Phase 7 — Simulation depth (parallel, gated by demand)

Docking/joint builds, debris, remote staging completeness, physics sanity checks, `PartModuleSync` coverage beyond parachute/fuel. **Do not** schedule this before Phase 3, because room/world boundaries change what a "shared build" is.

---

## 10. Acceptance criteria (cross-cutting, apply to every phase)

**Functional**
1. Every screen reachable from the shell has loading, empty, error and offline states; no dead button and no placeholder presented as complete.
2. Every new capability works in **standalone mode** (no platform, no directory) or is documented as requiring one.
3. No existing multiplayer flow regresses: connect, world create/join/leave, build sync, claims, chat, friends, time-warp, world clock.

**Security**
4. No credential, token, or key is logged, rendered, or placed in a URL.
5. Every client-supplied identifier (world id, room id, build id, account id, creation id) is authorized server-side; the world-download visibility hole and the upload allocation bomb are fixed before either feature is exposed in UI.
6. Uploads: declared and actual size capped, chunk totals bounded, content type validated, path traversal impossible, no partial artefacts left on failure.
7. Constant-time comparison for all secret/token/heartbeat comparisons (directory heartbeat currently uses `!=`).
8. Rate limits exist for chat, friend requests, claims, uploads, registration, and reports; limit values are documented.

**Data**
9. Every persisted record carries a schema version and can be migrated forward; a legacy record (including `AuthTokenHash`) still loads.
10. No hot-path whole-file writes; measured write rate is documented for 8 concurrent players.
11. Data survives restart: worlds, claims, friends, tokens, room metadata, creations.

**Quality**
12. Builds for all supported targets (`Server` win/linux publish, `Mod` net472, `Platform`), and the integration harness passes.
13. Independent QA review of each slice's diff; findings filed with reproduction steps.
14. Every slice states which files it owns; no two agents edit the same file concurrently.
15. Nothing in this report's "verified current" column may be broken without an explicit decision recorded by the manager.

---

## 11. Open decisions (need the manager, in this order)

1. **Identity provider.** Verify-a-Steam-ticket-only, first-party email+password, or both? This decides password storage, account recovery, and whether the platform can exist off-Steam (mobile/GOG players). `docs/COMMUNITY_PRIORITY.md` recommends Steam as canonical PC identity; `docs/SECURE_STEAM_AUTH.md` already specifies the ticket flow. Recommendation: Steam ticket as one credential kind, with a platform account above it, so non-Steam builds still work.
2. **Does a platform account own a server-local `PlayerId`, or does `PlayerId` become global?** Recommendation: keep `PlayerId` per server (Section 4.1). Choosing global identity invalidates every authorization check in `Server/`.
3. **Room lifetime model.** Does a room exist when no server is hosting its world (metadata yes, runtime no)? Recommendation: metadata permanent, runtime binding soft and expiring, like the directory's 45 s heartbeat.
4. **Creation asset formats.** Which are first-class at v1, and does SFS Enhanced integrate with the game's existing `sharing.spaceflightsimulator.app` client (`Assembly-CSharp/SFS.Sharing/SharingRequester.cs`) or replace it? Recommendation: start with world/save + blueprint as separate types, do not attempt to replace the stock sharing site in v1.
5. **Database and blob provider.** Unselected. If a hosted provider is chosen, research and compare options with the discovery tooling rather than picking from memory; also record the self-host cost, since self-hostability is a stated constraint.
6. **Is the platform's directory merged with `Directory/`, or separate?** Merging is cheaper to operate; separating keeps the "directory is trivially self-hostable" property. Recommendation: separate processes, shared deployment, distinct data.
7. **Physics authority.** Stay trust-the-client (documented in `docs/ARCHITECTURE.md`) or add sanity checks? Recommendation: stay, add numeric-plausibility checks only when a public competitive server exists.
8. **Retire `TestClient` and `scripts/fac-smoke-test.*`, or restore the project?** Recommendation: delete the stale references; the integration harness covers more.
9. **Harmony policy.** `agent.md` says no Harmony; `Mod/SFSEnhanced.Mod.csproj` references `Lib.Harmony` and `Mod/Integrated/VanillaUpgradesIntegration.cs` calls `PatchAll`. Decide and document one policy; the ambiguity will otherwise produce contradictory review feedback.
10. **Documentation ownership.** `README.md` currently describes TLS, certificate pinning and fingerprint publishing that do not exist. Decide who owns doc truth and require each slice to reconcile the docs it invalidates (see §12).

---

## 12. Documented claims contradicted by code (must be reconciled)

| Claim | Source | Reality |
| --- | --- | --- |
| "TLS-protected multiplayer transport", "TLS certificate fingerprint pinning and first-connection trust", "the server creates or loads a TLS certificate" | `README.md` §What it includes, §Security | No TLS, no certificate, no pinning. Transport is Lidgren UDP with an application-layer P256/AES-CTR/HMAC session (`Shared/Protocol/*`). `agent.md` and `docs/ARCHITECTURE.md` correctly say TLS was removed. |
| "Public directory listings can publish the server certificate fingerprint" | `README.md` §Security | `ServerListing` / `ServerRegisterRequest` (`Shared/Models/ServerDirectoryModels.cs`) have no fingerprint field; the client never verifies `AuthChallengePacket.IdentityFingerprint`. |
| "Windows and Linux dedicated-server support … same TCP protocol" | `docs/ARCHITECTURE.md` §Dedicated server platform boundary | TCP was removed; the transport is UDP (`LidgrenServerTransport`). Platform support is real; the protocol sentence is stale. |
| "No Harmony" | `agent.md` §Confirmed | True for core multiplayer integration (native `ModLoader.Mod`), false for QoL patches: `Mod/Integrated/VanillaUpgradesIntegration.cs` uses `Harmony` + `PatchAll`. |
| Feature status table marks "Server discovery / TLS transport / SRV / Persistent authentication" all as Implemented | `README.md` §Project status | Discovery and persistence of auth are implemented; TLS is not; SRV works but via hard-coded Cloudflare DoH with a silent 7777 fallback. |
| `docs/SETUP_MOD_SIDE.md` describes Harmony and `Mod/Dependencies/` | `docs/SETUP_MOD_SIDE.md` | Dependencies now live at repo root (`Mod/SFSEnhanced.Mod.csproj` uses `..\Dependencies\`); the doc also tells the implementer to wire TODO spawn/serializer paths that are now implemented. |
| `docs/ROADMAP.md` lists server browser, upload/download, world invites as done/underway | `docs/ROADMAP.md` | Server browser is a 7-row list; world upload/download has **no client UI at all**; invites only target world ids and do not survive logout. |

---

## 13. Files that will be touched by the roadmap (ownership map for the implementer)

Read-only is deliberate; this is the collision map so parallel agents do not overlap.

| Area | Files | Phase |
| --- | --- | --- |
| Protocol contract | `Shared/Protocol/PacketType.cs`, `Packets.cs`, `SecureHandshake.cs`, `SecureChannel.cs` | 1,3,4 |
| Platform models | new files under `Shared/Models/` | 2,3,4 |
| Server authority | `Server/Networking/NetServer*.cs`, `ClientConnection.cs`, `SecureSessionHandshake.cs` | 1,2,3 |
| Persistence | `Server/Persistence/FileStore.cs`, `AccountService.cs`, `Server/World/WorldManager.cs` | 0,2,4 |
| Social | `Server/Social/FriendsService.cs`, `ClaimsService.cs` | 1,6 |
| Directory | `Directory/Program.cs`, `Server/Networking/ServerDirectoryPublisher.cs` | 5 |
| Client transport | `Mod/Networking/NetClient.cs`, `ModSettings.cs`, `ServerDirectoryClient.cs`, `ServerEndpointResolver.cs` | 1,2 |
| Client UI (one owner at a time) | **In flux:** `MultiplayerMenu.Panels.cs` / `MultiplayerMenu.Packets.cs` are deleted; `MultiplayerMenu.cs`, `MultiplayerScreen.cs` remain; new uncommitted `Mod/UI/App/*`, `Mod/UI/Kit/*` | 1,2,3,4 |
| Profiles (uncommitted, needs review) | `Server/Social/ProfileService.cs`, `Server/Networking/NetServer.Profile.cs`, profile fields in `Shared/Models/WorldModels.cs`, packets 80–83 in `Shared/Protocol/{PacketType,Packets}.cs`, `Mod/Networking/NetClient.cs` | 2 |
| Simulation | `Mod/World/MultiBuildManager.cs`, `LocalRocketFleetSync.cs`, `RemoteRocketGhost.cs`, `PartModuleSync.cs` | 1,7 |
| Tests/CI | `scripts/multiplayer-integration/Program.cs`, `scripts/fac-smoke-test.*`, `.github/workflows/*`, `SFSEnhanced.sln` | 0 and every phase |

---

## 14. Bottom line for the manager

The multiplayer core is real and worth protecting: secure sessions, authoritative worlds, multi-build sync, claims, friends, chat, world clock, time-warp voting — all backed by a genuine two-client harness. The platform layer is absent, not merely unfinished, and the current client has two defects (single global token slot, no presence broadcast) that will undermine any identity work if not fixed first.

Recommend starting exactly here: **Phase 0 + Phase 1 in one slice** (verifiable baseline, per-server credentials, real presence), because both are small, both are pure defect/consistency work, and both remove traps that would otherwise surface later as "profile bugs". Then Phase 2 introduces the control plane against the constraint that standalone servers keep working unchanged.

Do not begin rooms or the creator library before the persistence split (§7.2): the room browser and creation uploads will contend with the existing whole-file world writes, and the resulting instability will be misdiagnosed as a networking problem.
