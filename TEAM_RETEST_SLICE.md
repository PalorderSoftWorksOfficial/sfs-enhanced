# Retest — Profile + App Shell slice after D1–D10 fixes

**Reviewer:** independent QA (read-only)
**Subject:** current working tree after the manager's D1–D10 fix pass (SetPlayerName/Connect, split notifications, disconnect event, rich-text hardening, privacy filters, claim restore, list counts, back stack, flight guard, BaseScene mount fallback).
**Snapshot tested:** tree as of the `ChatPage`/`ChatLogWindow` fix (2026‑11‑15 00:23). Recorded hashes: `ChatPage.cs 5473e2ac…`, `ChatLogWindow.cs f6023af4…`, `MultiplayerMenu.cs 21ff521f…`, `FriendsPage.cs aa5d6d6d…`. The tree moved once during this retest (chat sliding-window fix landed mid-review); everything below refers to the post-fix snapshot, not the earlier one.
**Files changed by reviewer:** this report only. No source, script or shared doc modified; no fixes applied.

---

## 1. Verdict

**NOT SHIPPABLE — two blockers, one layout residual.**

- **D2 (mount) is still a gate.** The fallback chain is implemented, but the shell has never been seen on screen and the fallback target (`Base_PC` / `UI`) cannot be confirmed from source. Until the probe is run, nothing at screen level is proven.
- **R1 is a new regression from the D10 fix.** `Hide()` routes through the new back-stack `Close()`, so **F8 on a sub-page goes back one page instead of closing**, and the scene-unload hide path can leave the shell undisposed.
- **R2 is a layout collision left by the D6 fix.** With 3+ friends, the third row overlaps the "PENDING REQUESTS" heading.

Eight of ten prior defects are fixed in source and confirmed by a clean build and a passing integration suite — including the **chat log regression** found earlier in this retest session, which was fixed during the review and is now covered by a harness assertion.

---

## 2. What was re-run (final snapshot)

```
$ rsync -a --exclude .git --exclude bin --exclude obj ./ /tmp/retest2/repo/
$ dotnet build SFSEnhanced.sln
  SFSEnhanced.Shared, BuildSfsPack, SFSEnhanced.Directory, SFSEnhanced.Mod, SFSEnhanced.Server
Build succeeded.  0 Warning(s)  0 Error(s)   BUILD_EXIT=0

$ dotnet build scripts/multiplayer-integration/MultiplayerIntegration.csproj
Build succeeded.  0 Warning(s)  0 Error(s)   HARNESS_BUILD_EXIT=0

$ dotnet Server/bin/Debug/net8.0/SFSEnhanced.Server.dll --port 18795 --data /tmp/retest2/data </dev/null &
$ SFS_INTEGRATION_PORT=18795 dotnet scripts/multiplayer-integration/bin/Debug/net8.0/MultiplayerIntegration.dll
[assert] chat window sliding math
MULTI_CLIENT_INTEGRATION_PASSED   HARNESS_EXIT=0
```

Coverage: self profile read, `SessionsCount >= 2` after token reconnect, over-long name rejected with reason, valid update applied and read by another player, presence privacy enforced for a non-self viewer, rate-limit rejection on a burst of 8 updates, friend accept/persist, world create/join/leave, and the new chat sliding-window math. It does **not** cover hidden `LastSeenUtc`, friend-list presence privacy, claim restore, or any UI behaviour.

Limitations: no SFS runtime, so **canvas mounting and visual appearance remain UNVERIFIED**. In-place `dotnet build` is still blocked by root-owned `obj/`; the clean copy is the authoritative build.

---

## 3. Status of D1–D10

| # | Defect | Status |
| --- | --- | --- |
| D1 | Settings player name dead / reverts | **FIXED** (source) |
| D2 | Canvas mount / invisible shell | **PARTIAL — UNVERIFIED GATE** |
| D3 | Page rebuild steals input focus | **FIXED** (source; chat window now covered by harness) |
| D4 | Transport drop never reaches UI | **FIXED** (source) |
| D5 | Profile controls overlap | **FIXED** (source geometry) |
| D6 | Silent list truncation | **FIXED for honesty; R2 layout residual** |
| D7 | Presence privacy partial | **FIXED** |
| D8 | TMP rich-text injection | **FIXED** |
| D9 | Claim state not restored | **FIXED** (source) |
| D10 | Escape/back, flight pause | **PARTIAL — back fixed, R1 regression** |

### D1 — FIXED
`SetPlayerName` validates and persists (`Mod/UI/MultiplayerMenu.cs:213–228`, writes at `:226`); `Connect` sends `ModSettings.PlayerName` (`:255`); the stale `_playerName` cache is gone; `BuildWindow` only defaults an empty value (`:133`). `SettingsPage.cs:20` routes the input to `app.SetPlayerName`, and the helper text is now true. Minor: an over-long paste leaves the input showing the rejected value while the stored name keeps its previous value.

