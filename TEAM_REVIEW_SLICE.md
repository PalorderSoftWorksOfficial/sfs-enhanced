# Review — First Profile + App Shell Vertical Slice

**Reviewer:** independent QA (read-only)
**Scope:** working tree at HEAD `78b25b0` + uncommitted slice (`Mod/UI/App/`, `Mod/UI/Kit/`, `ProfileService`, `NetServer.Profile`, profile packets 80–83, `MultiplayerMenu`, `NetClient`, `ModMain`, `WorldModels`).
**Files changed by reviewer:** this report only. No source, script or shared doc modified; no fixes applied.
**Compared against:** `TEAM_REPORT_QA.md` (baseline defects) and `TEAM_REPORT_DESIGNER.md` (slice-1/slice-3 acceptance).

---

## 1. Verdict

**Conditionally acceptable, not shippable as-is.** The server half of the slice is solid: profile read/update is self-scoped, validated, rate-limited, privacy-filtered for presence, and backed by a passing end-to-end test. The UI half works structurally (one shell, one content region, real pages, no fake rooms/creations) but has three functional defects the player will hit immediately — the Settings player name is a dead control that reverts, any incoming packet rebuilds the page and steals input focus, and a transport drop leaves the UI claiming it is still online. Two acceptance criteria from the designer's slice 1 are not met (Escape/back, directory error vs empty), and the in-flight pause problem is unchanged.

Two items could not be settled from source and must be probed in game before merge: the canvas mount (`SceneToAttach.CurrentScene` looks for a root literally named `--- UI ---`, which appears nowhere in `Assembly-CSharp`), and the residual idle CPU after the console-loop fix.

---

## 2. Build and run status — independently executed

Clean copy, `Dependencies/` included, `bin/`/`obj/` excluded:

```
$ rsync -a --exclude .git --exclude bin --exclude obj ./ /tmp/slice-review/repo/
$ dotnet build SFSEnhanced.sln
  SFSEnhanced.Shared -> .../netstandard2.0/SFSEnhanced.Shared.dll
  BuildSfsPack -> .../net8.0/BuildSfsPack.dll
  SFSEnhanced.Directory -> .../net8.0/SFSEnhanced.Directory.dll
  SFSEnhanced.Mod -> .../net472/SFSEnhanced.dll
  SFSEnhanced.Server -> .../net8.0/SFSEnhanced.Server.dll
Build succeeded.  0 Warning(s)  0 Error(s)   EXIT=0
```

Existing integration suite, run against a live server (same shell, `</dev/null`):

```
$ SFS_INTEGRATION_PORT=18791 dotnet scripts/multiplayer-integration/bin/Debug/net8.0/MultiplayerIntegration.dll
MULTI_CLIENT_INTEGRATION_PASSED      HARNESS_EXIT=0
```

That covers the new profile assertions: self read, `SessionsCount >= 2` after reconnect, over-long name rejected with a reason, valid update applied and read back by another player, `ShowPresence=false` enforced for other viewers, and a burst of 8 updates producing at least one rejection. Server log shows one expected `[auth-failed]` and clean disconnects.

`ConsoleCommandLoop` no longer spins on EOF (`Server/Program.cs:99` — `if (line == null) break;`) and the server stayed alive with stdin closed. Idle follow-up: 8 s with no clients still showed **~19 % instantaneous CPU** (26.7 % lifetime, per-thread sample failed to attribute it). Far below the old 118 %, but not zero; flagged, not blocking this slice.

Not run: in-place build (root-owned `obj` from the baseline report is still an environment problem), the mod in game, any UI test.

---

## 3. Highest-priority defects

### D1 — Settings "Player name" is a dead control that silently reverts — **S2**
`Mod/UI/MultiplayerMenu.cs:40` captures `_playerName` once; `:103` writes it back to `ModSettings.PlayerName` on every window open; `:172` `Connect` uses `_playerName`, not `ModSettings.PlayerName`. `Mod/UI/App/Pages/SettingsPage.cs:20–23` edits `ModSettings.PlayerName` and says "Reconnect to use the new name".
*Repro:* open shell → Settings → change name → close → reopen. *Expected:* new name used on next connect. *Actual:* value reverted on reopen, and Connect still sends the name captured at mod load until the game restarts. The status message is false.

