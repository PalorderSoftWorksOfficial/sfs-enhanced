# SFS Enhanced — Product Vision and Agent Operating Brief

## Product identity

SFS Enhanced is intended to become the all-in-one multiplayer and community platform for Spaceflight Simulator. It is not merely a multiplayer mod with a collection of unrelated features. The target experience combines the coherent identity, discovery, and user-generated-content ecosystem of Roblox; persistent multiplayer communities and rooms found in established multiplayer games; and a creator library comparable to Steam Workshop, integrated naturally into Spaceflight Simulator.

The current user experience is below expectation. The current interface is cluttered, navigation is fragmented, screens feel disconnected, and the platform lacks first-class profiles, rooms, and a usable public creation-sharing system. Do not solve this by adding more buttons or only applying a visual reskin. Rethink the information architecture, user journeys, platform boundaries, and design system from first principles while preserving working multiplayer functionality.

## Core platform pillars

### 1. Identity and accounts

- Persistent unique account identity independent of the server currently joined.
- Secure registration, login, session lifecycle, stable player ID, and username/display name.
- Public player profiles with avatar/profile customization, biography, statistics, activity, friends/followers, and published creations.
- Privacy settings, blocking/reporting, account settings, and safe token/session handling.
- Evaluate existing authentication and persistence before changing it. Never store plaintext credentials or expose secrets.

### 2. Home and discovery

- A useful landing page with recent/favorite rooms, friends online, featured/public rooms, popular/recent creations, activity, and clear primary actions.
- Search, categories, sorting, filters, pagination, and proper loading, empty, and error states.
- Make it easy to understand where to go and what can be done without reading instructions.

### 3. Rooms

- Rooms are first-class persistent multiplayer spaces, not simply a raw list of worlds.
- Public, private, and invite-only visibility; ownership; roles and permissions; player limits; descriptions; categories/tags; thumbnails; discovery; invites/shareable join identifiers; persistence; moderation; reconnect behavior; room chat/presence; and room-specific settings.
- Distinguish room metadata and lifecycle from the actual SFS world/save.
- Support dedicated public servers and private/community-hosted servers. Directory discovery must not imply the directory hosts the game world.

### 4. Creator library / Workshop

- Upload the user's own SFS saves/worlds as public, unlisted, or private creations.
- Metadata: title, description, screenshots/thumbnails, tags, categories, creator attribution, compatibility/game version, and asset type.
- Version history and update/publish workflow; downloads; favorites; ratings; reporting/moderation; browse/search; and safe download/import into local SFS saves.
- Treat uploaded worlds, saves, blueprints, planet packs, and scenarios as distinct asset types with validated formats, limits, and clear ownership/visibility.
- A published save is not automatically a live room. Users can create a room from a creation and optionally publish a room's save as a creation.
- Never expose arbitrary server filesystem paths or trust uploaded files.

### 5. Social

- Profiles, friend requests, friends list, online presence, invites, chat, notifications, blocking/reporting, and privacy controls.
- Integrate social actions into room and creation flows rather than scattering disconnected panels.

### 6. Server browser and hosting

- Discover servers and show accurate online status, player counts, version, region, and latency where available.
- Favorites, direct connect, server details, create/manage room on a server, and clear distinction between official directory and community servers.

### 7. Native SFS integration

- Preserve native SFS build/save workflows, rocket/base/rover/station synchronization, world time, timewarp voting, claims/permissions, secure networking, and headless dedicated-server support.
- UI must be usable within actual Unity/SFS screen dimensions and controls, not web-only assumptions.

### 8. Platform quality

- Administration/moderation, observability, settings, localization readiness, performance, reliability, security, accessibility, and privacy are platform-wide concerns.

## Design direction

Create a professional, premium-feeling spaceflight community product. Use strong information hierarchy, restrained dark space-inspired surfaces, high-contrast readable text, consistent spacing and typography, clear primary/secondary/destructive actions, reusable cards and lists, meaningful icons, deliberate empty states, responsive layouts within actual game screen sizes, consistent back/navigation behavior, and clear connection/account state.

Use restrained accent colors and real SFS/space imagery only where available and appropriate. Avoid generic AI gradients, random decorative panels, emoji UI, giant forms, inconsistent floating windows, and fake functionality. The experience must be fast to understand and easy to navigate.

Use the actual SFS UI toolkit and Unity APIs available in this repository. Do not assume a browser or modern UI framework exists in-game. If the native UI toolkit is limited, create reusable components/adapters rather than introducing an incompatible web UI.

Design tokens, geometry and component rules live in `DESIGN_SYSTEM.md`; screen states and interaction paths live in `UX_FLOWS.md`. This document states intent, not capability: `PRODUCT_ARCHITECTURE.md` states what exists today, and `IMPLEMENTATION_PLAN.md` sequences the rest.

## Architecture and engineering constraints

