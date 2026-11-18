# SFS Enhanced — UX Flows

Status: describes the shell that exists in `Mod/UI/App` and the server contracts it calls. Flows that
have no server contract are listed in §9 as blocked; they have no buttons.

Notation: `→` user step, `[P]` packet, `[S]` server rule, `[E]` failure state.

## 1. Entry and exit

| Entry | Behaviour |
| --- | --- |
| Play menu → Multiplayer | Opens `MultiplayerScreen` (`BasicMenu`) containing the shell window |
| F8 in a menu scene | Opens/closes the shell |
| F8 in `World_PC` | Inert; the shell pauses the simulation, so it is refused there |
| Escape / window close | `AppShell.Back()`: pops page history, then closes the screen |

The shell is one window with a nav rail (Home, Servers, Rooms, Friends, Chat, Profile, Settings). There
are no floating sub-windows; secondary pages replace the content area.

## 2. First connection and server trust

→ Servers → enter host (and optional port) → **CONNECT**

1. The client canonicalises the endpoint (`host:port` or `host#auto` when SRV decides the port).
2. `[P] Hello` (protocol 3.0) → `HelloAck` with nonce echo and challenge.
3. `[P] AuthResponse` (client ephemeral key) → `AuthChallenge` (server ephemeral + static key).
4. The client recomputes the fingerprint from the static key and evaluates the local pin.

| Pin state | Screen | Next |
| --- | --- | --- |
| Missing | "SERVER IDENTITY NOT TRUSTED" panel: 4-line fingerprint, grouped short form, out-of-band comparison instruction | **TRUST AND CONNECT** writes the pin and reconnects; **CANCEL** stores nothing |
| Match | no panel | continue |
| Changed | "SERVER IDENTITY CHANGED" panel with previous fingerprint in `Warning`, two-press replacement | **REPLACE TRUSTED FINGERPRINT** records `RePinCount`/`PreviousFingerprint` |
| Unreadable store | error panel with repair instruction | nothing sent |

5. `[P] IdentityProof` (sealed): the server proves it holds the static key before any credential moves.
   `[S]` A client that does not receive a valid proof refuses with `identity_proof_missing` /
   `identity_proof_invalid` and sends no token MAC.
6. `[P] AuthResponse` (token MAC) → `AuthResult` (sealed) → shell connected.

[E] Refusals are rendered by code: `unknown_account`, `bad_token`, `name_taken`,
`registration_disabled`, `registration_password_required`, `bad_registration_password`,
`protocol_version`, `identity_*`. Each has its own heading and next action; there is no automatic
fallback from sign-in to registration.

## 3. Account creation

→ Servers → **CREATE ACCOUNT** (explicit action; never automatic)

- `[P] Hello` with `mode=register`; the server answers with `HelloAck.registrationPasswordRequired`
  when the operator set a password. The client only then renders the password field.
- `[P] RegistrationPrompt` arrives sealed after the identity proof; the client answers with
  `RegistrationAuth` inside the channel. The password is never sent in the clear.
- `[S]` Registration is refused when closed (`registration_disabled`), when the name exists
  (`name_taken`), or when the password proof is wrong (`bad_registration_password`).
- On success the client stores the issued token per endpoint and connects.

[E] Name rules: trimmed once, canonicalised, 3–24 characters, no control characters; the same
canonical string is used for lookup, registration and the transcript.

## 4. Rooms

A room is a **live session** on one server that hosts exactly one world save. The world persists
after the room closes; the room does not survive a server restart.

### 4.1 Browse

→ Rooms → `[P] RoomListRequest` → `RoomListResponse`

`[S]` Room visibility: `Public` rooms are listed to everyone; `Friends` rooms are listed only to the
host and the host's accepted friends. No other room is listed.

Row content: name; `host · players/capacity · Public|Friends only · description`; `JOIN` unless the
room is full or is the room you are in. Truncation is disclosed ("Showing 3 of N rooms").

| State | Rendering |
| --- | --- |
| Disconnected | empty state + **GO TO SERVERS** |
| Loading | "Loading rooms..." |
| Empty | "No open rooms" + next step |
| Error (refusal) | status line with the server reason |

### 4.2 Open a room

→ Rooms → name, capacity, public/friends toggle, optional description → **OPEN ROOM**

Client validation: name 1–48, description ≤ 160, capacity 2–64. `[S]` The server re-validates all
three, enforces one live room per world, a 128-room process cap, a create rate limit, and that a
public room is hosted on a public world.

`[P] RoomCreate` → `RoomState { Accepted, Room, World }`; the host is moved into the new world and
receives the world snapshot. `[S]` Opening a room without a `WorldId` creates a private world named
after the room (public if the room is public).

[E] `capacity must be between 2 and 64`, `A public room needs a public world`,
`That world is already hosted by the room "…"`, `You can only host a room on your own world`,
`maximum number of rooms`.

### 4.3 Join

→ **JOIN** on a row, or accept a friend's world invite.