### D2 — PARTIAL, UNVERIFIED (acceptance gate)
`CreateScreenHolder` tries `BaseScene` then `CurrentScene` and rejects an unattached holder (`MultiplayerMenu.cs:106–127`); `Show` refuses to open an invisible window (`:84–96`). `Builder.CreateHolder(mode, name)` exists (`Assembly-CSharp/SFS.UI.ModGUI/Builder.cs:33`), `BaseScene` targets scene `Base_PC` root `UI` (`:242–252`), `CurrentScene` targets root `--- UI ---` (`:255–262`). Whether `Base_PC` is loaded and has that root at home-load time is unknowable from source; if not, `Show()` logs "No canvas accepted the multiplayer screen" and the feature is invisible.

### D3 — FIXED
Split notifications: `Changed → RenderChrome`, `ContentChanged → UpdateContent` (`Mod/UI/App/AppShell.cs:88–89`); rebuild only when `page.NeedsRebuild` (`:118–125`, `RenderPage` `:127–140`). Inputs are created only in `Build` and live on the page host; page `Update` mutates labels or clears a sub-region. Chat now also uses stable incremental rendering with a sliding window (`ChatPage.cs:78–100`) and an explicit redraw rule (`Shared/Models/ChatLogWindow.cs:16–19`): when the 13th message arrives, `NeedsRedraw` clears and re-renders the window at `start = count - 12`, so the log no longer freezes or stacks rows. Harness asserts this math for counts 13–60 (`[assert] chat window sliding math`). In-game focus/IME behaviour is still unverified.

### D4 — FIXED
`NetClient.Disconnected` fires once for a live drop (`Mod/Networking/NetClient.cs:254–263`, `wasConnected` guard, so explicit `Disconnect()` does not double-fire). `MultiplayerMenu` subscribes (`:44`), `HandleTransportDrop` resets connection/world/claims/chat and sets an error status with `NotifyContent` (`:204–211`); chrome then shows `OFFLINE` / `Not connected` (`AppShell.cs:158–190`). Code-verified; not exercised against a killed socket.

### D5 — FIXED (source geometry)
`formTop = -18` (`ProfilePage.cs:63`); inputs at `-18/-68/-118`, x −160, width 420 (x span −370…50); toggles at `-18/-68`, x 250, width 240 (130…370) (`:68–69`); SAVE/RESET at `-168` (`:71–72`). No overlapping x ranges, 10/18 px vertical gaps, form ends at −185 inside the content region (−228…212). Visual confirmation still needs the game.

### D6 — FIXED for honesty; R2 residual
Counts now disclose totals: friends (`FriendsPage.cs:79`), requests (`:120`), worlds (`RoomsPage.cs:141`), servers (`ServersPage.cs:113`). Caps remain (friends 3, incoming 2, outgoing 2, worlds 2, servers 3) but are no longer silent. **Residual R2:** the third friend row collides with the pending-requests heading (below).

### D7 — FIXED
`presenceVisible = isSelf || privacy.ShowPresence` gates `LastSeenUtc`, `Online`, `CurrentWorldId` (`Server/Social/ProfileService.cs:54,64–66`); `FriendsService.ToInfo` applies the same rule (`Server/Social/FriendsService.cs:75–77`); `AllowFriendRequests` is enforced on the request path (`:37`). Harness verifies `Online`/`ShowPresence` for another viewer; hidden `LastSeenUtc` is code-verified only. Minor disclosure: the `ShowPresence` preference itself is returned to other viewers.

### D8 — FIXED
`UiKit.Label` disables rich text on every label (`Mod/UI/Kit/UiKit.cs:14`), covering chat (`ChatPage.cs:99`), list rows and profile fields. Server `Strip` removes `<`, `>`, zero-width, bidi and BOM from profile text (`ProfileService.cs:149–160`). Residual: chat text is not sanitized server-side (`NetServer.Social.cs:52–65` trims/length-caps only), so protection is client-render-only; bio still accepts newlines but renders in a single 20 px label.

### D9 — FIXED
`WorldJoinAck` sets `_state.LocalClaimId = FindOwnClaim(join)` (`MultiplayerMenu.cs:601`); the helper matches own id + `ClaimShape.Build` + local build (`:710–724`). Rooms page updates the button text in place (`RoomsPage.cs:68–69,97–101,145`); leave/reset clear it. Packet shape is correct (`ClaimShape { Build, Region }`, claims stored with `Shape`). Not runtime-tested.

