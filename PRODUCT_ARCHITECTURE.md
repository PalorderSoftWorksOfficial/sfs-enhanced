# SFS Enhanced — Product Architecture

Status: describes the tree as it exists after the first vertical slice (in-game app shell +
per-server profile foundation). It is not a target-state design document.

## 1. Shape of the product

SFS Enhanced is a Spaceflight Simulator mod plus a dedicated server. The mod adds multiplayer to
the stock game; the server owns world state, builds, claims, time warp, chat, friends and accounts.

Three deployables, one protocol:

| Piece | Project | Runtime | Role |
| --- | --- | --- | --- |
| Mod client | `Mod/` | .NET Framework 4.7.2, Unity/Mono | Connects, renders the app shell, mirrors server state into the scene |
| Dedicated server | `Server/` | .NET 8 console | Authoritative world/social state, persistence, auth |
| Directory | `Directory/` | .NET 8 | Optional public server list |
| Shared | `Shared/` | netstandard2.0 | Packet DTOs, world/account models, crypto helpers |

`scripts/multiplayer-integration` is a headless two-client harness that exercises connect, auth,
world create/join, build sync, claims, chat, friends and profiles against a live server.

## 2. Transport and protocol

Lidgren UDP with a JSON envelope per packet. `Shared/Protocol/PacketType.cs` is the single
registry; `Shared/Protocol/Packets.cs` holds the DTOs. Packet numbers are grouped by domain
(connection, discovery, world lifecycle, builds, time warp, social, claims, chat, profile, errors).

Authentication is a mutual challenge/response:

1. `Hello` carries protocol version, player name, nonce and mode (`token` or `register`).
2. `HelloAck` returns the server challenge and exchange identifier.
3. `AuthChallenge` / `AuthResponse` exchange ephemeral and static **P-256** points (not X25519) plus
   an HMAC proof over the transcript.
4. `AuthResult` returns `PlayerId`, session id and a bearer token to persist.

The client verifies the server's identity between steps 3 and 4: it recomputes the fingerprint from
the validated static point and compares it to a per-endpoint pin. An unpinned or changed identity
stops the handshake before the token proof or registration secret is sent, so an untrusted endpoint
never receives a credential MAC and no account is created on it. See §5.1.

Signing in and registering are distinct wire modes. `ModeToken` authenticates an account that already
exists on that server and never creates one; an unknown name returns `unknown_account`. `ModeRegister`
is sent only by an explicit user action, and the server creates the account only when registration is
open and, if configured, the registration password proof is valid. The client never falls back from
signing in to registering, and every rejection carries a machine-readable code
(`Shared/Protocol/Packets.cs`, `AuthErrorCode`) so the UI can name the actual problem.

A registration password is never sent in the clear. When a server requires one, `HelloAck` marks it,
and after the client has validated the server identity key and derived the session channel, the server
sends a sealed `RegistrationPrompt` (`PacketType.RegistrationPrompt`) carrying its per-instance salt.
Only a peer holding the static private key can produce that sealed frame, so the client answers with a
sealed `RegistrationAuth` HMAC over the password hash and transcript. The salt lives in
`data/system/registration_salt.json`. This removes passive capture and cross-server precomputation; it
is not a PAKE, so a server that verifies the password can still test guesses offline.

Consequences that matter for UI work: the client is not "logged in" in any global sense. A
`PlayerId` is scoped to one server's account store. There is no cross-server identity, no friends
graph that spans servers, and no directory-level account.

## 3. Server

`Server/Program.cs` parses config, loads the file store, starts the transport and runs a console
command loop (`list`, `worlds`, `status`, `stop`). The loop treats end-of-input as shutdown, so
`< /dev/null` is a valid non-interactive run.

`NetServer` owns per-connection state (`ClientConnection`), session counting, world hosting and
fan-out. Handlers are split by domain, with `NetServer.Profile.cs` added by this slice.

Persistence is a file store (`Server/Persistence/FileStore.cs`) keyed by account and world id;
`AccountService` handles registration and token verification. No database is involved.

Transport pacing: `LidgrenServerTransport.PumpAsync` uses an adaptive idle backoff (2 ms while
there is traffic, ramping to 10 ms when idle), which cut steady-state idle CPU from ~118% to ~9%.

Rate limiting exists for profile updates (5 burst, then 1 per second per connection). Most other
packet families are still unthrottled; that is tracked as a gap, not as a feature.

Social rules enforced server-side:

