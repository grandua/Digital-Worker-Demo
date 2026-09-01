# SciCalc (MAUI Blazor Hybrid)

Scientific calculator built as a .NET 10 MAUI Blazor Hybrid app over the `SciCalc.Domain` engine. All calculation behavior lives in `SciCalc.Domain` (hand-written parser/evaluator, `Calculator` session aggregate); this project only sends `InputKey` presses to the `Calculator` aggregate (registered as a DI singleton in `MauiProgram.cs`) and renders its state.

## Solution layout

- `SciCalc.sln` — contains `SciCalc.Domain`, `SciCalc` (this project) and `SciCalc.Tests`.
- `src/SciCalc.Domain/` — calculator engine: tokenization, recursive-descent parser, evaluator, session state (input buffer, history, memory, ANS, angle mode, error lockout). No external dependencies.
- `src/SciCalc/` — MAUI Blazor Hybrid presentation (this project): `Components/CalculatorPage.razor` (+ scoped CSS) renders the keypad, two-line display, DEG/RAD badge, memory indicators and history panel; `MainPage.razor` hosts the `BlazorWebView`; `MauiProgram.cs` registers `Calculator` as a singleton; `_Imports.razor` supplies the shared Razor usings.
- `tests/SciCalc.Tests/` — xUnit v3 tests for the Domain layer.

The MAUI project is intentionally **not** in the solution: it targets only platform TFMs (`net10.0-android`, `net10.0-ios`, `net10.0-maccatalyst`, plus `net10.0-windows10.0.19041.0` conditioned on Windows), which require MAUI workloads. Keeping it out of the solution lets `dotnet test` pass at repo root on machines without workloads.

## Verify (no MAUI workloads required)

```
dotnet test
# or equivalently:
dotnet test tests/SciCalc.Tests/SciCalc.Tests.csproj
```

Caveat: on SDK 10.0.302, do **not** pass `--nologo` — it routes `dotnet test` through the legacy VSTest path, which reports "Zero tests ran (error: 1)" even though all tests pass.

## Build the MAUI app (workload machines only)

Windows is the primary dev/demo target (`net10.0-windows10.0.19041.0`); Android/iOS/MacCatalyst are configured to compile (`dotnet workload install maui` or the per-platform workloads are required).

On a machine without workloads (e.g. the Linux sandbox), `dotnet build src/SciCalc/SciCalc.csproj` fails with `NETSDK1147` (missing `maui-android` workload) — expected and accepted; it must not block verification. Razor/C# correctness of the UI components was compile-checked with a temporary `Microsoft.NET.Sdk.Razor` harness: a scratch project placed inside `src/SciCalc` (so default globs pick up `_Imports.razor`, `MainPage.razor` and `Components/*`) with plain `net10.0` TFM, `FrameworkReference Microsoft.AspNetCore.App`, package references `Microsoft.Maui.Controls` + `Microsoft.AspNetCore.Components.WebView.Maui`, a project reference to `SciCalc.Domain`, and `Platforms/**` excluded from compilation. The harness compiles with 0 errors / 0 warnings and is deleted after use.

## Behavior decisions

- **ANS before the first evaluated answer inserts `0`** (deterministic; the ANS key is never a dead key).
- History keeps the last 10 evaluations, newest first; tapping an entry restores its expression into the input buffer.
- A numeric literal beyond `double` range (309+ significant digits) locks the calculator with the `Overflow` error state; only AC unfreezes it. Evaluation-time overflow (e.g. `171!`, `2^10000`) uses the same `Overflow` error path.
- After an error, every keypress except AC is ignored (lockout); the error banner shows "Error" plus a short reason.
- Percent semantics: standalone `50%` → 0.5; baseline-scaled on `+`/`−` (`200 + 10%` → 220).
- No persistence of history or memory across launches.
