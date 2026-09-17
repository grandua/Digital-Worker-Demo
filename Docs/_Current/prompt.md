# User Prompt

Fix 7 end-to-end testing defects in SciCalc (see task card): (1) operators after `=` start from an empty expression and error; (2) scientific functions after `=` create invalid expressions; (3) no keyboard input; (4) error-lockout controls look active but do nothing; (5) results/errors/mode/memory lack accessible names and announcements; (6) layout breaks below ~720px width / short height; (7) keyboard focus is hard to see.

# High-Level [****]: SciCalc — Fix 7 E2E Defects (continuation, keyboard, lockout, accessibility, responsive layout, focus)

## Ground truth (verified in this worktree, HEAD 26bb24e)
All 7 defects are already fixed at HEAD by commit 26bb24e ("Auto-commit changes by Digital Worker"), which implements the [****] recorded below in this file. Verified by reading the code and running the full workload-free test gate: 251/251 SciCalc.Domain.UnitTests green (including 13 ContinuationTests) and 23/23 SciCalc.Maui.UnitTests green. This [****] therefore documents the fix design (as implemented) and reduces the remaining work to verification; if a target branch predates 26bb24e, applying that commit's diff IS the fix.

## New Classes analysis (pre-condition search, Domain-first)
No new classes. Searched `Calculator/Domain/SciCalc.Domain` (*.cs) and the presentation layer for existing owners of each planned behavior:
- Post-equals continuation state → host: `Calculator` aggregate (owns session state: `Buffer`, `History`, `LastAnswer`, `ActiveError`; `Press(InputKey)` is its behavior). Second candidate considered: `InputBuffer` — rejected (it is a token list with editing text only; it has no knowledge of answers/history, and answer seeding is session policy, not buffer mechanics). Implemented as the private `seedPending` flag plus `ContinuesFromAnswer`/`SeedAnswer` methods on `Calculator`.
- Keyboard mapping, accessible naming, announcements, lockout disabling, responsive/focus styling → host: existing `CalculatorPage.razor` (+ `.razor.css`), which already renders all domain state and owns all input routes. A separate KeyboardMapper class was rejected as a Lazy Class (one static dictionary, no per-call state → a static map field on the component).
- New Classes section is empty → [****] skipped (per workflow rule). Anti-procedural / Domain-first checklist passes: behavior lives on the existing aggregate; no new service or helper classes.

## Root causes (original defects) and the fix that addresses each
- Issues 1+2: `EvaluateEquals` cleared the buffer, so the next operator/function landed on an empty token list and the parser failed. Fix: `seedPending` set after a successful `=`; when the next key continues the expression (operator, function, percent) on an empty buffer, seed `LastAnswer` first — postfix-wrap functions wrap the answer (`sqr(5)`), prefix calls get the answer inside (`sin(5`); digits/dot/constants/parens start fresh. AC, history restore and memory recall clear the pending seed.
- Issue 3: no keydown handling → keyboard map on the focusable `.calc` container (`tabindex="0"`, `@onkeydown`): digits, `.`/`,`, `+ - * x / ^ %`, `m/M`, `(`, `)`, `=`, Backspace/Delete → DEL, Escape → AC; Ctrl/Alt/Meta combos ignored. Enter deliberately unmapped (it would double-activate the focused keypad button).
- Issue 4: lockout invisible → all controls `disabled` while `Calc.Locked` except AC; `:disabled` styling (opacity + not-allowed cursor); AC (and Escape) remain the only recovery path.
- Issue 5: accessible status → descriptive `aria-label`s (symbolic keys, per-slot STO/MR/MC, memory badge state, history items, mode toggle), `role="alert"` error banner, visually hidden `role="status"`/`aria-live="polite"` announcer for results/errors, `aria-live` on the mode badge.
- Issue 6: narrow/short layout → `overflow-y:auto` on `.calc`; media queries ≤900px, ≤720px (single-column body, wrapping sci pad, stacked memory, min-height history), ≤560px, and ≤640px height (compact display/keys/memory).
- Issue 7: focus visibility → `:focus-visible` rings on the `.calc` container and every control (3px outline + halo).

## Architecture check (/[****])
Layer split respected: domain behavior (continuation seeding, lockout rules) lives in the Domain layer aggregate; the presentation layer only maps physical keys to `InputKey` and renders/announces domain state. No UI types in Domain, no calculation logic in the component. Physical components and flow: SciCalc.Maui Blazor component (DI singleton `Calculator`) → `Calculator.Press(InputKey)` → domain state (Buffer/History/ActiveError) → re-render + aria announcements. Data flow only; control flow unchanged.

## Assumptions
- The keyboard focus model is the BlazorWebView content; the focusable `.calc` container is the input surface.
- The MAUI app cannot be compiled in this Linux container (no workloads); presentation verification is via the workload-free SciCalc.Maui.UnitTests plus static markup/CSS review.

## Decisions / trade-offs
- Seeding implemented in Domain (not UI): one rule for mouse/keyboard/ANS and it is unit-testable.
- Controls disabled (not hidden) during lockout: stable layout; AC always available.
- Escape doubles as AC during lockout (only keyboard recovery path).
- Continuation seeding restricted to operator/function/percent keys; value keys start fresh (mainstream calculator UX).

## Scope
- In: the 7 defects; 13 continuation tests; README behavior notes.
- Out: repeated-`=` repeat-last-operation semantics, persistence, MAUI on-device verification.

## Acceptance criteria (verified green in this worktree)
- `2+3=+4=` → 9; `2+3=` then `x²` → `sqr(5)` with preview 25; `2+3=` then `sin` → `sin(5`.
- Keyboard digits/operators/dot/parens/Backspace/Escape perform the corresponding actions when the calculator is focused.
- After `1/0=`, every control except AC is visibly disabled; AC/Escape recovers.
- Result/error/mode/memory states have distinct accessible names and live-region announcements.
- No cramped/clipped controls at ≤720px width or ≤640px height; the container scrolls.
- Focus position clearly visible on the container and all controls.
- `dotnet test` on Domain + SciCalc.Maui.UnitTests: 274/274 green (251 + 23).

## Test cases
ContinuationTests.cs (13): operator continuation (6 theory cases), `2+3=+4=` → 9, `×2=` → 10, square/sqrt/sin after `=`, percent after `=`, fresh-start value keys (5 theory cases), AC stale-answer guard, history-restore guard, memory-recall seeding + delete guard. Regression gate: remaining 238 domain tests + 23 presentation conformance tests.

## Verdict
Simple enough to implement from this high-level [****] alone — the full [****] workflow is NOT required (single-aggregate domain change plus presentation-only markup/CSS; no new classes, no API design). In this worktree the fix is already applied and verified; the remaining action is confirmation/merge of commit 26bb24e.

---

# High-Level [****]: SciCalc — 7 E2E defect fixes

