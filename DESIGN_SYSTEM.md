# SFS Enhanced — Design System

Status: describes the in-game shell as implemented in `Mod/UI/`. Every value here is taken from
`Mod/UI/Kit/Theme.cs`, `Mod/UI/Kit/UiKit.cs` and `Mod/UI/App/AppShell.cs`. If code and this document
disagree, the code is right and this document is a bug.

## 1. Toolkit reality

The shell runs inside Spaceflight Simulator's Unity UI, not a browser.

| Concern | What actually exists |
| --- | --- |
| Window/host | `SFS.UI.ModGUI.Builder.CreateWindow` → `Window`, mounted on a `BasicMenu` screen |
| Container | `Builder.CreateContainer` → `Container` with a `RectTransform` sized explicitly |
| Text | `Builder.CreateLabel` → `Label` over `TextMeshProUGUI`; `richText` is forced off in `UiKit.Label` |
| Buttons | `Builder.CreateButton` → `Button` with `TextColor` |
| Panels | `Builder.CreateBox` → `Box` with `Color`/`Opacity` |
| Inputs | `Builder.CreateInputWithLabel`, `Builder.CreateToggleWithLabel` |
| Screen stack | `SFS.Input.ScreenManager` (`SetStack`, `CloseStack`, `GetStackCount`) |
| Scenes | `Base_PC` / `UI` for menus, `World_PC` for flight |

Consequences that constrain every design decision:

- No CSS, no flexbox, no responsive breakpoints. Positions are integer pixel coordinates relative to
  a container centre; layouts are hand-computed and must fit the fixed content area.
- No vector icons or image pipeline in the shell. Labels are uppercase words, not glyphs. Emoji are
  forbidden (font coverage is not guaranteed).
- No animation system is used. Any "transition" would be a hard cut; do not specify animation that
  cannot be implemented with this toolkit.
- Text is single-line unless a label height and size are given that let TMP wrap. Long text must be
  truncated by the code before it is set, and truncation must disclose itself (`…`).

## 2. Geometry

```text
Window                1000 × 640, centered, opacity 0.99
Chrome header         y = 268  (title), y = 238 (subtitle), divider y = 216
Nav rail              180 wide at x = -405, buttons 180 × 36, first y = 246, step -44
Rail divider          2 × 496 at x = -300
Content container     740 × 440 at (105, -8)
Content coordinate    y = +200 is the top of the content area, y = -220 the bottom
Status line           y = -292 (left), presence label y = -292 (right)
```

Hard limits that follow:

- **Seven nav entries maximum.** The nav list ends at y = -18 with seven entries; an eighth would
  collide with the server/account labels at -132.
- **Content rows.** A `ListRow` is 44 tall with a 52 step. Three rows fit below a section header in
  the standard 170–210 tall list region; a fourth must not be added without changing the page.
- **Page top.** New pages start at `Theme.ContentTop` (200) and must end above -220.

## 3. Tokens

Colours (`Theme`):

| Token | Value | Use |
| --- | --- | --- |
| `Surface` | `#0C131B` | window and card background |
| `Row` | `#16202C` | list rows |
| `TextPrimary` | `#ECF2F8` | headings, values, primary button labels |
| `TextSecondary` | `#9CADBF` | subtitles, section labels |
| `TextMuted` | `#6F8092` | hints, disclosure, disabled-looking text |
| `Accent` | `#4FA8FF` | short fingerprint, single emphasis per screen |
| `Positive` | `#53D18C` | connected state |
| `Warning` | `#E8B34A` | refusal that the user can act on |
| `Danger` | `#E56B6B` | destructive action, identity change, errors |

Typography:

| Token | Size | Used by |
| --- | --- | --- |
| `TitleSize` | 24 | page title in chrome |
| `StatSize` | 18 | `Stat` values, current-room name |
| `BodySize` | 15 | body text, list row titles |
| `SectionSize` | 12 | uppercase section labels |
| `SmallSize` | 12 | list subtitles, hints, errors |

Spacing scale: 4 / 8 / 12 / 20 / 22 / 26 / 32 / 44 / 52. Use the existing `ListRow` step rather than
inventing a new rhythm.

Contrast: primary text on `Surface` is ≈ 15:1, secondary ≈ 7:1, muted ≈ 4.2:1. Muted is for
non-essential disclosure only; never put an error message in `TextMuted`.

## 4. Components

All components live in `UiKit` and return the created control so a page can mutate it in place.

| Component | Signature | Contract |
| --- | --- | --- |
| Section header | `Section(parent, text, y)` | uppercase 12 pt, secondary colour |
| Body | `Body(parent, text, y)` | 15 pt, primary |
| Small | `Small(parent, width, x, y, text, colour)` | 12 pt, one line, caller supplies width |
| Divider | `Divider(parent, width, y)` | structural separation |
| Card | `Card(parent, width, height, x, y, colour, opacity)` | panel behind a group |
| List row | `ListRow(parent, y, title, subtitle, action, onAction)` | card + title + subtitle + optional right-aligned button; no button when `action` is null |
| Stat | `Stat(parent, x, y, caption, value)` | fixed 236 × 76 tile with caption + value |
| Empty state | `EmptyState(parent, y, title, body)` | centred two-line card; used for empty **and** offline states |
| Region | `Region(parent, x, y, height)` | keyed sub-container that a page clears and rebuilds |
| Clear | `Clear(region)` | destroys children; called only inside a keyed rebuild |

