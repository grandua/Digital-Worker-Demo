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