## New Classes analysis (Domain-first / pre-condition search)
No new classes planned. Search results for hosts of the new behaviors:
- Post-equals continuation state: host candidates were `Calculator` (chosen — it already owns session state: buffer, history, LastAnswer, error lockout; `Press` is its behavior) and `InputBuffer` (rejected — it only stores tokens and has no knowledge of history/answers). The `seedPending` flag is per-session state, so it belongs on the `Calculator` aggregate.
- Keyboard handling, aria names, announcements: host is the existing `CalculatorPage.razor` component (already renders all state and owns all input routes). No state or responsibility overlap suggested a new class; a separate "KeyboardMapper" class was rejected as a Lazy Class (single static map, no per-call state).
- New Classes section is empty → [****] skipped (per workflow rule).

## Root causes (verified in this worktree)
- Issues 1+2: `Calculator.EvaluateEquals` clears the buffer; the next operator/function key lands on an empty token list, so the parser throws `ParseFailure` -> `Malformed` error.
- Issues 3-7: `CalculatorPage.razor` has no keydown handling, no disabled states while `Calc.Locked`, no aria labels/live regions, and the scoped CSS has no narrow/short-window affordances or container focus style.

## Changes
1. `Calculator/Domain/SciCalc.Domain/Calculator.cs` - add `seedPending` flag: set after a successful `=`, cleared by AC/restore/any append. When set and the next press continues the expression (operator, function, percent) on an empty buffer, seed the buffer with `LastAnswer` first; prefix functions (`sin`) get the answer inserted inside the call (`sin(5`), postfix wraps (`x²`) wrap the seeded answer (`sqr(5)`). Digits/dot/constants/parens after `=` start fresh.
2. `Calculator/Domain/SciCalc.Domain.UnitTests/ContinuationTests.cs` (new, tests-first, red->green) - 13 tests: operators after `=`, continued evaluation (`2+3=+4=`->9), square/sqrt/sin after `=`, percent after `=`, value keys start fresh, AC stale-answer guard, history-restore guard, memory-recall seeding.
3. `Calculator/Presentation/SciCalc.Maui/Components/CalculatorPage.razor` - keyboard map (`0-9`, `.`/`,`, `+ - * x / ^ %`, `m/M`, `(` `)`, `=`, `Backspace`/`Delete`, `Escape`) handled on the focusable calculator container (`tabindex="0"`, `@onkeydown`); all controls disabled while `Calc.Locked` except AC; per-slot memory button labels and badge state labels; descriptive `aria-label`s for symbolic keys; `role="status"`/`aria-live` result/error announcements; `role="alert"` on the error banner; `aria-live` mode badge.
4. `Calculator/Presentation/SciCalc.Maui/Components/CalculatorPage.razor.css` - `.sr-only` helper; strong `:focus-visible` rings for the container and all controls; `overflow-y` scrolling on `.calc`; `@media` blocks for <=720px width (stacked memory, wrapped sci pad, taller history), <=640px height (compact display/keys/memory) and <=560px width.
5. `Calculator/Presentation/SciCalc.Maui/README.md` - document continuation behavior, keyboard map, lockout UI, accessibility affordances.

## Architecture check ([****])
Domain logic stays in the Domain layer (`Calculator` aggregate); the presentation layer keeps mapping physical keys to `InputKey` and rendering domain state - no calculation logic in UI, no UI types in Domain. Anti-procedural checklist passes: no domain logic added to presentation; behavior remains on the existing aggregate.

## Assumptions
- Keyboard focus lives on the calculator container (WebView content), matching the browser-like input model of a Blazor Hybrid app.
- Repeated `=` on an empty buffer (double equals) is existing `Malformed` behavior and out of scope.

## Decisions / trade-offs
- Domain-level seeding (not UI-level) so all hosts (mouse, keyboard, ANS) share one rule and the behavior is unit-testable.
- Enter is deliberately not mapped to equals: with a keypad button focused, Enter activates that button; mapping it globally would double-fire. `=` maps to equals.
- Controls are disabled (not hidden) during error lockout so layout stays stable; AC stays enabled as the only recovery path.
- Escape is mapped to AC both normally and during lockout (the only keyboard recovery path while locked).

## Scope
- In: the 7 defects, 13 new domain tests, README behavior/accessibility docs.
- Out: repeated-equals repeat-last-operation semantics, persistence, MAUI workload builds, screenshots (unsupported in this container).

## Acceptance criteria
- `2+3=+4=` evaluates 9; `2+3=x²` wraps the answer (`sqr(5)`, preview 25); `2+3=sin` shows `sin(5`.
- Keyboard input performs the corresponding actions when the calculator container is focused.
- After `1/0=`, every control except AC is visibly disabled; Escape/AC recovers.
- Display state, errors, mode and per-slot memory actions have clear accessible names and live-region announcements.
- No cramped/clipped controls below ~720px width or at short heights; scrolling where needed.
- Focus position clearly visible on the container and all controls.
- `dotnet test Calculator/SciCalc.slnx` and `dotnet test Calculator/Presentation/SciCalc.Maui.UnitTests/SciCalc.Maui.UnitTests.csproj` green.

## Test cases
See ContinuationTests.cs (13 new) + existing 237 domain tests + 23 conformance tests as regression gate.

﻿# User Prompt

Create a root Digital-Worker-Demo.slnx solution file at the repo root that aggregates the existing Calculator and UrlShortener solutions/projects. The repo root contains Calculator/ and UrlShortener/ directories, each with its own .NET solutions/projects. The root solution must build successfully with `dotnet build Digital-Worker-Demo.slnx`. This is a simple config-level task: create one .slnx file referencing existing projects. Produce a high-level [****] per Rich Domain Model PEAA guidance where applicable (this task is config-only, so minimal planning is expected), then execute the workflow until completion.

# High-Level [****]: Root `Digital-Worker-Demo.slnx` Aggregate Solution

## 1. Objective (User Intent)

Create a single root solution file `Digital-Worker-Demo.slnx` at the repo root that aggregates the existing Calculator and UrlShortener .NET projects, such that `dotnet build Digital-Worker-Demo.slnx` succeeds from the repo root.

## 2. Current State (Explored)

- `Calculator/`: `SciCalc.slnx` (workload-free: Domain + Domain.UnitTests), `SciCalc.App.slnx` (adds MAUI app projects; needs MAUI workloads), plus `Directory.Packages.props` / `Directory.Build.props`. Projects: `Domain/SciCalc.Domain/SciCalc.Domain.csproj` (net10.0), `Domain/SciCalc.Domain.UnitTests/SciCalc.Domain.UnitTests.csproj` (net10.0), `Presentation/SciCalc.Maui/SciCalc.Maui.csproj` (net10.0-android/ios/maccatalyst[/windows] — MAUI workloads required), `Presentation/SciCalc.Maui.UnitTests/SciCalc.Maui.UnitTests.csproj`.
- `UrlShortener/`: `UrlShortener.slnx`, plus `Directory.Packages.props` / `Directory.Build.props`. Projects: `Presentation/UrlShortener.Api/UrlShortener.Api.csproj` (net10.0), `Presentation/UrlShortener.Api.UnitTests/UrlShortener.Api.UnitTests.csproj` (net10.0), `Presentation/UrlShortener.Api.IntegrationTests/UrlShortener.Api.IntegrationTests.csproj` (net10.0).
- Environment: .NET SDK 10.0.302 installed (native `.slnx` support). README documents the MAUI workload caveat (NETSDK1147 without workloads).

## 3. Key Design Decision: Project Inclusion