- `FriendsService` only reports presence when `ShowPresence` is set, and only accepts new requests
  when `AllowFriendRequests` is set.
- `ProfileService` validates lengths (display name 32, bio 280), accent format (`#RRGGBB`), strips
  control characters, zero-width/bidi characters and angle brackets, and filters `Online`,
  `LastSeenUtc` and `CurrentWorldId` for non-self viewers.
- Profile updates are self-only; there is no packet that edits another account.

## 4. Client mod

`Mod/ModMain.cs` constructs the subsystems: `NetClient`, `MultiBuildManager` (world builds and
interpolation), `LocalRocketFleetSync`, `FriendsUI`, `MultiplayerMenu`, `PackageManager`. A
`ModLoop` component pumps incoming packets, ticks deferred UI work, interpolates builds and binds
F8 to the shell.

`NetClient` owns the socket, handshake, token storage, packet dispatch and the `SelfProfile` /
`ProfileChanged` / `Disconnected` surface used by the UI.

### 4.1 UI shell

The multiplayer UI is a single window (`AppShell`) inside a `BasicMenu` screen, not a pile of
independent mod buttons.

```
ScreenManager → MultiplayerScreen (BasicMenu, Escape/close aware)
                     ↓ BuildWindow
                 AppShell (window, nav rail, header, chrome, content)
                     ↓ Build / Update
                 AppPage implementations (Home, Servers, Worlds, Friends, Chat, Profile, Settings)
```

Two notification channels drive rendering:

- `AppState.Changed` → `AppShell.RenderChrome()` — header, nav highlight, status line, presence.
  Cheap, text only.
- `AppState.ContentChanged` → `AppShell.UpdateContent()` — updates the active page.

