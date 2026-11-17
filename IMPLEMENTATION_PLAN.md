# SFS Enhanced — Implementation Plan

Continues from the first vertical slice (app shell + per-server profiles). Order matters: the
shell has to be trustworthy before anything is stacked on it.

## Gate 0 — release blockers for the current slice

1. **D2 mount verification (hard gate).** Run the mod in a real Spaceflight Simulator client:
   open the shell from the main menu and from the worlds menu, confirm it mounts on `Base_PC`,
   that navigation renders, that Escape walks the history stack and then closes, and that F8 is
   inert in `World_PC`. Until someone records that, the slice is "compiles and passes headless
   tests", nothing more.
2. **Layout pass in-game.** Confirm the 1000×640 window, the 740-wide content column and the
   per-page row limits fit at the game's UI scale, and that no list overflows the content area.
3. **QA retest of D1–D10** against the current tree, with build and harness logs attached.

## Gate 1 — finish the shell honestly

- Chat abuse handling: server-side length/character filtering for chat, matching profile rules.
  Keep rich text disabled everywhere user text is rendered.
- Explicit offline/empty states on every list (already present) plus error surface for failed
  profile saves (reason text from `ProfileUpdateResult`).
- Registration-password servers: either support the password field in the connect flow or state
  in the UI that such servers cannot be joined from the shell.

## Gate 2 — server identity trust (implemented, awaiting independent retest)

Shipped as Tier 1 of the design in `IDENTITY_GATE2_DESIGN.md`:

- Fingerprint recomputed locally from validated `StaticX/StaticY`; missing, malformed or off-curve
  keys are hard failures. The `IdentityFingerprint` packet field is never trusted.
- Identity is decided **before** the token proof or registration secret is sent, so an untrusted
  endpoint never receives a credential MAC and no account is created on it.
- Pins key the canonical user authority (`host:port` or `host#auto`), never the SRV target, with
  normalization, IDNA and IPv6 handling. First use requires the in-game trust dialog; a mismatch is
  refused and replacing a pin is a separate confirmed action that records the previous fingerprint.
- Auth tokens are stored per endpoint with a conservative one-time migration from the most recent
  successful `ServerHistory` entry; ambiguous cases keep the legacy token and surface a notice.
- Player names are trimmed once and the same canonical value is used for lookup, validation,
  registration and the transcript, closing duplicate-account names like `"Bob "`.
- The server prints its identity fingerprint at startup and in `status`, warns when registration is
  open without a password, and `README.md` no longer claims TLS or certificate pinning.

## Gate 2.5 — identity follow-ups

Done in this slice:

- Account creation is explicit. `ModeToken` authenticates existing accounts only and returns
  `unknown_account` otherwise; `ModeRegister` is only sent by the *Create account* action. Rejections
  carry codes (`unknown_account`, `bad_token`, `name_taken`, `registration_disabled`,
  `registration_password_required`, `bad_registration_password`) that the UI renders as distinct
  states, with no automatic switch from sign-in to registration.
- Registration passwords never cross the wire in the clear. The registration proof is answered inside
  the sealed channel, so it is never observable on the wire. Exact ordering, stated precisely: the
  client sends the token MAC in the plaintext `AuthResponse` as before, then the server sends the
  sealed `RegistrationPrompt`; producing that sealed frame requires the static private key. The trust
  decision is still the local pin or a one-time user acceptance, and the server demonstrates key
  possession later, when it returns sealed data. Move ordering is *not* claimed. Per-instance salt in
  `data/system/registration_salt.json`, new `…-v3` label, token labels untouched so every stored
  token verifier still works. A plaintext `RegistrationSecret` from an older client is refused with
  an actionable message.
- Endpoint storage writes are staged and read back; a dropped write cannot corrupt the store.

Still open:

- Tier 2 `IdentityProof`: server proves static-key possession with an HMAC after `AuthChallenge` and
  before the client reveals its token MAC. The sealing work above covers the registration password;
  the token MAC still travels in the second plaintext `AuthResponse`.
- A PAKE (or equivalent) if registration passwords are to be resistant to a malicious server or an
  offline attacker who obtains the sealed transcript. Until then, documented as not solved.
- Optional HTTPS `/.well-known` fingerprint bootstrap so a domain-hosting operator has an
  authenticated first contact without a manual comparison.

## Gate 3 — server hardening

Done in this slice and covered by the harness:

- World download authorization through a single `CanAccessWorld` helper (owner, whitelist, friend, or
  public). A stranger's download of a private world is refused; owner and public downloads stream.
- Upload bounds: 256 chunks, 8 MiB total, 96 KiB per chunk, ordered exactly-once delivery, one active
  upload per player, a global concurrent-upload cap (`--upload-max-concurrent`, default 32) and idle
  expiry (`--upload-idle-seconds`, default 120, swept lazily from the world-time loop). Every
  rejection releases the reservation and the buffer, and a fresh upload works immediately after.
- Imported saves are validated and rewritten by `SfsWorldSanitizer`: build ids unique and bounded,
  numeric state finite, blueprint and name sizes capped, foreign owner/controller ids replaced with
  the uploader, claim lists filtered against real builds and claim trust lists cleared.
- Claim bounds: finite centre and radius, radius 1–50000 m, centre within ±1e6 m, 32 region claims
  per player, 512 claims per world, build claims only for a build the caller owns, unknown claim
  shapes rejected with a precise error.
- Build control is released when a player leaves the world or disconnects, so nobody holds a rocket
  after dropping.
- Rate limits for world create, world join, world download, upload chunks, claims, chat and invites.
- Rate-limit buckets, pending uploads and the active-upload reservation are evicted on disconnect.

Still open:

- `SessionsCount` fix was already merged in the earlier slice; `_profileBuckets` eviction is present.
- Region overlap policy is not defined; overlapping regions are allowed today.

## Gate 4 — Rooms and creations (not started, do not fake)

Today's "world" conflates three concepts: the live simulation, the save file, and the discovery
row. The intended split:

- **World** — the simulation/save instance (planet packs, builds, time).
- **Room** — a joinable session hosting a world, with policy (public/friends/invite), capacity and
  lifecycle. Rooms can come and go without destroying the world.
- **Creation** — a player-authored artifact (world save, build pack) that can be published and
  hosted by others.

Rules for this gate: no role, permission, capacity or creator fields in the UI until the server
enforces them; no "Rooms" page that is a renamed world list; every new field needs a server-side
validator and a test in `scripts/multiplayer-integration`.

## Gate 5 — platform identity (only if the product still wants it)

Per-server accounts are the current, honest model. A global identity service would need its own
account store, key custody, and a migration story for existing per-server accounts. Until that
exists, no UI may imply a platform-wide account, friends graph or inventory.

## Working rules for the next slices

- One vertical slice at a time; stop and verify before expanding.
- Build clean (`dotnet build SFSEnhanced.sln`, 0 warnings / 0 errors) and run
  `scripts/multiplayer-integration` (`MULTI_CLIENT_INTEGRATION_PASSED`) after every group of
  changes.
- One passing run is not evidence. Run the harness at least five times sequentially against one
  long-lived server and require five `MULTI_CLIENT_INTEGRATION_PASSED` results; packet-wait
  timeouts must be cancellable so a timed-out wait can never consume a later frame.
- In-repo builds may fail on root-owned `obj/` trees; build a clean copy excluding `.git`, `obj`
  and `bin` when that happens.
- No code comments or XML doc comments in new code; documentation lives in Markdown.
- No deployment. Pushing this branch to `origin` is explicitly authorised by the user for this
  slice, after QA, build and integration evidence; do not force-push.
