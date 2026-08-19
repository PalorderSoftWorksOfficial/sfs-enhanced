# Starter ship designs

Two stock designs to ship with the mod so a fresh multiplayer world has something
fun to fly together immediately.

## Why these are templates, not finished blueprints

SFS build files are JSON exports of the in-game build editor (part IDs, positions,
stack order, fuel amounts, stage groupings). The exact part ID strings depend on
your installed game version and any part packs you have, and I don't have the game
to export real ones. **`blueprint_template.json`** shows the *shape* a build file
needs (this mirrors how SFS's own save/share format works — a flat list of placed
parts with parent links). To get real blueprints:

1. Build the ship in-game using the two designs below as a guide.
2. Use the game's own "Save" (or a save-export mod like `BuildUpgrade`) to get the
   real JSON.
3. Drop that JSON into `Mod/Ships/Blueprints/` and point `ModMain` at it as a
   built-in "starter build" the multiplayer menu can hand out to new players.

## Design 1 — "Kestrel" (staged orbital workhorse)

A 3-stage vertical rocket built for reliability over flash — meant to be the
"safe default" two friends can each fly a copy of without a big skill gap.

- **Stage 1:** wide-body booster, cluster of medium engines, the classic
  "more thrust than sense" first stage — gets you through the thick lower
  atmosphere fast so drag losses stay low.
- **Stage 2:** single efficient vacuum-optimized engine, longer burn, does
  the actual orbital insertion.
- **Stage 3 / payload:** a small crewed capsule + docking-compatible nose,
  RCS thrusters for fine maneuvering — built specifically so it can dock
  with a second Kestrel's payload stage, which is the point: two players
  in two Kestrels can meet in orbit.

## Design 2 — "Osprey" (reusable spaceplane / shuttle-style)

A horizontal-takeoff-styled or belly-lander spaceplane for the players who
want to feel like they're flying something, not just riding a stack.

- Delta-wing profile with a cargo/crew bay large enough to carry a small base
  module — ties directly into the co-op base-building feature (see
  `docs/FEATURE_RESEARCH.md`, item 1): the Osprey is the "moving truck" for
  station parts.
- Airbrakes + reasonable landing gear footprint for horizontal landings back
  on the runway, so it's reusable between multiplayer sessions without needing
  a fresh rocket each time — directly answers the "reusability" ask that shows
  up constantly in the SFS community alongside multiplayer requests.
- Small onboard RCS + docking port on the nose so it can dock to a station a
  group has built, drop off cargo, and fly home.

## Suggested loadout flow for a fresh multiplayer world

1. New players joining a public server via SFS Enhanced get offered "Kestrel"
   or "Osprey" as a starting build (server can send `BuildSpawn` with one of
   these blueprints pre-filled, see `NetServer.HandleWorldJoin`).
2. Once someone wants to build something bespoke, they use the normal in-game
   editor — anything they build gets picked up by `MultiBuildManager` the same
   way, no special-casing needed.
