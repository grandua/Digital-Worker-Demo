# SciCalc.Maui Windows unpackaged launch — Code Smells Audit

**Workflow:** `[****]-and-plan-refactoring`  
**Session:** `bd5e93164d0b461081f9e1518713f2e1`  
**Mode:** AUDIT ONLY — findings + `//TODO` markers only; no behavior/architecture/implementation fixes.  
**Date:** 2026-09-16  

## Scope (user-specified)

Unstaged / untracked only:

| Path | Status | Kind |
|------|--------|------|
| `Calculator/Presentation/SciCalc.Maui/Platforms/Windows/app.manifest` | modified | XML root `manifestVersion="1.0"` fix |
| `Calculator/Presentation/SciCalc.Maui/SciCalc.Maui.csproj` | modified | MSBuild: `WindowsPackageType=None`, `WindowsAppSDKSelfContained=true` + comments |
| `Calculator/Presentation/SciCalc.Maui.UnitTests/WindowsAppManifestTests.cs` | new (untracked) | sealed xUnit conformance tests (5 facts) |

**Task context:** config/XML + test-only delta enabling unpackaged Windows launch. **No production C# behavior changes.**

## Smell inventory (this scope)

| Severity | Count | Notes |
|----------|------:|-------|
| CRITICAL | 0 | No architecture/layer/circular issues |
| HIGH | 0 | No LPL, statics (non-const), Feature Envy, long methods >20, duplicates >2 |
| MEDIUM | 0 | No message chains 3+, method length 11–20, naming issues |
| LOW | 0 | Comments in csproj are clear intent |
| **Total new open** | **0** | |

## Explicit non-findings

- **app.manifest:** 1-line assembly root attribute fix; not OOP surface.
- **SciCalc.Maui.csproj:** two properties + explanatory comments; config only; comments state intent (unpackaged + self-contained WASDK).
- **WindowsAppManifestTests.cs:**
  - Sealed class, inherits `ConformanceTests` (sibling convention).
  - `private const` strings only (acceptable static constants).
  - No non-constant static members.
  - Methods ≤ ~6 lines; params ≤ 0 user params (xUnit facts).
  - No LPL, data clumps, speculative generality.
  - `project.Element(...)?.Element(...)` is 2-level chain (not 3+ message chain).
  - Duplicate shape of two csproj asserts appears twice only (HIGH needs >2).
  - Magic strings for XML local names are test assertions; attribute names already const where reused.
  - No domain/data layer code; presentation config guarded by tests only.
- **Prior [****]** (root `.slnx` session `cdd111bdd500450b8199084a4d1cecb8`) replaced — out of this scope.

## TODO markers

_None added._ No smell locations warrant `//TODO:` in changed files.

## Named refactoring steps (this scope)

_None._ No behavior-preserving structural steps required.

## XP simplicity (this delta)

- Intent clear (unpackaged direct exe launch + self-contained WASDK).
- No product duplicate code introduced.
- Fewest artifacts (manifest fix, two props, one test class).
- Mirrors sibling `*Tests : ConformanceTests` pattern.

## Verdict

**No smells found in review scope.** Expectation met (config/XML/test-only change set).

## Loop progress notes

| Iteration theme | Result |
|-----------------|--------|
| Member signatures (LPL, data clumps, speculative generality) | None — facts take no params; no unused members |
| Statics | None — only `private const` strings |
| Class state / Feature Envy / TDA / chains | None — short asserts; chains depth ≤2 |
| Layers (Domain/Data/Presentation) | N/A product code; config + tests only |
| Duplicates | None >2 occurrences |
| Complexity / cohesion / temporal coupling | CC=1 per fact; single responsibility |
| Primitive obsession / magic | Test XML literals only; no product magic |
| Naming / framework checklist | Names match sibling convention |
| Remaining checklist items | N/A / none |

*End Windows unpackaged-launch smells audit — session `bd5e93164d0b461081f9e1518713f2e1`.*
