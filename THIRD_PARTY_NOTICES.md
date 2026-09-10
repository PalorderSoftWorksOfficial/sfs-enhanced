# Third-party notices

SFS Enhanced is an independent project. It uses the SFS modding APIs and incorporates or studies open-source work from other SFS projects.

## AstroTheRabbit

The following repositories are reference sources for architecture, UI patterns, implementation techniques, and feature parity:

- Enhanced-UX-Mod-SFS
- Aero-Trajectory-SFS
- FSI-Info-Overload-Mod-SFS
- Smart-SAS-Mod-SFS
- Custom-Part-Creator-Mod-SFS
- Multiplayer-SFS
- Custom-Save-Data-SFS
- Part-Text-Mod-SFS
- Stages-Expanded-Mod-SFS
- Forceful-Fuel-Tanks-SFS
- World-Build-SFS

AstroTheRabbit's Enhanced UX project is published under GPL-3.0 and includes a full GPL license file. The Multiplayer SFS repository also includes a GPL license file. These projects are therefore treated as GPL-covered upstream sources when code is reused.

Upstream source repositories:

- https://github.com/AstroTheRabbit/Enhanced-UX-Mod-SFS
- https://github.com/AstroTheRabbit/Aero-Trajectory-SFS
- https://github.com/AstroTheRabbit/FSI-Info-Overload-Mod-SFS
- https://github.com/AstroTheRabbit/Smart-SAS-Mod-SFS
- https://github.com/AstroTheRabbit/Custom-Part-Creator-Mod-SFS
- https://github.com/AstroTheRabbit/Multiplayer-SFS
- https://github.com/AstroTheRabbit/Custom-Save-Data-SFS
- https://github.com/AstroTheRabbit/Part-Text-Mod-SFS
- https://github.com/AstroTheRabbit/Stages-Expanded-Mod-SFS
- https://github.com/AstroTheRabbit/Forceful-Fuel-Tanks-SFS
- https://github.com/AstroTheRabbit/World-Build-SFS

## Reuse policy

SFS Enhanced does not claim authorship of upstream code. When upstream code is copied or materially adapted, the relevant source, author, license, and modification status must remain identifiable in this file or in a source-level notice.

Feature ideas and general UI patterns are not presented as copied code. Independently implemented functionality remains SFS Enhanced code.

## GPL compliance

Where GPL-covered code forms part of SFS Enhanced, the resulting covered work is distributed under GPL-3.0. The repository includes the corresponding license text and source needed to exercise the rights granted by that license.

## SFSPlayer-sys Spaceflight Simulator MultiplayerMod

Repository: https://github.com/SFSPlayer-sys/Spaceflight-Simulator-MultiplayerMod

License: GPL-3.0.

The project is used as an implementation reference for multiplayer world state, packet design, interpolation, host/join flows, server UI, and Harmony patch organization. Any directly adapted source is treated as GPL-covered code and remains subject to the applicable GPL-3.0 terms.

## cucumber-sp UITools

Repository: https://github.com/cucumber-sp/UITools

The repository describes UITools as a reusable SFS UI dependency and provides builders, closable windows, position persistence, numeric inputs, and button helpers. No license file was exposed in the repository tree inspected for this project. SFS Enhanced therefore uses its public API concepts and UI architecture as inspiration rather than copying the repository wholesale.

## Lidgren Network

Library: AscensionGameDev.Lidgren.Network (NuGet), upstream https://github.com/lidgren/lidgren-network-gen3

License: MIT. Used as the UDP transport under both the dedicated server and the game mod. The MIT license text is reproduced in the packages consumed from NuGet; SFS Enhanced claims no authorship of Lidgren.

## 105-Code MorePartsMod

Repository: https://github.com/105-Code/MorePartsMod

License: Apache-2.0.

MorePartsMod is an example of the type of SFS mod package that SFS Enhanced's package installer is designed to consume. Its code is not bundled into SFS Enhanced.

## Neptune-Sky SFSVanillaUpgrades

Repository: https://github.com/Neptune-Sky/SFSVanillaUpgrades

The upstream README requests credit for reused code. SFS Enhanced integrates independently adapted Vanilla Upgrades functionality and credits NeptuneSky for the upstream implementation and feature design. The integrated layer currently covers extended camera limits, extended offline physics timewarp, accurate TWR, extended units, throttle display accuracy, and torque control.

SFSPlayer-sys/Spaceflight-Simulator-MultiplayerMod
The archived multiplayer project is GPL-3.0. SFS Enhanced uses its stable multiplayer architecture and packet/lifecycle concepts as a reference and adapts compatible portions under the project GPL-3.0 licensing.
