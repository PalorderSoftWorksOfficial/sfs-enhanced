# SFS Enhanced Community Priorities

## Highest priority

1. Multiplayer server browser with ping, country, capacity, favorites, direct connect, private servers, and community servers.
2. Reliable shared simulation with docking, vessel ownership, synchronized staging, and explicit time-warp arbitration.
3. Local split-screen and multi-vessel control, starting with two controlled rockets and expanding when the camera architecture allows it.
4. First-party blueprint discovery with search, categories, creator profiles, previews, ratings, favorites, download counts, and DLC/mod requirement filters.
5. Easier community distribution through self-hosted servers and discoverable community content.

## Strong follow-on features

6. Mission and challenge system for cooperative and competitive scenarios.
7. Shared space-station and colony construction permissions.
8. Server moderation with owner/admin/moderator roles, bans, mutes, reports, audit logs, and claim management.
9. Server-side persistence and recovery for worlds and builds.
10. Optional creator and server verification.
11. Mod-pack and dependency discovery integrated with community content.
12. Better docking, navigation, rendezvous, and flight-assistance tools exposed as optional Enhanced modules.

## Design rules

SFS Enhanced should treat multiplayer, content sharing, hosting, split-screen, and future integrated systems as one platform rather than unrelated menu additions.

Dedicated servers remain authoritative for persistent multiplayer state. The game client remains responsible for rendering and local input, with simulation ownership made explicit for every synchronized vessel.

Community servers must be clearly distinguished from official or verified servers. Direct-IP servers must remain usable without requiring the central directory.

Steam identity should become the canonical PC identity, but secure authentication must use a verifiable Steam authentication ticket rather than trusting a client-supplied SteamID alone.
