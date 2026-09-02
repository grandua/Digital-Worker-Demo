# User Prompt

Fix 3 defects found by PR agent on the SciCalc MAUI Blazor Hybrid scientific calculator app located in the current working directory (a git worktree). The 3 defects:
1. The Android platform (src/SciCalc/Platforms/Android/) has MainActivity but no MauiApplication subclass that initializes the shared MAUI app via MauiProgram.CreateMauiApp(); add the Android application bootstrap class (MainApplication.cs) with its [Application] registration and required constructor.
2. The Windows target (src/SciCalc/Platforms/Windows/) lacks its WinUI/MAUI platform application class; add conventional App.xaml and App.xaml.cs deriving from MauiWinUIApplication with CreateMauiApp() returning MauiProgram.CreateMauiApp().
3. Windows Package.appxmanifest references icon and splash files that are absent from the project; add source icon/splash resources via MAUI resource items (MauiIcon/MauiSplashScreen in SciCalc.csproj with the Resources/AppIcon and Resources/Splash files) or add correctly sized packaged files and update manifest paths consistently.

Fix focus areas: src/SciCalc/Platforms/Android/MainActivity.cs, src/SciCalc/MauiProgram.cs, src/SciCalc/Platforms/Windows/Package.appxmanifest, src/SciCalc/SciCalc.csproj. The user wants tests-first where possible (reproduce defects via failing tests — note these are platform bootstrap defects that may not be unit-testable on Linux without the MAUI workload; justify whatever test approach you choose; the workload-free test solution is SciCalc.sln with 230 passing tests).

Produce a high-level plan following Rich Domain Model PEAA and the repo architecture, deciding whether the task is simple enough to implement with just this high-level plan or needs a full /plan-and-design workflow.

Constraints: Work only in the current working directory. Do NOT commit. Do NOT modify files unless the workflow explicitly requires creating plan artifacts.

# High-Level Plan: SciCalc — Fix 3 Platform Bootstrap / Packaging Defects (Android MainApplication, Windows App.xaml, Icon/Splash Resources)

## REVIEW VERDICT: APPROVE WITH CHANGES

**Reviewer:** plan-reviewer (via /review-high-level-plan workflow)
**Date:** 2026-09-02
**Verdict:** APPROVE-WITH-CHANGES (5 required amendments, 1 optional)

The plan's overall structure, architecture analysis, test strategy, and implementation sequence are sound. However, the plan was written against a **stale sibling worktree** and contains several factual errors about the current state of files in THIS worktree (branch tip `a614baa`). These errors change the nature of some fixes. All amendments below MUST be applied before implementation begins.

### Required Amendments

**[A1] Section 0 - Environment note is WRONG: rewrite entirely.**
The SciCalc tree IS in this worktree. `SciCalc.sln`, `SciCalc.App.sln`, and `src/SciCalc/SciCalc.csproj` all exist at the repo root. The claim that "this worktree contains the UrlShortener project at commit 28bff5b" is false -- the branch tip is `a614baa`. The plan's instruction to "run in a tree that contains SciCalc" is satisfied here. Remove the stale-sibling-directory references. The executor works in the current working directory.

**[A2] Section 2 Defect 1 - Android MainApplication: NOT "verify-only"; it is a REAL fix (file is ABSENT).**
The plan says `Platforms/Android/MainApplication.cs` "already exists and is correct". In THIS worktree, the directory contains ONLY `MainActivity.cs` and `AndroidManifest.xml`. `MainApplication.cs` does NOT exist. The required action must be changed from "Verify-only" to "Add the file" (use the template in section 3, which is correct).

**[A3] Section 2 Defect 2 - Windows App.xaml.cs: both files are ABSENT, not just App.xaml.**
The plan says `App.xaml.cs` "exists but is broken" with wrong base class `MauiWinApplication`. In THIS worktree, `Platforms/Windows/` contains ONLY `Package.appxmanifest` and `app.manifest`. Neither `App.xaml` nor `App.xaml.cs` exists. The action changes from "Add App.xaml; rewrite App.xaml.cs" to "Create BOTH App.xaml AND App.xaml.cs from scratch". The content contracts in section 3 remain correct. The negative test for the bogus `MauiWinApplication` string in section 4 is no longer applicable (there is no existing file to contain it), but is still harmless as a safety guard if kept.

