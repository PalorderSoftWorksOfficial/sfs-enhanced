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
- Registration/auth modes live in server config; unknown names can auto-register in token
  mode and receive an issued token as the sealed first response.

## Verified game hooks (patch targets checked against decompiled source)

- `RocketManager.LoadRocket(RocketSave, out bool)` / `DestroyRocket`
- `RocketSave` + `JsonWrapper.ToJson` / `FromJson` for parts sync
- `GameManager.main.rockets`, `PlayerController.main.player`, `rocket.arrowkeys.rcs`
- `physics.SetLocationAndState(Location, …)` for remote interpolation
- `SFS.Builds.BuildStatsDrawer` (mass/TWR fields), `SFS.World.PlayerController` throttle,
  `Units.ToDistanceString`/`ToMassString`/`ToThrustString` for the VanillaUpgrades patches

## Server features

Lidgren UDP transport, secure sessions, multi-room worlds, world upload/download,
friends, claims, accounts, build spawn/state sync, server-authoritative world time with
timewarp voting — see `Server/Networking/NetServer.cs` and `docs/ARCHITECTURE.md`.

## Testing

1. `dotnet build SFSEnhanced.sln` — Server/Shared/Directory/Mod all build.
2. `dotnet run --project Server/SFSEnhanced.Server.csproj -- --config <config.json>`
3. `dotnet run --project scripts/multiplayer-integration` against it — exercises auth
   (incl. wrong-token rejection), worlds, chat, friends, claims, build/rocket sync, world
   time, timewarp voting, replay and tamper rejection, disconnect handling, and prints
   `MULTI_CLIENT_INTEGRATION_PASSED`.
4. `dotnet publish Server/SFSEnhanced.Server.csproj -r linux-x64 -c Release` — cross-platform check.

## Build notes

- Repo-root `nuget.config` clears the broken VS Offline Packages feed.
- Mod targets `net472` and references `../Dependencies/*.dll`.
- Do **not** redistribute `Dependencies/` or `Assembly-CSharp/` in releases.
