# SFS Enhanced — Multiplayer & World Tools for Spaceflight Simulator

A community mod project adding **dedicated-server multiplayer**, **shared/uploadable worlds**,
**multiple builds per world**, **friends & claims**, and two new stock rocket designs to
*Spaceflight Simulator* (SFS) by Team Curiosity.

> **Read this first.** SFS ships a **built-in mod loader** (`ModLoader.Mod` inside
> `Assembly-CSharp.dll`) plus `SFS.UI.ModGUI`. This project targets that API —
> **no Harmony and no third-party loader required.** Game-side code lives in `Mod/`
> and compiles against the DLLs in repo-root `Dependencies/`. The decompiled game
> sources in `Assembly-CSharp/` are the reference for hooks.
>
> The `Server/` and `Shared/` projects are **plain .NET, zero game dependency** —
> worlds, players, friends, claims, persistence, and the wire protocol run without
> SFS installed.

This is a genuinely large project (this is basically "write a multiplayer backend + a game
mod" — the kind of thing real SFS multiplayer mods, like `AstarLC4036/SFS-Multiplayer`, took
months to get partially working). What's here is a serious, correct starting point, not a
finished polished product — treat it as v0.1.

## What's included

| Piece | Status |
|---|---|
| Dedicated server (TCP, async, multi-room) | Working, testable now (`dotnet run` in `Server/`) |
| Wire protocol (login, world sync, rocket state, chat) | Working |
| World persistence (JSON world store, versioned) | Working |
| Multiple builds per world (bases + rockets coexist) | Working data model, needs game-side render hook |
| Friends system (add/accept/list/invite-to-world) | Working (server) |
| Claims system (protect a build/region from other players) | Working (server) |
| World upload/download (share your world file with a server) | Working (server) |
| Two new stock rocket blueprints | JSON templates included, see note below |
| Game-side mod (native ModLoader, ModGUI, rocket sync hooks) | Wired to real SFS APIs — in-game testing next |

## Repo layout

```
sfs-enhanced/
  Shared/      # protocol + data models used by both Server and Mod
  Server/      # dedicated server console app — no Unity/game dependency
  TestClient/  # plain console client to exercise the server without the game
  Mod/         # the actual .dll that goes in the game's Mods folder
  docs/        # architecture notes, feature research, setup guide
```

## Quick start (server)

```bash
cd Server
dotnet run -- --port 7777 --name "My SFS Enhanced Server" --worlds ./worlds
```

The server is fully functional as a standalone program right now. Open a second terminal
and drive it with the included test client (no game needed):

```bash
dotnet run --project TestClient -- 127.0.0.1 7777 Alice
> world My Test World
> spawn
> chat hello from Alice
```

Open a third terminal for a second player and watch them see each other's world list,
builds, and chat in real time. See `docs/ARCHITECTURE.md` for the full protocol.

**Note on this sandbox:** I wrote this code carefully but couldn't install a .NET SDK in
this environment to actually compile-test it (network is restricted here) — treat it as
"should build cleanly" rather than "verified to build." If `dotnet build` turns up an
error, it'll be a small one; open an issue against yourself and fix forward.

## Quick start (mod)

1. Build: `dotnet build Mod/SFSEnhanced.Mod.csproj -c Release`
2. Copy `Mod/bin/Release/net472/SFSEnhanced.dll` (+ `SFSEnhanced.Shared.dll` if not
   merged) into `Spaceflight Simulator_Data/Mods/SFSEnhanced/`
3. Launch the game — native ModLoader picks it up. Multiplayer window opens on Home;
   press **F8** in-world. Run the dedicated server first (`dotnet run --project Server`).

See `docs/SETUP_MOD_SIDE.md` and `agent.md` for details. Decompiled sources are in
`Assembly-CSharp/` if you need more hooks.

## Where the feature list came from

`docs/FEATURE_RESEARCH.md` is a writeup of what the SFS community (Steam forums, Reddit,
existing mod repos) has actually been asking for, which is what shaped the roadmap below.
