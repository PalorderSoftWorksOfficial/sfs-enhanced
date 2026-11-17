# SFS Enhanced — UX / Product Design Report

**Author role:** UX / Product Designer (specialist, reporting to the manager)
**Date:** 2026-11-14
**Mode:** READ-ONLY source audit. No source file, shared doc, or asset was modified. This file is the only artifact produced.
**Scope of audit:** `PRODUCT_VISION.md`, `agent.md`, `README.md`, `docs/`, `Mod/` (UI, Social, Networking, World, Integrated, Packaging, ModMain, ModSettings), `Shared/` (Protocol, Models), `Server/` (Networking, Social, Persistence, World, Directory program), `Directory/Program.cs`, and the decompiled toolkit `Assembly-CSharp/SFS.UI`, `Assembly-CSharp/SFS.UI.ModGUI`, `Assembly-CSharp/SFS.Input`, plus the game's own UI screens and settings code.

> **State drift during this audit (read this first).** While I was reading, a parallel implementer landed `Mod/UI/App/` (`AppPage`, `AppState`, `IAppActions`), `Mod/UI/Kit/` (`Theme`, `UiKit`), `Server/Social/ProfileService.cs`, `Server/Networking/NetServer.Profile.cs` and profile packets 80–83, and deleted `Mod/UI/MultiplayerMenu.Panels.cs` / `.Packets.cs`. The critique in §2 describes the tree as it was when the audit started. The report is **reconciled against the new files** in §6.0 (measured corrections to `Theme`/`UiKit`), §5.5 (Profile is now real), §9/C17 and §11. The new code is a genuine improvement and a sound direction — this report is written to **extend and correct it**, not to replace it.

---

## 0. Executive summary

The current multiplayer UI is **one 900×620 unit window with eleven buttons and five stacked text inputs**, plus a set of sibling windows that are opened *on top of* a hidden main window. It is not a navigation model, it has no persistent identity surface, no home/discover/library, and its "status line" is the only feedback channel. The problem is structural, not cosmetic: there is no shell, no destination model, no reusable component layer, and no state vocabulary (loading / empty / error / offline are all one grey label).

The SFS toolkit is **narrower than a web stack but sufficient** to build a real application shell. What it gives us for free: a screen stack with Escape semantics and pause control, real buttons with hover/selected/disabled states, TMP labels with alignment/size/style, TMP input fields, toggles, sliders, layout groups, a real scroll element with momentum, translucent surfaces, remote images, confirmation dialogs, a native toast, and a full-screen progress screen. What it does **not** give us: grids, list virtualization, local image loading, dropdowns in the mod GUI layer, tooltips, popovers, toasts that queue, per-item focus rings, or any accessibility beyond what we author.

Therefore every recommendation below is expressed as **either** a direct `SFS.UI.ModGUI.Builder` / `SFS.UI` API call, **or** a named adapter class with the exact Unity type it wraps. Where the *data* does not exist (profiles, creations, room metadata), I say so explicitly and specify the additive contract, rather than designing a screen that would have to lie.

**Three highest-value structural decisions I am asking the manager to accept:**

1. **One shell, one content region, replace-in-place navigation** (not stacked windows, not hidden main + panel windows). Escape = back one level, then close.
2. **Two shells, not one** — a full "Home" shell (Home/Build scenes) and a compact non-pausing in-flight overlay (World scene). The current single modal design cannot serve both, and pausing the game in flight is a *correctness* problem, not just a UX one (see §4.4).
3. **A component layer before any screen work.** `EnhancedUI` (tokens + primitives) must land first; otherwise screens get re-hand-built out of `CreateLabel`/`CreateButton` and we reproduce today's problem with nicer colors.

---

## 1. Ground truth: what the toolkit actually permits

Everything in this section is verified against the decompiled source, not assumed.

### 1.1 Screen lifecycle and navigation

`SFS.Input.ScreenManager` (`Assembly-CSharp/SFS.Input/ScreenManager.cs`)

- Holds `Stack<Func<Screen_Base>> screenStack`, exposes `CurrentScreen`, `OpenScreen(Func<Screen_Base>)`, `CloseCurrent()`, `CloseStack()`, `GetStackCount()`.
- `OpenScreen` **pushes**, calls `CurrentScreen.OnClose()`, instantiates the new screen, calls `OnOpen()`, then sets `Time.timeScale` from `PauseWhileOpen`.
- `CloseCurrent()` is a no-op when the stack count is 1 — **you cannot pop the root screen**. Consequences for us: the shell must be pushed onto an existing root, and "back" at depth 1 must call `Close()` on ourselves rather than relying on `CloseCurrent`.
- Screens are `MonoBehaviour`s; the stack stores *factories*, and `CloseCurrent()` re-instantiates the previous factory. Any screen whose `OnClose` destroys its GameObjects is therefore **rebuilt from scratch when you go back**. In-shell state (scroll offsets, filters, drafts) must be held in a controller, not in the view.

`SFS.Input.Screen_Base` / `Screen_Menu` / `BasicMenu`

- `Screen_Menu.ProcessInput()` runs registered key actions then handles `Escape` per `OnEscape` (`CloseMode.None|Current|Stack`).
- `Screen_Menu.pauseWhileOpen` is a **public field** defaulting to `true`; `PauseWhileOpen => pauseWhileOpen`. This is our lever for the in-flight overlay (§4.4).
- `BasicMenu.OnEscape => CloseMode.Current` (protected override; not virtual-public — **a subclass cannot widen it without re-declaring `ProcessInput` or overriding `Close`**). `BasicMenu.Close()` already falls back to deactivating `menuHolder` with `closeAnyway = true` — today's mod uses exactly that (`MultiplayerScreen.Bind` sets `closeAnyway = true`), which is why Escape on a sub-panel closes the *whole* multiplayer UI.
- `Screen_Game.PauseWhileOpen => false`, and `OpenScreen` calls `OnClose()` on it — meaning pushing any `Screen_Menu` in the World scene **removes the flight input element** (`TouchElements.elements`) and **freezes the sim**. Confirmed and material (see §4.4).

**Design consequence:** navigation *inside* our product must not use `ScreenManager`. It uses a controller-level "route" value that rebuilds the content region. `ScreenManager` is used only to enter/exit the product (one push, one pop).

### 1.2 Mount points and canvas

`SFS.UI.ModGUI.Builder.AttachToCanvas(gameObject, SceneToAttach)`:

- `BaseScene` → finds scene `"Base_PC"`, root object named `"UI"`, parents there.
- `CurrentScene` → finds the **active scene**'s root object named `"--- UI ---"`, parents there.
- If the named root is not found, the holder is created **but never parented to a canvas** (silently renders nowhere).

Scene names are `Base_PC`, `Home_PC`, `Hub_PC`, `Build_PC`, `World_PC` (`Assembly-CSharp/SFS/SceneLoader.cs`); the base scene is loaded additively and never made active.

⚠ **High-risk item, needs a 5-minute runtime probe:** `MultiplayerMenu.Show()` creates its holder with `SceneToAttach.CurrentScene`. From the Play menu the active scene is the *home* scene, while the `"UI"` root name is used by the `BaseScene` branch. If `Home_PC` has no `"--- UI ---"` root, the multiplayer screen is unparented and invisible while `ScreenManager` has already paused the game — which matches the vision's "screens feel disconnected / below expectation". **Probe:** log `SceneManager.GetActiveScene().name` and list root object names on `OnHomeSceneLoaded`, then check which branch parents successfully. The new shell should prefer an explicit, verified mount (`Base_PC/UI`, fallback `--- UI ---`, fallback canvas creation) and never depend on the active scene.

`SFS/VideoSettingsComponent.cs`: `scaler.referenceResolution = new Vector2(1f, 1600f / settings.uiScale)` — **this single line defines the entire layout coordinate system** and is why `ModGUI` positions are integers like `0`, `310`, `-572`. See §3.

### 1.3 Layout primitives available

| Need | Real API | Notes |
|---|---|---|
| Absolute placement | `GUIElement.Position` → sets `rectTransform.localPosition` **raw** | Anchor-dependent. `Rect_Utility.SetPosition` exists but `GUIElement` bypasses it — do not mix the two. |
| Free container | `Builder.CreateContainer(parent, x, y)` → `Container` (`GUIElement`) | No size; add children with `Position`. |
| Layout | `Window/Container/Box.CreateLayoutGroup(Type.Vertical|Horizontal, childAlignment, spacing, padding, disableChildSizeControl)` | Wraps `VerticalLayoutGroup`/`HorizontalLayoutGroup` + `GetOrAddComponent`. |
| Declarative layout | `SFS.UI.ElementGenerator.VerticalSpace(int)`, `HorizontalGroup(...)`, `MenuGenerator.CreateMenu(...)` with `ButtonBuilder`/`TextBuilder`/`SizeSyncerBuilder` | This is the **game screen** system (`Screen_Menu` + `OptionsMenuDrawer`), different from ModGUI. Produces an auto-sized stacked panel — good for confirmations/settings, bad for a card grid. |
| Scroll | `Window.EnableScrolling(Type.Vertical)` → `ChildrenHolder.GetComponent<ScrollElement>().vertical = true` | **`GetComponent`, not `GetOrAddComponent`.** If the window prefab's `Children Holder` lacks a `ScrollElement`, this throws. Adapter must guard/clone-or-add. |
| Scroll (manual) | `SFS.UI.ScrollElement` (`RegisterScrolling(Button)`, `RegisterScrolling(Screen_Game)`, `Move`, `PercentPosition` 0..1, `ClampType` None/Window/Mask, momentum) | `PercentPosition` is settable → our sticky-to-bottom chat and "reset to top on filter change" are both one line. |
| Masking | `RectMask2D` (game uses it in `WorldsMenu`) | Available to us; add manually. |
| Grid | **none in ModGUI** | `UnityEngine.UI.GridLayoutGroup` is a stock Unity component and can be `AddComponent`'d — but it is unused anywhere in this codebase, and pairing it with `NewElement` size logic is untested. **Recommendation: do not use GridLayoutGroup.** Compose cards into rows of N using `HorizontalLayoutGroup` (§6.4). Zero new risk. |
| Spacing | `Builder.CreateSpace(parent, w, h)`, `Builder.CreateSeparator(parent, width, x, y)` | Separator is fixed 10 units tall. |
| Surface | `Builder.CreateBox(parent, w, h, x, y, opacity)` → `Box` (`Color`, `Opacity`, `CreateLayoutGroup`) | Box color is a `TranslucentImage` → **blur cost**. |

