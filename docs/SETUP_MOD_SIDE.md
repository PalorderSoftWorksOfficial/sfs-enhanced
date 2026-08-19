# Setting up the mod side against your real game install

The `Server/` project needs nothing from you but the .NET SDK — see the README
Quick Start. This doc is only for `Mod/`, which needs the actual game.

## 1. Install a mod loader

Pick one (both are community projects, not official):

- [105-Code/SFS-Modloader](https://github.com/105-Code/SFS-Modloader) — drop
  `ModLoader.dll` into `Spaceflight Simulator_Data/Managed/`.
- [JordivdMolen/SFSModLoader](https://github.com/JordivdMolen/SFSModLoader) —
  alternative loader, different mod interface shape.

`Mod/ModMain.cs` is written against the general shape both provide (an
OnLoad-style entry point + Harmony); you'll need to adjust the base
class/interface to match whichever loader you pick — check that loader's own
example/template mod repo (e.g. `105-Code/sfs-mod`) for the exact signature.

## 2. Collect the game's DLLs

From `Steam\steamapps\common\Spaceflight Simulator\Spaceflight Simulator Game\
Spaceflight Simulator_Data\Managed\`, copy into `Mod/Dependencies/`:

```
0Harmony.dll
Assembly-CSharp.dll
Newtonsoft.Json.dll
UnityEngine.dll
UnityEngine.CoreModule.dll
UnityEngine.UI.dll
```
Plus your mod loader's own DLL (e.g. `ModLoader.dll`).

## 3. Open Assembly-CSharp.dll in a decompiler

Use [dnSpy](https://github.com/dnSpy/dnSpy) or [ILSpy](https://github.com/icsharpcode/ILSpy)
(both free) to find:

1. **The rocket/build spawn path** — search for whatever runs when you press
   "Launch" from the build screen, or "Load" from the load-game menu. That's
   what `MultiBuildManager.SpawnOrUpdateRemoteBuild`'s TODO needs to call.
2. **The build-JSON (de)serializer** — SFS already has one internally (it's
   how saves/blueprints work today); find its input type so
   `BuildSnapshot.PartsBlueprintJson` can round-trip through it instead of
   inventing a new format. This also tells you the real schema to replace
   `Mod/Ships/Blueprints/blueprint_template.json` with.
3. **Position/rotation/velocity fields on the rocket controller** — needed for
   `MultiBuildManager.TickInterpolation`'s TODO to actually move a GameObject,
   and for reading local values to send via `BuildStateUpdate`.
4. **The time-warp controller** — needed for the proximity-lock behavior
   described in `docs/ARCHITECTURE.md`.
5. **Whatever scene/root object ticks every frame regardless of current
   scene** — that's where `ModMain.Update()`'s pump call needs to live (or
   your mod loader may already give you a persistent MonoBehaviour for this).

## 4. Build

```
Open Mod/SFSEnhanced.Mod.csproj in Visual Studio (or `dotnet build` once the
Dependencies are in place) → builds SFSEnhanced.dll → copy to
Spaceflight Simulator_Data/Mods/SFSEnhanced/ per your loader's convention.
```

## 5. Point it at your server

`ModMain.ConnectToServer(host, port, playerName)` — wire this to an in-game
menu button. The dedicated server from `Server/` needs to already be running
and reachable (port-forward `--port` if hosting from home, or just run it on
a VPS).
