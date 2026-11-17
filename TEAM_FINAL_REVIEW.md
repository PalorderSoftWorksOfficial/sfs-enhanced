# Final QA Review — Profile + App Shell slice (R1/R2 pass)

**Reviewer:** independent QA (read-only)
**Snapshot:** current working tree at 2026‑11‑15 00:29 CET, hashes recorded: `MultiplayerMenu.cs 3e3ce911…`, `MultiplayerScreen.cs 7340fbec…`, `FriendsPage.cs 2e516e9a…`, `ChatPage.cs 3c827355…`, `ChatLogWindow.cs fac0b974…`, harness `Program.cs 864dcfb7…`.
**Files changed by reviewer:** this report only. No source, shared doc or script modified.
**Basis:** `TEAM_RETEST_SLICE.md` (D1–D10, R1, R2) and the manager's R1/R2 fix pass.

---

## 1. Verdict

**NOT SHIPPABLE.** Three gates remain open:

1. **Integration suite is NOT PASSING.** The independent run on port 18796 hung for >180 s after Alice and Bob connected and the wrong-token handshake was rejected; it never printed `MULTI_CLIENT_INTEGRATION_PASSED` and the server never logged the matching disconnects. The manager has identified the cause (orphaned `BlockingCollection.Take` readers stealing the next frame) and is fixing it; until a new independent 5-run batch passes, the suite result is **NOT REPRODUCIBLE**.
2. **D2 mount is still an in-game release gate.** The BaseScene→CurrentScene fallback is implemented in source, but the shell has never been seen on screen; the fallback target (`Base_PC` / `UI`) cannot be confirmed without the game.
3. **R1 is only half proven.** The current-screen force-close path is correct by trace; the **non-current-screen stack cleanup is unresolved** — it destroys the screen GameObject without `OnClose`, and the `CloseCurrent()` no-op at stack depth 1 remains an unhandled edge. No runtime proof either way.

R2's geometry collision is fixed (third friend row no longer hits the requests heading), with two small cosmetic residuals. No regressions were found in D1/D3/D4/D5/D7/D8/D9/D10; the capped-chat tail-sequence fix is correct in source and has harness assertions, but this run did not reach them.

---

## 2. Build — verified

```
$ rsync -a --exclude .git --exclude bin --exclude obj ./ /tmp/final/repo/
$ cd /tmp/final/repo && dotnet build SFSEnhanced.sln
  SFSEnhanced.Shared, BuildSfsPack, SFSEnhanced.Directory, SFSEnhanced.Mod, SFSEnhanced.Server
Build succeeded.
    0 Warning(s)
    0 Error(s)
BUILD_EXIT=0

$ dotnet build scripts/multiplayer-integration/MultiplayerIntegration.csproj
Build succeeded.  0 Warning(s)  0 Error(s)   HARNESS_BUILD_EXIT=0
```

Solution and harness compile cleanly on a fresh copy of the current tree.

## 3. Integration — NOT PASSING / NOT REPRODUCIBLE

```
$ dotnet Server/.../SFSEnhanced.Server.dll --port 18796 --data /tmp/final/data </dev/null &
$ SFS_INTEGRATION_PORT=18796 dotnet scripts/.../MultiplayerIntegration.dll
<no output; killed after >180 s>
```

`/tmp/final/server.log` ends at:

```
[connect] Alice_f8f1c253 (8f9b346b…)
[connect] Bob_f8f1c253 (8991d3e4…)
[auth-failed] handshake rejected
```

— no further packets, no `[disconnect]` lines. The harness produced no assertion output and no pass banner.

**Cause as identified by the manager (consistent with the code):** readers are left orphaned when a timeout race wins. In the harness's `PeerTransport.ReceiveRawAsync` (`scripts/multiplayer-integration/Program.cs:47–57`) the read is `Task.Run(() => _frames.Take(ct))`; the handshake paths race the connection task with `Task.WhenAny(connectedTcs.Task, Task.Delay(8000))` (`:142`, `:165`) and then abandon the transport (`PeerTransport` created at `:145`, `:167`, drained at `:152`). An abandoned `Take` stays blocked on the same `BlockingCollection` and consumes the *next* frame, so the leg that should receive the wrong-token `AuthError` can be starved indefinitely while the peer keeps waiting. (`TestPeer.ReceiveAsync` at `:193` uses a direct `TryTake(out frame, timeoutMs, token)` and is not itself the orphan source.)

