# SFS Enhanced — Implementation Plan

Continues from the app-shell slice. Order matters: a layer is only built on what the server actually
enforces. Every gate below states what is proven, not what is intended.

## Gate 0 — release blockers for the in-game shell

1. **D2 mount verification (hard gate, still open).** Run the mod in a real Spaceflight Simulator
   client: open the shell from the main menu and the worlds menu, confirm it mounts on `Base_PC`,
   that navigation renders, that Escape walks the history stack and then closes, and that F8 is inert
   in `World_PC`. Nothing in this repository can prove it; no SFS runtime exists in the build
   environment. **Status: blocked, unverified.**
2. **Layout pass in-game.** Confirm the 1000×640 window, 740-wide content column, the 7-entry nav and
   the per-page row limits fit at the game's UI scale. **Status: blocked with D2.**

## Gate 1 — shell honesty (done)

- Server-side chat filtering: `ChatTextFilter` refuses markup with `chat_rejected`, strips
  control/zero-width/bidi characters, collapses whitespace, caps length and refuses empty text.
  Rate-limited sends return `chat_rate_limited` instead of disappearing.
- Explicit loading / empty / offline / error states on every list, including a dedicated directory
  failure state with the reason and a retry action.
- Registration-password servers are joined from the shell: the field appears only after the server
  asks, and the proof is answered inside the sealed channel.
- Failures are distinct: `unknown_account`, `bad_token`, `name_taken`, `registration_disabled`,
  `registration_password_required`, `bad_registration_password`, `protocol_version`, `identity_*`.

## Gate 2 — server identity trust (done; in-game surface unverified)

- Fingerprint recomputed from validated `StaticX/StaticY`; the `IdentityFingerprint` field is ignored.
- The pin decision happens before any credential is derived; missing/changed/unreadable are hard stops.
- Pins key the canonical user authority, first use requires explicit acceptance, replacement is a
  separate two-step action that records the previous fingerprint.
- Tokens are per endpoint with a one-time migration from the most recent successful endpoint.
- Names are canonicalised once and used for lookup, registration and the transcript.

## Gate 2.5 — identity follow-ups

Done:

- Explicit registration vs sign-in; no silent account creation; machine-readable rejection codes.
- Registration password proof only inside the sealed channel, with a per-instance salt and a
  versioned KDF.
- **Tier 2 identity proof (this slice).** The server sends a sealed `IdentityProof` — an HMAC keyed
  from the static-key ECDH secret over the transcript — after `AuthChallenge` and before the client
  sends its token MAC. Missing proof → `identity_proof_missing`; forged proof →
  `identity_proof_invalid`; in both cases no credential is sent. Protocol version is now **3.0** and
  2.x peers are refused explicitly rather than downgraded.
- Staged endpoint storage with readback verification.
- Identity key integrity: a present-but-unreadable `server_static_key.json` now fails startup with an
  actionable message instead of silently generating a new identity (verified: restart keeps the
  fingerprint, corrupt file exits 1 and is left untouched).

Still open (documented, not solved):

- A PAKE. A server that verifies a registration password can still test guesses offline. Do not
  claim otherwise in UI or docs.
- Trust-on-first-use cannot detect a first-contact impostor; only out-of-band comparison can.
- Optional HTTPS `/.well-known` fingerprint bootstrap for domain-hosting operators.
- The token MAC is still a transcript-bound verifier of `SHA-256(token)`; it is safe because tokens
  are 32 random bytes, not because the value is unusable.

## Gate 3 — server hardening (done)

- World download authorization through `CanAccessWorld` (owner, whitelist, friend, public).
- Upload bounds: 256 chunks, 8 MiB, 96 KiB per chunk, ordered exactly-once delivery, one active
  upload per player, a global concurrent cap, idle expiry with `upload_expired`, a tombstone so a
  restarted upload with the same id works, operator log lines and pending/expired counts in `status`.