**[A4] Section 2 Defect 3 - Package.appxmanifest is NOT a "gutted stub"; it is a full standard manifest.**
The plan says the manifest is a "gutted stub (`<Deployment ...></Deployment>` only)". In THIS worktree, `Package.appxmanifest` is a complete, well-formed manifest with `Package/Identity` (Name=com.scicalc.app, Publisher=CN=SciCalc, Version=1.0.0.0), `Properties` (Logo=appicon.png), `Dependencies`, `Resources`, `Applications/Application`, and `uap:VisualElements` with Square150x150Logo/Square44x44Logo/DefaultTile logos all set to `appicon.png` and SplashScreen Image=`splashscreen.png`. The referenced PNG files (`appicon.png`, `splashscreen.png`) are ABSENT from the repo, and `src/SciCalc/Resources/` does not exist, and `SciCalc.csproj` has no `MauiIcon`/`MauiSplashScreen` items.
**Corrected action:** Do NOT "replace" the manifest wholesale. Instead: (a) add source SVG resources + `MauiIcon`/`MauiSplashScreen` csproj items (as the plan's section 3 correctly prescribes), and (b) update the manifest's logo/splash attribute values from literal `appicon.png`/`splashscreen.png` to `$placeholder$.png` so MAUI Resizetizer can substitute the generated asset paths. The rest of the manifest structure is retained. Section 3's manifest row and section 5 step 4 must be amended from "Replace stub with full template manifest" to "Update existing manifest asset references to $placeholder$.png".

**[A5] Section 2 - Remove the "incidental csproj hygiene issue" paragraph about duplicate Windows TFM.**
The plan claims `net10.0-windows10.0.19041.0` appears in both the unconditional `TargetFrameworks` AND the conditioned append. In THIS worktree, the csproj has:
- Line 4: `<TargetFrameworks>net10.0-android;net10.0-ios;net10.0-maccatalyst</TargetFrameworks>` (NO windows TFM)
- Line 5: `<TargetFrameworks Condition="...IsOSPlatform('windows')...">$(TargetFrameworks);net10.0-windows10.0.19041.0</TargetFrameworks>`
There is no duplication. This "hygiene issue" does not exist. Remove the paragraph and all references to "optional TFM dedupe" throughout the plan (sections 3, 5, 7, 8).

### Optional Amendment

**[A6] Section 0 - Architecture description mentions `SciCalc.Application` project; verify it exists.**
The plan references a three-layer architecture: `SciCalc -> SciCalc.Application -> SciCalc.Domain`. In this worktree, `SciCalc.sln` contains only `SciCalc.Domain` and `SciCalc.Tests`; `SciCalc.App.sln` adds the MAUI project. There is no `SciCalc.Application` project visible. The csproj references only `SciCalc.Domain`. The executor should verify whether the Application layer exists or if the architecture is two-layer (SciCalc -> SciCalc.Domain). This does not affect the plan's fixes (which are all in the presentation/platform layer) but the architecture description should be accurate.

### Items Confirmed Correct (no changes needed)

- **Section 1 (Architecture/RDM-PEAA Alignment):** Correct. All three defects are pure presentation/platform glue. No domain model impact. Anti-procedural checklist passes: no domain logic in platform bootstrap classes, no calculation or business state in `MainApplication`/`App`/manifest -- these are framework-mandated shells delegating to `MauiProgram.CreateMauiApp()`.
- **Section 3 content contracts for MainApplication.cs, App.xaml, App.xaml.cs:** All correct as specified.
- **Section 3 content contracts for SVG resources and MauiIcon/MauiSplashScreen csproj items:** Correct.
- **Section 4 (Test Strategy):** Sound. Static conformance tests are the right approach given no MAUI workload on Linux. File existence + XML parsing + content regex assertions are the strongest executable checks available. Test cases are well-specified.
- **Section 4 test for "not the empty `<Deployment>` stub":** Still valid as a guard, even though the manifest is not currently a stub. Keep it.
- **Section 5 (Implementation Sequence):** Correct order (tests-first, then fixes, then green). Amend step 4 per A4.
- **Section 6 (Assessment - simple task):** Agreed. No `/plan-and-design` escalation needed.
- **Section 7 (Option (a) chosen):** Correct. MAUI Resizetizer + `$placeholder$` is the idiomatic approach.
- **Section 8 (Scope):** Correct after removing TFM dedupe references.
- **Section 9 (Acceptance Criteria):** Correct. Red-green demonstration, full test gate, no domain modifications.
- **Anti-Procedural Checklist (per /system-architect):** PASSES. No domain logic introduced in platform classes. No external dependencies in domain layer. No calculations in presentation layer. `MainApplication` and `App` are thin bootstrap delegates -- anemic by design and correctly so for MAUI platform adapters.

---

## 0. Critical Environment Note (read first)

- **This worktree contains the SciCalc app** at branch tip `a614baa`. `SciCalc.sln`, `SciCalc.App.sln`, and `src/SciCalc/SciCalc.csproj` are present at the repo root. All paths below are relative to the repo root.
- **Toolchain:** .NET SDK 10, **no MAUI workload installed**, Linux. The MAUI head project `src/SciCalc/SciCalc.csproj` (TFMs `net10.0-android;net10.0-ios;net10.0-maccatalyst` plus `net10.0-windows10.0.19041.0` conditioned on Windows) **cannot be compiled on this machine**. `SciCalc.sln` contains only `SciCalc.Domain` and `SciCalc.Tests` (workload-free gate). `SciCalc.App.sln` includes the MAUI project for workload machines.

## 1. Architecture / RDM-PEAA Alignment

Repo architecture (from `Docs/_Current/plan.md` in the SciCalc tree): one-way dependency `SciCalc (MAUI Blazor Hybrid presentation) -> SciCalc.Application (thin facade/DTOs) -> SciCalc.Domain (all logic: Lexer, ExpressionParser, AST nodes, Calculator aggregate root, MemoryBank, HistoryLog)`. Domain has zero external dependencies; xUnit tests target only Domain/Application.

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

1. Scaffold `tests/SciCalc.Packaging.Tests` (xUnit, net10.0, Microsoft.NET.Test.Sdk 17.14.1 / xunit 2.9.3 / runner 3.1.0 — match existing test projects), add to `SciCalc.sln` with Build.0 entries; write §4 tests → **run → red** (defects reproduced).
2. Defect 1: add Android `MainApplication.cs` (§3 contract).
3. Defect 2: add both `App.xaml` and `App.xaml.cs` (partial, `MauiWinUIApplication`).
4. Defect 3: add `Resources/AppIcon/appicon.svg` + `Resources/Splash/splash.svg`; add `MauiIcon`/`MauiSplashScreen` to csproj; update manifest asset references from literal PNGs to `$placeholder$.png`.
5. **Run → green**: new conformance tests + full `dotnet test SciCalc.sln` (existing tests + new).
6. Manual diff review: no domain/application files touched; no commit (per constraints).

## 6. Assessment

**Simple enough to implement from this high-level plan — a full `/plan-and-design` workflow is NOT required.** The fixes are template-determined (MAUI conventions leave no design latitude), touch only platform glue + packaging metadata, involve zero domain-model or API design decisions, and the only option choice (asset strategy) is resolved in favor of the idiomatic MAUI source-resource approach (option a). Risks are environmental (no MAUI workload here), not architectural.

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

Fix 2 defects found by PR agent on SciCalc (.NET MAUI Blazor Hybrid scientific calculator): (1) `MainPage.razor` illegally doubles as a MAUI `ContentPage` and a Blazor component; (2) root-level `dotnet test` against `SciCalc.sln` hits `NETSDK1147` because the MAUI project is in the solution, contradicting the documented workload-free verification workflow. Assess, agree/disagree, plan the fix.

# High-Level Plan: SciCalc PR Defects D1 (MainPage page/component split) & D2 (workload-free verification solution)

## 0. Defect Assessment

Both defects are **agreed with — verified against the source tree** (branch tip `6c19549` on `origin/feature-card-6a95cde63dd6d80a97e9b10b-20260901001521336`; this worktree checked a stale tree — the code must be restored to this branch before implementation).

- **D1 confirmed.** `src/SciCalc/MainPage.razor` declares `@inherits ContentPage` while containing a `<BlazorWebView>` markup block. Blazor Razor components may only inherit from `IComponent`-compatible bases (default `ComponentBase`); `Microsoft.Maui.Controls.ContentPage` is not one, and the generated `BuildRenderTree` override has no valid base. Simultaneously, `App.cs` (`public App() => MainPage = new MainPage()`) requires `MainPage` to be a MAUI `Page`. The file claims both incompatible roles; on real MAUI TFMs the build/runtime contract cannot hold. Fix as the PR agent prescribed: a conventional MAUI `MainPage` (XAML + code-behind) hosting the `BlazorWebView`, with `Components/CalculatorPage.razor` as the sole Blazor root component.
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

**Simple enough for this high-level plan; `/plan-and-design` escalation not required.** The fixes are small, well-understood, and mechanical (one page split, one solution reorganization, docs updates); no cross-aggregate design work is involved.

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
