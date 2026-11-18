# Handoff brief — SFS Enhanced

Paste this file (or point Cursor at it) as the first message in a new session.
This is the **authoritative** layout for `d:\sfs-enhanced`. Ignore older notes that
mention `SFSMultiPlayer-fixed/` or Harmony — those are wrong for this repo.

## What this project is

Make **SFS Enhanced** the major community multiplayer mod for Spaceflight Simulator:
dedicated servers, uploadable/shared worlds, multiple builds per world, friends,
claims, and stock multiplayer ships.

```
sfs-enhanced/
  Shared/           # wire protocol + models + secure channel (netstandard2.0)
  Server/           # dedicated Lidgren UDP server (net8) — no Unity dependency
  Directory/        # server-directory publisher/discovery service (net8)
  Mod/              # game DLL for the built-in ModLoader
  Assembly-CSharp/  # full decompiled game source (reference, do not ship)
  Dependencies/     # game Managed DLLs for compiling Mod/
  scripts/multiplayer-integration/  # headless two-client integration test (net8)
  docs/             # architecture / research / setup
```

## Confirmed: native ModLoader — no Harmony

Decompiled `Assembly-CSharp` includes `ModLoader.Mod`, `ModLoader.Loader`,
`ModLoader.Helpers.SceneHelper`, and `SFS.UI.ModGUI.Builder`. Mods inherit
`ModLoader.Mod`, override `Load()`, and drop `Mods/<Folder>/<Folder>.dll`
(folder name must match DLL name — we use `SFSEnhanced/SFSEnhanced.dll`).

## Real game hooks already wired in Mod/

- `RocketManager.LoadRocket(RocketSave, out bool)` / `DestroyRocket`
- `RocketSave` + `JsonWrapper.ToJson` / `FromJson` for parts sync
- `GameManager.main.rockets`, `PlayerController.main.player`
- `physics.SetLocationAndState(Location, …)` for remote interpolation
- ModGUI connect window (`Mod/UI/MultiplayerMenu.cs`), F8 to toggle

## Transport & security (current)

- **Lidgren UDP only.** Reliable-ordered channel for events/state, unreliable-sequenced
  channel for the hot rocket stream (`LidgrenWire.IsHotStream`). TCP/TLS is removed.
- **Application-layer secure session** (`Shared/Protocol/SecureChannel.cs`,
  `SecureHandshake.cs`, `P256.cs`): ephemeral P256 ECDH → transcript-bound HMAC proof of
  the auth token → per-direction AES-CTR + HMAC-SHA256 sealed frames with per-stream
  sequence numbers and a replay window. Tokens are never sent on the wire; the server
  stores only SHA-256 token keys. Invalid MACs drop the packet; only repeated failures
  (32) or failed auth end the connection.
- Wire protocol is **3.0** (`HelloPacket.ProtocolVersion3`). After `AuthChallenge` the server sends
  a sealed `IdentityProof` (HMAC from the static-key shared secret over the transcript) and only then
  does the client send its token MAC; a missing/forged proof aborts with `identity_proof_missing` or
  `identity_proof_invalid`. 2.x peers get `protocol_version`; there is no silent downgrade.
- Registration/auth modes live in server config. Sign-in never creates an account; only the explicit
  register mode does, and the registration password proof is answered inside the sealed channel.

## Verified game hooks (patch targets checked against decompiled source)

- `RocketManager.LoadRocket(RocketSave, out bool)` / `DestroyRocket`
- `RocketSave` + `JsonWrapper.ToJson` / `FromJson` for parts sync
- `GameManager.main.rockets`, `PlayerController.main.player`, `rocket.arrowkeys.rcs`
- `physics.SetLocationAndState(Location, …)` for remote interpolation
- `SFS.Builds.BuildStatsDrawer` (mass/TWR fields), `SFS.World.PlayerController` throttle,
  `Units.ToDistanceString`/`ToMassString`/`ToThrustString` for the VanillaUpgrades patches

## Server features

Lidgren UDP transport, secure sessions, server-enforced rooms (`Server/Rooms/RoomManager.cs`,
`NetServer.Rooms.cs`), worlds, world upload/download, friends, claims with overlap policy, accounts,
filtered chat, build spawn/state sync, server-authoritative world time with timewarp voting — see
`Server/Networking/NetServer.cs` and `PRODUCT_ARCHITECTURE.md`.

Rooms are live sessions hosting one world save: visibility (public / friends), capacity, one room per
world, host-only close, auto-close when empty. They are process-scoped and do not survive a restart
(BUGS B-19); there is no host transfer (B-20). Do not describe them as persistent.

## Testing

1. `dotnet build SFSEnhanced.sln` — Server/Shared/Directory/Mod all build.
2. `dotnet run --project Server/SFSEnhanced.Server.csproj -- --config <config.json>`
3. `dotnet run --project scripts/multiplayer-integration` against it — 14 assertions covering auth
   (incl. wrong-token rejection and identity-proof omission/forgery), rooms (capacity, visibility,
   host close, auto-close), worlds, filtered chat, friends, claim overlap, build/rocket sync, world
   time, timewarp voting, replay and tamper rejection, disconnect handling. Prints
   `MULTI_CLIENT_INTEGRATION_PASSED`. Run it five times sequentially against one server set.
   Test servers need `--upload-max-concurrent 1 --upload-idle-seconds 2` for the upload assertions
   (`QA_PLAN.md`).
4. `dotnet publish Server/SFSEnhanced.Server.csproj -r linux-x64 -c Release` — cross-platform check.

## Documentation map

`PRODUCT_VISION.md` (product target), `PRODUCT_ARCHITECTURE.md` (current tree), `DESIGN_SYSTEM.md`
(UI tokens/geometry/components), `UX_FLOWS.md` (screen states and paths), `AGENT_ROLES.md` (role
briefs and review gates), `IMPLEMENTATION_PLAN.md` (gates), `QA_PLAN.md` (verification protocol),
`BUGS.md` (verified defects only).

## Build notes

- Repo-root `nuget.config` clears the broken VS Offline Packages feed.
- Mod targets `net472` and references `../Dependencies/*.dll`.
- Do **not** redistribute `Dependencies/` or `Assembly-CSharp/` in releases.