- Imported saves rewritten by `SfsWorldSanitizer` (ownership, controllers, claim trust, build ids).
- Claim bounds and **overlap policy**: a region may not overlap another player's region or contain
  another player's build; identical re-claims return the existing claim; build claims only for the
  caller's own build.
- Build control released on leave/disconnect.
- Per-family rate limits with eviction on disconnect.

## Gate 4A — room lifecycle (done in this slice)

Worlds were conflated with the discovery row. This slice separates the **room** (live session with
policy) from the **world** (the save/simulation) on the server, and only exposes what is enforced:

- `Server/Rooms/RoomManager.cs`: process-scoped room records (id, name, description, host, world,
  visibility, capacity, created).
- `NetServer.Rooms.cs`: create (name/description/capacity/visibility validation, rate limit, one room
  per world, public room requires a public world, 128-room cap), list (public + own + friends'
  friends-only), join (visibility → capacity → world access, idempotent re-join), leave, host-only
  close with `RoomClosed` fan-out, auto-close when empty, world-join routed into the hosting room so
  capacity cannot be bypassed.
- Packets 90–98 (`RoomCreate`, `RoomState`, `RoomListRequest/Response`, `RoomJoin`, `RoomLeave`,
  `RoomClose`, `RoomClosed`). Client `RoomsPage` replaced the world browser; `NetClient` owns
  `CurrentRoomId`.
- Harness: `[assert] rooms are server-enforced: capacity, visibility, host close and auto-close`.

Still open in this gate:

- Rooms do not survive a restart (BUGS B-19) and a host who leaves keeps the room until it is empty
  (BUGS B-20). Both need a lifecycle decision (room store + lease, host transfer) before the product
  can claim persistent rooms.
- Roles/permissions beyond host/guest, moderation of room text beyond filtering, and room thumbnails.

## Gate 4B — creator library (not started, do not fake)

Blocked on a server contract that does not exist: creation registry (asset type, owner, visibility,
version history), validated upload/download with size and format limits, moderation queue, and a
distinct `Creation` model. A published save must not imply a live room, and a room save may be
published as a creation only through an explicit action. No UI until that contract is implemented and
tested.

## Gate 4C — moderation and privacy (partial)

Present: chat filtering, profile field sanitisation, presence privacy, friend-request privacy, claim
protection, rate limits, host-only close. Missing: block lists, report intake and operator tooling,
account suspension, room/creation moderation, audit log retention. Do not add report/block buttons
before the server stores and enforces them.

## Gate 5 — platform identity (only if the product still wants it)

Accounts are per server and the UI says so. A global identity service needs its own account store, key
custody and a migration path for existing per-server accounts; until then no screen may imply a
platform-wide account, friends graph or inventory.

## Working rules for the next slices

- One vertical slice at a time; stop and verify before expanding.
- Build clean (`dotnet build SFSEnhanced.sln`, 0 warnings / 0 errors) and run
  `scripts/multiplayer-integration` after every group of changes.
- One passing run is not evidence. Run the harness at least five times sequentially against one
  long-lived test server set and require five `MULTI_CLIENT_INTEGRATION_PASSED` results.
- In-repo `obj/` trees may be root-owned; build a mirror excluding `.git`, `obj` and `bin` when that
  happens (see `QA_PLAN.md`).
- No code comments or XML doc comments in new code; documentation lives in Markdown.
- Wire changes are versioned explicitly. Protocol 3.0 is a hard break from 2.x.
- No deployment. Pushing the feature branch is authorised by the user; do not force-push.

## Next slice candidates (pick one, finish it)

1. **Room persistence and host transfer** (closes B-19/B-20): room store, lease/heartbeat, transfer
   action, restart test.
2. **Creation registry read path**: list/search published saves with real metadata and validated
   downloads into the local save folder, moderation queue stubbed only if the server refuses
   unpublished content.
3. **Blocking and reporting**: enforced block list affecting chat, invites and room joins, report
   intake with an operator command, plus harness coverage.