Include the 6 workload-free projects: `SciCalc.Domain`, `SciCalc.Domain.UnitTests`, `SciCalc.Maui.UnitTests`, `UrlShortener.Api`, `UrlShortener.Api.UnitTests`, `UrlShortener.Api.IntegrationTests`.

**[A1 — review amendment, applied]:** `SciCalc.Maui.UnitTests` is workload-free and IS included. It targets plain `net10.0` (`SciCalc.Maui.UnitTests.csproj:4`), has no project reference to `SciCalc.Maui.csproj` (no transitive MAUI dependency), builds with 0 errors and passes 18/18 tests on this Linux box without MAUI workloads (static packaging-conformance tests: `ConformanceTests.cs`, `PackagingManifestTests.cs`, `AndroidApplicationTests.cs`, `WindowsApplicationTests.cs`).

Exclude only `SciCalc.Maui.csproj`: the MAUI project targets `net10.0-android;net10.0-ios;net10.0-maccatalyst` and fails with `NETSDK1147` on machines without MAUI workloads (per README), which would violate the "must build successfully" acceptance criterion. Mirrors the existing workload-free `SciCalc.slnx` precedent; the MAUI app remains buildable via `Calculator/SciCalc.App.slnx` on workload-equipped machines.

## 4. [****] Steps

1. Create `Digital-Worker-Demo.slnx` at the repo root, following the established XML format of the existing `.slnx` files, with `<Folder Name="/Calculator/Domain/">`, `<Folder Name="/Calculator/Presentation/">`, and `<Folder Name="/UrlShortener/Presentation/">` containing the 6 `<Project Path="...">` entries (paths relative to the repo root).
2. Verify restore + build: `dotnet build Digital-Worker-Demo.slnx` from the repo root — must succeed with 0 errors.
3. Sanity check (optional but recommended): `dotnet test Digital-Worker-Demo.slnx` — all included projects are workload-free xUnit.

## 5. Rich Domain Model / PEAA Applicability