`[S]` Join order: room exists → visibility (public / host / friend) → capacity → world exists → world
access. Joining a room you are already in is idempotent. A direct `[P] WorldJoin` for a world that a
room currently hosts is routed into that room, so capacity cannot be bypassed by joining the world id.

`[P] RoomJoin` → `RoomState { Accepted, Room, World }`; everyone in the world gets a join line in chat.

[E] `Room not found. It may have closed.`, `This room is friends-only…`,
`This room is full (n/n).`, `The room's world is no longer available.`

### 4.4 Leave and close

- **LEAVE** → `[P] RoomLeave` → `RoomState { Room = null }`; build control is released and the player
  returns to the lobby. `[S]` A room that becomes empty closes immediately.
- **CLOSE ROOM** (host only, `Danger`) → `[P] RoomClose` → every member receives `[P] RoomClosed`
  with the reason and is returned to the lobby.

[E] `Only the room host can close this room.` A non-host never sees the close button.

### 4.5 In-room surface

Current-room card: room name, `builds · players · id`, **LEAVE**, **CLOSE ROOM** (host), claim toggle,
and the time-warp vote block. Room chat is the Chat page scoped to the room; local claim and vote
state reset on leave/close.

## 5. Server browser and direct connect

→ Servers → **REFRESH** → directory `GET /api/v1/servers`

| State | Rendering |
| --- | --- |
| No URL configured | "No server directory configured" + **OPEN SETTINGS** |
| Loading | "Loading public servers..." |
| Error | "COULD NOT LOAD THE DIRECTORY" + the reason (timeout, HTTP status, unreachable, unreadable) + **RETRY** |
| Empty | "No public servers listed" + direct-connect hint |
| Partial | rows + "Showing 3 of N servers" |

`JOIN` fills host/port and connects. A directory listing is not proof of identity; the identity panel
still gates the connection. Direct connect by hostname always works without a directory.

## 6. Friends and invites

→ Friends → add by exact player name → `[P] FriendRequest` → `[P] FriendListResponse` to both sides.
Accept/decline sends `FriendRequestResponse`. `[S]` Requests are refused when the target disallows them
or the target does not exist; presence is hidden unless `ShowPresence`.

Invite: `[P] FriendInviteToWorld` with the current world id → the target gets an invite card on Home.
Accepting joins that world; if a room hosts it, the join is routed into the room (§4.3).

## 7. Chat

→ Chat → type → Enter. `[P] ChatMessage` (room-scoped when in a world, lobby otherwise).

`[S]` Rate limit 8 burst / 2 per second. Text is filtered server-side: `<`/`>` are refused
(`chat_rejected`), control/zero-width/bidi characters are stripped, whitespace is collapsed, length is
capped at 512, and empty-after-filter messages are refused. Server join/leave lines are generated by
the server.

[E] The refusal message appears in the status line; nothing is echoed locally, so a refused message
never looks delivered.

## 8. Profile

→ Profile → edit display name, bio (≤ 280), accent `#RRGGBB`, privacy toggles → **SAVE**.

`[P] ProfileUpdate` → `ProfileUpdateResult { Accepted, Reason, Profile }`. The server returns the
sanitised profile and the client replaces its copy (write-then-reflect, no optimistic edit). Presence,
online state and current room are hidden from non-self viewers when `ShowPresence` is off. Profiles are
per server; the UI never claims a platform-wide account.

## 9. Blocked flows (no UI, by design)

| Product promise | Why no UI yet | Needed first |
| --- | --- | --- |
| Creator library / workshop (uploads, ratings, versions) | No creation registry, no moderation queue, no versioning contract | `Creation` model, publish/download/list endpoints, validation + moderation, storage limits |
| Public profile pages / followers | Profile page is self-only | read-only profile route, privacy rules for third parties |
| Blocking and reporting | No block list or report intake | block enforcement in chat/invite paths, report storage + operator surface |
| Room roles/permissions, persistent rooms, host transfer | Server only enforces host/guest and capacity | role model, ownership transfer, heartbeat/lease |
| Global identity, cross-server friends | Accounts are per server | account service and migration (Gate 5) |
| In-flight (non-pausing) overlay | A pausing `Screen_Menu` freezes flight | non-pausing Unity overlay; explicitly out of scope |

## 10. Acceptance map

| Flow | Verified headlessly |
| --- | --- |
| Identity gate + proof ordering | `[assert] identity is decided and proven before the auth proof is sent`, live connects assert `IdentityProofVerified` |
| Account modes | `[assert] account authentication is explicit and failures are distinct` |
| Chat filtering | `[assert] chat text is filtered and validated server-side` |
| Rooms | `[assert] rooms are server-enforced: capacity, visibility, host close and auto-close` |
| Claims overlap | `[assert] region claims cannot overlap another player's region or build` |
| Upload expiry | `upload_expired` assertion inside the data-plane test |
| Everything visual / mount | **Unverified**: no SFS runtime here (BUGS B-1) |
