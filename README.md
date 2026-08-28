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

The dedicated server is a .NET 8 application and does not load Spaceflight Simulator assemblies. The server is therefore designed to run independently on Windows or Linux while the SFS mod remains the game-side component.

### Windows

From PowerShell:

```powershell
cd Server
dotnet run -- --port 7777 --name "My SFS Server" --data ./data --max-players 32
```

For a self-contained Windows server build:

```powershell
./scripts/build-server.ps1
```

The resulting server is placed under `dist/server/win-x64`.

### Linux

From Bash:

```bash
cd Server
dotnet run -- --port 7777 --name "My SFS Server" --data ./data --max-players 32
```

For a self-contained Linux server build:

```bash
./scripts/build-server.sh
```

The resulting server is placed under `dist/server/linux-x64`.

The server's persistence and networking code uses .NET cross-platform APIs rather than Windows-specific APIs. Linux deployments can use the included `Server/sfs-enhanced.service.example` as a systemd template, while Windows deployments can run `Server/run-server.ps1` or the published executable directly.

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

## Dedicated server platform support

The dedicated server is a standalone .NET 8 application and does not load the
SFS executable, Unity, or Unity native libraries. SFS itself and the game-side
mod remain platform-dependent; the dedicated server is intentionally separate.

The same server binary architecture supports both Windows and Linux. For a
portable deployment, install the .NET 8 runtime and run `Server/SFSEnhanced.Server.csproj`.
For servers without a runtime, publish a self-contained build:

### Linux

```bash
bash scripts/publish-server.sh
./scripts/run-server.sh ./Server/server.json
```

The Linux publisher targets `linux-x64`.

### Windows PowerShell

```powershell
.\scripts\publish-server.ps1
.\scripts\run-server.ps1 .\Server\server.json
```

The Windows publisher targets `win-x64` and produces `SFSEnhanced.Server.exe`.

Both platforms use the same JSON configuration, TCP protocol, persistence
format, directory service integration, and world data. Server data should be
kept outside the game installation so a Linux or Windows host can be migrated
without installing SFS on the host.
