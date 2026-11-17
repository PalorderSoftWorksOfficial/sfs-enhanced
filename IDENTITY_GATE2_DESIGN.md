# Identity Gate 2 — design decision

Manager decision record. Supersedes the DRAFT in `docs/SERVER_IDENTITY_PINNING.md`.
Basis: `TEAM_SECURITY_IDENTITY.md`, `PRODUCT_ARCHITECTURE.md`, and source read at the cited lines.

---

## 1. Decision in one paragraph

Ship **Tier 1**: a purely client-side pin over the static key the server already proves it holds,
checked **before** the client sends any token MAC or registration secret, plus per-endpoint token
scoping, an explicit first-use trust dialog, and honest documentation. **Defer Tier 2**
(`IdentityProof`) — Tier 1 removes the window it was meant to close. No wire change, no protocol
version bump, no change to existing crypto labels. `.NET Framework 4.7.2` compatible throughout.

## 2. Verified facts

| Fact | Source |
| --- | --- |
| Fingerprint = lower-hex SHA-256(`SFSEnhanced-ServerIdentity-v1` ‖ X ‖ Y) = 64 chars | `SecureChannel.cs:84-90` |
| Static X/Y are in the transcript hash, so token and registration proofs bind them | `SecureChannel.cs:49-62` |
| `staticSecret = ECDH(…, serverStatic)` enters the pre-master HMAC key, so only the static **private** key holder can complete a session | `SecureChannel.cs:70-74`, `SecureHandshake.cs:122-127`, `SecureSessionHandshake.cs:128` |
| `AuthChallengePacket.IdentityFingerprint` is not in the transcript and is attacker-writable | `Packets.cs:36`; not read by the client |
| Client substitutes `new byte[32]` for absent/malformed static material and still proceeds | `SecureHandshake.cs:120-125`, `SecureSessionHandshake.cs:128` |
| Client sends the token proof before the peer has proven anything | `SecureHandshake.cs:129,138-143` |
| Server creates accounts on the `registerMode` and `existing == null` paths without validating `proof.Proof` | `SecureSessionHandshake.cs:116,155-181`; validation only at `:185` |
| Registration password hash uses a global fixed salt → the wire proof is an offline verifier | `SecureChannel.cs:31-48` |
| One global auth token for all servers, overwritten on every accepted connect | `NetClient.cs:73-76`, `ModSettings.cs:32-36` |
| Account lookup uses the untrimmed name, registration the trimmed one → duplicate `"Bob"` accounts | `SecureSessionHandshake.cs:68,70,155-183` |
| Fingerprint is never printed; README claims TLS, TLS certificate and fingerprint pinning | `Server/Program.cs:65-69`, `README.md` |
| Embedded `host:port` in the address is parsed then **discarded**; SRV target `.` becomes an empty host | `ServerEndpointResolver.cs:23-31,41-47` |

## 3. Threat model

| Attack | After this gate |
| --- | --- |
| Passive observer | Unchanged (sealed channel) |
| Key-substituting MITM / fake server / DNS-SRV redirect, **already-pinned endpoint** | **Refused before any proof is emitted** |
| Unconfirmed first contact, no acceptance given | **Stopped**: no proof sent, so no account is created on the real server |
| First contact, user clicks accept without comparing | **Still vulnerable.** Automatic TOFU cannot stop this. Stated in UI and docs; never labelled "verified". |
| Server re-key or data-dir loss | Surfaces as changed identity; requires deliberate two-step re-pin |
| Registration-password offline cracking | **Not fixed here.** Documented; the client no longer emits the secret to an unpinned server, which removes the passive-collection path. KDF/salt fix is versioned work. |
| Compromised host / compromised local machine | Out of scope. PlayerPrefs pins are readable and editable by a local attacker. |

The honest guarantee: **an endpoint you have trusted cannot change silently, and an untrusted
endpoint receives nothing but Hello plus an ephemeral public key.**

## 4. Trust states

```
PinState: Missing   -> block, show first-use dialog
          Match     -> proceed, refresh lastVerifiedUtc
          Changed   -> block, show mismatch, no write, no proof
```

A pin is written **only** by the explicit *Trust and connect* action, and it stores exactly the
fingerprint the dialog displayed. The client then retries the connection, so the next handshake sees
`Match`. A rejected attempt never writes anything, the untrusted challenge is never resumed, and the
user is never silently reconnected. `trust` is always `"pinned"`; provenance lives in `origin`
(`user_verified` | `token_migration` | `re_pin`) so the UI never implies third-party verification.

## 5. User flow

1. **First use** — connect attempt stops after the server's `AuthChallenge`. Dialog shows canonical
   authority, resolved target when it differs, full 64-hex fingerprint in four lines, and a
   16-char comparable short form (`XXXX-XXXX-XXXX-XXXX`). Copy states plainly that the fingerprint
   is unverified, that it must be compared out of band (server console prints it at startup), and
   that an impostor looks identical here. Actions: *Trust this server* / *Cancel*.
2. **Match** — connect proceeds; `lastVerifiedUtc` and `resolvedHost/Port` refresh.
3. **Changed** — mismatch dialog shows pinned vs presented, both hex and short forms. Actions:
   *Cancel* only, plus *Forget this pin* which is a separate confirmed step. After forgetting, the
   next connect re-enters the first-use flow, which is the second deliberate step. A changed key is
   never replaced in one action.

