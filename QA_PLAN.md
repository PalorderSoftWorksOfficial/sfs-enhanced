# QA plan

Read-only verification protocol for the current tree. Nothing here is a claim about code that was not
run; §7 lists what remains unverified.

## 1. Build (clean copy)

The in-repo `obj/` trees may contain root-owned files from earlier runs, so build a mirror that
excludes build output.

```bash
export DOTNET_ROOT=/home/administrator/.dotnet; export PATH=$DOTNET_ROOT:$PATH
rsync -a --delete --exclude .git --exclude bin --exclude obj ./ /tmp/qa/repo/
cd /tmp/qa/repo
dotnet build SFSEnhanced.sln                                        # expect 0 warnings, 0 errors
dotnet build scripts/multiplayer-integration/MultiplayerIntegration.csproj
```

## 2. Servers

Three servers are required: open registration, a registration password, and closed registration. The
upload tests depend on **one** concurrent upload and a short idle limit, so those flags are mandatory.

```bash
SERVER=/tmp/qa/repo/Server/bin/Debug/net8.0/SFSEnhanced.Server.dll
FLAGS="--upload-max-concurrent 1 --upload-idle-seconds 2"
setsid bash -c "tail -f /dev/null | dotnet $SERVER --port 18961 --data /tmp/qa/open $FLAGS"   > /tmp/qa/open.log   2>&1 &
setsid bash -c "tail -f /dev/null | dotnet $SERVER --port 18962 --data /tmp/qa/gated --registration-password integration-server-password $FLAGS" > /tmp/qa/gated.log 2>&1 &
setsid bash -c "tail -f /dev/null | dotnet $SERVER --port 18963 --data /tmp/qa/closed --no-registration $FLAGS" > /tmp/qa/closed.log 2>&1 &
```

Keep stdin open (`tail -f /dev/null |`); the console loop treats EOF as shutdown. Kill by the `dotnet`
process, not the `bash` wrapper. `status` reports `rooms` and `uploads pending/expired`; the log
records `[room]` lifecycle lines and `[upload] expired` lines.

## 3. Integration suite

```bash
SFS_INTEGRATION_PORT=18961 \
SFS_INTEGRATION_PASSWORD_PORT=18962 \
SFS_INTEGRATION_CLOSED_PORT=18963 \
dotnet scripts/multiplayer-integration/bin/Debug/net8.0/MultiplayerIntegration.dll
```

Required: fourteen `[assert]` lines and `MULTI_CLIENT_INTEGRATION_PASSED`, exit 0.

| Assertion | Covers |
| --- | --- |
| staged storage never promotes a dropped write | pending-key staging, readback verify, failure does not damage the previous value |
| account authentication is explicit and failures are distinct | token mode never creates; explicit registration creates once; duplicates/case/whitespace; wrong token; closed; password required/wrong/correct; 20 sequential registrations and sign-ins |
| chat text is filtered and validated server-side | `chat_rejected` for markup, whitespace/zero-width/newline collapse, no echo of a refused message |
| peer lifecycle survives 100 alternating handshakes and abrupt drops | transport/session churn cannot wedge the server |
| region claims cannot overlap another player's region or build | overlap refusal, build-containment refusal, identical re-claim idempotency |
| world data plane enforces access, upload bounds and controller release | private/public download auth, upload ordering/duplicate/size/count/sanitisation, expired upload code, control release |
| rooms are server-enforced: capacity, visibility, host close and auto-close | capacity bounds, one room per world, public-over-private refusal, listing population, full refusal, non-host close refusal, `RoomClosed` fan-out, friends-only listing/join for accepted friends, world-join routed into the room, empty-room auto-close |
| timed wait does not orphan the next frame | harness transport correctness |
| chat window sliding math | `ChatLogWindow` redraw rules |
| endpoint keys canonicalize without aliasing | `host:port` vs `host#auto`, IDNA, IPv6, port conflicts, invalid labels |
| identity pins: first use, match, changed, no overwrite, persistence, corrupt store | pin store contract |
| endpoint tokens migrate once and never cross endpoints | legacy token migration, per-endpoint scoping |
| identity is decided and proven before the auth proof is sent | pin gate ordering, IdentityProof verification, omission and forgery refusal, no MAC after either failure |
| player names are canonicalized before lookup | trim-once account namespace |

