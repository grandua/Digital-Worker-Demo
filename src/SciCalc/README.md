# SciCalc (MAUI Blazor Hybrid)

Presentation layer for SciCalc. All calculation behavior lives in `SciCalc.Domain`; this project only sends `InputKey` presses to the `Calculator` aggregate (registered as a singleton in `MauiProgram.cs`) and renders its state.

## Building

Windows is the primary target (`net10.0-windows10.0.19041.0`, conditioned on a Windows OS). Android/iOS/MacCatalyst are compile targets.

On Linux, `dotnet build src/SciCalc/SciCalc.csproj` fails with `NETSDK1147` (MAUI workloads not installed, e.g. `maui-android`) — expected and acceptable. Verification of calculator behavior runs via `dotnet test tests/SciCalc.Tests` (Domain + xUnit, net10.0), which does not require MAUI workloads. Razor/C# correctness of the UI component was compile-checked with a temporary `Microsoft.NET.Sdk.Razor` harness referencing only `Microsoft.AspNetCore.App` (0 errors); it can be recreated from `src/SciCalc/Components/CalculatorPage.razor` + a project reference to `SciCalc.Domain` if needed.

## Layout

- `MauiProgram.cs` — app entry, DI (Calculator singleton), BlazorWebView services, developer tools on Windows debug builds.
- `App.cs`, `MainPage.razor` — MAUI shell; `MainPage` hosts the `BlazorWebView` with the root component.
- `Components/CalculatorPage.razor` + `.razor.css` — root Blazor component: two-line display with error state, DEG/RAD toggle, memory rows with non-empty badges, history panel (newest first, tap to restore), scientific and main keypads. The scoped stylesheet bundle (`SciCalc.styles.css`) is referenced from `wwwroot/index.html`.
- `wwwroot/` — Blazor host page and global styles.
- `Platforms/` — standard MAUI platform scaffolding (Android, iOS, MacCatalyst, Windows).
