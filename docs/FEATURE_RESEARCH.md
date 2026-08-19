# Feature research — what SFS players actually ask for

Sources: SFS Steam community discussions, existing SFS multiplayer mod repos (AstarLC4036,
DrRobotikcs, L4z4r1's Mega-sync approach), App Store reviews, and general "space sim
multiplayer" wishlist threads on comparable games (Reentry, Flight of Nova, KSP) since
those communities overlap heavily with SFS's.

## Recurring requests, distilled

1. **Co-op base/station building.** The single most repeated wish: "collaborate with
   friends to build a space center / station / Mars base together." This is *the* headline
   feature this mod should nail — hence world-shared multi-build support as a first-class
   citizen, not an afterthought.
2. **A real server, not "sync two save files."** Every existing SFS multiplayer mod today
   works by two clients repeatedly overwriting each other's local save (or, in one case,
   literally uploading the save to a Mega.nz folder and polling it). That's why this project
   leads with an actual dedicated server holding authoritative world state.
3. **Persistent worlds you can leave and rejoin**, rather than session-only multiplayer —
   people want their station still there tomorrow.
4. **Minigames / structured play**, e.g. "first to the Mun wins," fastest-rocket challenges,
   career-style missions with a shared leaderboard — social systems beyond just "see each
   other's rocket."
5. **Protecting your stuff.** Once builds are shared, "can someone else blow up my rocket"
   becomes the immediate next question — hence claims (per-build or per-region ownership)
   from day one, not bolted on later.
6. **Friends lists / private servers**, not just open public lobbies — people want to invite
   specific friends and control who's on their world.
7. **Easy world sharing** — upload a world so friends (or the public) can download and fly
   in it, similar to how planet packs are already shared as files in this community.
8. **More parts / bigger build volume / new stock designs** — frequently requested
   alongside multiplayer, since people immediately want cooler things to fly together.
9. **Time-warp desync** is explicitly called out by players as *the* hard problem for SFS
   multiplayer (time warp + physics-heavy simulation + multiple players). This shaped the
   architecture decision below.

## Design decisions this drove

- **Server-authoritative world, client-predicted flight.** Time warp is per-player-locked
  while others are near a shared build/region (matches what players on the SFS forums
  themselves guessed would be necessary), full free time-warp when alone.
- **World = many builds, not one rocket.** The data model treats a "World" as a bag of
  independently-owned `BuildInstance`s (rockets *and* stationary bases), which is what lets
  multiple people build a station together and is also exactly the "multiple builds loaded
  in one world" feature you asked for.
- **Claims are per-build by default**, with an optional radius claim for a landing site/base,
  so people can protect a station without needing to understand a chunk-grid system.
- **Friends are a server-side concept**, not a code you paste in — add a friend once, then
  see them online, invite them to *any* world you host, matching what's asked for above.
