# Architecture

```
                       TCP, length-prefixed JSON (Shared/Protocol)
   ┌────────────┐   Hello / WorldJoin / BuildStateUpdate / Chat ...   ┌──────────────┐
   │  SFS + Mod │ ───────────────────────────────────────────────▶   │ SFS Enhanced │
   │ (NetClient)│ ◀───────────────────────────────────────────────   │    Server    │
   └────────────┘        BuildSpawn / FriendListResponse / ...       └──────┬───────┘
                                                                              │
                                                                     Server/Persistence
                                                                    (JSON files: worlds/, accounts/)
```

## Why raw TCP + hand-rolled framing instead of a netcode library

- Zero extra NuGet dependency on the mod side. The mod already has to ship as a
  single DLL alongside the game's own assemblies; pulling in a full networking
  library (LiteNetLib, Mirror, etc.) means bundling and version-matching its
  DLL too. A ~120-line framing helper (`Shared/Protocol/NetMessage.cs`) avoids that.
- SFS is not a twitch shooter — sub-16ms latency doesn't matter here, so TCP's
  reliability/ordering guarantees are a feature, not a cost. UDP-based netcode
  libraries earn their complexity in games where every millisecond of jitter
  matters; this isn't that game.
- JSON payloads (via Newtonsoft.Json, which SFS already ships with — see the
  `Mod.csproj` reference list, that DLL is already in `Managed/`) keep every
  packet human-readable, which matters a lot for a mod you'll be debugging
  against decompiled game internals you don't fully understand yet.

## Authority model

The server is authoritative for: world membership, which builds exist, claims,
friends. The server is **not** trying to be authoritative for full physics —
each client simulates its own build locally (same as singleplayer SFS) and
broadcasts position/velocity/rotation deltas via `BuildStateUpdate`. Other
clients interpolate remote builds toward those deltas (`MultiBuildManager.
TickInterpolation`). This is a deliberate trade-off:

- **Pro:** flight feel stays identical to singleplayer SFS — no rewriting the
  physics engine, no fighting Unity's fixed timestep from outside the game.
- **Con:** it's trust-the-client for physics, so a modified client could lie
  about its position. Fine for a community server among friends; if you're
  hosting a fully public server and care about cheating, the next step is
  server-side sanity checks (reject state updates that imply impossible
  accelerations) rather than full server-side physics — flag this as a v2 item.

## Time warp

Per `docs/FEATURE_RESEARCH.md`, time warp is *the* hard problem players
themselves flagged. The protocol has `TimeWarpRequest`/`TimeWarpState` as a
placeholder for this policy: a player can free-warp when no one else in the
world is within some proximity radius of their build; when someone is nearby,
warp is capped/denied so the two simulations don't diverge. The actual
proximity check and warp-multiplier enforcement needs to live in the mod's
Harmony patches over the game's own time-warp code — this repo defines the
protocol shape but the enforcement logic is a `// TODO(game-hook)` since it
depends on exactly how SFS implements warp internally.

## Data model summary

- **WorldRecord** (`Shared/Models/WorldModels.cs`) — one hosted world: a list
  of `BuildSnapshot`s (rockets/bases/rovers/stations, each independently
  owned) and a list of `ClaimInfo`s.
- **BuildSnapshot** — the unit of "a thing in the world." A world with 5
  players and a shared station might have 6 BuildSnapshots: 5 personal
  rockets + 1 shared station (owned by whoever placed the first module, or
  transferable via `BuildOwnershipTransfer`).
- **ClaimInfo** — either wraps one `BuildId`, or a circular region — see
  `Server/Social/ClaimsService.cs` for the interaction check.
- **PlayerAccount** — server-side identity + friends list, keyed by a
  self-issued token (no external auth provider required to stand this up).

## Extending this

- **Voice chat / richer chat:** `ChatMessagePacket` already exists; add a
  UI panel on the mod side.
- **Minigames** (race to the Mun, fastest rocket, etc. — requested per
  `docs/FEATURE_RESEARCH.md`): layer on top as a new packet family
  (`MinigameStart`/`MinigameResult`) plus a `Server/Minigames/` service; the
  world/build/claims plumbing here doesn't need to change.
- **SQLite instead of JSON files:** swap `Server/Persistence/FileStore.cs`'s
  internals; `AccountService`/`WorldManager` only call `Save`/`Load`/`ListIds`,
  so nothing above that layer needs to change.