### 1.4 Text

`SFS.UI.ModGUI.Label` exposes: `Text`, `Color`, `Opacity`, `TextAlignment` (`TextAlignmentOptions`), `AutoFontResize` (`enableAutoSizing`), `FontSize`, `FontStyle` (`FontStyles`). It does **not** expose wrapping, overflow, margins, or line spacing.

- Wrapping/margins require reaching `gameObject.GetComponent<TextMeshProUGUI>()` — a **one-line adapter**, safe because `Label.Init` already asserts TMP (`textAdapter = gameObject.GetComponent<TextMeshProUGUI>()`).
- Rich text is TMP, so `<b>`, `<size=NN>`, `<color=#RRGGBBAA>` work. `WorldElement` already uses `<size=25>` and `LoadingScreen` uses `<color=#00000000>` — precedent exists in shipped game code.
- Font: `SFS.Translations.FontSetter` swaps `Font` on `Text`/`TextMesh` at runtime per language (`Loc.main.Font`, `Base.language.fonts`). **TMP font assets are on the prefab and are not language-swapped by `FontSetter`.** Practical rule: inherit the prefab's TMP font, never ship a font, never assume TMP glyph coverage for CJK/RU without checking.

### 1.5 Controls and their real states

| Control | Real API | States available |
|---|---|---|
| Button | `Builder.CreateButton(...)` → `Button` (`Text`, `TextColor`, `TextOpacity`, `OnClick`); underlying `ButtonPC` has **public fields `Color`, `OverColor`, `ButtonBackOver`, `ButtonText`, `ButtonIcon`, `ButtonOverIcon`** and `SetSelected(bool)`, `SetEnabled(bool)`, `SetState` via tween | normal / hover (over) / selected / disabled (alpha × 0.4). Hover transitions are tweened (~0.1 s). |
| Icon button | `Builder` has **no** `CreateIconButton`. Game has `SFS.UI.IconButton` (`Image icon`, `Button button`, `TextAdapter text`, `Show`) and `ButtonBuilder.CreateIconButton(sizeSync, Sprite, …)` (screen system only) | Our nav icons need an adapter: instantiate `buttonPrefab`, add a sibling `Image` child, or clone the game's `IconButton` prefab. |
| Input | `Builder.CreateTextInput(...)` → `TextInput` (`Text`, `OnChange`, `FieldColor`, `FieldOpacity`, public `TMP_InputField field`) | `field` is public → hint text, character limit, content type, `onEndEdit`, `Select()`, caret, `readOnly` all reachable. |
| Input + label | `Builder.CreateInputWithLabel(...)` → `InputWithLabel { Label label; TextInput textInput; }` | **This is a horizontal label-left/field-right row.** Not a stacked field with helper/error text. Stacked field = composed container. |
| Toggle + label | `Builder.CreateToggleWithLabel(parent, w, h, Func<bool> getValue, Action onChange, x, y, label)` → `{ Toggle toggle; Label label; }` | `ToggleButton.Bind(toggle, getValue, out Action update)`; `Size` is `[Obsolete]` (use scaling). |
| Slider | `Builder.CreateSlider(parent, size, value, (min,max), wholeNumbers, onSliderChange, getValueWithUnits)` → shows value in a `MediumText` | fine for settings. |
| Dropdown | **none in ModGUI.** Game uses `TMP_Dropdown` (`VideoSettingsPC.videoResolutionDropdown`). | Options: (a) `TMP_Dropdown` adapter — instantiate from a game prefab, real popup, needs z-order/raycast care; (b) `SFS.UI.CycleSelector`/`MultiCycleUI` pattern; (c) **recommended default: SegmentedTabs** (2–4 `Button`s, `SetSelected`) for sort/filter, which needs no new type at all. |
| Confirm | `MenuGenerator.OpenConfirmation(closeMode, text, confirmText, onConfirm, denyText, onDeny)` and `OpenConfirmationAsync(...)` → `CallbackAwaiter<bool>` | **Real, native, already used by `WorldsMenu.DeleteWorld`.** Use for all destructive actions. |
| Long text | `Menu.read.Open(Func<string>, closeMode, background, centerText)` → `ReadMenu` | For error details / MOTD / license text. |
| Progress | `Menu.loading.Open(text, Func<bool> isReady)` → `LoadingScreen` (animated dots, closes itself when ready) | Full-screen and **modal**; `OnEscape => CloseMode.None`. Only for world load / large transfers. Inline progress must be `Label` + `TextInput`-less custom row. |
| Toast | `MsgDrawer.main.Log(string)`, `Log(msg, big)` | One line, animation-driven, **no queue** (a new call overwrites the current one), auto-hides. Non-blocking. Needs an adapter for queueing/priority, otherwise we lose messages during bursts (e.g. 6 players connect). |
| Image | `Builder.CreateWebImage(parent, w, h, x, y, link, useMask, LoadingIndicatorType, preserveAspect)` → `WebImage` (`ImageLink`, `LoadingIndicatorType`, `LoadingIndicatorColor`, `PreserveAspect`, `UseMask`) over `SDWebImage` | **Remote URL only.** No local-file variant is exposed. Loading indicators exist (`Circles` etc.) — usable as spinners. |
| Local image | `SFS.UI.ImageTools.RenderTextureTo2D`, `PartIconCreator.main.CreatePartIcon_Sharing(blueprint, 1000, 2000)` | Precedent for generating a creation thumbnail *at publish time*; does not solve rendering a local file back into a UI element. |

### 1.6 What does not exist (must be authored, not called)