Because this also explains a **flaky, not deterministic** failure, one green run would not be evidence. Required exit criterion: a fresh independent run of the full suite, five consecutive green runs on a fresh data directory, each printing `[assert] chat window sliding math` and `MULTI_CLIENT_INTEGRATION_PASSED` with exit 0 and matching `[disconnect]` lines.

---

## 4. R1 — force-close semantics

**Current-screen path: correct by trace.**
`Hide()` (`Mod/UI/MultiplayerMenu.cs:161–172`) detects whether the shell is `ScreenManager.main.CurrentScreen`, nulls `_screen` first, and calls `MultiplayerScreen.ForceClose()` (`:169`; implementation `Mod/UI/MultiplayerScreen.cs:29–32` = `base.Close()`). `BasicMenu.Close()` → `ScreenManager.CloseCurrent()` → `OpenScreen(previous)` → `CurrentScreen.OnClose()` on the shell → `DestroyWindow()` + `OnScreenClosed()` (`MultiplayerScreen.cs:34–40`). `Hide()` then repeats `OnScreenClosed()`/`DestroyWindow()`, both idempotent (`AppShell.Dispose` returns when `!_built`). F8 on Settings now closes in one press, and the scene-unload path disposes the shell.

**Non-current path: UNRESOLVED.** When `_screen != null` but not current (another screen pushed on top), `Hide()` skips `ForceClose` and calls `OnScreenClosed()` directly, which destroys the holder GameObject **without** `OnClose()`/`ScreenManager` notification. The destroyed screen can remain in `ScreenManager.screenStack`, and `ScreenManager.Update()` calls `CurrentScreen.ProcessInput()` each frame. Also, if the shell is the only stack entry, `CloseCurrent()` is a no-op (`Assembly-CSharp/SFS.Input/ScreenManager.cs:60–67`), so `ForceClose()` would not close it. Both are unreachable in the common home-menu flow (stack depth ≥ 2) but are unproven and unhandled.

**Verdict:** R1 fixed for the reported repro (F8 / scene unload while the shell is open and current); non-current stack cleanup remains an open risk, to be closed by an in-game check (open shell, push a dialog/screen over it, press F8).

## 5. R2 — FriendsPage geometry

New layout (`Mod/UI/App/Pages/FriendsPage.cs:34–37,64–77,92–124`) with `y = 200`:

| Element | Host y | Span |
| --- | --- | --- |
| Friends region (140 tall) | 10 | −60…80 |
| Friend rows (start 24, step 44) | 34 / −10 / −54 | 12…56 / −32…12 / −76…−32 |
| "PENDING REQUESTS" heading | −102 | −93…−111 |
| Requests region (96 tall) | −154 | −106…−202 |
| Incoming cards (20, −22) | −134 / −176 | −112…−156 / −154…−198 |
| Outgoing line (fixed −60) | −214 | −204…−224 |

- Third friend row now ends at −76, **17 px above** the heading at −93 → original collision **fixed**.
- Residual A (S4): the "Showing 3 of N friends" line (only when >3 friends) lands at host −88, spanning −78…−98, overlapping the heading band −93…−111 by 5 px.
- Residual B (S4): incoming cards step 42 with height 44 → 2 px overlap between the two cards.
- Residual C (S4): the outgoing line at −214 sits 12 px outside the requests region and 4 px above the content bottom (−228); visible because containers do not mask.

In-game rendering remains **UNVERIFIED** (no SFS runtime); numbers are rect arithmetic.

## 6. Regression check on D1–D10

| Item | Status | Evidence |
| --- | --- | --- |
| D1 player name | **No regression** | `SetPlayerName` / `Connect` unchanged (`MultiplayerMenu.cs:213–228`, `:255`) |
| D3 stable inputs | **No regression** | chrome/content split unchanged (`AppShell.cs:88–89,118–140`); chat renders only into `_logRegion` |
| D4 disconnect propagation | **No regression**; minor improvements | `Disconnected` path unchanged; `ResetConnection` now also clears `LoadingServers/LoadingWorlds` (`AppState.cs:94–95`) |
| D5 profile geometry | **No regression** | `ProfilePage.cs` untouched in this pass |
| D6 truthful counts | **No regression** | counts still emitted (`FriendsPage.cs:79,128`; `RoomsPage.cs:141`; `ServersPage.cs:113`) |
| D7 presence privacy | **No regression** | `ProfileService.cs:54,64–66`; `FriendsService.cs:75–77` |
| D8 rich text / sanitization | **No regression** | `UiKit.cs:14`; `ProfileService.cs:155` |
| D9 claim restore | **No regression** | `MultiplayerMenu.cs:601,710–724`; `RoomsPage.cs:68–69,97–101` |
| D10 back/flight | **No regression** | `AppShell.cs:95–116`; `MultiplayerScreen.Close`; `CanOpenHere`/`World_PC` guard; F8 check |

