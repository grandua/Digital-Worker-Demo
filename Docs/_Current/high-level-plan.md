# High-Level [****]: Root `Digital-Worker-Demo.slnx` Aggregate Solution

## 1. Objective (User Intent)

Create a single root solution file `Digital-Worker-Demo.slnx` at the repo root that aggregates the existing Calculator and UrlShortener .NET projects, such that:

```
dotnet build Digital-Worker-Demo.slnx
```

succeeds from the repo root.

## 2. Current State (Explored)

Repo root: `/workdir/.DigitalWorker/worktrees/feature-card-6aa054cc8a4f392030781066-20260908183625485`

```
Calculator/
  SciCalc.slnx                              (workload-free: Domain + Domain.UnitTests)
  SciCalc.App.slnx                          (adds MAUI app projects; needs MAUI workloads)
  Directory.Packages.props, Directory.Build.props
  Domain/SciCalc.Domain/SciCalc.Domain.csproj                                   (net10.0)
  Domain/SciCalc.Domain.UnitTests/SciCalc.Domain.UnitTests.csproj               (net10.0)
  Presentation/SciCalc.Maui/SciCalc.Maui.csproj                                 (net10.0-android/ios/maccatalyst[/windows] — MAUI workloads required)
  Presentation/SciCalc.Maui.UnitTests/SciCalc.Maui.UnitTests.csproj             (packaging-conformance tests)
UrlShortener/
  UrlShortener.slnx
  Directory.Packages.props, Directory.Build.props
  Presentation/UrlShortener.Api/UrlShortener.Api.csproj                         (net10.0)
  Presentation/UrlShortener.Api.UnitTests/UrlShortener.Api.UnitTests.csproj     (net10.0)
  Presentation/UrlShortener.Api.IntegrationTests/UrlShortener.Api.IntegrationTests.csproj (net10.0)
Docs/_Current/            (existing [****] artifacts)
README.md                 (documents MAUI workload caveat: NETSDK1147 without workloads)
```

Environment: .NET SDK 10.0.302 installed (native `.slnx` support).

## 3. Key Design Decision: Project Inclusion

**Include only workload-free projects (5):**

| Solution folder | Project |
|---|---|
| `/Calculator/Domain/` | `Calculator/Domain/SciCalc.Domain/SciCalc.Domain.csproj` |
| `/Calculator/Domain/` | `Calculator/Domain/SciCalc.Domain.UnitTests/SciCalc.Domain.UnitTests.csproj` |
| `/UrlShortener/Presentation/` | `UrlShortener/Presentation/UrlShortener.Api/UrlShortener.Api.csproj` |
| `/UrlShortener/Presentation/` | `UrlShortener/Presentation/UrlShortener.Api.UnitTests/UrlShortener.Api.UnitTests.csproj` |
| `/UrlShortener/Presentation/` | `UrlShortener/Presentation/UrlShortener.Api.IntegrationTests/UrlShortener.Api.IntegrationTests.csproj` |

**Exclude** `SciCalc.Maui.csproj` and `SciCalc.Maui.UnitTests.csproj`: the MAUI project targets `net10.0-android;net10.0-ios;net10.0-maccatalyst` and fails with `NETSDK1147` on machines without MAUI workloads (per README). Including it would violate the "must build successfully" acceptance criterion. This mirrors the existing `SciCalc.slnx` (workload-free) precedent. MAUI projects remain buildable via `Calculator/SciCalc.App.slnx` on workload-equipped machines.

## 4. [****] Steps

1. **Create `Digital-Worker-Demo.slnx`** at the repo root, following the established XML format of the existing `.slnx` files, with `<Folder Name="/Calculator/Domain/">` and `<Folder Name="/UrlShortener/Presentation/">` containing the 5 `<Project Path="...">` entries above (paths relative to the repo root).
2. **Verify restore + build:** run `dotnet build Digital-Worker-Demo.slnx` from the repo root — must succeed with 0 errors.
3. **Verify tests still run (sanity):** `dotnet test Digital-Worker-Demo.slnx` (optional but recommended; all projects are workload-free xUnit).

## 5. Rich Domain Model / PEAA Applicability

Config-only task: no domain code, no new classes, no architecture changes. RDM/PEAA guidance (anemic-vs-rich model, layer boundaries, CRC cards) is **not applicable**. Existing per-directory `Directory.Packages.props` / `Directory.Build.props` continue to apply unchanged because they resolve relative to each project's own directory — no root-level CPM file is introduced, so no central-package-management conflicts arise.

## 6. Risks & Mitigations

| Risk | Mitigation |
|---|---|
| Including MAUI projects breaks build without workloads (NETSDK1147) | Exclude them (Section 3); document in the file/PR |
| Wrong relative paths in `.slnx` | Paths relative to root; verify with build in step 2 |
| `.slnx` tooling support | SDK 10.0.302 installed; existing `.slnx` files already build in this repo |

## 7. Complexity Verdict

**Simple config-level task.** The [****] is sufficient to implement directly (one file + build verification). A full `[****]` workflow is **not required**.