### D2 — Canvas mount is unverified and likely to leave the shell invisible — **S1 if it fails (UNVERIFIED)**
`MultiplayerMenu.cs:87` creates the holder with `Builder.SceneToAttach.CurrentScene`. `Assembly-CSharp/SFS.UI.ModGUI/Builder.cs:255–262` parents only when the active scene has a root named exactly `--- UI ---`; `Builder.cs:260` is the **only** occurrence of that string in the entire decompiled game. The new guard at `MultiplayerMenu.cs:88–95` (`transform.parent == null` → log + destroy) correctly refuses an unparented window, but the observable result would be: click MULTIPLAYER, nothing happens.
*Probe (5 min, in game):* log `SceneManager.GetActiveScene().name` and the root object names on home load; if `--- UI ---` is absent, switch to `BaseScene` (`Builder.cs:242–252`, root `"UI"` in `Base_PC`) with a fallback chain. Baseline used the same mount without the guard, so this is pre-existing risk, not a regression — but the slice cannot be accepted until the shell is seen on screen.

### D3 — Every state change rebuilds the page and destroys focused inputs — **S2**
`Mod/UI/App/AppShell.cs:110` calls `ClearContent()` (`:143`, destroys every child) on each `Render()`; `Render` is subscribed at `AppShell.cs:87` to `AppState.Changed`, which `Notify()` raises on every chat message, every status line, every friend list response, and every profile change.
*Repro:* connect, open Chat, type while another player sends messages. *Expected:* text keeps being entered. *Actual:* the TMP input is destroyed and recreated per incoming line; focus, caret and any IME composition are lost, and the chat becomes unusable on an active server. Same for Profile inputs while `Saving profile...` updates arrive.

### D4 — Transport drop never reaches the UI — **S2**
`Mod/Networking/NetClient.cs:251` sets `_connected = false` in `PumpAsync`'s `finally` but raises no event; nothing resets `MultiplayerMenu._state.Connected` (only `AuthResult`/`AuthError`/explicit `Disconnect()` do). *Repro:* connect, kill the server. *Expected:* offline state + error. *Actual:* green "N/M online", DISCONNECT shown, chat/sends silently no-op, `AppState.PlayerId` still displayed.

### D5 — Profile page controls overlap — **S3**
`Mod/UI/App/Pages/ProfilePage.cs:58` places the presence toggle at `y + 26` and `:59` the friend-request toggle at `y - 14`, where `y = -166` — i.e. −140 and −180 — while the accent field sits at −118 (height 40, spans −98…−138) and the SAVE/RESET row at −166 (`:62–63`). Toggle 1 overlaps the accent field; toggle 2 overlaps SAVE. *Expected:* separated rows. *Actual:* clipped/overlapping hit targets; the SAVE button is partly covered.

### D6 — Lists are silently truncated with no "more" affordance — **S3**
`FriendsPage.cs:47` (`shown >= 3`), `:67` (`pendingShown >= 2`), `:79` (`pendingShown >= 3`); worlds and servers cap at 3/4. No scroll, no count, no "view all". *Expected:* truthful "showing 3 of N" or scrolling. *Actual:* the list looks complete; the 4th friend is unreachable, and the designer's "30 friends without overflow" criterion is not met.

### D7 — Presence privacy is partial — **S3**
`Server/Social/ProfileService.cs:64` always returns `LastSeenUtc`, even when `ShowPresence` is false, and `FriendsService.BuildFriendList` still reports `Online`/`CurrentWorldId` to friends regardless of the toggle. *Expected:* privacy toggle hides presence consistently. *Actual:* a viewer who is not the player learns the exact last-seen timestamp and friends still see "Online · in a room". The toggle's effect is narrower than its label implies.

### D8 — Player-supplied text reaches TMP labels unescaped — **S3**
`ProfileService.cs:147–155` strips control characters only; `<`, `>`, zero-width and bidi characters pass. Rendered as rich text at `ChatPage.cs:42` (`line.From`, `line.Text`), `ProfilePage.cs:30–33` (name, bio), and in `ListRow` titles for world names. *Repro:* send chat `<size=200>X` (or set it as a world name). *Expected:* literal text. *Actual:* markup executes in every viewer's chat/room list — layout break and impersonation surface. Strip `<`/`>` (or disable rich text) server-side and client-side.

