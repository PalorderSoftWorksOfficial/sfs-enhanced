# Native mod-loader lifecycle

## Integration point

The runtime manager installs Harmony patches from the SFS Enhanced entry type's static constructor. The native loader creates that type while it scans mod DLLs, before it starts the LoadMod pass. The manager hooks the end of the native LoadMod(Mod) call, the end of Initialize_Load(), and ModsListElement.Init.

The native loader remains responsible for discovering assemblies, creating mod instances, checking game versions and resolving startup dependencies. SFS Enhanced does not replace the game's assembly discovery or change how the default load context loads DLLs.

## Runtime toggle behavior

The native Mods menu toggle delegates to the lifecycle manager for mod entries.

A mod that implements IModRuntimeLifecycle can be disabled live. Its OnRuntimeDisable() callback must stop new work, cancel owned tasks, unsubscribe events, disconnect services, restore modified game state and destroy owned Unity objects. The manager then removes only the Harmony owner IDs returned by HarmonyOwners and removes the mod instance from the loader's active list.

A lifecycle-aware mod can be re-enabled live. The manager checks its declared dependencies and versions, invokes its keybinding registration and calls OnRuntimeEnable(startupComplete). For a mod enabled after startup, OnRuntimeEnable(true) owns the complete runtime startup path; the native early-load phase has already passed and is not replayed. Before startup completes, OnRuntimeEnable(false) restores lifecycle state and the native loader performs the regular Load() phase later.

Mods that do not implement IModRuntimeLifecycle retain restart-required behavior when a live change would otherwise be necessary. Their settings are still saved through the native settings system and the native relaunch warning remains available.

A loaded dependency cannot be disabled while another loaded mod declares it as a dependency.

## Harmony ownership

The lifecycle manager uses its own permanent owner ID for loader and menu hooks. Those hooks remain installed when SFS Enhanced itself is disabled so the menu can re-enable it.

Each lifecycle-aware mod must return only the Harmony owner IDs that it owns. The manager unpatches those IDs, never all Harmony patches. A mod must not share an owner ID with another mod.

## Assembly unload limitation

The built-in SFS loader calls Assembly.Load(byte[]) and keeps mod instances in the default game runtime context. Harmony unpatching removes method patches; it does not unload the managed assembly. This implementation performs a logical runtime unload: callbacks and owned runtime state are torn down, the instance is removed from the active mod list, and it can be initialized again. The DLL remains resident until the game process exits.

True managed assembly unload requires a collectible loader context or an unloadable application domain supported by the actual SFS Player runtime, with every reference, callback, thread, Unity object and stack frame released. The deployed Player runtime has not been inspected in this environment, so this implementation does not claim physical assembly unload.

## Validation boundary

The .NET Framework 4.7.2 Release build and package assembly succeed. Actual SFS Player validation is still required for Harmony patch application timing, native Mods menu toggle behavior, disable/re-enable behavior, third-party lifecycle implementations, dependent-mod protection and restoration of Unity/game state.
