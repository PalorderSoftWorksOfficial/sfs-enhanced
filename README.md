# SFS Enhanced

SFS Enhanced is a multiplayer mod and dedicated server project for Spaceflight Simulator.

The current release focuses on a reliable multiplayer entry point and client connection flow. Experimental synchronization and server features remain under development and are not advertised as finished.

## Current status

| Feature | Status |
|---|---|
| SFS native mod loader integration | Working |
| Main-menu MULTIPLAYER button | Working |
| F8 multiplayer menu toggle | Working |
| Player name configuration | Working |
| Direct TCP server connection | Working |
| Server disconnect | Working |
| World list request | Working |
| World creation request | Working |
| World joining request | Working |
| Basic server information handling | Working |
| Remote rocket synchronization | Experimental |
| Build ownership and claims | Experimental |
| Friends | Experimental |
| Chat | Experimental |
| Public server directory | Experimental |
| World upload/download | Experimental |
| Player-hosted server launch | Experimental |
| Missions and events | Planned |
| Factions and economies | Planned |
| Creator and blueprint platform | Planned |

## Installing the mod

Build or download the `SFSEnhanced` mod package. The installed directory must contain both files:

```text
Mods/
  SFSEnhanced/
    SFSEnhanced.dll
    mod.json
```

The GitHub Actions build artifact contains both files in the correct package directory.

## Building the mod

The mod targets .NET Framework 4.7.2 and references the Spaceflight Simulator game assemblies stored under `Dependencies/`.

```powershell
dotnet restore Mod/SFSEnhanced.Mod.csproj --configfile nuget.config
dotnet build Mod/SFSEnhanced.Mod.csproj -c Release
```

The compiled assembly is produced at:

```text
Mod/bin/Release/net472/SFSEnhanced.dll
```

## Multiplayer menu

When the mod loads, it waits for the SFS home screen's `Buttons` object and adds a `MULTIPLAYER` button. The menu provides:

- Player name
- Server host
- Server port
- Direct connection
- Server disconnect
- World list refresh
- World creation
- World joining

Press `F8` to toggle the menu in-game.

## Dedicated server

The dedicated server is a separate .NET application. It does not load the Spaceflight Simulator executable.

Windows:

```powershell
cd Server
dotnet run -- --port 7777 --name "My SFS Server" --data ./data --max-players 32
```

Linux:

```bash
cd Server
dotnet run -- --port 7777 --name "My SFS Server" --data ./data --max-players 32
```

## Protocol

The client and server share the packet definitions under `Shared/Protocol/`. The current protocol includes connection, server discovery, world lifecycle, build synchronization, time control, social, claims, chat, and error message types.

Protocol support does not mean every feature is production-ready. Check the status table above before depending on experimental systems.

## Development

The project is split into:

```text
Mod/         SFS client mod
Server/      Dedicated server
Shared/      Client/server protocol and models
Directory/   Public server directory service
TestClient/  Protocol test client
```

The client entry point is `Mod/ModMain.cs`. The multiplayer UI is `Mod/UI/MultiplayerMenu.cs`.