## 6. Pin key schema

`endpoint_key` is the **user's chosen service identity**, never the SRV target.

- explicit port (typed or in the address) → `host:port`
- no port (SRV lookup or default) → `host#auto`
- IPv6 keeps brackets: `[2001:db8::1]:7777`, `[2001:db8::1]#auto`
- normalization: trim, strip `sfs://`, lowercase invariant, strip one trailing dot, IDNA punycode
  where the platform supports it, reject empty / >253 chars / control chars / `@` / non-`sfs` scheme.
- `host` and `host#auto` are **never** silently aliased. `EXAMPLE.com.` and `example.com` are one key.
- `resolved_host` / `resolved_port` are informational. A changed SRV target is **not** a trust event;
  a changed fingerprint is.

```json
{"endpoint_key":"play.example.com#auto","host_input":"play.example.com","declared_port":null,
 "resolved_host":"eu1.example.com","resolved_port":7777,"fingerprint":"<64 hex>",
 "short_fingerprint":"ABCD-EFGH-JKMN-PQRS","server_name":"Example Orbital","trust":"pinned",
 "origin":"user_verified","first_seen_utc":"…","last_verified_utc":"…","re_pin_count":0,
 "previous_fingerprint":null}
```

Stored in PlayerPrefs under `sfs_enhanced.pins`, whole-document rewrite under a lock, max 64 pins
evicting the oldest `lastVerifiedUtc`. Corrupt/truncated JSON → treated as no pins, logged, **never
overwritten**. Writes are staged to a `…pending` key and committed after the readback verifies.

## 7. Token scoping and migration

Per-endpoint tokens in `sfs_enhanced.tokens` (map `endpoint_key → token`). The legacy global
`sfs_enhanced.token` is migrated once, conservatively:

1. Read `ServerHistory.GetAll()`; take the entry with the greatest `LastConnectedUtc` (not
   "current host", which may have been edited after a failure).
2. Canonicalise **that entry's** `Host`/`Port` into an `endpoint_key`. If the entry is missing,
   malformed, or yields no key → **do not migrate**. Leave the legacy value untouched, attach it to
   nothing, and surface a notice: the account exists but its server could not be determined.
3. If the target key already has a token → leave both alone (no overwrite).
4. Write the map, read it back, verify; only then clear `sfs_enhanced.token`. Any failure leaves the
   legacy token in place.

A failed, refused or mismatched handshake never writes a token. `AuthToken` becomes a per-endpoint
lookup performed before `Hello`, so connecting to server B can no longer destroy server A's credential.

## 8. Protocol decision — Tier 2 deferred

Tier 2 (`IdentityProof` HMAC before the client reveals its token MAC) was considered and **not
shipped in this slice**:

- Its stated benefit is preventing the client from emitting a token MAC to an unproven peer. Tier 1
  already guarantees the client emits no MAC unless the endpoint is pinned to this exact static key
  (or the user has just accepted it), so the gap Tier 2 closes is already closed by ordering.
- It requires a new packet/field plus negotiation and dual-path server code, which is a protocol
  change the release gate does not need.
- Its real remaining value is third-party identity assertions and directory trust, which belong with
  the signed-directory work, not here.

Recorded as Gate 2.5: additive `ServerIdentity { algo, x, y }` + `IdentityProof` in `AuthChallenge`,
gated by server config and client preference, with `RequireIdentityProof` defaulting on for new
deployments. Tier 3 (ECDSA over the transcript) stays out of scope until signed directory listings.

## 9. Compatibility

- Wire format unchanged; `Hello.ProtocolVersion` stays `"2.0"`; all `…-v2` labels untouched, so
  every stored account continues to authenticate.
- New code lives in `Shared/` (netstandard2.0): no `Convert.ToHexString`, no `Span`, no `AesGcm`.
  Hex via `StringBuilder`; base32 by hand; `IdnMapping`/`IPAddress` only.
- Unknown/absent static material becomes a hard, specifically-worded failure rather than a silent
  zero-secret session.
- Server-side trim-once canonicalisation must be matched by the client, so both trim the player name
  once at handshake entry and use that single value for `Hello`, lookup, validation and transcript.

## 10. Tests (harness, `MULTI_CLIENT_INTEGRATION_PASSED`)

First pin refuses and writes nothing · matching pin proceeds and refreshes · changed pin refuses,
names both fingerprints, writes nothing · no pin change on auth failure · distinct endpoints
(`host:port` vs `host#auto` vs a second port) · normalization collisions (`EXAMPLE.com.` ≡
`example.com`; `example.com` ≢ `example.com:7777`; `[::1]:7777` ≡ `[0:0:0:0:0:0:0:1]:7777`) · SRV target
change keeps the pin · empty-root SRV target falls back · store persistence across reload · corrupt
store yields Missing, not Match · token migration picks the most recent history entry only ·
ambiguous migration leaves the legacy token and reports a notice · whitespace names (`Bob`, `"Bob "`,
`" bob"`) resolve to one account · absent/off-curve/oversized static material rejected before the
proof frame. Five consecutive green runs against one long-lived server.

## 11. Out of scope

Per-server registration salt and KDF versioning; signed directory listings and `/.well-known`
bootstrap; cross-server identity; Steam auth; any change to token *format* or to the existing crypto
labels. `_profileBuckets` eviction and other Gate 3 hardening stay queued.