Every live connect additionally asserts `SecureHandshakeResult.IdentityProofVerified`, so the whole
suite fails if the real server ever stops proving its identity key.

Run it five times sequentially against the same three servers and require five pass banners. One green
run is not evidence.

## 4. Server-side identity evidence

`data/system/server_static_key.json` must persist across restarts and the startup line
`Identity: <64 hex>` must stay identical. `data/system/registration_salt.json` is created on first
start; deleting it starts a new salt for that instance.

Corrupt-key refusal (must fail closed, never regenerate):

```bash
mkdir -p /tmp/qa/id && printf '{"PrivateKeyBase64":""}' > /tmp/qa/id/system/server_static_key.json 2>/dev/null || \
  (mkdir -p /tmp/qa/id/system && printf '{"PrivateKeyBase64":""}' > /tmp/qa/id/system/server_static_key.json)
dotnet $SERVER --port 18966 --data /tmp/qa/id < /dev/null; echo "exit=$?"   # expect exit 1
```

The output must say `Startup failed: The server identity key file is present but does not contain a
usable key…` and the file must be unchanged. A server that regenerated the key would silently
invalidate every existing pin.

Protocol refusal: a 2.x `Hello` must receive `AuthError` with code `protocol_version`; this is
code-verified only (the harness always speaks 3.0).

## 5. Manual review checklist (no runtime needed)

1. `SecureHandshake.RunAsync` returns on a `FirstUse`/`Changed` decision before the key exchange and
   before any `AuthResponse`.
2. The client sends its token MAC only after `VerifyIdentityProofAsync` returns null; a missing or
   forged proof returns with `IdentityProofMissing`/`IdentityProofInvalid`.
3. No path sends `AuthResponsePacket.RegistrationSecret`; only `RegistrationAuthPacket.Proof` inside
   the sealed channel, after a sealed `RegistrationPrompt`.
4. A `RegistrationPrompt` that arrives as a plain frame is ignored by the receive loop.
5. `ServerIdentityPinStore.Accept` is only reachable from an explicit user action; `Lookup` never
   writes.
6. `HandleChat` refuses before broadcasting and never echoes to the sender.
7. `HandleRoomJoin` checks visibility, capacity and world access in that order, and `HandleWorldJoin`
   routes a room-hosted world through it.
8. `RoomClose` is host-only; `CloseRoomIfEmpty` runs on leave and disconnect.

## 6. Release/publish checks

```bash
dotnet publish Server/SFSEnhanced.Server.csproj -r linux-x64 -c Release
dotnet publish Server/SFSEnhanced.Server.csproj -r win-x64 -c Release
```

Verify the published binary starts, prints the identity fingerprint, and exits on EOF (no interactive
console hang).

## 7. Unverified

- **No Spaceflight Simulator runtime is available here.** D2 mount, window geometry, the identity
  panel, the room page and every visual claim are unproven; this is a release gate for the user.
- The directory error state is code-verified only; the harness never exercises a failing directory.
- Real DNS SRV lookups are untested; only pin-key invariance around a resolved target is tested.
- Live `NetClient` behaviour against a hostile server is untested; the hostile cases are exercised
  with the scripted harness transport.
- Sealed-frame confidentiality is argued from the channel construction, not from a captured-packet
  experiment.
- Rooms are not persisted, so no restart test exists yet (BUGS B-19).

## 8. In-game probe (user)

1. Build the mod (`dotnet build SFSEnhanced.sln -c Release`, or install the produced `.pack`).
2. Launch SFS, open Play → Multiplayer. Confirm the window mounts on `Base_PC` at 1000×640, that the
   seven nav entries fit, and that Escape walks back and then closes.
3. Connect for the first time: the identity panel shows the 64-hex fingerprint in four lines plus the
   grouped short form; CANCEL stores nothing; TRUST AND CONNECT succeeds.
4. Create an account on a fresh server, reconnect with it, and confirm a wrong name reports
   "no account on this server".
5. On a password server, confirm the password field appears only after the server asks.
6. Rooms: open a public room, join from a second client, fill capacity, leave, close as host, and
   confirm the closed room disappears from the list.
7. Chat: send `<b>x</b>` and confirm the refusal message; send a newline/zero-width message and confirm
   it renders collapsed.
8. F8 must be inert inside the flight scene.