### D10 — PARTIAL
Back stack: `Navigate` pushes (`AppShell.cs:95–107`), `Back` pops (`:108–116`), `MultiplayerScreen.Close` returns early on successful back (`Mod/UI/MultiplayerScreen.cs:23–27`) — Escape from Settings returns Home, Escape from Home closes. Flight: `CanOpenHere` (`MultiplayerMenu.cs:77`), `World_PC` guard in `Show` (`:88–92`), F8 check (`Mod/ModMain.cs:101`) — the shell no longer opens or pauses in flight. Trade-offs: in-flight chat is now unavailable entirely, and **R1** below.

---

## 4. New defects

### R1 — F8/“hide” navigates back instead of closing — **S2**
`MultiplayerMenu.Hide()` (`:161–169`) calls `_screen.Close()` when the screen is current; `Close()` is now overridden to `Back()` first (`MultiplayerScreen.cs:23–27`).
*Repro:* open the shell → Settings → press F8 (or leave a world while the shell is open, via `SceneHelper.OnWorldSceneUnloaded += Hide`).
*Expected:* shell closes and disposes.
*Actual:* F8 pops to Home and the shell stays open; several F8 presses are needed. On the scene-unload path the window is not disposed at all (`OnScreenClosed`/`DestroyWindow` skipped), so the old `AppShell` and its `Changed` subscriptions survive until the next close and keep rendering into destroyed labels.

### R2 — Third friend row overlaps the requests section — **S3**
Friends region is 150 tall at host y 14 (`FriendsPage.cs:33`); rows start at +30 and step −48 (`:63,75`), so with 3 friends the third row sits at host −52 (span −30…−74). The "PENDING REQUESTS" heading is at host −54 (`:34`) and the requests region starts at −25 (`:35`), so the row crosses both. Visible whenever a player has 3+ friends.

---

## 5. Explicitly unverified

- **Canvas mounting (D2)** — BaseScene vs CurrentScene resolution, parent chain, canvas scale, visibility at 1000×640.
- **Visual appearance** of every page — overlaps, TMP sizes, toggle/input alignment, contrast.
- **Input behaviour in game** — caret/focus/IME under packet traffic; the source path is correct.
- **Claim restore** end-to-end (needs a real build in a real world).
- **Disconnect propagation** against a real socket kill.

---

## 6. Remaining non-blocking items (unchanged)

1. `_profileBuckets` never evicted (`NetServer.Profile.cs:14`).
2. Rate-limit token consumed before validation (`NetServer.Profile.cs:52`).
3. `SessionsCount++` before the `MaxPlayers` rejection (`NetServer.cs:120`).
4. `ProfileRequest` unthrottled and answers by id or name — account-existence oracle.
5. Directory failure indistinguishable from "no servers".
6. `LoadingWorlds`/`LoadingServers` not reset in `ResetConnection`.
7. Saved `ModSettings.Port` not prefilled on the Servers page.
8. Raw player GUID shown as connection detail.
9. Bio input/label single-line; newlines allowed by the server.

---

## 7. Prioritized list for the manager

1. **BLOCK — D2 mount.** Run the 5-minute in-game probe (active scene, `Base_PC` presence, root names, resulting `transform.parent`). *(S1)*
2. **BLOCK — R1 hide semantics.** Add a force-close path for `Hide()` that bypasses `Back()` (clear history then close, or `ScreenManager.main.CloseCurrent()`), keeping `Back()` for Escape only. *(S2)*
3. **FIX — R2 friends region.** Move "PENDING REQUESTS" below the third row, or start rows higher / reduce row pitch. *(S3)*
4. **Then** the unchanged minors in section 6; none block the slice, but #1–#4 there are cheap.

**Shippability:** not shippable while D2 is unverified and R1 is open. D1, D3, D4, D5, D7, D8, D9 can be accepted on source evidence plus the harness, with D5/D9 still awaiting the same in-game session. The chat regression found during this retest is fixed and covered.

---

## 8. Commands used

```bash
export DOTNET_ROOT=/home/administrator/.dotnet; export PATH=$DOTNET_ROOT:$PATH
rsync -a --exclude .git --exclude bin --exclude obj ./ /tmp/retest2/repo/
cd /tmp/retest2/repo && dotnet build SFSEnhanced.sln                      # 0 warnings / 0 errors
dotnet build scripts/multiplayer-integration/MultiplayerIntegration.csproj
dotnet Server/bin/Debug/net8.0/SFSEnhanced.Server.dll --port 18795 --data /tmp/retest2/data </dev/null &
SFS_INTEGRATION_PORT=18795 dotnet scripts/multiplayer-integration/bin/Debug/net8.0/MultiplayerIntegration.dll
# [assert] chat window sliding math
# MULTI_CLIENT_INTEGRATION_PASSED
```

Read-only throughout: source touched only by `git diff`/`grep`/`sed`; the build copy lives under `/tmp/retest2/repo`.
