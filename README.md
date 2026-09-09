# SFS Enhanced

SFS Enhanced is a large-scale quality-of-life and multiplayer platform for Spaceflight Simulator. The project combines a native SFS mod, a standalone cross-platform server, public server discovery, persistent shared worlds, social systems, creator tooling, and gameplay extensions.

The project is designed around the native SFS modding APIs and a clean separation between game-side code and server-side code.

## What it includes

- First-class Multiplayer entry in the SFS Play menu
- Modal multiplayer UI that follows native SFS menu behavior
- Direct server connection with optional port override
- `_sfs._tcp` SRV discovery when no port is supplied
- TLS-protected multiplayer transport
- Persistent server-issued player authentication tokens
- TLS certificate fingerprint pinning and first-connection trust
- Public server directory and server browser
- Persistent shared worlds
- Multiple independently synchronized builds per world
- Build ownership and region/build claims
- Friends, requests, invitations, and world presence
- World chat and server information
- World upload and download support
- Time-warp arbitration
- Windows and Linux dedicated-server support

## Project status

| Area | Status |
| --- | --- |
| Multiplayer menu | Active development |
| Server discovery | Implemented |
| SRV endpoint resolution | Implemented |
| TLS transport | Implemented |
| Persistent authentication | Implemented |
| Shared worlds | Implemented foundation |
| Multi-build synchronization | Implemented foundation |
| Friends and invites | Implemented foundation |
| Claims | Implemented foundation |
| Chat | Implemented foundation |
| Creator systems | In development |
| Advanced SFS QoL systems | In development |
| Full feature parity with referenced mods | Roadmap |

## Multiplayer UX

The multiplayer entry belongs to the Play flow rather than floating independently over the home screen. The multiplayer window is opened through SFS's screen system, so the underlying Play menu is no longer left active behind a second unrelated window.

Secondary multiplayer pages are treated as pages of the same flow. Server browsing, world browsing, friends, chat, and world tools are designed to replace the front page rather than stack small windows on top of it.

The connection form accepts a normal hostname. A port override is optional. Without one, SFS Enhanced first attempts `_sfs._tcp.<hostname>` SRV discovery and falls back to the default multiplayer port.

## Security

Multiplayer traffic is not sent as plaintext TCP. The server creates or loads a TLS certificate and the client establishes a TLS session before the SFS Enhanced protocol begins.

Player authentication uses persistent opaque server-issued tokens. Tokens are stored server-side as SHA-256 hashes and are transmitted only inside the TLS connection.

The client supports certificate fingerprint pinning. Public directory listings can publish the server certificate fingerprint so a client can verify the endpoint it connects to. Direct connections use first-connection trust when no fingerprint has previously been configured.

This security model is intentionally separate from gameplay authorization. The server remains authoritative for worlds, builds, claims, player identity, and social actions.

## Architecture

```text
SFS Enhanced
├── Mod
│   ├── native SFS integration
│   ├── multiplayer UI
│   ├── networking client
│   ├── synchronized build systems
│   └── creator and gameplay modules
├── Shared
│   ├── protocol
│   └── shared models
├── Server
│   ├── TLS networking
│   ├── accounts
│   ├── worlds
│   ├── builds
│   ├── claims
│   ├── friends
│   └── chat
├── Directory
│   └── self-hostable public server registry
└── scripts
    └── builds and integration tests
```

The dedicated server targets .NET 8 and does not load Unity or Spaceflight Simulator assemblies. The same server architecture is intended to run on Windows and Linux.

## Dedicated server

Windows:

```powershell
dotnet run --project Server -- --port 7777 --name "My SFS Server" --data ./data
```

Linux:

```bash
dotnet run --project Server -- --port 7777 --name "My SFS Server" --data ./data
```

Self-contained publishing is available through the existing Windows and Linux publishing scripts.

A server can advertise through a self-hosted directory with `--advertise`, `--directory`, `--public-host`, and `--region`.

The server generates a TLS certificate at first launch when one is not supplied. The generated certificate fingerprint is printed to the server console and can be published with the server listing.

## Development workflow

SFS Enhanced follows a concrete implementation workflow:

1. Implement the next feature completely.
2. Inspect the affected code and surrounding systems.
3. Build every affected target.
4. Run integration or smoke tests.
5. Fix every discovered issue.
6. Rebuild and retest.
7. Only then begin the next feature.

Repository inspection, builds, and verification are performed against the actual development checkout.

## Design references and open-source work

The project takes substantial design and implementation inspiration from established open-source SFS mods, particularly AstroTheRabbit's work. The referenced projects include Enhanced UX, Multiplayer SFS, Aero Trajectory, FSI Info Overload, Smart SAS, Custom Part Creator, Custom Save Data, Part Text, Stages Expanded, Forceful Fuel Tanks, World Build, and related SFS tooling.

AstroTheRabbit's Enhanced UX repository is GPL-3.0 licensed, and the Multiplayer SFS repository is also distributed with a GPL-3.0 license. Those licenses permit reuse under their stated conditions and require corresponding licensing obligations for derivative GPL-covered work.

Code is only incorporated where its license permits the intended reuse, and reused code is tracked in `THIRD_PARTY_NOTICES.md`. Features that are merely inspired by another project are implemented independently rather than represented as copied source.

## Feature roadmap

The long-term goal is to consolidate useful capabilities from the referenced SFS ecosystem into one coherent project instead of producing a collection of disconnected menus and systems.

Planned modules include:

- Enhanced UX and settings infrastructure
- Advanced trajectory and flight information
- Smart SAS modes and targeting helpers
- Expanded staging controls
- Custom part authoring and part-data editing
- Persistent custom save data for modded builds
- Advanced part text tooling
- World-build and creator workflows
- Forceful fuel-tank behavior and other construction QoL features
- Performance and loading optimizations
- Expanded multiplayer moderation and administration
- Server permissions, moderation, and account management
- Blueprint and creator sharing

Each module will be integrated into the shared SFS Enhanced architecture instead of being added as an isolated feature dump.

## Repository layout

```text
sfs-enhanced/
├── Mod/                     SFS game-side mod
├── Shared/                  shared protocol and models
├── Server/                  standalone dedicated server
├── Directory/               public server directory
├── scripts/                 build and integration tooling
├── docs/                    architecture and development notes
├── THIRD_PARTY_NOTICES.md   third-party attribution and license notes
└── LICENSE                  project license
```

## License

SFS Enhanced is distributed under the GNU General Public License v3.0 where GPL-covered upstream code has been incorporated. See `LICENSE` and `THIRD_PARTY_NOTICES.md` for the licensing and attribution record.