- Repository: `/home/administrator/sfs-enhanced`.
- Read `agent.md`, `README.md`, and the existing docs before implementation.
- Verify the actual code and tests rather than assuming the handoff is perfectly current.
- Existing layout includes `Shared/`, `Server/`, `Directory/`, `Mod/`, `Assembly-CSharp/`, `Dependencies/`, and multiplayer integration scripts.
- Existing handoff describes native ModLoader integration (no Harmony), Lidgren UDP, application-layer P256 ECDH/HMAC/AES-CTR/HMAC secure sessions, accounts/friends/claims, multi-room worlds, upload/download, build sync, world time, and timewarp. Validate each claim against implementation.
- `Assembly-CSharp/` and `Dependencies/` are reference/build inputs only and must not be redistributed in releases.
- Keep dedicated server hosting self-hostable. Be honest about what belongs in the game mod versus a hosted account/directory/creator service. Evaluate operational costs before introducing external dependencies.
- Protocol changes require explicit versioning/migration strategy, validation, size/rate limits, authorization, and integration tests.

## Team model and manager responsibilities

The primary Freebuff model is the manager and single decision-maker. It owns the product outcome, assigns bounded work to specialist agents, synthesizes findings, resolves conflicts, reviews changes, decides priorities, routes bugs, and handles work that cannot be delegated cleanly.

Use Freebuff's actual native specialist/subagent capabilities where available. Assign distinct responsibilities:

1. Product planner / systems architect: read-only repository audit, capability map, architecture, dependencies, phased roadmap, acceptance criteria, migration plan.
2. UX/UI designer: inspect real SFS/Unity UI constraints and current screens; create screen-by-screen information architecture, interaction flows, reusable design system, states, and implementable specifications.
3. Developer / implementer: implement one bounded task at a time, with clear file ownership and acceptance criteria. Avoid overlapping edits.
4. Independent tester / QA: independently inspect diffs, build all projects, run integration tests, verify security/protocol/data migration, reproduce bugs, and report prioritized actionable defects. The tester should not silently change the code it is reviewing.
5. Security reviewer: review identity, sessions, uploads, authorization, moderation, rate limits, replay/tamper protections, path traversal, privacy, and resource exhaustion.

If Freebuff has no suitable dedicated designer or planner subagent, the manager should assign the closest available specialist a precise role-specific brief. Do not claim a team role ran unless it actually did. Use parallel agents only for independent tasks. Keep file ownership separated; use worktrees/branches if supported and safe.

## Workflow rules

1. Audit first. Read relevant code, protocol, persistence, docs, UI toolkit constraints, and tests before proposing broad changes.
2. Maintain actionable project docs: `PRODUCT_VISION.md`, `PRODUCT_ARCHITECTURE.md`, `DESIGN_SYSTEM.md`, `UX_FLOWS.md`, `IMPLEMENTATION_PLAN.md`, `AGENT_ROLES.md`, `QA_PLAN.md`, and `BUGS.md`. Reuse existing docs where appropriate instead of creating duplicate competing documents.
3. Prioritize complete vertical slices. Establish a coherent in-game application shell/navigation and account/profile foundations first, then rooms and creator publishing/discovery, then deeper social/community features. Adjust sequencing based on the real architecture.
4. Each task assignment must state scope, owned files, prerequisites, deliverables, acceptance criteria, and verification commands.
5. Manager reviews every plan and implementation; never blindly accept agent output.
6. QA should run `dotnet build SFSEnhanced.sln`, the multiplayer integration harness (`dotnet run --project scripts/multiplayer-integration` against a correctly configured test server), and Linux publish checks where feasible. Verify actual game UI where possible. Never claim a test passed unless it was run and confirmed.
7. Maintain `BUGS.md` with severity, reproduction steps, expected/actual behavior, affected files, owner, status, and regression test. Tester reports bugs to manager; manager routes fixes and requests retest.
8. No fake buttons, placeholder screens presented as complete, mock production data, unimplemented API contracts, or false completion claims. If a backend is missing, implement a complete vertical slice or explicitly document the dependency.
9. Preserve existing features and wire compatibility where possible. Do not delete user data, secrets, server data, or functionality. Never commit credentials.
10. Do not push, deploy, or release without explicit approval. Do not force-push. Local coherent commits are optional; prioritize verified changes.
11. Follow repository conventions. Avoid code comments unless required by an existing API or language construct; explain behavior in docs.
12. After each slice: build, test, inspect diff, obtain independent QA review, fix defects, retest, then continue.
13. Make documented, reversible decisions for ordinary ambiguities. Ask only when a decision materially changes product direction, adds cost, requires credentials, or risks destructive behavior.
14. Maintain a live manager status: completed, in progress, blocked, next, agent assignments, test evidence, and open decisions. Report meaningful milestones and blockers rather than routine questions.

## First assignment

Discover Freebuff's actual orchestration and subagent capabilities and current model availability. Use the primary model as manager. Dispatch parallel read-only work for product/codebase audit, UI/design constraints, and QA/test readiness; then synthesize findings into one architecture and prioritized implementation plan. Do not spend the whole task planning: once architecture and first slice are clear, implement the first complete vertical slice establishing coherent in-game navigation/application shell and foundations for persistent account/profile UX without breaking connect/world/friends/chat flows. Inspect the actual SFS UI toolkit and decompiled source before changing UI. Build, test, obtain independent QA review, fix defects, and retest. Report actual agent roles/models used, files changed, tests run/results, and next queued work.
