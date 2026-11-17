# TEAM_SECURITY_IDENTITY.md — Server Identity & Authentication Review

Read-only inspection. No source was edited. Labels used throughout:

- **[FACT]** — verified by reading the code at the cited file:line.
- **[DERIVED]** — a conclusion that follows from the code plus standard cryptographic reasoning; the
  reasoning is spelled out so it can be checked.
- **[HYPOTHESIS]** — plausible but not proven here; needs a runtime experiment to settle.

Scope inspected: `Shared/Protocol/{P256,SecureChannel,SecureHandshake,LidgrenWire,Packets}.cs`,
`Server/Networking/{SecureSessionHandshake,NetServer,ClientConnection,LidgrenServerTransport,ServerDirectoryPublisher}.cs`,
`Server/Persistence/{AccountService,FileStore}.cs`, `Server/Program.cs`,
`Server/Social/FriendsService.cs`, `Mod/Networking/{NetClient,ServerEndpointResolver,ServerHistory,ServerDirectoryClient}.cs`,
`Mod/ModSettings.cs`, `Shared/Models/{WorldModels,ServerDirectoryModels}.cs`, `Directory/Program.cs`.

---

## 0. First-order correction: documentation claims an identity system that does not exist

**[FACT]** `README.md:13,15,33,54,56,58,76,109` state that the transport is "TLS-protected", that "the
server creates or loads a TLS certificate and the client establishes a TLS session", that "TLS
certificate fingerprint pinning" is implemented, and that "the generated certificate fingerprint is
printed to the server console". None of this exists:

- No TLS, X.509 or `SslStream` reference anywhere in `Server/`, `Shared/`, `Mod/`, `Directory/`
  (grep for `TLS|X509|certificate|SslStream` over non-`obj`/`bin` sources returns nothing).
- The transport is Lidgren UDP with a 5-byte type/length header plus JSON
  (`Shared/Protocol/LidgrenWire.cs:9-25`), and an application-layer secure channel
  (`Shared/Protocol/SecureChannel.cs:100-260`).
- The server identity fingerprint is **never printed** to the console: `_identityFingerprint` is only
  ever consumed by the `AuthChallenge` packet (`Server/Networking/SecureSessionHandshake.cs:41,110`),
  and `Server/Program.cs:64-69` prints only name/port/data/advertised.

Consequence: any operator or reviewer trusting `README.md` will believe endpoints are
certificate-authenticated. They are not. Treat the README security section as an unimplemented
specification; it must be corrected regardless of what else is built.

---

## 1. Does a stable server identity already exist, and how is it generated/persisted?

**Yes — a stable *keypair* exists. A stable *user-visible, comparable* identity does not.**

**[FACT]** `SecureSessionHandshake.InitializeStaticKey` (`SecureSessionHandshake.cs:24-44`) runs once per
process from `NetServer.RunAsync` (`NetServer.cs:43`):

- Loads `StaticKeyRecord` from `FileStore` folder `"system"`, id `"server_static_key"`
  (`SecureSessionHandshake.cs:13,29`) → on disk that is `<DataDir>/system/server_static_key.json`.
- If absent, generates a fresh P-256 private scalar (`P256.cs:15-26`) and saves
  `{ PrivateKeyBase64: <base64 of raw scalar> }` (`SecureSessionHandshake.cs:36-38`).
- Derives the public point once and caches `_staticX/_staticY/_identityFingerprint` in static fields
  (`SecureSessionHandshake.cs:40-41`).

So: **generated on first start, persisted in plaintext JSON in the data directory, stable across
restarts as long as that file survives.** Nothing rotates it, nothing deletes it, and there is no
configuration knob to supply an external identity key (`Server/Program.cs:9-30` has no identity
fields).

**[FACT] Fingerprint definition** (`Shared/Protocol/SecureChannel.cs:84-90`):

```
fingerprint = lower-hex( SHA-256( "SFSEnhanced-ServerIdentity-v1" || staticX || staticY ) )   // 64 chars
```

**[FACT] Operational hazards of the current persistence:**