### D9 — Claim state is not restored on join — **S3**
`MultiplayerMenu.OnPacket` handles `WorldJoinAck` but never reads `join.Claims`, and `Mod/UI/App/Pages/RoomsPage.cs:85` drives the button from `state.LocalClaimId`. *Repro:* claim your build, leave, rejoin. *Expected:* "UNCLAIM". *Actual:* "CLAIM BUILD"; pressing it issues a duplicate `ClaimCreate`.

### D10 — Escape closes the whole product; there is no back — **S3**
`MultiplayerScreen` does not override `OnEscape`; `BasicMenu` inherits `CloseMode.Current` (`Assembly-CSharp/SFS.UI/BasicMenu.cs`), and `AppShell.Navigate` is a flat page id switch with no stack. Also `MultiplayerScreen : BasicMenu` keeps `pauseWhileOpen = true` (`Assembly-CSharp/SFS.Input/Screen_Menu.cs:9`), so opening the shell during flight still sets `Time.timeScale = 0` and removes flight input via `ScreenManager.OpenScreen`.
*Expected (designer slice 1):* Escape from Settings → Home; Escape from Home → close. *Actual:* Escape from any page closes multiplayer entirely. In-flight chat remains pausing — unchanged from baseline, but the shell does not address it.

---

## 4. Profile security / privacy checks

| Check | Result | Evidence |
| --- | --- | --- |
| Updates restricted to the authenticated self | **PASS** | `NetServer.Profile.cs:47` uses `conn.Account`; no target id is accepted on update |
| No `AuthTokenKey` / private fields on the wire | **PASS** | `Packets.cs` `PlayerProfile` is an explicit whitelist; `AuthTokenKey` never copied |
| Length limits enforced server-side | **PASS** | 32 name / 280 bio / `#RRGGBB` regex (`ProfileService.cs:15,18,114,125`); over-long rejected with a reason (verified by harness) |
| Control characters stripped | **PASS (partial)** | `ProfileService.cs:147–155`; rich-text/zero-width/bidi not handled (D8) |
| Rate limiting | **PASS** | 5 burst / 1 per s (`NetServer.Profile.cs:11,52,86`); verified by harness (8 sends → ≥1 rejection) |
| Bucket lifetime | **FAIL (minor)** | `_profileBuckets` (`:14`) is never evicted — one entry per player id forever |
| Token burned on rejected edits | **FAIL (minor)** | token taken (`:52`) before validation, so 5 invalid attempts lock the editor for a second |
| Presence privacy | **PARTIAL** | `ProfileService.cs:64` leaks `LastSeenUtc`; friends still see `Online` (D7) |
| Read-other-player path | **PASS server / UNREACHABLE UI** | `ProfileRequest.PlayerId` works (harness); `IAppActions` has no open-profile action and no page calls it |
| Backward compatibility with existing account JSON | **PASS by construction, untested** | New fields are public fields with initializers (`WorldModels.cs:42–47`), so absent JSON values keep defaults; `Privacy` is null-checked in `ProfileService.Build`/`TryApply` and `FriendsService`. No legacy fixture test exists |
| Persistence / races | **PARTIAL** | `ProfileService.cs:140` saves the whole record; `FileStore.Save` locks only the write (`FileStore.cs:32–40`), not read-modify-write, so a concurrent friend accept / `Touch` / second session can clobber the profile |
| Session counter | **INACCURATE** | `NetServer.cs:120` increments before the `MaxPlayers` rejection and once per handshake, so full-server rejects and duplicate sessions inflate a stat the UI shows |

---

## 5. UI lifecycle

