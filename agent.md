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
  Shared/           # wire protocol + models (netstandard2.0)
  Server/           # dedicated TCP server (net8) — no Unity dependency
  TestClient/       # console client to exercise the server without the game
  Mod/              # game DLL for the built-in ModLoader
  Assembly-CSharp/  # full decompiled game source (reference, do not ship)
  Dependencies/     # game Managed DLLs for compiling Mod/
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

## Server features already present

Multi-room worlds, world upload/download packets, friends, claims, accounts,
build spawn/state sync — see `Server/Networking/NetServer.cs` and `docs/ARCHITECTURE.md`.

## Suggested order of work

1. `dotnet build SFSEnhanced.sln` — verify Server/Shared/TestClient.
2. `dotnet build Mod/SFSEnhanced.Mod.csproj` — verify Mod against Dependencies/.
3. Two-client TestClient session against Server (no game needed).
4. In-game: connect → create/join world → confirm remote rockets spawn.
5. Ownership validation on `BuildStateUpdate` (reject spoofed build ids).
6. Time-warp proximity lock (protocol placeholders exist).
7. Ship two stock rockets as `RocketSave` JSON assets.
8. Harden friends/claims UI on ModGUI.

## Build notes

- Repo-root `nuget.config` clears the broken VS Offline Packages feed.
- Mod targets `net472` and references `../Dependencies/*.dll`.
- Do **not** redistribute `Dependencies/` or `Assembly-CSharp/` in releases.
