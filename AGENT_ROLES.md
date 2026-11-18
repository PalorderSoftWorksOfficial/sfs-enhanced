# SFS Enhanced — Agent Roles

## 1. What this session can actually do

The manager runs as a single agent with shell, file-editing, Git and document tools. **No native
subagent dispatch tool was exposed in this session**, so no specialist process was spawned and none
may be claimed as having run. The manager performs each role as a separate, named review pass with its
own checklist and evidence, and records the result here.

Rules that follow and that future sessions must keep:

- Never report "QA agent passed" or "security agent reviewed" unless a distinct process actually ran
  and produced evidence. Write "manager QA pass" instead.
- A role pass that only read code is "code-verified"; one that ran `dotnet` commands is
  "build/test-verified"; anything depending on the game is "unverified (no SFS runtime)".
- The manager owns the decision. Role output is input, not authority.

## 2. Roles and bounded briefs

### R1 — Product planner / systems architect

Owns `PRODUCT_VISION.md`, `PRODUCT_ARCHITECTURE.md`, `IMPLEMENTATION_PLAN.md`.

Brief: map each product promise to present capability, missing backend/contract, security model, UX
flow and acceptance test. Decide the next vertical slice; refuse slices whose server enforcement is
missing. Keep "world", "room" and "creation" distinct.

Acceptance: every shipped feature in the plan has a server-side validator and a harness assertion or
an explicit "unverified" label.

### R2 — Native SFS UX/UI designer

Owns `DESIGN_SYSTEM.md`, `UX_FLOWS.md`, `Mod/UI/Kit/*`, `Mod/UI/App/Pages/*`.

Brief: work only from the toolkit that exists (`SFS.UI.ModGUI`, `SFS.Input.ScreenManager`, the fixed
1000 × 640 window). Specify screen states, geometry and interaction paths that fit the content column.
No web assumptions, no fake controls.

Acceptance: `DESIGN_SYSTEM.md` measurements match `Theme.cs`/`UiKit.cs`; the design checklist in §10 of
that document holds for every page.

### R3 — Implementer

Owns `Shared/`, `Server/`, `Mod/Networking/`, `Mod/World/`, `scripts/multiplayer-integration/`.

Brief: one bounded task at a time, declared file ownership, no overlapping edits, no comments in code,
wire changes versioned explicitly.

Acceptance: `dotnet build SFSEnhanced.sln` is 0 warnings / 0 errors and the harness reaches
`MULTI_CLIENT_INTEGRATION_PASSED`.

### R4 — Independent QA

Owns `QA_PLAN.md` and bug reports. Does not edit product code during review.

Brief: build a clean mirror, start the three test servers, run the harness five times sequentially,
run the Linux release publish, inspect the diff for accidental files and secrets, reproduce each open
bug, and report severity + repro + expected/actual + affected files.

Acceptance: five pass banners from one long-lived server set; every claim in the report backed by a
command and its output.

### R5 — Security reviewer

Owns the security sections of `PRODUCT_ARCHITECTURE.md`, `BUGS.md` security entries and
`TEAM_SECURITY_IDENTITY.md`.

Brief: review identity, sessions, uploads, authorization, rate limits, replay/tamper, path traversal,
privacy and resource exhaustion. Attack the claims, not the code style. Downgrade any claim the code
does not support.

Acceptance: each security statement names what is proven, what is argued from construction, and what
is not solved (e.g. no PAKE, TOFU cannot detect a first-contact MITM).

## 3. File ownership matrix

| Area | Owner | Reviewers |
| --- | --- | --- |
| `Shared/Protocol/*` | R3 | R5, R4 |
| `Server/Networking/*`, `Server/Social/*`, `Server/Rooms/*` | R3 | R5, R4 |
| `Mod/Networking/*` | R3 | R5, R2 |
| `Mod/UI/**` | R2 | R4 |
| `scripts/multiplayer-integration/*` | R3 | R4 |
| `PRODUCT_*.md`, `IMPLEMENTATION_PLAN.md` | R1 | R5 |
| `DESIGN_SYSTEM.md`, `UX_FLOWS.md` | R2 | R1 |
| `QA_PLAN.md`, `BUGS.md` | R4 | R1 |
| `README.md` | R1 | R5 |

Two roles never edit the same file in the same step. When a fix crosses ownership, the manager
sequences it: implementation first, then QA re-verification, then documentation.

## 4. Handoff protocol

Each assignment states: scope, owned files, prerequisites, deliverable, acceptance criteria and the
exact verification commands. A handoff is only complete when the receiving role can reproduce the
evidence from the commands alone.

Review gates, in order:

1. Design gate — does the flow exist in the server contract?
2. Implementation gate — build clean, no comments, wire versioned.
3. QA gate — harness passes repeatedly, publish works, no secrets/artifacts in the diff.
4. Security gate — threat claims match code, limits enforced, no silent fallbacks.
5. Truthfulness gate — no UI or doc claims a capability that is not enforced.

## 5. Current assignment status (this slice)

| Role | Pass | Evidence | Limits |
| --- | --- | --- | --- |
| R1 planner | completed | capability map and slice choice recorded in `IMPLEMENTATION_PLAN.md` | no external product research this session |
| R2 designer | completed | `DESIGN_SYSTEM.md`, `UX_FLOWS.md`, Rooms/Servers page rewrites | in-game layout unverified (B-1) |
| R3 implementer | completed | protocol v3, identity proof, chat filter, claim overlap, upload expiry, directory states, rooms | — |
| R4 QA | completed (manager pass) | full build 0/0, five harness runs, Linux publish, diff review | no SFS runtime; no independent process |
| R5 security | completed (manager pass) | proof ordering, token-MAC exposure, rate limits, privacy filters reviewed | PAKE and first-contact MITM remain unsolved and are documented as such |

## 6. Escalation

Stop and ask the user only when a decision changes product direction, adds cost or a service
dependency, needs credentials, or risks destructive behaviour (data deletion, force-push, deployment).
Ordinary ambiguities are decided by the manager and written down in the plan or architecture.
