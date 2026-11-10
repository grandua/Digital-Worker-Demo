# Encapsulate Review

## Finding

- **P2: Event-field initializers are never analyzed** (`Encapsulate/Domain/Codebase.cs:51`). The unit selector accepts variable initializers only when their containing declaration is `FieldDeclarationSyntax`; C# event fields use `EventFieldDeclarationSyntax`. Consequently both rules miss accesses and calls inside valid event initializers. Reproduce with `dotnet Encapsulate/bin/Debug/net10.0/encapsulate.dll Docs/_Current/encapsulate-review-case.cs`: line 3 contains `Make(new Order().X, new Order().Y)` and should report Feature Envy and Primitive Obsession, but the output contains only line 5. Add coverage for event-field initializers alongside the ordinary field initializer cases in `Encapsulate.UnitTests/Domain/AnalysisUnitTests.cs`.

## Verification

- `dotnet restore Encapsulate.slnx` succeeded; `dotnet test Encapsulate.slnx --no-restore` passed 120 tests.
- Scoped InspectCode was rerun after restore with warning severity and no build: `Docs/_Current/encapsulate-review-restored.sarif`. It reports eight private-field naming warnings and one parameter-shadowing warning, with no errors. These are style/maintainability warnings, not demonstrated functional defects.
- Existing `Docs/_Current/issues.md` records a separate open low-risk sealing-convention decision.
- Architecture review: the approved design directly depends on Roslyn in Domain and uses a standalone solution with locally pinned package versions. The shared glossary file is absent; existing CRC cards document both cross-class concepts. These are design/structural tradeoffs, not additional proven behavioral defects.
- No implementation fixes or Git writes were made. Review workflow remains in progress; this document is not a claim of complete workflow coverage.