- The private key file is written by `FileStore.Save` via `File.WriteAllText`
  (`Server/Persistence/FileStore.cs:37-47`) with default permissions — no `0600`, no encryption at
  rest, no backup guidance.
- The identity is silently destroyed if the operator changes `--data`
  (`Server/Program.cs:144`), recreates the data directory, or loses a container volume. There is no
  warning, no export/import, and no way to recover the previous identity.
- `NetServer.RunAsync` constructs a **second** `FileStore` over the same `DataDir`
  (`NetServer.cs:43`) rather than reusing `Program`'s instance (`Server/Program.cs:71`). They agree
  today because both point at `_config.DataDir`; it is a latent divergence risk if data-dir
  resolution ever changes.
- There is **no** place where a human can read the fingerprint: no console print, no `status`
  command field (`Server/Program.cs:118-121`), no file with the public key or fingerprint.

**[FACT] The client does not consume identity material at all.** `AuthChallengePacket` carries
`StaticX`, `StaticY`, `IdentityFingerprint` (`Shared/Protocol/Packets.cs:30-38`) and the server fills
all three (`SecureSessionHandshake.cs:105-113`), but `SecureHandshakeClient.RunAsync` never reads
`challenge.IdentityFingerprint` (grep: the only occurrences are in `Packets.cs` and the server). The
client only *derives* from `StaticX/StaticY` (`SecureHandshake.cs:122-127`).

**[FACT] No pin storage exists.** `ModSettings` (`Mod/ModSettings.cs:8-60`) has exactly six keys
(`host`, `port`, `name`, `token`, `directory`, `server_executable`) and no fingerprint/pin/identity
key. `ServerHistory` (`Mod/Networking/ServerHistory.cs:14-39`) stores `Host/Port/Name/LastConnectedUtc`
only. The server directory listing has no identity field (`Shared/Models/ServerDirectoryModels.cs:6-19`;
`Directory/Program.cs:9-50` returns listings verbatim).

Net: identity **material** exists and is already woven into the key schedule; identity **trust** does
not exist anywhere in the product.

---

## 2. Is the current static key authenticated / bound into the transcript?

**Bound: yes. Authenticated to the client: no.** These are different properties and the current code
has only the first.

**[FACT] Binding.** `ComputeTranscriptHash` (`SecureChannel.cs:49-62`) covers, in order: the label
`"SFSEnhanced-Transcript-v2"`, client nonce, server challenge, client ephemeral X/Y, server ephemeral
X/Y, **server static X/Y**, and the player name. `ComputePreMaster` (`SecureChannel.cs:70-74`) is
`HMAC-SHA256(key = SHA-256(ephemeralSecret ‖ staticSecret), msg = label ‖ transcriptHash ‖
exchangeIdentifier ‖ clientNonce)`. Server side: `staticSecret = ECDH(staticPrivate, clientEphemeral)`
(`SecureSessionHandshake.cs:128`), client side:
`staticSecret = ECDH(clientEphemeral, staticPublic)` (`SecureHandshake.cs:122-127`).

**[FACT] The transcript does not cover everything the handshake negotiates.** `HelloPacket.ProtocolVersion`
and `HelloPacket.Mode` are absent from `ComputeTranscriptHash`'s inputs, and `ClientModVersion` is never
included anywhere. They are plaintext and unauthenticated. (Impact is limited today — see §3 — but any
v3 transcript must fix this, and must add length prefixes before adding fields to the concatenation,
since `SessionAuth.Concat` is unseparated (`SecureChannel.cs:105-117`) and is only unambiguous because
every field except the last is fixed-width.)