Config-only task: no domain code, no new classes, no architecture changes — RDM/PEAA guidance is not applicable. Existing per-directory `Directory.Packages.props` / `Directory.Build.props` continue to apply unchanged (they resolve relative to each project's own directory); no root-level CPM file is introduced, so no central-package-management conflicts arise.

## 6. Risks & Mitigations

- Including MAUI projects breaks the build without workloads (NETSDK1147) → exclude them; document the decision.
- Wrong relative paths in `.slnx` → paths relative to root; verified by the build step.
- `.slnx` tooling support → SDK 10.0.302 installed; existing `.slnx` files already build in this repo.

## 7. Verdict

Simple config-level task (1 new file + build verification). This high-level [****] is sufficient to implement directly; the full [****] workflow is NOT required.

Full [****] artifact: `Docs/_Current[****].md`.

---

# User Prompt

Fix SciCalc.Maui Windows build: add Microsoft.Extensions.Logging.Debug package reference and condition mobile TFMs on desktop builds. Context: Calculator/Presentation/SciCalc.Maui/SciCalc.Maui.csproj is a MAUI project (Sdk Microsoft.NET.Sdk.Razor) with TargetFrameworks net10.0-android;net10.0-ios;net10.0-maccatalyst and a conditional addition of net10.0-windows10.0.19041.0 on Windows. Calculator/Presentation/SciCalc.Maui/MauiProgram.cs line 22 calls builder.Logging.AddDebug(), which requires the Microsoft.Extensions.Logging.Debug NuGet package (not currently referenced). The repo uses Central Package Management: Calculator/Directory.Packages.props (ManagePackageVersionsCentrally=true). A unit test project Calculator/Presentation/SciCalc.Maui.UnitTests targets plain net10.0.

# High-Level [****]: SciCalc.Maui — Windows build fix (Logging.Debug package + TFM conditioning)

## Findings (verified in this worktree)
- `Calculator/Presentation/SciCalc.Maui/SciCalc.Maui.csproj` line 4: `<TargetFrameworks>net10.0-android;net10.0-ios;net10.0-maccatalyst</TargetFrameworks>`; line 5 conditionally appends `net10.0-windows10.0.19041.0` on Windows (`$([MSBuild]::IsOSPlatform('windows'))`). PackageReferences: `Microsoft.AspNetCore.Components.WebView.Maui`, `Microsoft.Maui.Controls` (versionless, CPM).
- `MauiProgram.cs` line 22 calls `builder.Logging.AddDebug()` → fails to compile without the `Microsoft.Extensions.Logging.Debug` package.
- `Calculator/Directory.Packages.props`: `ManagePackageVersionsCentrally=true`; existing versions 10.0.100 for the MAUI packages. New `PackageVersion` entries must be added there (CPM forbids versions on `PackageReference`).
- On a Windows machine driven by the `dotnet` CLI (no VS full MSBuild, no Android/iOS SDKs guaranteed), unconditionally targeting mobile TFMs breaks restore/build; restricting to the Windows TFM under CLI is the standard MAUI template pattern.

## [****]
1. `Calculator/Directory.Packages.props` — add to the existing `ItemGroup`:
   `<PackageVersion Include="Microsoft.Extensions.Logging.Debug" Version="10.0.0" />` (aligns with the .NET 10 / MAUI 10.0.100 wave).
2. `Calculator/Presentation/SciCalc.Maui/SciCalc.Maui.csproj`:
   a. Add `<PackageReference Include="Microsoft.Extensions.Logging.Debug" />` (versionless; CPM supplies 10.0.0) to the existing package `ItemGroup`.
   b. Replace the two `TargetFrameworks` lines with CLI-aware conditioning (order matters — last one wins):
      - `<TargetFrameworks>net10.0-android;net10.0-ios;net10.0-maccatalyst</TargetFrameworks>`
      - `<TargetFrameworks Condition="$([MSBuild]::IsOSPlatform('windows'))">$(TargetFrameworks);net10.0-windows10.0.19041.0</TargetFrameworks>`
      - `<TargetFrameworks Condition="$([MSBuild]::IsOSPlatform('windows')) and '$(MSBuildRuntimeType)' != 'Full'">net10.0-windows10.0.19041.0</TargetFrameworks>`
      Effect: Visual Studio (full MSBuild, `MSBuildRuntimeType == 'Full'`) keeps all four TFMs; Windows `dotnet` CLI builds only `net10.0-windows10.0.19041.0` (no mobile SDKs required); non-Windows hosts keep mobile TFMs.
3. Verification: on Windows CLI, `dotnet build Calculator/Presentation/SciCalc.Maui/SciCalc.Maui.csproj -f net10.0-windows10.0.19041.0` succeeds and `AddDebug()` compiles; `dotnet test` on SciCalc.Maui.UnitTests (net10.0, unaffected) stays green. No MAUI workload on this Linux box — Windows build verification is deferred to a Windows dev/CI machine.

## Verdict
Simple config-only change (2 files, ~4 XML lines). No new classes, methods, domain logic, or API design — Rich Domain Model / PEAA impact: none. This task does NOT need the full [****] workflow; this high-level [****] is sufficient to implement.

## Assumptions
- `Microsoft.Extensions.Logging.Debug` 10.0.0 exists on NuGet and matches the .NET 10 runtime shipped with MAUI 10.0.100.
- SciCalc.Maui.UnitTests (plain net10.0) does not reference the MAUI project in a way that forces mobile-TFM restore.

## Decisions / trade-offs
- Version pinned centrally (10.0.0) rather than floating — deterministic restore, consistent with repo CPM.
- `MSBuildRuntimeType` condition chosen over per-developer `Directory.Build.props` overrides so Visual Studio retains full multi-target behavior while Windows CLI stays buildable.

## Scope
- In scope: `Calculator/Directory.Packages.props`, `Calculator/Presentation/SciCalc.Maui/SciCalc.Maui.csproj` (2 files).
- Out of scope: `MauiProgram.cs` logic, the UnitTests project, CI pipeline changes, Android/iOS/MacCatalyst verification.

## Acceptance criteria
- Windows `dotnet build` of SciCalc.Maui succeeds for `net10.0-windows10.0.19041.0` with `AddDebug()` compiling.
- Visual Studio on Windows still targets all four TFMs.
- CPM integrity preserved (single central `PackageVersion`, versionless `PackageReference`).

## Test cases
- No new tests (config-only change; mechanism-only assertions discouraged). Regression gate: existing SciCalc.Maui.UnitTests (net10.0) remain green.

---

# User Prompt

Delete global.json from repo root — it breaks dotnet test for UrlShortener VSTest projects: The repo-root global.json forces Microsoft.Testing.Platform on all projects. UrlShortener's test projects use VSTest (xUnit v2 + xunit.runner.visualstudio), so dotnet test. 2. UrlShortener integration tests fail on Windows — SQLite temp DB file locked during DisposeAsync: All 21 integration tests fail on Windows. TestServerFixture.DisposeAsync() at UrlShortener/Presentation/UrlShortener.Api.IntegrationTests/TestServerFixture.cs:29-30 calls File.Delete() on the SQLite temp .db file while SQLite still holds the handle. Wrap the delete calls in a try-catch — the temp files will be cleaned by the OS.

# High-Level [****]: UrlShortener — global.json removal + Windows SQLite temp-file dispose fix

## Findings
- global.json does NOT exist in the working tree or HEAD; it was already deleted in commit ceeac9e on this branch (idempotent no-op). Its last content was {"test":{"runner":"Microsoft.Testing.Platform"}} — confirming the diagnosis. Zero remaining Microsoft.Testing.Platform references; test projects use xUnit 2.9.2 + xunit.runner.visualstudio 2.8.2 + Microsoft.NET.Test.Sdk 17.12.0 (classic VSTest).
- TestServerFixture.DisposeAsync (TestServerFixture.cs:29-30) deleted SQLite temp files (db, -shm, -wal) unguarded; on Windows, SQLite (WAL mode) can hold handles after Factory.DisposeAsync(), so File.Delete throws IOException and fails all 21 integration tests.

## [****]
1. global.json: idempotently verify absence (git ls-files + filesystem check) — no action needed.
2. TestServerFixture.cs: wrap the delete loop in try-catch — catch (IOException) with a brief rationale comment (Windows SQLite handle lock; temp files OS-cleaned) and catch (UnauthorizedAccessException) defense-in-depth (per [****] review). No other changes.
3. Verify: dotnet build UrlShortener/UrlShortener.slnx (0 warnings/errors); dotnet test (40 unit + 21 integration passing on Linux). Windows-only failure mode verified by construction.

## Verdict
Simple enough for high-level [****] only — no [****] escalation (review verdict: APPROVE-WITH-CHANGES; recommendation to add UnauthorizedAccessException catch adopted). No new classes/methods/types — New Classes section is empty, [****] not required.

## Assumptions
- Temp files live in Path.GetTempPath(); leftovers are harmless (OS cleans temp).
- The Windows lock cannot be reproduced on the Linux runner; verification is via construction + green Linux suite.

## Decisions / trade-offs
- Deliberate, documented exception swallow in test-fixture cleanup only (accepted exception to Fail-Fast with genuine recovery fallback).
- Minimal diff; no new tests (no testable logic; mechanism-only assertions discouraged; existing 61 tests exercise DisposeAsync on every integration test run).

## Scope
- In scope: TestServerFixture.DisposeAsync cleanup hardening; global.json absence verification.
- Out of scope: production code, retry loops for deletion, logging infrastructure in the fixture.

## Acceptance criteria
- dotnet test unblocked for VSTest projects (no Microsoft.Testing.Platform forcing).
- DisposeAsync never throws on locked/unauthorized temp files; 61/61 tests green on Linux.

## Test cases
- No new tests (rationale above); regression coverage = existing 40 unit + 21 integration tests.

---

(Contents below are artifacts from a previous card/task — preserved as-is.)

# User Prompt

Fix 3 defects found by PR agent on the SciCalc MAUI Blazor Hybrid scientific calculator app located in the current working directory (a git worktree). The 3 defects:
1. The Android platform (src/SciCalc/Platforms/Android/) has MainActivity but no MauiApplication subclass that initializes the shared MAUI app via MauiProgram.CreateMauiApp(); add the Android application bootstrap class (MainApplication.cs) with its [Application] registration and required constructor.
2. The Windows target (src/SciCalc/Platforms/Windows/) lacks its WinUI/MAUI platform application class; add conventional App.xaml and App.xaml.cs deriving from MauiWinUIApplication with CreateMauiApp() returning MauiProgram.CreateMauiApp().
3. Windows Package.appxmanifest references icon and splash files that are absent from the project; add source icon/splash resources via MAUI resource items (MauiIcon/MauiSplashScreen in SciCalc.csproj with the Resources/AppIcon and Resources/Splash files) or add correctly sized packaged files and update manifest paths consistently.

Fix focus areas: src/SciCalc/Platforms/Android/MainActivity.cs, src/SciCalc/MauiProgram.cs, src/SciCalc/Platforms/Windows/Package.appxmanifest, src/SciCalc/SciCalc.csproj. The user wants tests-first where possible (reproduce defects via failing tests — note these are platform bootstrap defects that may not be unit-testable on Linux without the MAUI workload; justify whatever test approach you choose; the workload-free test solution is SciCalc.sln with 230 passing tests).

Produce a high-level [****] following Rich Domain Model PEAA and the repo architecture, deciding whether the task is simple enough to implement with just this high-level [****] or needs a full [****] workflow.

Constraints: Work only in the current working directory. Do NOT commit. Do NOT modify files unless the workflow explicitly requires creating [****] artifacts.

# High-Level [****]: SciCalc — Fix 3 Platform Bootstrap / Packaging Defects (Android MainApplication, Windows App.xaml, Icon/Splash Resources)

## REVIEW VERDICT: APPROVE WITH CHANGES

**Reviewer:** [****] (via [****] workflow)
**Date:** 2026-09-02
**Verdict:** APPROVE-WITH-CHANGES (5 required amendments, 1 optional)

The [****]'s overall structure, architecture analysis, test strategy, and implementation sequence are sound. However, the [****] was written against a **stale sibling worktree** and contains several factual errors about the current state of files in THIS worktree (branch tip `a614baa`). These errors change the nature of some fixes. All amendments below MUST be applied before implementation begins.

### Required Amendments

**[A1] Section 0 - Environment note is WRONG: rewrite entirely.**
The SciCalc tree IS in this worktree. `SciCalc.sln`, `SciCalc.App.sln`, and `src/SciCalc/SciCalc.csproj` all exist at the repo root. The claim that "this worktree contains the UrlShortener project at commit 28bff5b" is false -- the branch tip is `a614baa`. The [****]'s instruction to "run in a tree that contains SciCalc" is satisfied here. Remove the stale-sibling-directory references. The executor works in the current working directory.

**[A2] Section 2 Defect 1 - Android MainApplication: NOT "verify-only"; it is a REAL fix (file is ABSENT).**
The [****] says `Platforms/Android/MainApplication.cs` "already exists and is correct". In THIS worktree, the directory contains ONLY `MainActivity.cs` and `AndroidManifest.xml`. `MainApplication.cs` does NOT exist. The required action must be changed from "Verify-only" to "Add the file" (use the template in section 3, which is correct).

**[A3] Section 2 Defect 2 - Windows App.xaml.cs: both files are ABSENT, not just App.xaml.**
The [****] says `App.xaml.cs` "exists but is broken" with wrong base class `MauiWinApplication`. In THIS worktree, `Platforms/Windows/` contains ONLY `Package.appxmanifest` and `app.manifest`. Neither `App.xaml` nor `App.xaml.cs` exists. The action changes from "Add App.xaml; rewrite App.xaml.cs" to "Create BOTH App.xaml AND App.xaml.cs from scratch". The content contracts in section 3 remain correct. The negative test for the bogus `MauiWinApplication` string in section 4 is no longer applicable (there is no existing file to contain it), but is still harmless as a safety guard if kept.

**[A4] Section 2 Defect 3 - Package.appxmanifest is NOT a "gutted stub"; it is a full standard manifest.**
The [****] says the manifest is a "gutted stub (`<Deployment ...></Deployment>` only)". In THIS worktree, `Package.appxmanifest` is a complete, well-formed manifest with `Package/Identity` (Name=com.scicalc.app, Publisher=CN=SciCalc, Version=1.0.0.0), `Properties` (Logo=appicon.png), `Dependencies`, `Resources`, `Applications/Application`, and `uap:VisualElements` with Square150x150Logo/Square44x44Logo/DefaultTile logos all set to `appicon.png` and SplashScreen Image=`splashscreen.png`. The referenced PNG files (`appicon.png`, `splashscreen.png`) are ABSENT from the repo, and `src/SciCalc/Resources/` does not exist, and `SciCalc.csproj` has no `MauiIcon`/`MauiSplashScreen` items.
**Corrected action:** Do NOT "replace" the manifest wholesale. Instead: (a) add source SVG resources + `MauiIcon`/`MauiSplashScreen` csproj items (as the [****]'s section 3 correctly prescribes), and (b) update the manifest's logo/splash attribute values from literal `appicon.png`/`splashscreen.png` to `$placeholder$.png` so MAUI Resizetizer can substitute the generated asset paths. The rest of the manifest structure is retained. Section 3's manifest row and section 5 step 4 must be amended from "Replace stub with full template manifest" to "Update existing manifest asset references to $placeholder$.png".

**[A5] Section 2 - Remove the "incidental csproj hygiene issue" paragraph about duplicate Windows TFM.**
The [****] claims `net10.0-windows10.0.19041.0` appears in both the unconditional `TargetFrameworks` AND the conditioned append. In THIS worktree, the csproj has:
- Line 4: `<TargetFrameworks>net10.0-android;net10.0-ios;net10.0-maccatalyst</TargetFrameworks>` (NO windows TFM)
- Line 5: `<TargetFrameworks Condition="...IsOSPlatform('windows')...">$(TargetFrameworks);net10.0-windows10.0.19041.0</TargetFrameworks>`
There is no duplication. This "hygiene issue" does not exist. Remove the paragraph and all references to "optional TFM dedupe" throughout the [****] (sections 3, 5, 7, 8).

### Optional Amendment

**[A6] Section 0 - Architecture description mentions `SciCalc.Application` project; verify it exists.**
The [****] references a three-layer architecture: `SciCalc -> SciCalc.Application -> SciCalc.Domain`. In this worktree, `SciCalc.sln` contains only `SciCalc.Domain` and `SciCalc.Tests`; `SciCalc.App.sln` adds the MAUI project. There is no `SciCalc.Application` project visible. The csproj references only `SciCalc.Domain`. The executor should verify whether the Application layer exists or if the architecture is two-layer (SciCalc -> SciCalc.Domain). This does not affect the [****]'s fixes (which are all in the presentation/platform layer) but the architecture description should be accurate.

### Items Confirmed Correct (no changes needed)

- **Section 1 (Architecture/RDM-PEAA Alignment):** Correct. All three defects are pure presentation/platform glue. No domain model impact. Anti-procedural checklist passes: no domain logic in platform bootstrap classes, no calculation or business state in `MainApplication`/`App`/manifest -- these are framework-mandated shells delegating to `MauiProgram.CreateMauiApp()`.
- **Section 3 content contracts for MainApplication.cs, App.xaml, App.xaml.cs:** All correct as specified.
- **Section 3 content contracts for SVG resources and MauiIcon/MauiSplashScreen csproj items:** Correct.
- **Section 4 (Test Strategy):** Sound. Static conformance tests are the right approach given no MAUI workload on Linux. File existence + XML parsing + content regex assertions are the strongest executable checks available. Test cases are well-specified.
- **Section 4 test for "not the empty `<Deployment>` stub":** Still valid as a guard, even though the manifest is not currently a stub. Keep it.
- **Section 5 (Implementation Sequence):** Correct order (tests-first, then fixes, then green). Amend step 4 per A4.
- **Section 6 (Assessment - simple task):** Agreed. No `[****]` escalation needed.
- **Section 7 (Option (a) chosen):** Correct. MAUI Resizetizer + `$placeholder$` is the idiomatic approach.
- **Section 8 (Scope):** Correct after removing TFM dedupe references.
- **Section 9 (Acceptance Criteria):** Correct. Red-green demonstration, full test gate, no domain modifications.
- **Anti-Procedural Checklist (per [****]):** PASSES. No domain logic introduced in platform classes. No external dependencies in domain layer. No calculations in presentation layer. `MainApplication` and `App` are thin bootstrap delegates -- anemic by design and correctly so for MAUI platform adapters.

---

## 0. Critical Environment Note (read first)

- **This worktree contains the SciCalc app** at branch tip `a614baa`. `SciCalc.sln`, `SciCalc.App.sln`, and `src/SciCalc/SciCalc.csproj` are present at the repo root. All paths below are relative to the repo root.
- **Toolchain:** .NET SDK 10, **no MAUI workload installed**, Linux. The MAUI head project `src/SciCalc/SciCalc.csproj` (TFMs `net10.0-android;net10.0-ios;net10.0-maccatalyst` plus `net10.0-windows10.0.19041.0` conditioned on Windows) **cannot be compiled on this machine**. `SciCalc.sln` contains only `SciCalc.Domain` and `SciCalc.Tests` (workload-free gate). `SciCalc.App.sln` includes the MAUI project for workload machines.

## 1. Architecture / RDM-PEAA Alignment

Repo architecture (from `Docs/_Current[****].md` in the SciCalc tree): one-way dependency `SciCalc (MAUI Blazor Hybrid presentation) -> SciCalc.Application (thin facade/DTOs) -> SciCalc.Domain (all logic: Lexer, ExpressionParser, AST nodes, Calculator aggregate root, MemoryBank, HistoryLog)`. Domain has zero external dependencies; xUnit tests target only Domain/Application.

**RDM impact: none.** All three defects live in the MAUI head's *platform adapter* layer (`Platforms/*`) and in packaging metadata (csproj resource items, appxmanifest). No domain entity, value object, or application service changes. The new classes (`MainApplication`, Windows `App`) are framework-mandated bootstrap shells — anemic by design and correctly so: they are PEAA "presentation/platform glue", not domain objects, and their only behavior is delegating to `MauiProgram.CreateMauiApp()`. The rich domain model (`Calculator` aggregate, expression AST, `CalculatorAppService` facade) is untouched, and the existing 230-test suite remains the domain regression gate.

## 2. Defect Analysis (ground truth in THIS worktree, branch tip a614baa)

| # | Reported defect | Actual state in this worktree | Required action |
|---|---|---|---|
| 1 | Android: no `MainApplication.cs` | `Platforms/Android/` contains ONLY `MainActivity.cs` and `AndroidManifest.xml`. **`MainApplication.cs` does NOT exist.** | **Real fix (add file).** Create `MainApplication.cs` per §3 template. Pin with conformance test (§4). |
| 2 | Windows: lacks WinUI/MAUI app class | `Platforms/Windows/` contains ONLY `Package.appxmanifest` and `app.manifest`. **Neither `App.xaml` nor `App.xaml.cs` exists.** | **Real fix (create both files).** Add `App.xaml` and `App.xaml.cs` per §3 templates. |
| 3 | Manifest references absent icon/splash files | `Package.appxmanifest` is a **complete, well-formed manifest** (Package/Identity, Properties, Dependencies, Resources, Applications, VisualElements) referencing `appicon.png` and `splashscreen.png` — but those PNG files are **absent from the repo**. `src/SciCalc/Resources/` **does not exist**. `SciCalc.csproj` has **no** `MauiIcon`/`MauiSplashScreen` items. | **Real fix.** Add source SVG resources + MauiIcon/MauiSplashScreen csproj items + update manifest asset paths from literal PNGs to `$placeholder$.png` (§3). Do NOT replace the manifest wholesale — only update logo/splash attribute values. |

Note: the csproj has Windows TFM only in the conditioned `IsOSPlatform('windows')` line; there is no duplication. No TFM hygiene fix needed.

## 3. New/Changed Files Planned

| File | Action | Content contract |
|---|---|---|
| `src/SciCalc/Platforms/Android/MainApplication.cs` | **Add** | `[Application]` attr; `public class MainApplication : MauiApplication`; ctor `MainApplication(IntPtr handle, JniHandleOwnership ownership) : base(handle, ownership)`; `protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();` |
| `src/SciCalc/Platforms/Windows/App.xaml` | **Add** | Root `<maui:MauiWinUIApplication x:Class="SciCalc.Platforms.Windows.App" xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation" xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml" xmlns:maui="using:Microsoft.Maui" xmlns:local="using:SciCalc.Platforms.Windows">` with `Resources > ResourceDictionary.MergedDictionaries > <XamlControlsResources xmlns="using:Microsoft.UI.Xaml.Controls" />` (standard MAUI template). |
| `src/SciCalc/Platforms/Windows/App.xaml.cs` | **Add** | `namespace SciCalc.Platforms.Windows;` `public partial class App : MauiWinUIApplication { public App() { this.InitializeComponent(); } protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp(); }` |
| `src/SciCalc/Resources/AppIcon/appicon.svg` (+ optional `appiconfg.svg` foreground) | **Add** | Simple original SVG glyph (e.g., "√x" / calculator motif on brand background `#1e1e28` matching root `App.xaml` `PageBackgroundColor`). Source art only — MAUI Resizetizer generates all platform sizes at build time. |
| `src/SciCalc/Resources/Splash/splash.svg` | **Add** | Simple centered glyph SVG; MAUI generates splash per platform. |
| `src/SciCalc/SciCalc.csproj` | **Edit** | Add `<MauiIcon Include="Resources\AppIcon\appicon.svg" />` and `<MauiSplashScreen Include="Resources\Splash\splash.svg" BaseSize="128,128" Color="#1e1e28" />` item group (add `ForegroundFile`/`Color` on MauiIcon only if a foreground SVG is used). No other csproj changes; `WindowsPackageType=None` stays (unpackaged local runs still fine). No TFM dedupe needed (no duplication exists). |
| `src/SciCalc/Platforms/Windows/Package.appxmanifest` | **Edit (update asset references only)** | The existing manifest structure is correct and complete. Update logo/splash attribute values from literal `appicon.png`/`splashscreen.png` to `$placeholder$.png` in: `Properties/Logo`, `uap:VisualElements` `Square150x150Logo`/`Square44x44Logo`, `uap:DefaultTile` logos, and `uap:SplashScreen Image`. This lets MAUI Resizetizer substitute generated asset names from `MauiIcon`/`MauiSplashScreen`. Do NOT replace the manifest wholesale. |
| `tests/SciCalc.Packaging.Tests/` (new xUnit project, net10.0, **no reference to the MAUI head**) | **Add** + register in `SciCalc.sln` with full `Build.0` config entries (keep MAUI project excluded) | Static conformance tests (§4). Locates repo root by walking up from `AppContext.BaseDirectory` to the dir containing `SciCalc.sln`. |

No changes to `MauiProgram.cs` (already correct: `UseMauiApp<App>()`, BlazorWebView, DI for `Calculator`/`CalculatorAppService`) or `MainActivity.cs` — focus-area files inspected and cleared.

## 4. Test Strategy (tests-first) — and justification

**Constraint:** these are platform bootstrap/packaging defects in the MAUI head, which cannot compile or run on this Linux box (no MAUI workload; Windows/Android TFMs and SDKs unavailable). Therefore runtime/unit tests against `MainApplication`/WinUI `App` are **impossible here**, and referencing the MAUI project from tests would break the workload-free gate. The strongest *executable* check available on Linux is **static conformance tests** — file/XML content assertions that encode exactly the contract the PR agent verifies. They reproduce all three defects as **failing tests first** (on the inspected tree: App.xaml missing, manifest stub, no Resources, no MauiIcon/MauiSplashScreen items → red), then go green after the fix. This is the chosen approach; compile-level verification on Windows/Android is documented as a deferred manual/CI step (§8).

**Test cases to implement (`tests/SciCalc.Packaging.Tests`, xUnit `[Fact]`/`[Theory]`, file-I/O + `XDocument` only):**

Android (defect 1):
- `Platforms/Android/MainApplication.cs` exists.
- Content contains `[Application]`, `: MauiApplication`, `MauiProgram.CreateMauiApp()`, and a ctor signature with `IntPtr` + `JniHandleOwnership`.

Windows bootstrap (defect 2):
- `Platforms/Windows/App.xaml` exists; parses as XML; root local-name `MauiWinUIApplication`; `x:Class="SciCalc.Platforms.Windows.App"`.
- `App.xaml.cs` contains `partial class App : MauiWinUIApplication` and `MauiProgram.CreateMauiApp()`; does **not** contain the bogus `MauiWinApplication` (word-boundary match so `MauiWinUIApplication` doesn't false-positive).

Resources & manifest (defect 3):
- `SciCalc.csproj` contains ≥1 `MauiIcon` and ≥1 `MauiSplashScreen` item; each `Include` path resolves to an existing file under `src/SciCalc/Resources/`.
- `Resources/AppIcon/` and `Resources/Splash/` each contain ≥1 file.
- `Package.appxmanifest` parses; has `Package/Identity` with non-empty `Name`+`Version`, `Applications/Application`, and `VisualElements`; every logo/splash attribute value is either `$placeholder$.png` (allowed only when csproj has MauiIcon/MauiSplashScreen — consistency rule) or a path that exists on disk; the manifest is **not** the empty `<Deployment>` stub.
- Cross-check: if manifest uses `$placeholder$`, csproj MUST declare both `MauiIcon` and `MauiSplashScreen` (and vice versa).

Regression gate (unchanged): `dotnet test SciCalc.sln` → the existing 230 Domain/Application tests stay green; new packaging tests included via sln registration.

## 5. Implementation Sequence

1. [****] `tests/SciCalc.Packaging.Tests` (xUnit, net10.0, Microsoft.NET.Test.Sdk 17.14.1 / xunit 2.9.3 / runner 3.1.0 — match existing test projects), add to `SciCalc.sln` with Build.0 entries; write §4 tests → **run → red** (defects reproduced).
2. Defect 1: add Android `MainApplication.cs` (§3 contract).
3. Defect 2: add both `App.xaml` and `App.xaml.cs` (partial, `MauiWinUIApplication`).
4. Defect 3: add `Resources/AppIcon/appicon.svg` + `Resources/Splash/splash.svg`; add `MauiIcon`/`MauiSplashScreen` to csproj; update manifest asset references from literal PNGs to `$placeholder$.png`.
5. **Run → green**: new conformance tests + full `dotnet test SciCalc.sln` (existing tests + new).
6. Manual diff review: no domain/application files touched; no commit (per constraints).

## 6. Assessment

**Simple enough to implement from this high-level [****] — a full `[****]` workflow is NOT required.** The fixes are template-determined (MAUI conventions leave no design latitude), touch only platform glue + packaging metadata, involve zero domain-model or API design decisions, and the only option choice (asset strategy) is resolved in favor of the idiomatic MAUI source-resource approach (option a). Risks are environmental (no MAUI workload here), not architectural.

## 7. Assumptions / Decisions / Trade-offs

- **Option (a) chosen** (MauiIcon/MauiSplashScreen source SVGs + `$placeholder$` manifest) over hand-sized PNGs: single source of truth, Resizetizer guarantees size/manifest consistency, matches every MAUI template; trade-off is that actual PNG generation is only verifiable on a Windows/MAUI build (deferred, §8).
- SVG art will be simple original glyphs (no external assets/licensing concerns).
- Static conformance tests accepted as the defect-reproduction mechanism; justification in §4. They pin the PR-agent contract, run in the workload-free gate, and are honest about not proving compilation.
- `WindowsPackageType=None` retained; manifest correctness still enforced because packaging is a supported future path and the PR agent flags it.
- The existing `Package.appxmanifest` is retained and only its asset reference values are updated (not replaced wholesale), since it is a complete and valid manifest.

## 8. In Scope / Out of Scope

- **In:** the 3 defects; packaging conformance test project; keeping the test gate green; csproj resource items; manifest asset path updates; all three platform bootstrap files created.
- **Out:** domain/application code changes; MAUI workload installation; actual Windows/Android/MacCatalyst compilation or on-device runs (deferred to a Windows+MAUI CI lane: `dotnet build src/SciCalc -f net10.0-windows10.0.19041.0` and, if packaged, verifying generated `Assets/*` against the manifest); iOS/MacCatalyst bootstrap review (not flagged); committing changes.

## 9. Acceptance Criteria

- All §4 conformance tests fail before and pass after the fix (red→green demonstrated).
- `dotnet test SciCalc.sln` green: 230 pre-existing tests + new packaging tests.
- `Platforms/Windows/App.xaml` + corrected `App.xaml.cs` (`partial : MauiWinUIApplication`) present; Android `MainApplication.cs` present and correct.
- csproj declares `MauiIcon`/`MauiSplashScreen` pointing at existing `Resources/AppIcon`/`Resources/Splash` files; manifest is a complete WinUI manifest whose asset references are consistent with those resources.
- No modifications under `src/SciCalc.Domain`, `src/SciCalc.Application`, or existing test projects; no commits.

---

---

---

# User Prompt

Fix 2 defects found by PR agent on SciCalc (.NET MAUI Blazor Hybrid scientific calculator): (1) `MainPage.razor` illegally doubles as a MAUI `ContentPage` and a Blazor component; (2) root-level `dotnet test` against `SciCalc.sln` hits `NETSDK1147` because the MAUI project is in the solution, contradicting the documented workload-free verification workflow. Assess, agree/disagree, [****] the fix.

# High-Level [****]: SciCalc PR Defects D1 (MainPage page/component split) & D2 (workload-free verification solution)

## 0. Defect Assessment

Both defects are **agreed with — verified against the source tree** (branch tip `6c19549` on `origin/feature-card-6a95cde63dd6d80a97e9b10b-20260901001521336`; this worktree checked a stale tree — the code must be restored to this branch before implementation).

- **D1 confirmed.** `src/SciCalc/MainPage.razor` declares `@inherits ContentPage` while containing a `<BlazorWebView>` markup block. Blazor Razor components may only inherit from `IComponent`-compatible bases ([****] `ComponentBase`); `Microsoft.Maui.Controls.ContentPage` is not one, and the generated `BuildRenderTree` override has no valid base. Simultaneously, `App.cs` (`public App() => MainPage = new MainPage()`) requires `MainPage` to be a MAUI `Page`. The file claims both incompatible roles; on real MAUI TFMs the build/runtime contract cannot hold. Fix as the PR agent prescribed: a conventional MAUI `MainPage` (XAML + code-behind) hosting the `BlazorWebView`, with `Components/CalculatorPage.razor` as the sole Blazor root component.
- **D2 confirmed.** `SciCalc.sln` references `src/SciCalc/SciCalc.csproj` (MAUI). `dotnet test` against the solution builds every project, so on workload-free machines it fails with `NETSDK1147`, while `README.md` advertises root-level `dotnet test` as the quick verification path. The documentation and solution membership contradict each other.

`SciCalc.Tests` references only `SciCalc.Domain` — the two-project verification set is already clean; only the solution/document wiring is wrong.

## 1. Architecture / Approach Overview

Two independent, mechanical fixes; no domain-model changes.

- **D1 — separate the MAUI host page from the Blazor component.** Replace `src/SciCalc/MainPage.razor` with a conventional MAUI page pair `MainPage.xaml` + `MainPage.xaml.cs` (class `SciCalc.MainPage : ContentPage`). The XAML declares `BlazorWebView` with `HostPage="wwwroot/index.html"` and one `RootComponent` (`Selector="#app"`, `ComponentType="Components.CalculatorPage"`). `Components/CalculatorPage.razor` remains the Blazor component. `App.cs` continues to construct `new MainPage()`; `_Imports.razor` and `MauiProgram.cs` are untouched.
- **D2 — split verification from app packaging.** `SciCalc.sln` is reduced to `SciCalc.Domain` + `SciCalc.Tests` (workload-free; root `dotnet test` works as documented). A new `SciCalc.App.sln` includes all three projects for workload machines doing MAUI builds. Both READMEs are updated to state this layout explicitly.

Physical components and interaction: **Host (MAUI `App` → `MainPage : ContentPage` → `BlazorWebView` → root component `CalculatorPage`) → `Calculator` domain singleton (DI via `MauiProgram`) → `SciCalc.Domain` engine.** Verification path: `SciCalc.sln` {`SciCalc.Domain`, `SciCalc.Tests`} only; app path: `SciCalc.App.sln` adds the MAUI `SciCalc` project.

## 2. New Classes / Changes Planned

| Class | Responsibilities | New State/Fields | Associations | Methods |
|---|---|---|---|---|
| `MainPage` (new, in `MainPage.xaml` + `MainPage.xaml.cs`) | MAUI `ContentPage` that hosts the `BlazorWebView` | none beyond `ContentPage` | owns the `BlazorWebView`; root component = `Components.CalculatorPage` | `InitializeComponent()` (from XAML codegen); constructor `public MainPage()` |
| (deleted) `MainPage.razor` | incorrectly merged roles | — | — | — |

New solution artifact: `SciCalc.App.sln` (Domain + Tests + MAUI app). Modified: `SciCalc.sln` (drop MAUI project GUID `{0E0EA705-C8A8-4691-AD58-F620FF2B56A6}` from project list, configuration platforms, and nested-projects sections), `README.md`, `src/SciCalc/README.md`.

No new domain classes, associations, or methods — the Domain-first / anti-procedural checklist is satisfied because this change introduces no domain behavior; behavior remains on the existing `Calculator` aggregate and value objects.

## 3. Data Flow / Control Flow

- **App startup**: `MauiProgram.CreateMauiApp()` → `App` ctor → `MainPage = new MainPage()` → XAML builds `BlazorWebView` → Blazor renders `CalculatorPage` → presses are routed to the DI singleton `Calculator`. (Control flow only; unchanged semantics.)
- **Verification**: root `dotnet test` ([`SciCalc.sln` = Domain + Tests]) → `xUnit` runs 230 tests without enumerating MAUI targets. `SciCalc.App.sln` is only used on machines with `maui` workloads.

## 4. Integration Points / Structure

```
SciCalc.sln              -> SciCalc.Domain, SciCalc.Tests        (workload-free verification)
SciCalc.App.sln          -> + src/SciCalc (MAUI)                 (workload machines only)
src/SciCalc/
  MainPage.xaml(+cs)     (replaces MainPage.razor)
  Components/CalculatorPage.razor (unchanged)
README.md, src/SciCalc/README.md (updated layout + verify sections)
```

The README-described scratch Razor harness (plain `net10.0` SDK, `FrameworkReference Microsoft.AspNetCore.App`, MAUI packages, `Platforms/**` excluded) remains the Linux compile-check technique for the UI files, since real MAUI TFMs cannot build here.

## 5. Implementation Sequence

1. **D1**: delete `src/SciCalc/MainPage.razor`; add `MainPage.xaml` + `MainPage.xaml.cs` with the `BlazorWebView` / `CalculatorPage` wiring; verify `App.cs` compiles against the new type.
2. **D2**: remove the MAUI project from `SciCalc.sln`; create `SciCalc.App.sln` including all three projects.
3. Update `README.md` and `src/SciCalc/README.md` (solution layout, verify commands, MAUI workload caveat now references `SciCalc.App.sln`).
4. Validate: `dotnet test` (or `dotnet test SciCalc.sln`) at repo root → all 230 tests pass; compile-check UI via the scratch Razor harness → 0 errors.

## 6. Assessment

**Simple enough for this high-level [****]; `[****]` escalation not required.** The fixes are small, well-understood, and mechanical (one page split, one solution reorganization, docs updates); no cross-aggregate design work is involved.

## 7. Assumptions, Decisions, Trade-offs

- **XAML over a C#-only `ContentPage`**: chose the conventional XAML pair because it matches the standard MAUI template and keeps declarative WebView wiring readable; a C#-only page was the alternative. Either satisfies the defect; XAML is the normative choice.
- **Two solutions rather than one with conditional membership**: `SciCalc.sln` (verification) + `SciCalc.App.sln` (full app). Trade-off: two files to maintain, but root `dotnet test` becomes genuinely workload-free as documented, and IDE/Windows users keep a complete solution.
- **READMEs updated in both locations**: root README's "Quick verification" claim becomes true; `src/SciCalc/README.md` Solution layout describes both solutions and re-points the `NETSDK1147` caveat to `SciCalc.App.sln`.
- **Scratch harness retained for Linux compile verification** — MAUI TFMs stay unbuildable in the sandbox; that gate is documented and accepted.
- **No CI workflow file exists in-repo** (Digital Worker executes verification directly), so "configure CI" reduces to documenting the root command; misconfiguration risk is minimal.

## 8. In Scope / Out of Scope

- **In**: MainPage split (D1), solution split + README updates (D2), re-verification of the 230 domain tests, UI compile-check via scratch harness.
- **Out**: any calculator behavior change, MAUI-workload installation on the sandbox, restructuring of `Calculator`/Domain, CI pipeline files, packaging/deployment.

## 9. Acceptance Criteria

- `src/SciCalc` has no `MainPage.razor`; `MainPage` is a `ContentPage` (XAML + code-behind) whose `BlazorWebView` registers `Components.CalculatorPage` as its sole root component; `App` still sets `MainPage = new MainPage()`.
- Scratch Razor harness compile-check of `src/SciCalc` → 0 errors / 0 warnings.
- `SciCalc.sln` contains exactly `SciCalc.Domain` and `SciCalc.Tests`; root-level `dotnet test` exits 0 (230/230) with no `NETSDK1147` and no reference to the MAUI project.
- `SciCalc.App.sln` includes the MAUI project for workload machines.
- Both READMEs describe the new solution layout and the correct verification/build commands.

## 10. Test Cases to Implement

- **Regression (existing)**: all 230 `SciCalc.Tests` pass via root `dotnet test` on `SciCalc.sln`.
- **D1 verification**: scratch harness compile-check passes; `grep` guards — no `MainPage.razor` remains under `src/SciCalc`; `App.cs` target type resolves.
- **D2 verification**: root `dotnet test` output lists only Domain/Tests projects; no `NETSDK1147`; exit code 0.
- Optional manual check on a workload machine (out of sandbox scope): `dotnet build SciCalc.App.sln` succeeds.

---

