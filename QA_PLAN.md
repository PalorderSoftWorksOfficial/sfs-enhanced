# QA plan

Read-only verification protocol for the current tree. Nothing here is a claim about code that was not
run; section 6 lists what is still unverified.

## 1. Build (clean copy)

The in-repo `obj/` trees are root-owned, so build a mirror that excludes build output.

```bash
export DOTNET_ROOT=/home/administrator/.dotnet; export PATH=$DOTNET_ROOT:$PATH
rsync -a --exclude .git --exclude bin --exclude obj ./ /tmp/qa/repo/
cd /tmp/qa/repo
dotnet build SFSEnhanced.sln                                        # expect 0 warnings, 0 errors
dotnet build scripts/multiplayer-integration/MultiplayerIntegration.csproj
```

## 2. Servers

Three servers are required. The suite exercises open registration, closed registration and a
registration password. Each needs its own data directory.

```bash
SERVER=/tmp/qa/repo/Server/bin/Debug/net8.0/SFSEnhanced.Server.dll
setsid bash -c "tail -f /dev/null | dotnet $SERVER --port 18961 --data /tmp/qa/open"   > /tmp/qa/open.log   2>&1 &
setsid bash -c "tail -f /dev/null | dotnet $SERVER --port 18962 --data /tmp/qa/gated --registration-password integration-server-password" > /tmp/qa/gated.log  2>&1 &
setsid bash -c "tail -f /dev/null | dotnet $SERVER --port 18963 --data /tmp/qa/closed --no-registration" > /tmp/qa/closed.log 2>&1 &
```

Keep stdin open (`tail -f /dev/null |`); the console loop treats EOF as shutdown. Killing the wrapper
does not kill the server, so terminate by the `dotnet` process, not the `bash` parent.

## 3. Integration suite

```bash
SFS_INTEGRATION_PORT=18961 \
SFS_INTEGRATION_PASSWORD_PORT=18962 \
SFS_INTEGRATION_CLOSED_PORT=18963 \
dotnet scripts/multiplayer-integration/bin/Debug/net8.0/MultiplayerIntegration.dll
```

Required: nine `[assert]` lines and `MULTI_CLIENT_INTEGRATION_PASSED`, exit 0.

| Assertion | Covers |
| --- | --- |
| staged storage never promotes a dropped write | pending-key staging, readback verify, failure does not damage the previous value |
| account authentication is explicit and failures are distinct | token mode on an unknown name creates nothing; explicit registration creates exactly one; duplicate and case/whitespace variants resolve deterministically; wrong token; registration closed; password required, wrong and correct |
| timed wait does not orphan the next frame | harness transport correctness |
| chat window sliding math | `ChatLogWindow` redraw rules |
| endpoint keys canonicalize without aliasing | `host:port` vs `host#auto`, IDNA, IPv6, port conflicts, invalid labels |
| identity pins: first use, match, changed, no overwrite, persistence, corrupt store | pin store contract |
| endpoint tokens migrate once and never cross endpoints | legacy token migration, per-endpoint scoping |
| identity is decided before the auth proof is sent | pin gate ordering |
| player names are canonicalized before lookup | trim-once account namespace |

Run it five times sequentially against the same three servers and require five pass banners. One green
run is not evidence.

## 4. Server-side identity evidence

`data/system/server_static_key.json` must persist across restarts, and the startup line
`Identity: <64 hex>` must remain identical. `data/system/registration_salt.json` exists only after a
server has started; deleting it starts a new salt for that instance and invalidates nothing else.

## 5. Manual review checklist (no runtime needed)

1. `SecureHandshake.RunAsync` returns on a `FirstUse`/`Changed` decision before `ComputeTokenProof`,
   before the key exchange, and before any `AuthResponse` is sent.
2. No path sends `AuthResponsePacket.RegistrationSecret`; only `RegistrationAuthPacket.Proof` inside
   the sealed channel is sent, and only after a sealed `RegistrationPrompt`.
3. A `RegistrationPrompt` that arrives as a plain frame is ignored by `ReceiveAnyAsync`, so an attacker
   cannot drive the password proof out of the client.
4. `ServerIdentityPinStore.Accept` is reachable only from an explicit user action, and `Lookup` never
   writes.
5. `NetClient` keeps `PendingIdentity` after a refused attempt and writes a token only after
   `AuthResult`.

## 6. Unverified

- **No Spaceflight Simulator runtime is available here.** The D2 mount, window geometry, the new
  trust/identity panels, the account panels and every visual claim are unproven. This is a release
  gate for the user to close in game.
- Real DNS SRV lookups are untested; only pin-key invariance around a resolved target is tested.
- Live `NetClient` behaviour against a hostile server (as opposed to the scripted harness transport)
  is untested.
- The registration password is exercised end to end, but sealed-frame confidentiality is argued from
  the channel construction, not from a captured-packet experiment.

## 7. In-game probe (user)

1. Build the mod (`dotnet build SFSEnhanced.sln -c Release`, or install the produced `.pack`).
2. Launch SFS, open the Play menu, open Multiplayer. Confirm the window appears on the `Base_PC`
   canvas at 1000×640, that navigation renders, and that Escape walks back and then closes.
3. Connect to a server for the first time: confirm the identity panel shows the 64-hex fingerprint in
   four lines plus the grouped short form, that *Cancel* leaves nothing stored, and that *Trust and
   connect* succeeds and lists the server under Settings.
4. Press *Create account* against a fresh server, then *Connect* afterwards; confirm both work and that
   a wrong name reports "no account on this server".
5. Start a server with `--registration-password <pw>`: confirm the password field appears only after
   the server asks for it, and that a wrong password is refused with a distinct message.
6. F8 must be inert inside the flight scene.
