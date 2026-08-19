# SFS Enhanced — The Multiplayer Platform & Gameplay Expansion for Spaceflight Simulator

SFS Enhanced is being built as a platform layer for *Spaceflight Simulator*: a first-class multiplayer experience, player-hosted dedicated servers, public server discovery, persistent shared worlds, social systems, creator tools, and large-scale gameplay expansions.

SFS provides a built-in mod loader (`ModLoader.Mod` inside `Assembly-CSharp.dll`) and `SFS.UI.ModGUI`. This project targets those native APIs without Harmony or another third-party loader.

The `Server/`, `Directory/`, and `Shared/` projects do not require SFS to run. The game-side integration lives in `Mod/`.

## Current platform

| Feature | Status |
|---|---|
| Dedicated TCP server | Implemented |
| Persistent player identity | Implemented |
| Shared worlds | Implemented |
| Multiple builds per world | Implemented |
| Remote rocket synchronization | Implemented foundation |
| Build ownership and claims | Implemented foundation |
| Friends and world invites | Implemented foundation |
| World upload/download | Implemented foundation |
| Main-menu Multiplayer button | Implemented |
| Multiplayer front door UI | Implemented |
| Direct server connection | Implemented |
| Player-hosted dedicated server launch | Implemented foundation |
| Public server directory service | Implemented |
| Server advertising and heartbeat | Implemented |
| Public server browser | Implemented client foundation |
| Time-warp arbitration | Implemented foundation |
| Missions and events | Planned |
| Factions and economies | Planned |
| Creator/blueprint platform | Planned |

## Architecture

```text
SFS Enhanced Mod
  |
  +-- Multiplayer UI
  |     +-- Browse Servers
  |     +-- Host Server
  |     +-- Direct Connect
  |     +-- World management
  |
  +-- NetClient
  |     +-- persistent player identity
  |     +-- live world synchronization
  |
  +-- Shared protocol/models
  |
  +-- Dedicated Server
  |     +-- accounts
  |     +-- worlds
  |     +-- builds
  |     +-- claims
  |     +-- friends
  |     +-- chat
  |     +-- time-warp arbitration
  |
  +-- Server Directory
        +-- public server listings
        +-- registration
        +-- heartbeat
        +-- expiration
```

## Repository layout

```text
sfs-enhanced/
  Shared/      protocol and models shared by Server and Mod
  Server/      dedicated multiplayer server
  Directory/   self-hostable public server directory
  TestClient/  console client for protocol testing
  Mod/         native SFS game mod
  docs/        architecture, research, setup and roadmap
```

## Dedicated server

```bash
cd Server
dotnet run -- --port 7777 --name "My SFS Server" --data ./data --max-players 32
```

A server can advertise itself through a self-hosted directory:

```bash
dotnet run -- \
  --port 7777 \
  --name "My SFS Server" \
  --data ./data \
  --max-players 32 \
  --advertise \
  --directory https://your-directory.example \
  --public-host play.example.com \
  --region EU
```

## Server directory

```bash
cd Directory
dotnet run --urls http://0.0.0.0:8080
```

The directory exposes the public server browser API under `/api/v1/servers`.

## Mod

The mod uses SFS's native `ModLoader.Mod` and `SFS.UI.ModGUI` APIs.

Once installed, `MULTIPLAYER` appears as a first-class button on the SFS home screen. The in-game multiplayer UI provides public server discovery, direct connection, hosting, world creation, and world management.

F8 remains available as an in-world shortcut.

## Scope

SFS Enhanced is intentionally larger than multiplayer alone. The roadmap includes missions, events, shared stations, persistent infrastructure, factions, moderation, creator tools, blueprint sharing, custom rulesets, and optional gameplay modules.

See `docs/ROADMAP.md` for the current project direction.