**Capped-chat tail-sequence fix (60-message history):** correct. `ChatLine.Sequence` is assigned per message (`MultiplayerMenu.cs:615,621`) and `ChatLogWindow.NeedsRedraw` now also compares the tail sequence when count is unchanged (`Shared/Models/ChatLogWindow.cs:13–21`), so when the log is capped at 60 a new message still forces a redraw and the window re-renders indexes 48–59. Harness assertions cover `NeedsRedraw(48,48,60,60,100,101) == true` and the stable-tail case == false (`Program.cs:545–550`) — but **this run never reached them**, so the evidence is source + assertion code, not an executed pass.

**Other fixes seen and code-verified:** session counter moved after the max-players rejection (`NetServer.cs:120–128`); rate-limit bucket removed on disconnect (`:260`); token refunded on rejected validation (`NetServer.Profile.cs:64,80–88`); saved port prefilled (`ServersPage.cs:27`).

## 7. D2 — mount and visuals (release gate, UNVERIFIED)

`CreateScreenHolder` still tries `BaseScene` (`Base_PC` root `UI`, `Builder.cs:242–252`) then `CurrentScene` (`--- UI ---`, `:255–262`), rejecting unattached holders (`MultiplayerMenu.cs:106–127`). No SFS runtime is available here, so canvas mounting, window visibility at 1000×640 and every visual claim (D5/R2) remain **UNVERIFIED** and must be signed off in game before release.

## 8. Remaining issues (non-blocking except as noted)

1. **[BLOCKER]** Integration suite hangs — orphaned `Take` race; needs fix + 5 green runs.
2. **[BLOCKER]** D2 mount and all visuals unverified in game.
3. **[OPEN RISK]** R1 non-current screen stack cleanup (destroyed screen left in `ScreenManager`; `CloseCurrent` no-op at stack depth 1).
4. `LastSeenUtc` privacy is code-verified only (harness asserts `Online`, not the timestamp).
5. Friend-list presence privacy and claim restore are not covered by automated tests.
6. Residual R2 cosmetics: 5 px summary/heading overlap, 2 px card overlap, outgoing line outside its region.
7. Pre-existing: chat text is not sanitized server-side (client `richText=false` is the only defence); bio single-line but newlines accepted; `ProfileRequest` unthrottled (account-existence oracle); directory errors indistinguishable from an empty list; raw GUID shown as connection detail.
8. Persistence race: profile/friend writes still use unlocked read-modify-write (`FileStore` locks the write only).

## 9. Shippability statement

**Not shippable.** A clean build is not evidence of correctness: the current tree compiles with zero warnings and zero errors, but the end-to-end suite does not complete, the mount is unproven, and one UI lifecycle edge is unhandled. The correct next step is the harness fix followed by a repeatable 5-run green batch, then a single in-game session covering mount, F8/back/force-close, profile layout, friends layout with 3+ friends and 2+ requests, and the chat log past 60 messages.

---

## 10. Commands used

```bash
export DOTNET_ROOT=/home/administrator/.dotnet; export PATH=$DOTNET_ROOT:$PATH
rsync -a --exclude .git --exclude bin --exclude obj ./ /tmp/final/repo/
cd /tmp/final/repo && dotnet build SFSEnhanced.sln                    # 0 warnings / 0 errors, exit 0
dotnet build scripts/multiplayer-integration/MultiplayerIntegration.csproj   # 0 / 0, exit 0
dotnet Server/bin/Debug/net8.0/SFSEnhanced.Server.dll --port 18796 --data /tmp/final/data </dev/null &
SFS_INTEGRATION_PORT=18796 dotnet scripts/multiplayer-integration/bin/Debug/net8.0/MultiplayerIntegration.dll
# hung >180 s; server log stops at [auth-failed] handshake rejected; no pass banner
```

Read-only throughout: source touched only by `git diff`/`grep`/`sed`; the build copy lives under `/tmp/final/repo`.