- One holder, one window, one content container per open — the stacked-window model is genuinely gone (`AppShell.Build`, `MultiplayerMenu.DestroyWindow`).
- `AppShell.Dispose` unsubscribes `Changed` and destroys the window (`AppShell.cs:127–134`); `MultiplayerMenu`'s own `Client.OnPacket`/`ProfileChanged`/scene subscriptions (`:42–43,45–50`) are never removed, but the menu lives for the mod's lifetime, so the leak is bounded.
- `Builder.GetRandomID()` is called per shell open and its static set is never cleared (`Builder.cs:17–30`) — one leaked id per open/close.
- `MultiplayerScreen.OnClose` destroys the holder that hosts the screen component; Unity defers destruction so `OpenScreen` still completes, but this is fragile ordering worth simplifying.
- `RefreshWorlds` sets `LoadingWorlds = true` with no timeout or error path (`MultiplayerMenu.cs:236`); a dropped request leaves "Loading rooms..." forever. The nav account label has the same failure mode ("Syncing account..." if the initial `ProfileRequest` is lost, `NetClient.cs:78`).
- Directory failure is still rendered as an empty result: `ServerDirectoryClient.cs:23–27` returns `new List<>()` on every exception, and `ServersPage` shows "The directory returned nothing" — the designer's "error vs empty distinguishable" criterion fails.
- Pages are truthful about what exists: the worlds page says rooms are a planned layer, `HOME` stats are real, no fake creations or creator tools. The remaining honesty gaps are the false "Player name saved" message (D1), the truncated lists (D6), and the online state after a drop (D4).

---

## 6. Reachability of existing functionality

| Feature | Reachable | Notes |
| --- | --- | --- |
| Direct connect / disconnect | Yes | Servers page; port field starts empty, saved `ModSettings.Port` is not shown (`ServersPage.cs:12,27`) |
| Public directory | Yes | error rendered as empty |
| Host locally | Yes | requires executable path in Settings |
| Create / join / leave world | Yes | public worlds only |
| Chat | Yes | usable only when traffic is quiet (D3) |
| Friends add / accept / decline / invite | Yes | capped at 3 / 2 / 3 rows (D6) |
| Claims | Yes | state lost on rejoin (D9) |
| Time warp request / vote | Yes | Rooms page, unchanged |
| Build + rocket sync | Yes | automatic, untouched by the slice |
| Profile read / edit / privacy | Yes (self) | other-player profile unreachable from UI |
| F8 toggle, Play-menu button | Yes | same mount risk as D2 |
| Registration-password servers | **No** | unchanged baseline defect: no field, `ConnectAsync(..., registrationPassword: null)` |
| World upload / download | **No UI** | pre-existing; no controls for it in the shell |

---

## 7. Hygiene

- New comments were introduced against the repo rule (`PRODUCT_VISION.md:105`): `Shared/Protocol/Packets.cs` adds `// --- Profiles ---`, `// ---- Profiles ----` and a `/// <summary>` on `ProfileRequestPacket`. All new files under `Mod/UI/App`, `Mod/UI/Kit`, `ProfileService` and `NetServer.Profile` are comment-free.
- Pre-existing dead protocol paths are still present and now more visible: `ClaimListRequest/Response` (62/63) have no handler, and the client still handles `PlayerConnected/Disconnected`, which the server never sends.
- `ServersPage.cs:35` prints the raw `PlayerId` GUID as connection detail (designer C20).
- Client caps world names at 48 chars; the server accepts any length, and names are rendered as rich text (D8).

---

## 8. Commands used

```bash
export DOTNET_ROOT=/home/administrator/.dotnet; export PATH=$DOTNET_ROOT:$PATH
rsync -a --exclude .git --exclude bin --exclude obj ./ /tmp/slice-review/repo/
cd /tmp/slice-review/repo && dotnet build SFSEnhanced.sln                 # 0 warnings, 0 errors
dotnet build scripts/multiplayer-integration/MultiplayerIntegration.csproj
dotnet Server/bin/Debug/net8.0/SFSEnhanced.Server.dll --port 18791 --data /tmp/slice-review/data </dev/null &
SFS_INTEGRATION_PORT=18791 dotnet scripts/multiplayer-integration/bin/Debug/net8.0/MultiplayerIntegration.dll
# MULTI_CLIENT_INTEGRATION_PASSED
```

**Limitations:** no in-game verification (Unity assets and a running game are unavailable here), so D2 and all layout claims are from source geometry, not screenshots; the idle-CPU residue is measured but unattributed; no legacy `accounts/*.json` fixture was replayed.

---

## 9. For the manager

1. Block merge on **D2** until someone opens the shell in game and reports the mount probe result.
2. Assign **D1, D3, D4** first — they are small, player-visible, and each contradicts a string the UI shows.
3. Decide slice scope on **D5–D10**; D8 (markup injection) should be fixed now because chat reaches every client.
4. The server profile work can be accepted as-is with two follow-ups: evict rate-limit buckets and stop counting rejected sessions.
