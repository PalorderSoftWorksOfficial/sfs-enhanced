# SFS Enhanced Roadmap

SFS Enhanced is being built as a platform layer for Spaceflight Simulator rather than a single multiplayer feature.

## Platform

- First-class Multiplayer button on the SFS home screen
- Public server browser
- Direct server connection
- Player-hosted dedicated servers
- Self-hostable server directory
- Server advertising and heartbeat
- Persistent player identity
- Persistent friends
- Shared worlds
- Multiple independent builds per world
- Build ownership and claims
- World upload and download
- World invitations
- Server MOTD and player counts

## Multiplayer simulation

- Smooth remote rocket interpolation
- Authoritative build ownership
- Authoritative pilot control
- Time-warp arbitration
- Server-side validation of world, build, and claim access
- Docking and joint-build synchronization
- Debris synchronization
- Remote staging and throttle state
- Recovery and respawn handling
- Server-side anti-spoof validation

## SFS expansion

- Mission system
- Contracts and objectives
- Multiplayer race events
- Launch and landing competitions
- Server events and scheduled scenarios
- Player achievements and statistics
- Shared space-station construction
- Persistent orbital infrastructure
- Resource and logistics systems
- Server economies as an optional module
- Faction and alliance systems
- Territory and planetary-base systems
- Server moderation tools
- Server permissions and roles
- Ban, mute, kick, and whitelist controls
- Audit logs

## Creator platform

- Shareable rocket blueprints
- Server-hosted blueprint libraries
- Featured community builds
- Build versioning
- Shared mission templates
- Custom server rulesets
- Optional gameplay modules
- Server-side configuration packs

## Technical direction

- Keep Shared independent of Unity
- Keep the dedicated server independent of SFS installation
- Keep game integration on the native SFS ModLoader
- Treat the server as authoritative for persistent multiplayer state
- Keep the protocol versioned
- Validate every client-controlled identifier on the server
- Make directory services self-hostable
- Keep public infrastructure optional so private communities can operate independently

## Current implementation focus

1. Multiplayer main-menu entry and platform UI
2. Public server directory and server discovery
3. Reliable player-hosted servers
4. Robust world and build synchronization
5. Persistent multiplayer social systems
6. Docking and simulation synchronization
7. Missions, events, and creator systems
8. SFS gameplay enhancement modules