**[DERIVED] The static key is a genuine proof-of-possession gate, not a decorative field.** `staticSecret`
is a shared ECDH value that enters the pre-master HMAC key. A peer that presents a *copied* static
public key (real key, attacker's ephemeral) can compute `ephemeralSecret` but not `staticSecret`, so it
cannot derive `preMaster`/`sessionId` (`SecureChannel.cs:70-88`) and cannot seal a valid `AuthResult`.
This means: **a pin over the observed static public point is a sufficient detector of impersonation**,
and impersonation requires either the static private key or a substituted static public key (which the
pin rejects). That is the property that makes pinning cheap to implement — no new cryptography is
needed for the core defence.

**[DERIVED] Two sloppy fallbacks undermine the "missing static key" case.**
`SecureHandshake.cs:120-125` substitutes `new byte[32]` when `StaticX/StaticY` are absent, and
`SecureSessionHandshake.cs:128` does the same when ECDH returns null. The client therefore *accepts*
a challenge with no static material, computes a transcript over nulls, and only fails later with an
opaque "connection closed"/reject — after it has already sent its token proof (§3). For pinning, absent
or off-curve static material must be a **hard, early, explicitly-worded failure**.

---

## 3. Can an attacker MITM or impersonate on first connect?

**Yes.** This is a [DERIVED] conclusion from three [FACT]s:

1. All handshake packets are plaintext and unsigned: `RawFrame.PlainTag` frames are JSON
   (`Shared/Protocol/SecureHandshake.cs:17-48`), sent with no MAC, and the server approves any Lidgren
   connection unconditionally (`LidgrenServerTransport.cs:83-85`).
2. The client performs **no** verification of the server's static key: `IdentityFingerprint` is ignored,
   no pin exists (§1).
3. There is no transport authentication (no TLS, no DTLS, no pre-shared key): §0.

An on-path attacker therefore terminates the handshake with its own ephemeral **and** static keypair.
The client happily derives a session against it, the attacker derives the same session, and
`AuthResult` is accepted (`SecureHandshake.cs:146-160`). No pin, no user prompt, no warning: the client
believes it is on the real server. Everything after that — chat, profile, friend list, world list,
build download (`NetServer.cs:154-260`) — is attacker-controlled content delivered over a
cryptographically valid channel to the wrong peer.

**What the attacker can and cannot get from the token design** (all [DERIVED]):

- The raw bearer token never crosses the wire. The server stores only `base64(SHA-256(token))`
  (`Server/Persistence/AccountService.cs:27-30,52-58`) and compares via
  `HMAC-SHA256(SHA-256(token), label ‖ transcriptHash)` (`SecureChannel.cs:64-68`,
  `AccountService.cs:37-47`). A MITM gets a **MAC oracle** under the token key on transcript hashes of
  its choosing, not the key itself. With a 24-byte random token
  (`AccountService.cs:50-53`) brute force is not viable. This is a real but **bounded** exposure.
- The exposure happens *before* the server proves anything. Ordering is: client sends ephemeral
  (`SecureHandshake.cs:97-102`) → receives `AuthChallenge` → **computes and sends the token proof**
  (`SecureHandshake.cs:129,138-143`) → only then can it tell whether the peer completed the handshake.
  A rogue server that copies the real static public key cannot finish, but it still **collects the
  token MAC** before failing.

**New-account / registration implication — this is the worst part** (all [FACT] then [DERIVED]):

- `ComputePasswordHash` (`SecureChannel.cs:31-42`) uses a **fixed, global** salt string
  `"SFSEnhanced-Registration-Salt-v1"` and 600k SHA-256 rounds. It is deterministic across every
  server on the planet.
- The client sends `RegistrationSecret = HMAC-SHA256(passwordHash, "SFSEnhanced-Registration-v2" ‖
  transcriptHash)` (`SecureChannel.cs:44-48`, `SecureHandshake.cs:134-136,138-143`).
- An attacker who sees that value knows `transcriptHash` (it derives from values it supplied or
  observed), so it holds an **offline verifier for `passwordHash`** — the shared server
  registration/join password — at 600k SHA-256 per guess on commodity hardware. Weak or reused
  registration passwords are recoverable, and because the salt is global, one cracked password
  transfers to every server using it.
- Separately: on the token-mode path, if no account exists, the server **creates one**
  (`SecureSessionHandshake.cs:155-183`). A fake server therefore auto-provisions an account named
  after the victim and returns an attacker-issued token, which the client immediately stores **globally**
  (`NetClient.cs:73-76` → `ModSettings.cs:32-36`). One global token key, no per-endpoint scoping: after
  a MITM the user's stored credential for the *real* server has been overwritten by an attacker-known
  value (availability loss, plus confusion about which account is which).

Also [FACT], an identity-namespace hole independent of the network: the server looks up
`accounts.FindByName(hello.PlayerName)` on the **untrimmed** name (`SecureSessionHandshake.cs:68`) but
registers/uses `hello.PlayerName.Trim()` (`:70,155-183`), while `FindByName` compares stored (trimmed)
names case-insensitively (`AccountService.cs:13-23`). Sending `"Bob "` or `"Bob  "` misses the existing
`"Bob"` and **creates a second account whose `PlayerName` is also `"Bob"`** (`PlayerId` is a random GUID
per account, `WorldModels.cs:31-33`). Friend requests resolve targets by name
(`Server/Social/FriendsService.cs:34`), so duplicate display names are a direct impersonation and
mis-delivery vector. `"Bob "` also bypasses the 1–32 length check's intent, since only the trimmed
length is validated.

---

## 4. Exact material and protocol changes required for trustworthy identity

### 4.1 Material

1. **Identity key** — keep the existing P-256 static ECDH key as the long-term identity key. It already
   gates the pre-master (§2), so it is a real possession proof. Do not introduce X25519: the
   .NET 8.0.34 reference assemblies on this machine contain `ECDsa`, `ECDiffieHellman`, `AesGcm` and
   **no** `X25519`/`Ed25519`/`MLKem` type (`strings ~/.dotnet/packs/Microsoft.NETCore.App.Ref/8.0.34/ref/net8.0/System.Security.Cryptography.dll`).
2. **Fingerprint** — keep `SHA-256("SFSEnhanced-ServerIdentity-v1" ‖ X ‖ Y)` as the canonical value, but
   version it (`…-v2`) when the transcript changes, and always **recompute it locally from the observed
   `StaticX/StaticY`**. Never trust `AuthChallengePacket.IdentityFingerprint`
   (`Packets.cs:36`) as an input to a trust decision; it is an attacker-controlled string from the
   same unauthenticated peer, exactly the construction this review must not endorse. It may remain
   purely as a display hint *after* local verification agrees, and should otherwise be deleted.
3. **Human-comparable form** — add a short grouping (e.g. base32 of the first 20 digest bytes as
   `ABCD-EFGH-…`) for out-of-band verbal comparison, alongside the 64-char hex. A 64-hex string is not
   realistically comparable by a player.
4. **Operator affordances** — print the fingerprint and identity key path at startup and in `status`;
   add `--identity-key <path>` to import/provide a key and `--print-fingerprint` to print and exit;
   create the key file with restrictive permissions and document that it is durable state that must be
   backed up and never regenerated casually.
5. **Durability guard** — reuse one `FileStore` instance for key init (`NetServer.cs:43`) and refuse to
   start if the identity key is unreadable rather than silently generating a new identity
   (`SecureSessionHandshake.cs:29-39` currently cannot distinguish "absent" from "corrupt").

### 4.2 Protocol

Three tiers, ordered by cost and by what each actually buys.

**Tier 1 — ship now, zero wire change (fixes impersonation on reconnect):**

- Client: hard-fail on absent/invalid/off-curve static material; recompute the fingerprint; look up the
  pin for the logical endpoint (§5); **if a pin exists and mismatches, abort before computing or
  sending the token proof or the registration secret** (`SecureHandshake.cs:129,138`).
- Client: if no pin exists, refuse to proceed without an explicit user decision (§5.3).
- This is purely client-side and works against already-deployed v2 servers, because the static key is
  already on the wire and already in the transcript.

**Tier 2 — additive wire field, no new primitives (removes the token-MAC oracle and the
"fail after leaking" window):**

- Server, after sending `AuthChallenge`, sends an `IdentityProof` = `HMAC-SHA256( SHA-256(label ‖
  staticSecret), label2 ‖ transcriptHash )` (new `PacketType`, or an added field on a second
  `AuthChallenge` frame). Only the holder of the static private key can produce it, because
  `staticSecret` requires it (`SecureSessionHandshake.cs:128`).
- Client verifies `IdentityProof` against the pinned public key **before** emitting the token proof
  (`SecureHandshake.cs:138`).
- Cost: one HMAC each way, reusing `SecureChannel.cs` helpers. No signature algorithm, no new curve
  code, nothing that .NET 4.7.2/Mono must implement.

**Tier 3 — optional signature (needed only for *third-party* identity assertions):**

- `ECDSA-P256-SHA256` over `transcriptHash`, carried as an added `AuthChallenge` field. Server side is
  straightforward on .NET 8 (`ECDsa.Create(ECCurve.NamedCurves.nistP256)`,
  `ImportParameters(new ECParameters { Curve = …, D = scalar, Q = … })`, `VerifyHash`/`SignHash`).
- Client side is the risk: `Shared/Protocol/P256.cs` is hand-rolled specifically because BCL EC on the
  Unity/Mono net472 runtime has historically been unreliable. **[HYPOTHESIS]** `ECDsaCng`/
  `ECDsa.Create` do not work on the target Unity Mono runtime; this must be tested on a real game build
  before anyone depends on it. The safe path is ~50 lines of ECDSA verify added to `P256.cs` (point ops
  and modular inverse already exist at `P256.cs:88-146`).
- Value beyond Tier 2: reusable for **signed directory listings** and operator-published fingerprints.
  It does not strengthen the client's own decision relative to a pin, which is why it is optional.

### 4.3 Directory (must not become the trust anchor)

**[FACT]** The directory is unauthenticated: `POST /api/v1/servers/register` accepts any `Host`,
`Port` and metadata with no proof of control (`Directory/Program.cs:21-50`), and the listing schema has
no identity field (`Shared/Models/ServerDirectoryModels.cs:6-19`). Anyone can publish a listing
claiming any host.

**[FACT]** The mod fetches the directory over HTTP(S) with no signature check
(`Mod/Networking/ServerDirectoryClient.cs:17-32`).

Therefore: a fingerprint added to the listing is an **advisory hint at best** and must never be
auto-trusted, precisely because it arrives from a party that has not proven control of the endpoint. If
listings are to carry identity, they need Tier 3 signatures *from the server identity key itself*, and
the client must still compare against its local pin when one exists. A better bootstrap that avoids
trust-on-first-use entirely: fetch
`https://<host>/.well-known/sfs-enhanced-identity.json` over HTTPS (DNS + TLS authenticate the hostname,
which is what a domain-hosting server actually controls) and compare the published fingerprint before
connecting. `HttpClient` is already used on net472 (`ServerEndpointResolver.cs:16`,
`ServerDirectoryClient.cs:12`; see `docs/BUILD_FIXES_2026-08-21.md:7` for the reference requirement).

---

## 5. TOFU pin storage schema, alias normalization, confirmation, refusal, re-pin

### 5.1 Explicit statement about TOFU

**TOFU does not, and cannot, stop a first-contact MITM.** On the first connection there is nothing to
compare against; an attacker who is on-path at that moment is pinned and thereafter looks legitimate.
TOFU converts "MITM any time" into "MITM only if present at first contact, or at re-pin". It is still
worth shipping, because it is cheap and it removes the persistent, unauthenticated attack. But it is
not "trustworthy server identity" by itself — that requires one of: an HTTPS
`/.well-known` fingerprint (§4.3), an operator-published fingerprint compared out of band, or a
long-lived pin that the user accepted for a *specific* server. Say this plainly in UI and docs; do not
label a TOFU-pinned server "verified".

### 5.2 Per-endpoint pin key

```jsonc
// key: endpoint_key                       (see normalization below)
// value:
{
  "endpoint_key":      "play.example.com#auto",
  "host_input":        "play.example.com",   // exactly what the user typed, for display
  "declared_port":     null,                 // int when the user gave one; null when SRV/default
  "resolved_host":     "eu1.example.com",    // informational only, NOT part of the key
  "resolved_port":     7777,
  "fingerprint_algo":  "p256-sha256-v1",
  "fingerprint":       "9f2c…64 hex…",       // computed locally from observed StaticX/StaticY
  "server_name":       "Example Orbital",    // what the server claimed, for display
  "trust":             "pinned",             // "pinned" | "pinned_legacy" | "unverified"
  "first_seen_utc":    "2026-11-16T12:00:00Z",
  "last_verified_utc": "2026-11-16T12:00:00Z",
  "re_pin_count":      0
}
```

Storage: `PlayerPrefs` under `sfs_enhanced.pins`, mirroring `ServerHistory`
(`Mod/Networking/ServerHistory.cs:10-39`), with atomic replaces (temp + swap, as `FileStore.Save` does
at `FileStore.cs:37-47`) and `PlayerPrefs.Save()`. Note that PlayerPrefs is plaintext and readable by
other local processes; a local attacker can edit pins, and that is accepted — a pin store defends
against network attackers, not against a compromised machine.

### 5.3 Normalization and SRV alias preservation

The key must follow **the name the user chose to trust**, not whatever SRV happened to resolve to
today. Otherwise legit SRV rotation (target or port change) silently produces false pin-change alarms,
and a hostile resolver can steer the connection while still matching the pin.

- Strip scheme and whitespace; lowercase; strip a single trailing `.`; reject empty or >253 chars.
  Do not attempt IDN remapping beyond the platform's; if the input is non-ASCII, show the exact string
  being pinned. Bracketed IPv6 literals keep their brackets: `[2001:db8::1]:7777`.
- User supplied a port → `endpoint_key = "host:port"`.
- No port supplied (SRV or default 7777) → `endpoint_key = "host#auto"`.
- The SRV target and port from `ServerEndpointResolver.ResolveAsync` (`ServerEndpointResolver.cs:24-55`)
  are recorded as `resolved_host`/`resolved_port` **for display and diagnostics only**. The pin belongs
  to `host#auto`. A pin must never be keyed on the resolved target, because that both breaks on
  legitimate rotation and hands the resolver the ability to choose a different pin bucket.
- Do not silently alias `host` and `host#auto`; if a user first connects via SRV and later types
  `host:7777`, that is a different key and must be confirmed as a separate server, or the UI must offer
  an explicit "same server?" merge. Silent aliasing is how pins get bypassed.
- Note [FACT]: resolution goes through Cloudflare DoH (`ServerEndpointResolver.cs:31-35`) with a
  `catch {}` that falls back to port 7777 (`:44-54`). A hostile or failed resolver changes the target
  but must never be able to change what the client believes about identity — the pin check is what
  enforces this, provided the key is name-based as above.

### 5.4 First-connect confirmation

- **No pin** → do not connect silently. Show: server name as claimed, `host:port` (and resolved
  target if it differs), the full fingerprint, the short comparable grouping, and an explicit
  "I have not verified this server" statement. Actions: *Trust this server* / *Cancel* / *Compare
  fingerprint* (paste or read out the short form).
- **Existing account for this endpoint but no pin** (i.e. upgrading an install that already has a stored
  token, `ModSettings.cs:32-36`) → prior trust in the endpoint exists without a key record; mark the new
  pin `pinned_legacy` and show a one-time notice. Do not pretend this is a verified identity.
- **Pin matches** → connect. Update `last_verified_utc` only.
- Never accept pin material from the server. There must be no packet that can request, suggest or
  auto-apply a re-pin; the only path is a local user action.

### 5.5 Pin change: refuse, then explicit re-pin

- Mismatch → abort before the token proof is sent (§4.2 Tier 1). Wording must name the failure
  plainly: "This server's identity has changed. The server at `play.example.com#auto` was previously
  `<old>`; it is now `<new>`. If you did not expect this, do not continue — someone may be
  impersonating this server."
- Re-pin requires a separate, second, explicit step (a distinct *Replace pin* action, not the default
  button, ideally typing the first grouping of the new fingerprint), never a single click on the
  mismatch screen, and it increments `re_pin_count` and records `previous_fingerprint` for auditing.
- Refusal must also clear any in-memory session state so a half-established channel cannot be reused
  (`NetClient.Disconnect` already tears down the channel and peers, `NetClient.cs:88-107`).
- The stored token must not be overwritten on a refused or failed connection: `NetClient.cs:73-76`
  currently writes `ModSettings.AuthToken` as soon as the result is accepted, and the token is global
  rather than per endpoint. Per-endpoint token scoping belongs with this work, since a single global
  token means connecting to a different server destroys the credential for the first (§3).

---

## 6. Protocol version and migration strategy

**[FACT]** `HelloPacket.ProtocolVersion2 = "2.0"` (`Packets.cs:11`); the server requires exact string
equality (`SecureSessionHandshake.cs:49-53`); the client always sends `"2.0"`
(`SecureHandshake.cs:57`). There is no negotiation and no downgrade logic. Sealed frames additionally
carry a hard-coded `ProtocolVersion = 2` byte (`SecureChannel.cs:104,206`) and the replay window and
channel count are fixed constants (`SecureChannel.cs:105-109`).

**[FACT]** Token verification depends on: `ComputeTokenKey` (`SecureChannel.cs:29`), label
`"SFSEnhanced-Auth-v2"` (`:14`), and `ComputeTranscriptHash`'s field list and label (`:49-62`). The
registration proof depends on label `"SFSEnhanced-Registration-v2"` and the global salt (`:31-48`).

Migration plan:

1. **Phase 1 (no protocol change).** Client-side pinning per §4.2 Tier 1. Works against deployed "2.0"
   servers. Tokens need no migration because `ComputeTokenKey` and the `AuthLabel` are untouched. The
   only change to trust state is new pin records; treat pre-existing installs as `pinned_legacy`
   (§5.4).
2. **Phase 2 (additive, backward compatible).** Add `IdentityProof` (§4.2 Tier 2) and a structured
   `ServerIdentity { algo, x, y, fingerprint_version }` block to `AuthChallengePacket`. Because the
   payloads are JSON (`LidgrenWire.cs:13-25`), unknown fields are ignored by old peers, so "2.0"
   servers keep working with new clients and vice versa. Gate *requiring* it with server config
   (`RequireIdentityProof`, `MinProtocolVersion`) and client preference, defaulting new deployments to
   required.
3. **Phase 3 ("3.0").** Only when semantics change: bump `HelloPacket` to `"3.0"` and adopt a v3
   transcript that (a) uses length-prefixed fields, (b) includes protocol version, mode,
   `ClientModVersion`, the exchange identifier, and the cipher-suite/identity identifiers, and (c)
   order the messages so the server proves identity before the client reveals the token MAC or
   registration secret. Server accepts `["3.0"]` or `["2.0","3.0"]` per config during the transition,
   and logs which one each client used. Do not change existing labels in place: `…-v2` labels are pinned
   into every stored token verifier, so reusing them would invalidate all accounts. Add `-v3` labels.
4. **Registration password (independent, do now).** Replace the global fixed salt
   (`SecureChannel.cs:33`) with a per-server-instance random salt generated at first start and stored
   next to the identity key, and derive the proof inputs so the wire value is not a reusable offline
   verifier (§3). This breaks old clients unless it is versioned, so it belongs with Phase 3, or must
   accept both proofs during transition. Either way, the current construct must not survive into a
   "trustworthy identity" release.
5. **Explicit non-migration.** Do not attempt to convert existing per-server accounts into a
   cross-server identity; `IMPLEMENTATION_PLAN.md:26-32` and `PRODUCT_ARCHITECTURE.md:24-28` are correct
   that `PlayerId` is server-scoped. Identity pinning here is about *which server you are talking to*,
   not about a global account.

---

## 7. Concrete tests

Runnable against the existing harness (`scripts/multiplayer-integration/Program.cs:155-180` already
constructs `SecureHandshakeClient` directly, so handshake-level tests need no game client).

**Identity material**

1. Start the server twice on one data dir → identical fingerprint; delete
   `data/system/server_static_key.json` → fingerprint changes (locks in the durability requirement).
2. `ComputeIdentityFingerprint` is exactly 64 lowercase hex chars and equals the value the client
   computes from the `StaticX/StaticY` it received on the wire (proves the pin is derivable without
   trusting `IdentityFingerprint`).
3. Startup prints the fingerprint; `--print-fingerprint` matches the live handshake.

**Refusal and ordering**

4. Pin `P1`, connect to a server whose key is `P2` → `ConnectAsync` returns false, reason names the
   mismatch, and **no** `AuthResponse.Proof` frame was emitted (instrument the transport, assert frame
   ordering).
5. Rogue server A (own static key) vs pinned client → refused.
6. Rogue server B (copies the real `StaticX/StaticY`, own ephemeral) → handshake fails, and the client
   reports identity/verification failure rather than a generic "connection closed".
7. Rogue server C (omits `StaticX/StaticY`) → hard failure with a specific reason; regression for the
   `new byte[32]` fallbacks at `SecureHandshake.cs:120-125`.
8. Pin match → `trust = pinned`, `last_verified_utc` advances, and `ModSettings.AuthToken` is unchanged
   on any refused connection.

**Pin store / normalization**

9. `PLAY.Example.COM.` and `play.example.com` map to one key; `play.example.com:7777` is a **different**
   key from `play.example.com#auto` (no silent aliasing).
10. SRV target/port change with unchanged name → same pin, no prompt, `resolved_host` updated.
11. Re-pin requires the second explicit confirmation; a single click on the mismatch screen does not
    change anything; `re_pin_count` and `previous_fingerprint` are recorded.
12. Fuzz the store: truncated/missing/garbage JSON yields "no pin" (prompt), never an implicit accept.

**Account/identity namespace (currently broken, see §3)**

13. Connect as `Bob`, then `"Bob "`, then `" bob"` → exactly one account exists; all three authenticate
    to it.
14. Friend request by name resolves to exactly one account.

**Registration password**

15. Capture a registration handshake's wire bytes from a rogue server and assert the new scheme's proof
    is not a reusable offline verifier (i.e. it cannot be recomputed from the transcript hash plus a
    candidate password without a per-instance salt).

**Migration**

16. Client "2.0" (no pin) ↔ server "2.0": connects, prompts for trust.
17. Client "3.0" ↔ server "2.0" with `RequireIdentityProof=false`: connects with a logged downgrade.
18. Client "3.0" ↔ server "2.0" with `RequireIdentityProof=true`: refused with an actionable message.
19. Existing token still authenticates on the new server build (labels unchanged), in both phases 1
    and 2.

**Robustness**

20. `StaticX` length 0/31/33/1024, off-curve point, point at infinity, `StaticX` valid with `StaticY`
    invalid, `StaticX` equal to `StaticY` → all rejected before the token proof is emitted.
21. Flipping any transcript input on the wire (player name, ephemeral, static, challenge) causes the
    handshake to fail; assert the client never accepts a sealed `AuthResult` on a divergent pre-master.
22. Handshake flood: connections that never send `Hello` must be released within the Lidgren
    connection timeout and must not accumulate server tasks; run the harness five consecutive times
    per `IMPLEMENTATION_PLAN.md:39-42`.

---

## 8. Summary judgements

| Question | Answer |
| --- | --- |
| Stable server identity exists? | Keypair yes, persisted in `data/system/server_static_key.json`; user-visible identity no. |
| Stable *comparable* fingerprint? | Computable (`SHA-256(label‖X‖Y)`), but never displayed, printed, pinned or checked. |
| Static key bound into transcript? | Yes — transcript hash and pre-master. |
| Static key authenticated to client? | **No.** The client ignores `IdentityFingerprint` and has no pin store. |
| TOFU alone stops first-connect MITM? | **No.** It bounds the attack to first contact / re-pin. |
| Current first-connect MITM possible? | **Yes**, trivially, for full session impersonation, plus a token-MAC/registration-password oracle. |
| Minimum viable fix | Client-side pin over the observed static point, checked before the token proof — no wire change. |
| Needed for real trust | Tier 2 identity-proof MAC (and/or HTTPS `/.well-known` fingerprint); Tier 3 signature only for third-party assertions. |
| Blocking non-identity defects found | Global fixed registration salt; global (not per-endpoint) auth token; duplicate account names via whitespace; README falsely claims TLS + pinning. |
