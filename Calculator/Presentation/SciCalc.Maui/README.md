# SciCalc (MAUI Blazor Hybrid)

Scientific calculator built as a .NET 10 MAUI Blazor Hybrid app over the `SciCalc.Domain` engine. All calculation behavior lives in `SciCalc.Domain` (hand-written parser/evaluator, `Calculator` session aggregate); this project only sends `InputKey` presses to the `Calculator` aggregate (registered as a DI singleton in `MauiProgram.cs`) and renders its state.

## Solution layout

Two solutions, split by verification vs. app packaging:

- `SciCalc.slnx` (Calculator folder) — `SciCalc.Domain` + `SciCalc.Domain.UnitTests` only. This is the workload-free verification solution; `dotnet test SciCalc.slnx` never touches MAUI targets.
- `SciCalc.App.slnx` (Calculator folder) — `SciCalc.Domain` + `SciCalc.Domain.UnitTests` + `SciCalc.Maui` (this project) + `SciCalc.Maui.UnitTests`. For machines with MAUI workloads installed (IDE builds, packaging, `dotnet build SciCalc.App.slnx`).
- `Domain/SciCalc.Domain/` — calculator engine: tokenization, recursive-descent parser, evaluator, session state (input buffer, history, memory, ANS, angle mode, error lockout). No external dependencies.
- `Presentation/SciCalc.Maui/` — MAUI Blazor Hybrid presentation (this project): `Components/CalculatorPage.razor` (+ scoped CSS) renders the keypad, two-line display, DEG/RAD badge, memory indicators and history panel; `MainPage.xaml` (+ `MainPage.xaml.cs` code-behind) is the MAUI `ContentPage` hosting the `BlazorWebView` with `Components/CalculatorPage` as the root component; `MauiProgram.cs` registers `Calculator` as a singleton; `_Imports.razor` supplies the shared Razor usings.
- `Domain/SciCalc.Domain.UnitTests/` — xUnit unit tests for the Domain layer, co-located with its subject under the Domain layer folder.
- `Presentation/SciCalc.Maui.UnitTests/` — xUnit packaging-conformance unit tests for this project (csproj, `Package.appxmanifest`, resources), co-located beside it under the Presentation layer folder.

Both solutions use the `.slnx` format and shared `Directory.Build.props` / `Directory.Packages.props` (Central Package Management) at the `Calculator/` root.

The MAUI project targets platform TFMs (`net10.0-android`, `net10.0-ios`, `net10.0-maccatalyst`, plus `net10.0-windows10.0.19041.0` conditioned on Windows) and is deliberately kept out of `SciCalc.slnx`. On machines without workloads, building this project (or `SciCalc.App.slnx`) fails with `NETSDK1147` — expected and accepted; `dotnet test SciCalc.slnx` (which builds `SciCalc.Domain` + `SciCalc.Domain.UnitTests`) still passes.

## Verify (no MAUI workloads required)

```
dotnet test SciCalc.slnx
# or equivalently:
dotnet test Domain/SciCalc.Domain.UnitTests/SciCalc.Domain.UnitTests.csproj
```

Always name the solution file explicitly — the repo also hosts `UrlShortener/UrlShortener.slnx` from an unrelated demo, so a bare `dotnet test` is ambiguous. All test projects use the standard xUnit (v2) + VSTest setup (`Microsoft.NET.Test.Sdk` + `xunit.runner.visualstudio`), so plain `dotnet test` (with or without `--nologo`) discovers and runs them normally.

## Build the MAUI app (workload machines only)

Windows is the primary dev/demo target (`net10.0-windows10.0.19041.0`); Android/iOS/MacCatalyst are configured to compile (`dotnet workload install maui` or the per-platform workloads are required).

On a machine without workloads (e.g. the Linux sandbox), `dotnet build Presentation/SciCalc.Maui/SciCalc.Maui.csproj` (or `dotnet build SciCalc.App.slnx`) fails with `NETSDK1147` (missing `maui-android` workload) — expected and accepted; it must not block verification. Razor/C# correctness of the UI files was compile-checked with a temporary `Microsoft.NET.Sdk.Razor` harness: a scratch project placed inside `Presentation/SciCalc.Maui` (so default globs pick up `_Imports.razor`, `App.cs`, `MauiProgram.cs`, `MainPage.xaml`/`MainPage.xaml.cs` and `Components/*`) with plain `net10.0` TFM, `FrameworkReference Microsoft.AspNetCore.App`, package references `Microsoft.Maui.Controls` + `Microsoft.AspNetCore.Components.WebView.Maui`, a project reference to `SciCalc.Domain`, and `Platforms/**` excluded from compilation. The harness reported 0 errors, with a single expected `CS0618` obsolescence warning from `App.cs`'s `MainPage = new MainPage()` assignment (MAUI 10 deprecates that setter; the assignment is kept intentionally), and the MAUI XAML source generator validated `MainPage.xaml` by generating its `InitializeComponent`. The harness is deleted after use.

## Behavior decisions

- **ANS before the first evaluated answer inserts `0`** (deterministic; the ANS key is never a dead key).
- History keeps the last 10 evaluations, newest first; tapping an entry restores its expression into the input buffer.
- A numeric literal beyond `double` range (309+ significant digits) locks the calculator with the `Overflow` error state; only AC unfreezes it. Evaluation-time overflow (e.g. `171!`, `2^10000`) uses the same `Overflow` error path.
- After an error, every keypress except AC is ignored (lockout); the error banner shows "Error" plus a short reason.
- Percent semantics: standalone `50%` → 0.5; baseline-scaled on `+`/`−` (`200 + 10%` → 220).
- No persistence of history or memory across launches.