Pages build their controls exactly once per route entry. `AppPage.Update` mutates existing labels
and buttons in place; anything that can change shape lives in its own keyed region (`UiKit.Region`
plus `UiKit.Clear`) that is rebuilt only when its key changes, and `NeedsRebuild` triggers a full
page rebuild only for structural changes (connection state, presence of a loaded profile). Chat is
append-oriented: incoming lines are added as new labels, and once the visible window fills the log
labels alone are redrawn on a sliding 12-line window (`Shared/Models/ChatLogWindow.cs`, covered by
assertions in the integration harness, including the capped-log case where `AppState` holds 60 lines
and only the newest line's sequence changes). The input field lives outside the log region, so its
text and caret state are never touched.

Navigation keeps a bounded page history. Escape (and `MultiplayerScreen.Close`) pops that history
before closing; F8 and scene unload use `ForceClose()`, which bypasses it.

The shell also has to respect `ScreenManager`'s own stack. `OpenScreen` stores a `Func<Screen_Base>`
and invokes it again when that entry is popped back to, so the holder and screen must outlive being
covered. `MultiplayerScreen.OnClose` therefore only disposes the `AppShell` and deactivates the
holder; the holder is destroyed in `OnScreenClosed`, which runs only once the stack entry is gone.
`Hide()` distinguishes two cases: when the shell is `CurrentScreen` it force-closes (popping its own
entry); when a child screen is above it, it calls `ScreenManager.main.CloseStack()` to reset to the
initial screen before destroying the holder, so no stale lambda can reopen a destroyed screen. F8
does nothing while another screen is current; the player closes that screen first. Screen stack
counts are logged around open and hide for the in-game probe. `AppShell.Dispose` removes both
`Changed` and `ContentChanged` handlers, and `DestroyWindow` nulls the shell reference, so cleanup
is idempotent.

Mounting: `ScreenManager` needs a canvas. `MultiplayerMenu.CreateScreenHolder` tries
`Builder.SceneToAttach.BaseScene` (`Base_PC`/`UI`) first and falls back to
`Builder.SceneToAttach.CurrentScene` (`--- UI ---`); an unparented holder is destroyed and the
shell refuses to open rather than drawing invisibly. **This path is compile-verified only. It has
not been observed in a running game, and is a hard release gate.**

In-flight behaviour: the shell refuses to open in `World_PC` and F8 is gated there, because a
pausing `Screen_Menu` freezes the simulation and removes flight input. A non-pausing overlay is
future work, not a shipped feature.

## 5. Profile slice (what actually exists)

Per-server account fields added to `PlayerAccount`: display name, bio, avatar accent, privacy
flags, `MemberSinceUtc`, `LastSeenUtc`, `SessionsCount`, owned-world and friend counters.

Packets 80–83: `ProfileRequest`, `ProfileResponse`, `ProfileUpdate`, `ProfileUpdateResult`.

Client flow: connect → `AuthResult` → profile request → `SelfProfile` populated → `ProfileChanged`
→ shell chrome and Profile page update. Editing writes through `UpdateProfileAsync`; the server
returns the sanitized profile, which replaces the local copy (write-then-reflect, no optimistic
edits).

Player name is edited locally in Settings and is the account key on the server; it takes effect on
the next connection, not on the current session.

## 5.1 Server identity trust (Gate 2)

- The server keeps a long-lived P-256 identity key in `data/system/server_static_key.json` and prints
  its fingerprint at startup and in `status`.
- `ServerIdentityCalculator.FromStaticPoint` requires exactly 32-byte on-curve coordinates and
  recomputes `SHA-256("SFSEnhanced-ServerIdentity-v1" ‖ X ‖ Y)`. The `IdentityFingerprint` field in
  `AuthChallenge` is attacker-writable and is never used. A missing, short or off-curve static key is
  a hard failure, not a zero-secret session.
- Pins (`Shared/Networking/ServerIdentityPinStore`, PlayerPrefs key `sfs_enhanced.pins`) are keyed by
  the canonical user-supplied authority: `host:port` when a port was given, `host#auto` when the port
  comes from SRV. Normalization lowercases, strips one trailing dot, applies IDNA, brackets IPv6 and
  rejects invalid labels. The resolved SRV target is recorded for display only and never defines the
  key, so failover does not fake an identity change and a hostile resolver cannot pick a pin bucket.
- States are `Missing` (trust dialog, connect blocked), `Match` (proceed, refresh last-verified) and
  `Changed` (refuse; replacing the pin is a separate confirmed action that records the previous
  fingerprint). A pin is written only by explicit user acceptance of the fingerprint shown.
- Tokens are per endpoint (`sfs_enhanced.tokens`). The legacy global token migrates once, and only to
  the canonical key of the most recent successful `ServerHistory` entry; if that cannot be derived the
  token is kept and attached to nothing, and Settings shows a notice.
- Endpoint storage writes are staged: `StagedEndpointStorage` writes a `…pending` value, verifies the
  readback, commits, verifies again, and clears the staged key. A dropped write is reported as failure
  and leaves the previous document intact.

Honest limits: trust-on-first-use cannot detect an attacker present on the very first connection.
The UI says "trusted on this device", never "verified", and instructs out-of-band comparison of the
fingerprint the server console prints. Pins live in plaintext PlayerPrefs, so they defend against
network attackers, not against a compromised local machine.

## 6. Truthfulness rules baked into the UI

- "Worlds" is the transitional label. A world is currently both the live save and the discovery
  entry. Rooms, creations, permissions, capacities and roles do not exist and are not shown.
- Profiles are per-server. No screen claims a platform-wide account.
- Lists show real counts and disclose truncation ("Showing 3 of 12 servers", "latest 12 messages").
- Empty and offline states are explicit; nothing is rendered as a fake success.

## 7. Known gaps

- D2 mount chain: unverified in a real client (see 4.1).
- Chat text is relayed verbatim; TMP rich text is disabled and profile fields are sanitized, but
  chat markup/abuse filtering is not implemented.
- Registration-password servers are not usable from the shell.
- The registration password uses a global fixed salt, so the wire proof is an offline verifier. Fixing
  it needs a per-instance salt and a versioned KDF/protocol bump; it is not changed in Gate 2.
- Tier 2 identity proof (server proves static-key possession with an HMAC before the client reveals its
  token MAC) is deferred, not shipped. Gate 2 achieves the same ordering with a client-side gate.
- The server still creates accounts on the register and no-account paths without validating the token
  proof. This is inherent to unauthenticated first registration; the client-side gate means no proof
  and no account are sent to an untrusted endpoint, and a registration password authenticates the
  server for operators who need it.
- World download authorization, upload caps and ordering, save-import sanitisation, claim bounds,
  controller release and per-family rate limits are implemented in `NetServer.Worlds.cs`,
  `NetServer.Limits.cs`, `NetServer.Social.cs` and `SfsWorldSanitizer`.
- Region overlap policy is undefined; overlapping claims are allowed.
- Rate-limit buckets are still per-process, so a multi-process deployment does not share limits.
- Upload buffers expire lazily from the world-time loop; there is no dedicated background worker.