- List virtualization / pooling. Today's mod **destroys and recreates every row on every packet** (`RenderChatLog`, `RenderFriendsList`, `OnPacket` handlers). At 15 Hz build traffic + chat bursts this is a per-frame GC and layout-rebuild cost.
- Tooltips, popovers, context menus, badge/pill/chip components, avatars, skeleton loaders, inline validation, breadcrumbs, side rails, status bars.
- Any accessibility layer: no screen-reader hooks, no focus ring sprite, no keyboard focus traversal in ModGUI (the game's own menus navigate by mouse; `Screen_Menu` only binds Escape and explicit `Screen_Base.AddOnKeyDown` keys).
- Mod-side localization. All strings in `Mod/UI/*` are hardcoded English and concatenated (`$"{friend.PlayerName}  {state}  {world}"`).

---

## 2. Evidence-based critique of the current implementation

Each item is a concrete defect with a design consequence; all were observable in `Mod/UI/MultiplayerMenu*.cs` at audit start (`.Panels.cs` and `.Packets.cs` were deleted by a parallel implementer mid-audit — they are still worth reading because several defects were carried into the new code; see §6.0 K1–K6).

**2.1 There is no hierarchy, only a button matrix.**
The window is 900×620 with 8 equal nav buttons in two rows at y −118/−172, then 5 label+input rows at −240…−440, a toggle at −480, 3 action buttons at −530, status at −572. Eleven interactive targets of identical weight, all 112–420 units wide, none more important than another. A first-time player cannot tell whether to BROWSE, HOST, CONNECT, or CREATE WORLD first, and CONNECT (the actual prerequisite for four of the other buttons) sits at the bottom with the same prominence as DISCONNECT.

**2.2 Panels are second windows stacked over a hidden screen.**
`ShowFriendsPanel()` (and chat/browser/world tools) do `_screenHolder.SetActive(false)`, then `Builder.CreateWindow(...)` on a *new* holder. So the user is looking at window B with window A still existing underneath, and every panel repeats four `Hide*Panel()` calls that each do `if (_visible && _screenHolder != null) _screenHolder.SetActive(true)` — i.e. each hide re-shows the main screen, causing flicker and re-running layout 4×. `BACK` destroys B. There is no history, no route, no breadcrumb; you cannot go "Rooms → Room → Player" and back twice.

**2.3 Escape closes the whole product from any depth.**
`MultiplayerScreen : BasicMenu` keeps `OnEscape => CloseMode.Current` from `BasicMenu`, so Escape inside FRIENDS kills multiplayer entirely and (in the World scene) drops the flight input. This is the exact "inconsistent back/navigation" the vision names.

**2.4 Resource leaks and dead position memory.**
Every panel does `Builder.GetRandomID()` and passes `savePosition: true`. `Builder` keeps a static `HashSet<int> RandomIDs` and a `Dictionary<int, Vector2> windowPositions` that are **never cleared** — each panel open permanently adds two entries and a saved position that is never reused, because the next open gets a new random ID. Over a session with many panel transitions this grows without bound.

**2.5 Lists are capped and overflow the window.**
Server browser renders `i < 7`; world browser `i < 7`; chat shows the last 12 lines. Friends list has **no cap and no scroll**: rows are placed at `y = -205 - index * 38` inside a 500-unit-tall window, so the 8th friend is drawn at −471 (behind the BACK button) and the 9th is off-window. The data model supports far more.

**2.6 The row is a button, so all metadata is thrown away.**
`WorldSummary` carries `Name, OwnerName, IsPublic, PlayersOnline, BuildCount, LastModifiedUtc`; the row renders `"{name}  {players} online  {builds} builds"` flattened into a button caption. `ServerListing` carries `Name, Host, Port, Region, GameVersion, ModVersion, Motd, OnlinePlayers, MaxPlayers, PasswordProtected, LastHeartbeatUtc`; the row renders `"{name}  {n}/{max}  {region}"`. Owner, MOTD, password flag, versions, heartbeat age — all discarded. Also note `ServerListing` has **no latency/ping field**, so a "ping" column would be fabricated; show region + version + MOTD instead.

**2.7 The empty state lies.**
`ServerDirectoryClient.ListAsync` catches every exception, logs a warning, and **returns an empty list**. `LoadPublicServersAsync` then renders "No public servers found." A dead directory, a DNS failure, a 500, and a genuinely empty directory are indistinguishable. This violates the vision's "no fake states" rule in reverse — a real error is presented as a valid empty result.

**2.8 Identity is a GUID.**
`SetStatus($"Online as {hello.PlayerId}")` shows a 32-char hex GUID. `World Tools` shows `World: {CurrentWorldId}` — again a GUID — as its primary line. The user's own display name is available (`ModSettings.PlayerName`), and `FriendInfo.PlayerName` / `WorldSummary.OwnerName` exist, so this is pure presentation debt.

**2.9 One status label carries eight unrelated concerns.**
`_status` (bottom of the window) receives connection progress, action confirmations, server errors, invite notices, timewarp results, and join failures. Concurrent events (a chat message, a friend coming online, a claim denial) overwrite each other. There is no per-surface feedback, no error persistence, no unread indicator.

**2.10 Filters, search, and pagination do not exist.**
`WorldListResponse` returns all public worlds in one packet with no server-side paging or query. The UI shows the first 7. So the *only* way to see world #8 is to leave and re-enter. Any real discovery screen must client-side sort/filter the received list and virtualize it; a server-side `page`/`query` field is a protocol addition (additive JSON field is backwards compatible).

**2.11 In-flight usage is architecturally broken.**
`F8` toggles from any scene (`ModLoop.Update`). In `World_PC` that pushes a `Screen_Menu` → `Time.timeScale = 0` and `Screen_Game.OnClose()` (flight input removed). Meanwhile: `MultiBuildManager.TickInterpolation` and `TickLocalPublish` receive `Time.deltaTime = 0`, so remote ghosts freeze and the local build stops publishing while the **server keeps advancing world time at 250 ms ticks**; on close, `TickInterpolation` clamps the correction to ±2 s per 0.25 s, so the player snaps. Chat/social during flight (the single most wanted multiplayer surface) is currently unusable.

**2.12 Timewarp vote stacks a third translucent window.**
`ShowTimeWarpVote` creates a new 420×260 `TranslucentImage` window on top of whatever is open. Each translucent window triggers a blur; three stacked translucent surfaces is both visually muddy and a real GPU cost. It also appears with no audible/visual attention cue beyond a new window, and `SubmitTimeWarpVote` destroys it — if two votes arrive, the first is silently destroyed.

**2.13 The Play-menu entry is fragile.**
`EnsurePlayButton` clones `WorldsMenu.worldButtons[0]`, retries only while `_playButtonRetry > 0` (5 s set on home load), and stores `_playMenu` to detect staleness. `WorldsMenu.LoadUI()` **destroys and recreates all world elements** (on world create/delete/rename/reorder) — the clone is a sibling of those, so it survives; but any `WorldsMenu` rebuild or scene reload after the 5 s window leaves the entry missing with no recovery path until the scene reloads. Also the retry loop calls `EnsurePlayButton()` every frame for 5 s and re-instantiates if `_playMenu` differs.

---

## 3. Layout rules for real SFS resolutions

### 3.1 The coordinate system (derived, then verify)

From `VideoSettingsComponent`: `referenceResolution = (1, 1600 / uiScale)` with `CanvasScaler.ScaleWithScreenSize`. With height-matched scaling, the usable UI space is:

```
units tall   = 1600 / uiScale
scaleFactor  = Screen.height * uiScale / 1600      (pixels per unit)
units wide   = Screen.width / scaleFactor
             = 1600 / uiScale * (Screen.width / Screen.height)
```

**The critical consequence: unit width depends only on *aspect ratio* and *uiScale* — never on pixel resolution.** 1280×720, 1920×1080 and 3840×2160 at the same aspect and uiScale produce the *same* unit layout. So we design against aspect + uiScale, and everything else is scaling. This is why hardcoded integers work at all in the current mod.

| Aspect | Example res | units wide @ uiScale 1.0 | @ 0.8 | @ 1.3 |
|---|---|---|---|---|
| 21:9 | 2560×1080 | 3733 | 4667 | 2692* |
| 16:9 | 1920×1080 | 2844 | 3556 | 2188 |
| 16:10 | 1920×1200 | 2560 | 3200 | 1970 |
| 3:2 | 2160×1440 | 2400 | 3000 | 1846 |
| 4:3 | 1024×768 | 2133 | 2667 | 1641 |

\*uiScale 1.3 divides by 1.3 in both axes; the "wide" column shrinks faster than aspect suggests. **Design to the worst case: 4:3 at uiScale 1.3 ≈ 1641 units wide, 1231 units tall.** That is our minimum canvas. (Confirm the scaler's `screenMatchMode`/`matchWidthOrHeight` in the `Base_PC` canvas prefab — not present in source. One Editor inspection closes this.)

Current mod UI in *pixels at 1920×1080, uiScale 1*: window 900×620 units = **607×418 px**; buttons 42 units tall = **28 px**; nav row hit targets are 28 px tall — well below any touch/accessibility minimum, and cramped for mouse at 4 K downsample.

### 3.2 Unit → pixel formula (for accessibility work)

```
px per unit = Screen.height * uiScale / 1600
units for 44 physical px = 44 * 1600 / (Screen.height * uiScale)
```
At 1080p / uiScale 1: **65 units ≈ 44 px.** Use these floors:

| Target | Units @1080p/uiScale1 |
|---|---|
| Minimum interactive height | 64 (43 px) |
| Comfortable row | 72–80 |
| Primary button | 80 |
| Icon target | 64 × 64 |
| Body text baseline | FontSize 24 (≈16 px) |

**Note:** raising in-game `uiScale` shrinks the unit canvas, so our elements grow in pixels automatically. We must survive uiScale 1.3 (canvas 2188×1231 at 16:9) — i.e. **the shell must be fluid in units, not fixed at 2400.**

**Note on the in-flight `AppPage` contract.** `Mod/UI/App/AppPage.cs` documents a content origin at the centre of the content area, asks pages to stay within roughly ±205 on Y, and `Theme.ContentWidth` is a fixed 740. That is a **compact panel**, not the full shell in §3.3: at 16:9/uiScale 1 it is ≈500 × 277 px, and its fraction of the canvas varies from 26 % (16:9) to 45 % (4:3@1.3). Keep that box as the **overlay** content area — it is a sensible size for a docked flight panel — but do not reuse it for Home/Discover/Library, which need the fluid widths below.

### 3.3 Shell layout (full shell, Home/Build/Hub scenes)

At 16:9 / uiScale 1 (2844 × 1600 units):

```
┌──────────────────────────────────────────────────────────────┐
│ HEADER  96 units: [icon] SFS ENHANCED   tab tabs   [avatar]  │  ← 96
├────────────┬─────────────────────────────────────────────────┤
│ RAIL 360   │ CONTENT                                        │
│ nav items  │  max-width 2160, centered, gutters 60          │
│ (64 tall)  │  page title 48 / subtitle 24                    │
│            │  body scrolls                                   │
│            │                                                 │
├────────────┴─────────────────────────────────────────────────┤
│ STATUS BAR 64: connection • ping-less presence • version      │  ← 64
└──────────────────────────────────────────────────────────────┘
        outer margin 48 units each side
```

Rules:
- **Shell size:** `w = min(2600, unitsWide − 96)`, `h = min(1440, unitsTall − 96)`, centered. At 4:3/uiScale1.3 this becomes 1545×1135 — still usable because the rail collapses (§3.5).
- **Header 96**, **rail 360** (collapses to 96 icon-only below 1900 units canvas width), **status bar 64**, **content padding 40/60**.
- **Content max width 2160**; never full-bleed text — a 2400-unit line of body text is unreadable (target 90–110 characters at FontSize 24 ≈ 900–1100 units; long-form copy gets a 900-unit column).
- Scroll region = content area minus header/footer; the header and rail **never scroll**.

### 3.4 Card grid by available content width

Rows of N cards, computed once per resize, using `HorizontalLayoutGroup` with fixed `spacing` and fixed child widths (not `DistributeEvenly`, which ignores `NewElement` preferred sizes):

| Content width (units) | Columns | Card w × h (16:9 thumb) |
|---|---|---|
| ≥ 1900 | 4 | 460 × 300 (thumb 460×259) |
| 1400–1899 | 3 | 440 × 288 |
| 900–1399 | 2 | 430 × 284 |
| < 900 | 1 | full width, row-style card |

Because 4:3@1.3 gives 1545 canvas → content ≈ 1089 → **2 columns**, the layout must be genuinely responsive, not hand-positioned. This is the single strongest argument for a layout adapter over `CreateButton(x, y)`.

### 3.5 Responsive breakpoints (in units of canvas width)

| Canvas width | Behaviour |
|---|---|
| ≥ 2400 | rail with labels, 3–4 column grids, detail views side-by-side (list + inspector 60/40) |
| 1900–2399 | rail labels, 2–3 columns, detail replaces list |
| 1400–1899 | rail icon-only (96), 2 columns, segmented tabs wrap to two rows |
| < 1400 | rail becomes a top tab row under the header, 1 column |

Implementation: a single `LayoutMode` enum computed from `Screen.width/height` and `VideoSettingsPC.main.settings.uiScale`, re-evaluated on resize (`SFS.UI.UIUpdater` exists and watches `Screen.width/height`; we can also poll in our controller's `Update` with a cheap compare on `(int)w,(int)h`). **Never** listen to a `Resolution` event — none exists.

### 3.6 Focus/scroll ownership

Only **one** scroll region may own the wheel at a time. `ScrollElement.RegisterScrolling(Button)` binds drag/scroll per button; with pooled rows, register on the **region's background blocker**, not on each row, otherwise wheel-over-a-row scrolls nothing (the row button consumes it) — a common and confusing failure. Recommended: put an invisible full-region `Button` (transparent `Image` + `ButtonPC`) behind the rows, register `ScrollElement` on it, and let rows handle clicks.

---

## 4. Navigation model and interaction architecture

### 4.1 Information architecture

```
SFS Enhanced
├── Home                landing: resume, friends online, featured, recent, activity
├── Discover            (search + categories)
│   ├── Servers         directory + history + direct connect
│   ├── Rooms           rooms on the connected server
│   └── Creations       public library
├── Rooms               (contextual; = Rooms on current server + joined room)
│   └── Room detail     overview / players / builds / claims / settings (owner)
├── Library             creator workspace
│   ├── My creations    drafts, published, unlisted, private
│   ├── Publish flow    3 steps + validation
│   └── Creation detail view / owner management
├── Social
│   ├── Friends         online, offline, requests (in/out), blocked
│   ├── Chat            server lobby + room channel
│   └── Notifications   invites, requests, moderation
├── Profile             self (editable) and other players (read-only)
└── Settings            Account · Network · Interface · Privacy · Advanced
```

**Not** a destination: Server browser, World tools, Friends panel, Chat panel, Timewarp vote. See §8 for consolidation.

### 4.2 Two shells

| | **Full shell** | **In-flight overlay** |
|---|---|---|
| Scenes | Home_PC, Hub_PC, Build_PC | World_PC |
| Size | fluid, centered, up to 2600×1440 | 820 × 900 units, docked bottom-left, 40 from edges |
| Pause | yes (`pauseWhileOpen = true`) | **no** (`pauseWhileOpen = false`) |
| Nav | rail (7 destinations) | tabs: Room · Chat · People · Flight |
| Content | full IA | compact lists, 1 column, max 6 visible rows + scroll |
| Entry | Play menu `MULTIPLAYER` | `F8`, plus an always-visible status chip (§7.6) |
| Escape | back one level → close | collapse overlay (not close connection) |

### 4.3 Navigation rules

1. **Replace-in-place**, single content region. Route = `{ destination, subRoute, entityId }`. Depth ≤ 3.
2. **One Back control**, top-left of content, `← {parent label}`. Escape maps to it.
3. **Route stack** lives in the controller (`List<Route>`), not in `ScreenManager`. Back pops ours; when empty, close the shell via `Close()`.
4. **No `ScreenManager` pushes inside the product.** Exception: native modals we don't own — `MenuGenerator.OpenConfirmation`, `Menu.textInput`, `Menu.read`, `Menu.loading`. Those are acceptable because they are the game's own vocabulary and are single-purpose.
5. **State survives navigation.** Scroll offset, filters, search text, and selection are held per route key in the controller and reapplied on rebuild. (`ScreenManager.CloseCurrent` re-instantiates factories, so we cannot rely on view state.)
6. **Never show two shells.** Entering a room from Home switches destination; entering World scene closes the full shell and opens the overlay (§4.4).
7. **Breadcrumb** at depth 3 (e.g. `Discover › Rooms › Kerbin Station`), text-only, clickable segments.
8. **Deep entry points land on a route, not a mode**: "invite received" opens `Room detail` for that room, not a modal.

### 4.4 In-flight overlay: the pause problem (must be decided by manager)

Facts: pushing any `Screen_Menu` in `World_PC` sets `Time.timeScale = 0` **and** calls `Screen_Game.OnClose()`, removing flight input. `MultiBuildManager` ticks on `Time.deltaTime` → frozen; server world time keeps advancing → on resume, correction is clamped to ±2 s per 0.25 s → visible snap, and the local build stops publishing for the duration.

Options, with honest costs:

| Option | How | Cost |
|---|---|---|
| **A. `pauseWhileOpen = false`** | Subclass `Screen_Menu`, set `pauseWhileOpen = false`. | Sim is live, but `Screen_Game` is still closed → **player cannot fly while the overlay is open**. Acceptable for a panel opened deliberately. Server time no longer snaps. |
| **B. Plain canvas overlay, no `ScreenManager`** | Parent under `--- UI ---`, own raycast blocker, own close key; never touch the stack. | Sim keeps running **and** flight input keeps working (as long as the blocker doesn't cover the control area). But we lose Escape/`Screen_Base` key plumbing and must re-implement input priority via `UI_Raycaster`/`Raycastable`. Highest fidelity, most work, least framework support. |
| **C. Keep pausing (status quo)** | — | Disconnects the player from the world every time they read chat. Reject. |

**Recommendation: A now, B as the target** for a bottom-left docked overlay that covers ~40 % of the screen and leaves the flight HUD and touch controls live. This is a spike (2 days) with a clear pass/fail: open overlay, confirm rocket still controllable, confirm remote ghost continues to interpolate, confirm no timewarp snap on close.

### 4.5 Input model

- **Mouse first** (SFS PC build: `ButtonPC.OnMouseEnter/Exit`, `DownloadsMenu` uses `GUIUtility.systemCopyBuffer`).
- Hover: 100 ms tween (inherited), hover reveals *secondary* affordances only (kebab menu, copy code). Never gate information behind hover.
- Press: `ButtonPC` handles; do not add our own scale/color tweens on top.
- Double-click a card/row = open detail; single click = select (visual `SetSelected(true)`).
- Right-click / kebab → context menu (adapter, §6.9).
- Wheel over a region scrolls the region (§3.6). PageUp/PageDown not available generically → provide explicit `↑ ↓` scroll buttons only where a region is short.
- Keyboard: `F8` toggle (existing), `Escape` back (existing), `Enter` = primary action of the focused field (`TMP_InputField` handles submit; wire `onSubmit`), `Ctrl+C` copy join code. **Tab traversal is not provided by the toolkit** — implement an explicit ordered list of focusable elements and handle `Tab`/`Shift+Tab` in our `ProcessInput` override, calling `Select()` on inputs and `SetSelected(true)` on buttons. This is ~60 lines in one adapter and is the only way to make the shell keyboard-usable.
- Every async action enters a `pending` state: control disabled + inline "Working…" text. No optimistic success messages (current code says "Friend request sent." before any confirmation — replace with pending → confirmed/denied from the server, and note the server does respond with `Error`/`FriendListResponse`).

---

## 5. Screen specifications

Format: **purpose** · entry · layout (units) · data (real source / MISSING) · states · notes. All sizes are for the full shell at 16:9 unless noted. Data marked **MISSING** is not obtainable from any current packet or service — the screen must show an honest "not available" state rather than mock content.

### 5.1 Home

**Purpose:** answer "where should I go?" in under 5 seconds. This is the product's front door.
**Entry:** Play menu `MULTIPLAYER`; auto-open on returning to Home scene if a session is live.
**Layout:** header + rail. Content in 3 vertical bands:
1. **Continue band** (h 220): "Resume: *Kerbin Station*" (if a world was joined) + `JOIN AGAIN` primary + `LEAVE` ghost; if not connected: a single wide card "Not connected — *Browse servers* / *Direct connect*" (the only two actions).
2. **Two-column (60/40):** left = **Friends online** (max 6 rows, `ListRow` h 72, presence dot + room + `JOIN`), right = **Activity** (3–6 items, `h 64`).
3. **Featured rooms** (card row, 3 cards) then **Popular creations** (card row, 3).
**Data:** friends → `FriendListResponse` (real). Rooms → `WorldListResponse` (real, server-scoped). Activity → **MISSING** (no event feed; derive only from live client events in-session; label "This session"). Creations → **MISSING** (no service).
**States:** disconnected (no room/friend data — show two-action empty state, never a blank grid) · loading (3 placeholder cards) · error (band-level inline error with Retry).
**Notes:** Home must never show a card it cannot fill. If the library service doesn't exist, the creations band is absent, not "Coming soon".

### 5.2 Discover

**Purpose:** one search box, three scopes. Segmented tabs: `SERVERS · ROOMS · CREATIONS`. Persistent search field (h 72) with 250 ms debounce, client-side filter over the loaded set (no server query exists) + `Sort: Players | Recent | Name` segmented + `Filter: Public only | Include password` (servers) / `All | Owned` (rooms).
**Layout:** results as `ListRow` (h 88) not cards, because metadata density matters more than imagery and thumbnails are unavailable for rooms/servers.
**States:** each scope independently owns loading/empty/error (see §7).
**Notes:** keep the search text when switching scope? **No** — clear it; a search for a server name silently returning no rooms is a lie.

**Servers sub-screen** (`Server browser` merged here):
- Row: name (24 semibold) · `region · game/mod version · n/max` (20 muted) · badges `PASSWORD`, `VERSION MISMATCH` · trailing `FAVORITE` (star) + `CONNECT`.
- Sections: **Favorites** (local, `ServerHistory` extended), **Recent** (`ServerHistory.GetAll()`, real, 12 entries), **Public** (directory `GET /api/v1/servers`, real).
- Toolbar: `REFRESH` (with last-refreshed timestamp), `DIRECT CONNECT` (opens the retained compact dialog), `OPEN DIRECTORY URL` (shortcut into Settings › Network).
- **Do not render a ping column.** No latency data exists (`ServerListing` has none). Region + version + players + MOTD is the honest row.

**Creations sub-screen:** grid of cards. **Data MISSING** (no creations service, no `/api/v1/creations`, not even a thumbnail model). Specify it now so the shell is built for it, but its state today is a single honest empty state: "The creator library needs a library service. *Set one in Settings › Advanced*." — no fake cards.

### 5.3 Rooms (server-scoped) and Room detail

**Purpose:** rooms are first-class spaces, not a world list.
**Entry:** Discover › Rooms; Home featured; invite; deep route.
**List:** `ListRow` h 88: room name · `owner · players online · builds` · badges `PUBLIC` / `JOINED` · trailing `JOIN`. Sections: `JOINED` (1), `PUBLIC` (`WorldListResponse`, real), filter/search from the row above.
**Data:** `WorldSummary { WorldId, Name, OwnerName, IsPublic, PlayersOnline, BuildCount, LastModifiedUtc }` — all real. **MISSING:** description, tags, category, thumbnail, max players per room, roles/permissions, invite code, join-by-code.

**Room detail** (the most important screen after Home):
```
← Rooms                                   [JOIN]/[LEAVE]  [⋯]
Kerbin Station                              Owner · Public
─────────────────────────────────────────────────────────────
Players (n)              │  About
  ⊙ Pilot (you)          │  Builds 14 · Updated 2 h ago
  ⊙ Nova                 │  World time 3 d 04:12 · 1× (locked)
  ⊙ Kess                 │  Claims 3
─────────────────────────────────────────────────────────────
Builds (14)  [you: 1]                                    ⋯
  ▪ Pilot — Rocket · on pad · controlled by Pilot
  ▪ Nova — Station · orbit · idle
```
Right column 40 % at ≥1900 units, stacked below at narrower widths.
**Actions:** JOIN/LEAVE · COPY JOIN CODE (**MISSING** → omit until protocol) · INVITE FRIEND (real: `FriendInviteToWorld`) · CLAIM LOCAL BUILD / UNCLAIM (real: `ClaimCreate`/`ClaimRemove`) · TIMEWARP (real: `TimeWarpRequest` → vote) · owner-only: rename/visibility (**partially real**: `WorldCreate` has `IsPublic`, there is no update packet → MISSING).
**States:** not connected (single CTA to Servers) · joined (shows Leave, claims enabled, timewarp enabled) · full/private (join disabled with reason) · deleted (route stale → back to Rooms + toast).

### 5.4 Creator library (Library)

**Purpose:** the Workshop pillar. Must not ship a fake version of this.
**Screens:**
1. **My creations** — grid of owned items, tabs `DRAFTS · PUBLISHED · UNLISTED · PRIVATE`, row actions `Edit · Unlist · Delete` (destructive → `MenuGenerator.OpenConfirmation`).
2. **Publish flow** — 3 steps, never one giant form: **① Select asset** (pick a local SFS save/blueprint; `WorldsMenu.GetOrdered()`/`FileLocations.WorldsFolder` are real local sources) → **② Details** (title, description, category, tags, visibility) → **③ Review & publish** (validation checklist: title length, description, format support, game/mod version, thumbnail). Persistent stepper at the top; `BACK`/`NEXT` in a fixed footer; validation inline per field.
3. **Creation detail** — hero thumbnail (16:9, `WebImage`), title/creator/version/updated, description, `DOWNLOAD` primary, `FAVORITE`, `REPORT`, version history list (**MISSING**).
**Data:** entirely **MISSING** — no models, no endpoints, no asset-type enum, no versioning, no moderation. `Shared/Models` has only `WorldRecord`, `PlayerAccount`, `ServerListing`; `Directory` serves servers only; `Directory/Program.cs` has 4 routes, none about content.
**Design decision that *is* implementable today:** local-only **"My drafts"** backed by a manifest file under the mod folder (`Mod/Packaging/PackageManager.cs` shows the project already writes manifests under the game root safely). Draft metadata (title/description/tags) is real and persisted; *publishing* is gated behind the service. This gives a working, honest vertical slice instead of a mock.

### 5.5 Profile — **now backed by real data**

`Server/Social/ProfileService.cs` plus packets `ProfileRequest/Response/Update/UpdateResult` (80–83) landed during this audit, so this screen is no longer blocked. Available (from `PlayerProfile`): `PlayerId, PlayerName, DisplayName, Bio (≤280), AvatarAccent (#RRGGBB, default #4FA8FF), MemberSinceUtc, LastSeenUtc, Online, CurrentWorldId, IsSelf, ShowPresence, AllowFriendRequests, WorldsOwned, FriendsCount, SessionsCount`. Server validation already exists (`MaxDisplayNameLength = 32`, accent regex, control-character stripping, privacy filtering), so the UI must surface its rejections verbatim rather than pre-validating differently.

**Two variants, one component.**
- **Self (editable):** 64-unit `Avatar` (§6.5), `DisplayName` (single-line input, helper "32 characters max"), `Bio` (multiline input + live `n/280` counter), accent picker (8 preset swatches + current hex), then `StatTile`s **Worlds owned · Friends · Sessions** (all real). Privacy toggles `AllowFriendRequests` and `ShowPresence` with effect-stating helper text ("Off — friends see you as offline"). Collapsed **Advanced**: `PlayerName` (immutable login name, `mono-id`), `PlayerId` (muted, copyable), `Sign out this device` → destructive confirm (clears `ModSettings.AuthToken`).
- **Other player (read-only):** same header component, no inputs. Relationship: `ADD FRIEND` / `REQUEST PENDING` / `FRIEND`, `INVITE TO ROOM`, `BLOCK` (**MISSING** — no block model), `REPORT` (**MISSING**). Respect privacy: when `ShowPresence` is off, render `presence hidden by this player` — **not** `offline`; those are different facts and conflating them is a lie.
**States:** self (editable, save on debounce/blur) · other · offline (cached snapshot + `OfflineBanner`; edits disabled **with reason**, never silently queued) · not found ("No player with that name has connected to this server" — mirrors `FriendsService` wording) · rejected update (inline field error + `ProfileUpdateResult` reason).
**Rule:** the headline identity is `DisplayName`/`PlayerName`, never the `PlayerId` GUID. `WorldsOwned`/`FriendsCount` are **per-server** facts → label the panel "On this server". No image avatar yet: `AvatarAccent` + initials only (§6.5).

### 5.6 Social

- **Friends** — sections `ONLINE (n)` / `REQUESTS (n)` / `OFFLINE (n)`; `ListRow` h 72 with presence dot + name + `in Kerbin Station` + trailing `JOIN`/`INVITE`; requests show `ACCEPT`/`DECLINE` in the row (real: `FriendRequestResponse`). Add-friend field lives in a header action, not as a permanent input row.
- **Chat** — full page (shell) and compact (overlay). Channel switch: `LOBBY` / `ROOM` (real: `ChatMessagePacket.WorldId` null = lobby). Sticky-to-bottom via `ScrollElement.PercentPosition = 1` unless the user scrolled up; `NEW MESSAGES ↓` pill when detached. Message row: time (20 muted) · name (24, colored from a deterministic hash of `PlayerId`) · text (24, wrap 1600 units). Composer: input + `SEND`; `Enter` submits; `Up` recalls last sent (local). Cap in-memory history at 200 with an explicit "older messages are not stored" note (**no persistence exists**).
- **Notifications** — single list combining invites (`FriendInviteToWorld`), friend requests (`FriendListResponse`), claim denials (`ClaimDenied`), errors (`Error`). **MISSING:** a `Notification` model — derive from live packets and keep in memory; badge count on the rail.

### 5.7 Settings

Reuse the game's own pattern: a **vertical list of label+control rows** inside a scroll region (this is exactly what `SFS.UI.SettingsMenuSizer` and the game settings screens do; `Builder.CreateInputWithLabel` / `CreateToggleWithLabel` / `CreateSlider` are the right primitives). Sections, each ~10 rows max, collapsible:

- **Account** — display name (input), signed-in state (read-only; token **never displayed**), `Sign out this device` (destructive confirm), "accounts are per-server" disclosure (true: `AccountService` is server-local, no global identity).
- **Network** — directory URL (input; empty = no discovery, and say so inline), default port (input, numeric), `Test connection` (real: `ConnectAsync` to a probe endpoint is heavy → instead re-run directory fetch and report status), `Clear server history`.
- **Interface** — density (`Comfortable / Compact`), show status bar, chat timestamps, reduced motion (disable hover tweens/inline animations), **`UI scale note`** (read-only: explains that SFS's Video settings scale the whole UI, and links nowhere — do not duplicate the game's slider).
- **Privacy** — appear in server browser presence (`MISSING` server support → show as unavailable, disabled, with reason), friend requests (`Everyone / Friends of friends` — **MISSING**), blocked players (local list, real, but block enforcement is **MISSING**; label it "hidden on this device" honestly).
- **Advanced** — server executable path (existing `ModSettings.ServerExecutablePath`, used by HOST), `Open mod folder`, diagnostics (`Copy connection log` via `GUIUtility.systemCopyBuffer`).

Set on change → persist immediately (`ModSettings` → `PlayerPrefs`) + inline "Saved" toast (queued `Toast` adapter). Never a separate Save button.

### 5.8 Direct connect (retained, reduced)

Keep — it is the only path when discovery is down. **Reduced to 3 fields** (host, optional port, name) + `CONNECT`; directory URL moves to Settings › Network; world name/visibility move to Create Room. Convert "Port override" to a disclosure row (`Advanced ▸ port`) so it stops competing for attention. This is the *only* thing that survives of today's main window, and that is correct.

---

## 6. Component design system (`EnhancedUI`)

### 6.0 Reconciliation with the code that landed during this audit

`Mod/UI/Kit/Theme.cs` and `UiKit.cs` are the right shape: one token module, one helper surface that every screen goes through, `AutoFontResize = false` with explicit `FontSize`. Keep them. Six measured corrections, all cheap, and worth doing **before** more pages exist:

| # | Finding (measured) | Fix |
|---|---|---|
| K1 | `Theme.ContentWidth = 740` is a fixed column. At 16:9/uiScale 1 that is 740/2844 = 26 % of canvas width (≈500 px); at 4:3/uiScale 1.3 the canvas is 1641 wide, so 740 = 45 %. The same screen reads narrow on widescreen and near-full-width on a small display. | Keep `Theme` for colour/type only; add a computed `Layout` (width from §3.3/§3.5 breakpoints, from which `RowHeight`/`RowStep` derive). |
| K2 | `BodySize = 15`, `SmallSize = 12`, `SectionSize = 12`. With 1 unit = `H·uiScale/1600` px, at 1080p/uiScale 1 those render at **≈10 px / 8 px / 8 px** — below comfortable reading and below the game's own text. | Body 24 (≈16 px), caption 20 (≈13.5 px), section label 20 Bold, h3 26, h2 32, h1 48. §6.2 is authoritative. |
| K3 | `RowHeight = 44` → **≈30 px**; `ListRow`'s trailing action `110 × 30` → **74 × 20 px**. Both under the 44 px target (§3.2), and only the trailing sliver is clickable. | Row 72 units (49 px), action 132 × 48, whole row clickable. |
| K4 | `Primary`/`Secondary`/`Danger` differ **only in `TextColor`**. Fill is prefab-authored (`ButtonPC.Color` / `ButtonPC.OverColor`, both public fields), so a "primary" action has no more visual weight than a secondary one — directly against the vision's primary/secondary/destructive requirement. | Set `ButtonPC.Color`/`OverColor` per variant in the kit; clone the prefab per variant once if mutating the instance proves unsafe. |
| K5 | Every `Card`, `Stat` and `EmptyState` is a `TranslucentImage` `Box`. A 20-row Discover list means **20 blur surfaces** behind one window. | One translucent window surface; rows use flat `Box` at opacity ≥ 0.9 (or a solid `Image` child). Reserve translucency for ≤ 3 hero surfaces per screen. |
| K6 | `UiKit.ListRow` hardcodes `-155` / `+288`, so it breaks the moment `ContentWidth` changes; `Theme.State(bool)` encodes status as colour only. | Derive row geometry from the passed width; status becomes `Badge` = dot + word (§6.6). |

Two carried-over defects to fix while the code is still small: `AppState.PendingVote` is a **single** slot, so two concurrent timewarp votes still destroy each other (§2.12) — make it a queue; and `Changed` must drive a dirty-flag rebuild with **pooled rows**, not a full page teardown per chat line (C4).

One namespace, e.g. `SFSEnhanced.Mod.UI.Components`, built on `SFS.UI.ModGUI` + `SFS.UI`. Each component is a class that owns prefab instantiation, exposes setters, and never leaks `GameObject` to screens.

### 6.1 Structure

```
EnhancedUI (static: tokens, metrics, LayoutMode, BuildRow/BuildGrid helpers)
├─ Shell (header, rail, content host, status bar, route stack)
├─ NavItem            icon + label + badge + selected state
├─ PageHeader         title, subtitle, back control, primary action
├─ Card               thumb/plate, title, meta, badges, overflow
├─ ListRow            leading, two-line text, trailing meta + action
├─ SplitRow           label ←→ value/control (settings)
├─ StatTile           label + value (+ caption)
├─ Avatar             initials + deterministic color
├─ Badge / Pill       2–4 states with icon+text
├─ Button variants    primary / secondary / ghost / destructive / icon
├─ TextField          label + input + helper + error
├─ ToggleRow, SelectRow, SegmentedTabs, Stepper
├─ EmptyState, ErrorState, LoadingRows, OfflineBanner
├─ Toast (queued), ConfirmDialog (wraps MenuGenerator), ProgressRow
├─ ContextMenu        anchored, dismiss-on-outside-click
└─ FocusRing + TabOrder (accessibility adapter)
```

### 6.2 Tokens

Spacing scale (**units**): `4 · 8 · 12 · 16 · 24 · 32 · 48 · 64 · 96`. Radius: ModGUI prefabs are pre-authored; **do not attempt to change corner radius** (it lives on sprites we don't own) — achieve grouping with spacing and `Separator`, not rounded cards.

Type scale (**FontSize units**, TMP):
| Token | Size | Style | Use |
|---|---|---|---|
| display | 64 | Bold | Home greeting (rare) |
| h1 | 48 | Bold | Page title |
| h2 | 32 | Bold | Section title |
| h3 | 26 | Bold | Card title / row primary |
| body | 24 | Regular | Body, row secondary |
| caption | 20 | Regular | Meta, timestamps |
| button | 22 | Bold, uppercase | Buttons, tabs, badges |
| mono-id | 18 | Regular, muted | PlayerId, world id (debug rows) |

All labels: `AutoFontResize = false`, explicit `FontSize`, `TextAlignment` `Left`(body)/`Center`(buttons), `enableWordWrapping = true`, `overflowMode = Ellipsis` (via TMP adapter). **Never rely on auto-size** — it makes two identical rows render differently.

### 6.3 Color roles (mapped to real properties)

| Role | Value (RGBA) | Where it lands |
|---|---|---|
| `surface.shell` | `#0B1017` @ 0.96 | `Window.WindowColor` / `WindowOpacity` |
| `surface.raised` | `#141C26` @ 0.94 | `Box.Color` / `Box.Opacity` |
| `surface.rowHover` | `#1B2532` @ 0.55 | row `Box` on hover |
| `stroke.subtle` | `#243040` @ 0.7 | `Separator` (or 1-px `Box`) |
| `text.primary` | `#F2F6FA` | `Label.Color`, `Button.TextColor` |
| `text.secondary` | `#9DB0C2` | meta/caption labels |
| `text.disabled` | `#5A6A7A` | disabled labels |
| `accent` | `#4FA8FF` (aligned to `Kit/Theme.Accent` and the server's `DefaultAccent`) | primary button `ButtonPC.Color`; `ButtonPC.OverColor = #7CC0FF` |
| `accent.muted` | `#2A6C97` | selected nav indicator |
| `success` | `#4CC38A` | online dot, join state |
| `warning` | `#E6B34A` | version mismatch, timewarp pending |
| `danger` | `#E4655B` | destructive button, error text |
| `focus` | `#7FC4F0` @ 0.9 | 2-unit ring `Box` behind focused element |

Rules: accent is **≤ 10 % of pixels**; exactly one primary button per view; destructive actions are never adjacent to primary; status is never color-only (icon + text always). Because windows are `TranslucentImage` (blurred backdrop), **verify contrast against the brightest backdrop** (Earth/space daytime in World scene): if a label sits on a translucent surface in-flight, raise the local `Box.Opacity` to ≥ 0.9 or use an opaque `Box`. Target 4.5:1 body / 3:1 large text after blur. Where `Kit/Theme` already defines a role its value is canonical (accent `#4FA8FF`, positive `#53D18C`, warning `#E8B34A`, danger `#E56B6B`, surface `#0C131B`, row `#16202C`, text `#ECF2F8`/`#9CADBF`/`#6F8092`); the roles `Theme` lacks — `stroke.subtle`, `surface.rowHover`, `accent.muted`, `focus`, `text.disabled` — should be **added to `Theme`**, not kept as a second palette.

### 6.4 Grid adapter (no `GridLayoutGroup`)

`BuildGrid(parent, cards, columns)`: create one `Container` per row, `CreateLayoutGroup(Type.Horizontal, MiddleLeft, spacing: 24)`, then N cards with fixed `Size` and `childControlWidth/Height = false` (already the default in `CreateLayoutGroup` via `DisableChildControl`). Row container height = card height. Cards must have explicit `Size` — layout groups here do not size children unless `childControl*` is enabled.

### 6.5 Avatar (no image loader required)

`Avatar(sizeUnits, profile)` = `Box` (opaque) filled with `Theme.Parse(profile.AvatarAccent)` — server-validated `#RRGGBB`, default `#4FA8FF` — plus a centered `Label` with 1–2 initials of `DisplayName` (`FontSize = size * 0.4`, Bold, `TextPrimary`). Fallback when no profile is loaded (room owner before profiles arrive, offline cache): deterministic colour from `hash(PlayerId)` over an 8-hue accessible palette, so one player keeps one colour everywhere. Initials need no network and read clearly at 48 units. Image avatars stay out of scope until the profile stores a URL — `WebImage` is remote-only (C5).

### 6.6 Loading / empty / error / offline components

- **`LoadingRows(n)`** — n × `Box` (surface.raised, opacity .35, h = row height) + one `Label` "Loading…" over the first. Cheaper and calmer than 5 spinners. Animate nothing (no shimmer — cost, and no reduced-motion signal available).
- **`EmptyState(icon?, title, body, action?)`** — centered column, width 900, `h2` title, `body` subtitle, one optional secondary button.
- **`ErrorState(title, reason, retry?, details?)`** — same geometry; `title` in `danger`, `reason` in `text.secondary`, `RETRY` primary, `DETAILS` ghost → `Menu.read.Open`. Persistent until retried; never auto-dismisses.
- **`OfflineBanner`** — 64-unit strip under the header, `warning` text + `RECONNECT` ghost + last endpoint from `ServerHistory`. Shows whenever `!Client.IsConnected` while a session was previously established.

### 6.7 Toast adapter (required)

`MsgDrawer.main.Log` cannot queue. `Toast.Show(text, kind)` keeps a `Queue<(string,Kind)>` and a 0.18 s gap, suppresses exact duplicates within 2 s, and routes `kind = Error` to an `ErrorState` instead of the transient drawer (errors must not vanish in 2 s). Also suppress toast spam for presence events (6 connects = 6 toasts otherwise); aggregate as "Nova and 2 others joined".

### 6.8 Progress adapter

`ProgressRow(label, percent)` = `Label` (left) + `Box` track + `Box` fill (width = pct × track). Drive from real counts: `WorldUploadChunkPacket.ChunkIndex/TotalChunks` and `WorldDownloadChunkPacket` — both exist. Use `Menu.loading` only for scene loads and transfers >2 s where the whole UI must block; otherwise inline.

### 6.9 Context menu / tooltip adapters

- **ContextMenu**: `Box` (raised, opaque, no blur if possible) + `ListRow`s, positioned in the content region with edge clamping; dismissed by a full-screen transparent `Button` blocker (`SFS.UI.Raycastable`/`UI_Raycaster` exist for priority) or by rebuilding the route on outside click.
- **Tooltip**: avoid. Use the **status bar** to describe the hovered control ("`Version mismatch: server 0.0.9, you have 0.1.0`") — no z-order problems, and it doubles as the discoverability channel. `ButtonPC.OnMouseEnter/OnMouseExit` can drive it, but they are `override`s on the game component; if we cannot subclass the prefab instance, poll `EventSystem.current.IsPointerOverGameObject` is not reliable per-control → **status-bar hints are wired from our own `Button` wrappers** (we already own the click handlers; add a hover callback in the wrapper via a child `Button`-less `Image` + `IPointerEnterHandler` component, which is plain Unity and always available).

---

## 7. Key user journeys

Each journey lists the exact states the implementation must handle. These are the acceptance paths for QA.

### J1 — First launch → public server → room
1. Home scene. Play menu shows the `MULTIPLAYER` entry (fixed lifecycle, §8.9).
2. Shell opens on **Home**, disconnected state: one card, two actions.
3. `Browse servers` → Discover › Servers. Loading rows → results with region/version/players. If directory URL is unset: inline callout "No directory configured" + `SET DIRECTORY` (→ Settings › Network), **not** "no servers".
4. Select server → `CONNECT`. Pending state ("Connecting to *name*…", 12 s hard timeout from `NetClient.ConnectAsync`). Failure → `ErrorState` with the real reason (`TimeoutException` / `InvalidOperationException` message) + `RETRY` + `DIRECT CONNECT`.
5. Success → Home repopulates: server name, MOTD (via `ServerInfoRequest`), rooms list. Toast "Connected to *name*".
6. `Discover › Rooms` → row → Room detail → `JOIN` → on `WorldJoinAck.Accepted` show "Enter the world" and switch scene (`WorldBaseManager`/`SceneLoader` path is real: `WorldsMenu.OpenWorld` precedent). On `Accepted == false` show `RejectReason`.
7. In `World_PC`: overlay available via F8, room name in the status chip, chat works without pausing (J6).

### J2 — Host a room and invite
Home → `Discover › Servers` → pick server → `CONNECT` → `Rooms` → `CREATE ROOM` (form: name, visibility, planet pack) → `WorldCreate` → room detail → `INVITE FRIEND` (list of online friends) → `FriendInviteToWorld` → invitee sees notification (J5). Owner-only controls appear (claim, visibility — the latter disabled with reason until an update packet exists).

### J3 — Publish a save (creator slice, real today: drafts)
Library → `My creations` → `NEW DRAFT` → step ① pick local save (`FileLocations.WorldsFolder`) → step ② title/description/tags/visibility with inline validation → step ③ review → `SAVE DRAFT` (real, manifest on disk). `PUBLISH` is present but disabled with an explicit reason ("A library service is not configured") — never a fake success.

### J4 — Browse and import a creation (blocked on service)
Discover › Creations → card → detail → `DOWNLOAD` → `ProgressRow` → on completion a confirm dialog "Import into your saves?" → import → toast + "Open in Play menu". **Today this journey ends at the honest "no library service" empty state.** Design it as specified so it drops in when the service lands; do not stub the middle.

### J5 — Friend request → invite → join
Social › Friends → `ADD FRIEND` (header action) → input name → pending row appears from `FriendListResponse.PendingOutgoing`; recipient sees a notification badge on the rail (derived from `PendingIncoming`) → accept → both sides swap to friends. Presence updates set `Online`/`CurrentWorldId` (real). `JOIN` on an online friend → route to their room detail → join. Edge cases: name not found (`FriendsService` error string "No player with that name has connected to this server yet." — surface verbatim, it is accurate and explains the limitation), already friends, self-request.

### J6 — In-flight: chat and timewarp vote without pausing
In `World_PC`: status chip bottom-left shows `⊙ SFS Enhanced · Kerbin Station · 3 online`. F8 opens overlay (non-pausing, option A/B). Tabs: `Room` (players, world time, timewarp state), `Chat` (lobby/room, sticky bottom), `People` (friends in this room). A `TimeWarpVote` arrives → **inline card at the top of the Room tab** with `APPROVE`/`REJECT` and a countdown (no new window, no stacking); multiple votes queue as a list. Approving sends `TimeWarpVoteResponse`; `TimeWarpResult` updates the world-time line. Verify the rocket is still controllable and remote ghosts still interpolate (**this is the pass/fail of the pause decision**).

### J7 — Drop and reconnect
Connection lost (server restart): `NetClient` sets `_connected = false` and clears `CurrentWorldId` silently. UI must detect it (poll `IsConnected` in the controller `Update`, or raise a synthetic event) → `OfflineBanner` + status chip turns `warning` → actions disabled with reason. `RECONNECT` uses `ServerHistory` entry + `ModSettings.AuthToken` (token mode, no re-registration) → on success, re-fetch server info, rooms, friends; if the previous `WorldId` was recorded, offer "Rejoin *room*" (store it in the controller, since `CurrentWorldId` is cleared).

---

## 8. Consolidation: what to keep, merge, split, or delete

| Current surface | Verdict | Target |
|---|---|---|
| Main 900×620 connect window (11 buttons, 5 inputs) | **Dissolve** | Its four jobs split: server discovery → Discover › Servers; hosting → Rooms › Create; player name/directory/port → Settings; direct connect → retained compact dialog |
| `ShowServerBrowser` window | **Merge** | Discover › Servers (adds favorites/recent from `ServerHistory`, rows with real metadata, real error state) |
| `ShowWorldBrowser` window | **Merge** | Discover › Rooms + Room detail (adds owner/players/builds/last-modified from `WorldSummary`, pagination, filter) |
| `ShowWorldToolsPanel` | **Split** | Claim + Leave → Room detail actions; timewarp → Room tab / in-flight; invite accept → Notifications |
| `ShowFriendsPanel` | **Merge** | Social › Friends (keep actions, add sections, scroll, row CTAs; remove the duplicated "Player name" input row and the manual `REFRESH` — refresh on open + on packet) |
| `ShowChatPanel` | **Merge** | Social › Chat + overlay Chat tab (sticky bottom, 200-line cap, unread badge) |
| `ShowTimeWarpVote` window | **Replace** | Inline vote card/queue (one translucent window fewer; no destruction of a pending vote) |
| `MultiplayerScreen` (`BasicMenu`, `closeAnyway`) | **Replace** | `EnhancedShell` (full) + `FlightOverlay`; Escape = back-one-level |
| `EnsurePlayButton` clone | **Retain, harden** | Idempotent injection; subscribe `WorldsMenu.OnEnable` + re-check after `LoadUI` instead of a 5 s frame loop; hide when not in Home |
| `F8` toggle | **Retain** | Shell/overlay toggle (still the only keyboard affordance players already know) |
| `_status` single label | **Replace** | Status bar (persistent connection/server facts) + `Toast` (transient) + `ErrorState` (persistent errors) |
| `ModSettings` (PlayerPrefs) | **Retain** | Back Settings screens; add nothing security-sensitive |
| `FriendsUI` (controller) | **Retain** | Becomes the data source for Social › Friends; add `Notification` synthesis |
| `ServerHistory` | **Retain + extend** | Favorites flag, last room id, used by Home/Discover/Servers/reconnect |
| `MultiplayerMenu.Packets.cs` handlers | **Done — verify** | Already moved into `AppState` with one `Changed` event: correct direction, and it fixes the old "data lost if you're on another panel" bug. Verify no page still guards on its own holder, and add row pooling + dirty-flag rebuilds before list sizes grow (C4). |

---

## 9. Technical constraints and required adapters

| # | Constraint (verified) | Impact | Resolution |
|---|---|---|---|
| C1 | `Builder.AttachToCanvas(CurrentScene)` needs a root named `"--- UI ---"`; home scene likely uses `"UI"` in `Base_PC` | Shell may silently not render from the Play menu | Verified mount helper with fallbacks + probe (§1.2) |
| C2 | `CloseCurrent()` no-ops at stack depth 1 | Cannot implement back-to-root via the framework | Controller-owned route stack |
| C3 | `Window.EnableScrolling` uses `GetComponent<ScrollElement>()` | NRE if prefab lacks it | Adapter: `GetOrAddComponent` + `RectMask2D` |
| C4 | No list virtualization; current code destroys/rebuilds rows per packet | GC + layout churn at 15 Hz | Pooled rows (fixed pool of N, reposition/retext), rebuild only on data-version change |
| C5 | `WebImage` is remote-URL only | No local thumbnails/avatars | `Avatar` initials component; procedural `Box` plate; creation thumbnails generated at publish via `PartIconCreator` and hosted |
| C6 | No dropdown/tooltip/popover/grid in ModGUI | Would force per-screen hacks | SegmentedTabs + ContextMenu + row-composed grids (§6) |
| C7 | `Label` lacks wrap/overflow/margins | Long titles overflow | TMP adapter (`enableWordWrapping`, `overflowMode`, margins) |
| C8 | `Toggle.Size` is `[Obsolete]` (scaling-based) | Visual inconsistency if sized | Never size toggles; scale only |
| C9 | `MsgDrawer` has no queue | Lost messages | Queue adapter (§6.7) |
| C10 | `TranslucentImage` blur per window; each surface costs a blur sample | Stacked windows = mud + GPU | One shell window, opaque `Box` for inner surfaces, max 2 translucent layers |
| C11 | `Builder.GetRandomID()` static, unbounded; `windowPositions` never cleared | Leak (proof §2.4) | Stop using random IDs; one stable ID per shell/overlay; report as bug |
| C12 | `Screen_Menu` pause freezes `Time.deltaTime` consumers | Flight desync (§4.4) | `pauseWhileOpen = false` now, no-`ScreenManager` overlay later |
| C13 | `ServerDirectoryClient` swallows exceptions → `[]` | Error indistinguishable from empty | Change to a `DirectoryResult { List, Error }` (adapter/refactor; small, safe) |
| C14 | `ScreenManager.CloseCurrent` re-instantiates factories | View state lost on back | State in controller, not view |
| C15 | No focus traversal, no screen reader | Keyboard-only users blocked | `TabOrder` + `FocusRing` adapter (explicit, ~60 lines) |
| C16 | `Screen_Base.OnEscape` is a protected abstract property on `Screen_Menu`; `BasicMenu` hard-codes `Current` | Can't have per-depth Escape | Override `ProcessInput` in our shell screen (allowed: `public override void ProcessInput()`) |
| C17 | **Profiles are now real** (`ProfileService`, packets 80–83). Still absent: creations, room metadata beyond `WorldRecord`, notifications, blocks/reports (`Directory` = 4 server routes only) | Library / Room-detail-metadata / Notifications have no data; Profile is unblocked | Full Profile per §5.5; honest unavailable states for creations; Library "drafts" locally now (§5.4) |
| C18 | `WorldListResponse` = all public worlds, no paging/query | Can't scale rooms | Client-side filter + sort now; additive `query/page/sort` fields later (JSON-extra fields are ignored by old readers) |
| C19 | `ServerListing` has no latency field | Ping column would be fabricated | Show region/version/MOTD/players |
| C20 | Player identity is `PlayerId` GUID; display name only via `ModSettings.PlayerName` (self) / `FriendInfo` / `WorldSummary.OwnerName` | "Online as {guid}" | Never show a GUID as the primary identity; `mono-id` token only in Advanced |
| C21 | `Menu.loading` is full-screen and Escape-disabled | Can trap the user on a hung transfer | Only for ≤30 s known-duration ops; always paired with a timeout that closes it |
| C22 | Text strings hardcoded English in `Mod/UI/*` | Localization-blocked | `ModStrings` static with `Loc`-style key/default fallback; all new strings go through it from day one |
| C23 | `Time.timeScale = 0` while the shell is open on Home is harmless, but `MultiBuildManager` still ticks | no impact | — |

**Backend contracts the UI needs (additive, JSON-tolerant):** profiles are **delivered** — `PlayerProfile` + packets 80–83 already carry `DisplayName`, `Bio`, `AvatarAccent`, privacy and real counts; do not re-specify them. Remaining gaps: an `AvatarUrl` field (image avatars) and `Block/Report`; `RoomRecord` additions `{ Description, Tags[], Category, MaxPlayers, JoinCode, ThumbnailUrl }` + `WorldUpdate` packet; `CreationRecord { CreationId, OwnerPlayerId, Title, Description, AssetType(World|Save|Blueprint|PlanetPack|Scenario), Tags[], Visibility, GameVersion, ModVersion, Version, ThumbnailUrl, SizeBytes, DownloadCount, Rating, CreatedUtc, UpdatedUtc }` + list/detail/publish/favorite/report endpoints on the Directory (or a separate creator service), plus `Notification` and `Block/Report` packets (profile: done). Without these, the corresponding screens must remain unavailable-but-honest.

---

## 10. Accessibility (what is actually achievable here)

1. **Contrast**: enforce the token palette against the worst backdrop; opaque inner `Box` for any text over a translucent window in flight. Minimum 4.5:1 body, 3:1 ≥ h2.
2. **Hit targets**: ≥ 64 units (≈44 px at 1080p/uiScale 1), ≥ 72 preferred. Current 42-unit buttons are ~28 px — replace.
3. **No color-only meaning**: presence = dot + word; badges = text; errors = title + icon glyph + color.
4. **Keyboard**: Tab order + visible `FocusRing`; Enter/Space activate; Escape back; F8 toggle. Explicitly scoped — the toolkit has no focus system.
5. **Text scale**: rely on SFS's own `uiScale` (units rescale automatically) and verify at 0.8 and 1.3. Do not implement a second type-size setting.
6. **Motion**: keep transitions ≤ 120 ms and functional (hover, inline card entrance). Provide `reduced motion` which disables entrance animations and hover tweens. No autoplaying animation beyond `LoadingScreen`'s dots (which is native).
7. **Reading order & structure**: page title is always the first focusable/label; sections have visible headings (not icons alone); lists have counts in the heading ("Rooms · 14").
8. **Not achievable, state it in docs**: screen readers, TTS, input remapping beyond what SFS provides, RTL layout (TMP alignment + locale fonts are game-managed and untested for RTL).
9. **Clarity for color blindness**: avatar palette chosen from 8 hues with distinct luminance; status uses shapes/words.

---

## 11. Phasing and design acceptance criteria

Design-side slices, aligned to the vision's "vertical slice first" rule. Each is independently reviewable.

**Slice 1 — Shell + design foundations (unblocks everything; partially started)**
`Kit/Theme` + `UiKit` + `App/AppState`/`AppPage`/`IAppActions` exist: keep them and apply K1–K6 (§6.0) first. Still needed: fluid `Layout`, `Shell` (header/rail/content/status), a **route stack** (today `Navigate(pageId)` is flat — there is no back), `NavItem`, `PageHeader`, `EmptyState`, `ErrorState`, `LoadingRows`, `OfflineBanner`, `Toast` (queued), TMP/Label adapter, focus adapter. Ship **Home + Settings** on it.
*Accept:* 4:3@1.3 and 16:9@1.0 both lay out without clipping; Escape from Settings returns to Home; Escape from Home closes; no window is created per navigation (single holder, verified by counting `SFSEnhanced_*` root objects); directory error vs empty are distinguishable.

**Slice 2 — Discover (Servers/Rooms) + Room detail**
`Card`-less rows, `Badge`, favorites/recent, kinetic rows with real metadata, `Claim`/`Leave`/`Timewarp` actions, join/leave flow.
*Accept:* 50 rooms scroll at a stable row count (pooled, no per-packet rebuild — verify with a CPU counter or `Profiler` mark); join failure shows `RejectReason`; GUID never appears in the primary layer; server-directory-down shows `ErrorState`.

**Slice 3 — Social + Profile**
Friends sections, requests, chat with sticky bottom + unread, notifications from live packets, and the **Profile screen (§5.5) over the now-real `ProfileService`**: self-editing (display name, bio, accent, privacy), read-only other players, presence-privacy honesty.
*Accept:* 30 friends scroll without overflow; friend request round trip shows pending → accepted; chat survives navigating away and back; block/private options are disabled with visible reasons where unsupported.

**Slice 4 — In-flight overlay**
Non-pausing overlay, tabs Room/Chat/People, inline timewarp vote queue, status chip.
*Accept:* rocket controllable while overlay open; remote ghost keeps interpolating; no world-time snap on close; votes queue instead of destroying each other.

**Slice 5 — Creator library**
Drafts (real, local), publish flow with gated Publish action, Discover › Creations honest unavailable state.
*Accept:* a draft persists across game restart; validation blocks empty titles; Publish is disabled *with a reason*, not silently missing.

**Cross-cutting acceptance (all slices):** every async control shows pending; every list has loading/empty/error; no string concatenation of user data without a separator token; no GUID/`WorldId` in primary copy; single primary action per view; contrast and hit-target tokens honored at 0.8/1.0/1.3 uiScale and 4:3/16:10/16:9/21:9.

---

## 12. Open questions / spikes for the manager

1. **Mount probe (5 min, blocking Slice 1):** what is the active scene root name in `Home_PC`? Does `SceneToAttach.CurrentScene` parent successfully? (§1.2, C1)
2. **CanvasScaler match mode (5 min):** confirm `matchWidthOrHeight` in `Base_PC/UI` canvas — my unit math assumes height-matching (§3.1).
3. **Pause decision (2-day spike, blocking Slice 4):** option A vs B in §4.4. Needs an in-flight experiment, not a debate.
4. **Creator library service ownership:** does a creations service belong in `Directory` (self-hostable, cheap) or a separate deployment? The UI is designed to be indifferent, but the DTOs must be agreed before Slice 5.
5. **Room metadata protocol additions:** adding `Description/Tags/Category/MaxPlayers/JoinCode/ThumbnailUrl` to `WorldRecord` + a `WorldUpdate` packet is small and additive; confirm with the architect that it lands before Room detail is finished, otherwise Room detail ships with owner/players/builds only (which is still honest and useful).
6. **Presence cross-server:** `FriendsService` is per-server; friends on another server are invisible. Decide whether to say so in the UI ("friends are per-server") or defer. I recommend saying it, once, in Social › Friends' empty state.
7. **Thumbnail generation for creations:** `PartIconCreator.main.CreatePartIcon_Sharing(blueprint, 1000, 2000)` gives us real art at publish time. Confirm we may upload those to the library service (privacy/size), and that `SDWebImage` can consume the returned URL over plain HTTP for self-hosted libraries.
8. **Layout reconciliation (§6.0):** accept the fluid `Layout` + type-scale corrections (K1–K3) **now**, while only the kit exists. Retrofitting after six pages are written costs several times more. This needs one decision, not a spike.

---

## 13. Files inspected (audit trail)

`PRODUCT_VISION.md`, `agent.md`, `README.md`, `docs/*` (listing), `Mod/ModMain.cs`, `Mod/ModSettings.cs`, `Mod/UI/MultiplayerMenu.cs`, `Mod/UI/MultiplayerMenu.Panels.cs`, `Mod/UI/MultiplayerMenu.Packets.cs`, `Mod/UI/MultiplayerScreen.cs`, `Mod/Social/FriendsUI.cs`, `Mod/Networking/NetClient.cs`, `Mod/Networking/ServerDirectoryClient.cs`, `Mod/Networking/ServerHistory.cs`, and — landed mid-audit and reconciled in §6.0/§5.5 — `Mod/UI/Kit/Theme.cs`, `Mod/UI/Kit/UiKit.cs`, `Mod/UI/App/AppPage.cs`, `Mod/UI/App/AppState.cs`, `Mod/UI/App/IAppActions.cs`, `Server/Social/ProfileService.cs`, `Mod/World/MultiBuildManager.cs`, `Mod/Integrated/VanillaUpgradesIntegration.cs`, `Shared/Models/WorldModels.cs`, `Shared/Models/ServerDirectoryModels.cs`, `Shared/Protocol/PacketType.cs`, `Shared/Protocol/Packets.cs`, `Server/Networking/NetServer.cs`, `Server/Social/FriendsService.cs`, `Server/Persistence/AccountService.cs`, `Directory/Program.cs`, and from `Assembly-CSharp`: `SFS.Input/ScreenManager.cs`, `Screen_Base.cs`, `Screen_Menu.cs`, `Screen_Game.cs`, `CloseMode.cs`, `SFS.UI/BasicMenu.cs`, `Menu.cs`, `MenuGenerator.cs`, `ElementGenerator.cs`, `NewElementBuilder.cs`, `ButtonBuilder.cs`, `TextBuilder.cs`, `Label.cs`, `Button.cs`, `ButtonPC.cs`, `TextInput.cs`, `TextInputMenu.cs`, `ToggleButton.cs`, `RadioButton.cs`, `IconButton.cs`, `ScrollElement.cs`, `Container.cs`, `Box.cs`, `Window.cs`, `GUIElement.cs`, `Type.cs`, `Separator.cs`, `Space.cs`, `InputWithLabel.cs`, `ButtonWithLabel.cs`, `ToggleWithLabel.cs`, `WebImage.cs`, `ModGUIPrefabsLoader.cs`, `TextAdapter.cs`, `MsgDrawer.cs`, `LoadingScreen.cs`, `ReadMenu.cs`, `ReadTextContainer.cs`, `DownloadMenu.cs`, `OptionsMenuDrawer.cs`, `HomeManager.cs`, `WorldsMenu.cs`, `WorldElement.cs`, `ProductThumbnail.cs`, `SettingsMenuSizer.cs`, `ScreenOrientation_Local.cs`, `UIUpdater.cs`, `LayoutGroup.cs`, `HorizontalLayout.cs`, `Rect_Utility.cs`, `SFS/SceneLoader.cs`, `SFS/VideoSettingsPC.cs`, `SFS/VideoSettingsComponent.cs`, `SFS/FOV_Sync.cs`, `SFS.Translations/FontSetter.cs`, `SFS.Translations/Field.cs`.

**Not verifiable from source (Unity assets absent):** prefab colors/fonts/sprites, `CanvasScaler.screenMatchMode`, whether `Window`'s `Children Holder` carries a `ScrollElement`, and `SDWebImage`'s support for local/`file://` URLs.