Buttons: `Primary` (one per screen region), `Secondary` (navigation/neutral), `Danger`
(destructive or identity-affecting). A button must have a handler that performs the action it names.
There is no disabled visual state in the toolkit, so a control that would be a no-op is not rendered
at all (a full room shows `FULL` in its subtitle and no button).

Inputs: always labelled (`CreateInputWithLabel`), max width 700, 40 tall. Username, room name,
capacity and description all get client-side validation messages through the status line before a
packet is sent, and the server re-validates every field.

## 5. Screen states

Every data surface must render exactly one of these, and the state must be derived from real state,
never from a placeholder:

| State | Rendering | Example |
| --- | --- | --- |
| Not configured | `EmptyState` + action | directory URL missing → "No server directory configured" + Open Settings |
| Loading | `Small` "Loading …" in `TextMuted` | `LoadingRooms` |
| Loaded, empty | `EmptyState` with a next step | "No open rooms" |
| Loaded, partial | rows + "Showing 3 of N" | list truncation |
| Error | `Warning` heading + reason + retry action | directory failure, room refusal |
| Offline | `EmptyState` + "GO TO SERVERS" | rooms page while disconnected |
| Blocked by trust | full-page identity panel replacing the page body | first-use fingerprint |

Errors always name the cause and the next action. Server rejection text is shown verbatim; the
client adds a heading but does not rewrite the reason.

## 6. Interaction rules

- Navigation is a bounded history (8 entries) inside one window. Escape and the screen close button
  call `AppShell.Back()`; when history is empty the screen closes.
- F8 toggles the shell from the menu scenes only. It is inert in `World_PC`, because the shell opens a
  pausing `Screen_Menu`.
- A page is built once per route entry. `Update` mutates existing labels; anything whose shape can
  change (lists, error blocks, current-room cards) lives in a keyed `Region` and is rebuilt only when
  its key changes. Keys are built from the data actually rendered, so a no-op notification does not
  destroy input focus.
- Chat is append-oriented: the log is a sliding 12-line window (`ChatLogWindow`) and the input field
  lives outside the rebuilt region. The input is never destroyed by an incoming message.
- Destructive actions are `Danger` and are labelled with the object they affect ("CLOSE ROOM",
  "REPLACE TRUSTED FINGERPRINT"). Replacing a trusted fingerprint is a separate, explicitly labelled
  action; there is no automatic re-pin.
- Identity is described as "trusted on this device", never "verified", and the panel always states
  that accepting without comparing cannot detect a first-connection impostor.

## 7. Copy rules

- Sentence case in body copy, uppercase in section labels and button labels.
- No claims of features that do not exist. "Rooms" only appears because the server enforces rooms; a
  world list is not labelled as rooms.
- Counts are real and say what they count ("1/2 players", "3 builds", "Showing 3 of 12").
- Privacy and identity copy states the actual scope: accounts are per server, presence is
  per server, and pins are local to the device.
- Errors name code-level causes in user terms ("friends-only") and never imply success.

## 8. Accessibility

- Minimum body text 12 pt; hints must not be the only place critical information appears.
- Do not rely on colour alone: status text is always explicit ("Connected", "FULL", "Friends only").
- Hit targets are ≥ 28 px tall; primary actions are 38–40 px.
- Contrast follows §3; muted text is not used for errors or destructive warnings.
- Localisation: strings are currently concatenated in code, including formatted counts. Any localised
  build needs a string table and culture-invariant formatting for counts; this is a gap, not a claim.

## 9. Reference measurements

Servers page: host input (420 at x=-140) + port input (200 at x=240), primary CONNECT, secondary
CREATE ACCOUNT, status line, error region, directory section with REFRESH, three rows.

Rooms page: current-room card 740 × 96 with name and `builds · players · id`, LEAVE at x=276 y=68 and
CLOSE ROOM (host only) at y=30, claim button at x=140; create form uses 300/110/210/700 wide controls
at y=60/60/60/14 and a primary OPEN ROOM at y=-34; three room rows fit below.

Profile/friends/chat/settings keep their existing geometry; new fields must reuse the row and stat
components rather than adding bespoke panels.

## 10. Design acceptance checklist

1. One primary action per screen region; destructive actions are `Danger` and named precisely.
2. No button without a working handler; no control that silently does nothing.
3. Every list has loading, empty, error and offline states, and discloses truncation.
4. No emoji, no gradients, no decorative panels, no invented iconography.
5. Fixed geometry respected: 1000 × 640 window, 740 content width, ≤ 7 nav entries, ≤ 3 list rows.
6. Muted text never carries errors; errors always include a next step.
7. Identity and scope wording matches the security model in `PRODUCT_ARCHITECTURE.md` §5.1.
