# CRC Cards: `encapsulate`

Status: implemented (Encapsulate/, Encapsulate.UnitTests/). Source: `prompt.md` section 5a as amended by section 7a, and `navigation-map.md`. The user confirmed approval of the plan.

## As-Is CRC Cards

None: this was a greenfield CLI. No class implemented the smell scan before this change.

## To-Be CRC Cards (as built)

All classes, types, and members in this table are new. Updated after the /refactor pass (plan P0–P9): layers per P0-a (analysis classes in Data), new types SmellKind (N-2), ReceiverKeyExtensions (N-6, replacing the planned MemberAccess-hosted predicate — Domain must not reference Data), TempDirectoryTests (N-5).
| Class (layer) | Responsibilities | Collaborators | Status |
|---|---|---|---|
| `Program` (Presentation) | `Main` (entry point, plan R12) calls `Run(args, output, error)`: checks the usage rule (at most one argument), composes objects with `new`, writes output, returns exit code 0 or 2; root resolution lives on `SourceTree.FromArguments` (P7-a). | `SourceTree`, `Codebase`, `Report` | Implemented. Tests: `ProgramTests` (7 facts). |
| `Report` (Presentation) | Knows findings (already de-duplicated and ordered by `Codebase.FindSmells`/`SmellFinding`); `Render()` groups one header per file plus smell lines; `DisplayName` owns the `SmellKind` display text. | `SmellFinding` | Implemented. Tests: `ReportTests` (5). |
| `SourceTree` (Data) | Knows root; `Exists`; `ReadSources()` enumerates `*.cs` recursively (R9), reads text, forms header paths (R10). | `SourceFile` | Implemented. Tests: `SourceTreeTests` (7). |
| `SourceFile` (Domain) | Immutable record: header path and source text (R14). | none | Implemented (record). |
| `Codebase` (Data) | Knows sources; builds one compilation with C# 14 parse options, trusted references and the SDK implicit global usings (R11); `FindSmells()` selects analysis units via `CodeUnit.For` and returns distinct, ordered findings. | `SourceFile`, `CodeUnit`, `SmellFinding` | Implemented. Tests: `AnalysisUnitTests`, `CorpusRegressionTests`. |
| `CodeUnit` (Data) | Knows one analysis unit — the unit-scope rule lives here (`For(model, node)` factory + `IsAnalysisUnit`, incl. field/event initializer scopes); `CollectFindings()` walks operations (lambdas included, local functions skipped), groups FE accesses by receiver and collects PO call sites. | `MemberAccess`, `Receiver`, `CallSite`, `SmellFinding` | Implemented. Renamed from the planned `Findings`. |
| `MemberAccess` hierarchy (Data) | `MemberAccess` is now an abstract base (Data layer after P0-a); static factory `For(IOperation)` selects the receiver kind — the only read of the conditional-access discriminator. Subclasses: `ConditionalMemberAccess` (receiver is `o?.…`), `ContainingInstanceMemberAccess` (`this`/`base`/implicit receiver), `ChainedMemberAccess` (receiver is another member access; key is the receiver's key), `PlainMemberAccess` (any other receiver). | `MemberAccess` (base), `ReceiverAccess` | Implemented (refactoring plan N-1). Tests: rule test classes. |
| `ReceiverAccess` (Domain, refactoring P0-a) | Plain access-fact record: zero-based line/column of the receiver start. Decouples Domain `Receiver` from Roslyn; the 1-based report conversion lives in `SmellFinding.At`. | `Receiver`, `SmellFinding` | Implemented (refactoring plan N-4, adapted to a zero-based int pair so Domain stays free of Microsoft.CodeAnalysis). |
| `Receiver` (Domain) | Knows its receiver key and accesses (constructor); `RepetitionThreshold`/`ThisKey`/`BaseKey` constants; static `EnviedFrom(...)` factory owns the envy selection; `IsEnvied` (>= threshold, excludes containing instance via `ReceiverKeyExtensions`); `CreateFinding(path, kind)` reports at the earliest access. | `MemberAccess`, `SmellFinding` | Implemented. `Record` was not needed: grouping happens in the constructor. |
| `CallSite` (Data) | Knows separate syntactic arguments; `CollectFindings(path)` groups counted argument accesses by receiver key and reports keys with two or more. | `MemberAccess`, `Receiver`, `SmellFinding` | Implemented. `PassesObjectParts` is not a separate member; the same rule is `Receiver.HasRepeatedAccess`. |
| `SmellFinding` (Domain) | Immutable record: file, 1-based line, column and `SmellKind`; `At(...)` owns the zero-to-one-based conversion; value equality and the natural report order (IComparable). | `SmellKind` | Implemented. |
| `SmellKind` (Domain, N-2) | Closed enum of reported smells: `FeatureEnvy`, `PrimitiveObsession`. | `SmellFinding` | Implemented. |
| `ReceiverKeyExtensions` (Domain, N-6) | Extension `IsContainingInstance(this string)` on the receiver key; owns the containing-instance vocabulary with `Receiver`'s key constants. | `Receiver` | Implemented. |
| `TempDirectoryTests` (UnitTests/Support, N-5) | Abstract test fixture: fresh temp directory, disposal, `WriteFile` helper. | `ProgramTests`, `SourceTreeTests` | Implemented. |

Totals at the time of this update: 111 tests passing before the corpus addition, 3 corpus regression tests added (114 total, see the final test run in the report).

## Cross-Class Concepts Glossary

- **Receiver Key** (identity key): normalized receiver expression text. Canonical owner `MemberAccess` (Data produces it); vocabulary and containing-instance predicate in Domain (`Receiver` constants, `ReceiverKeyExtensions`); participants `Receiver`, `CallSite`. No runtime identity or alias analysis.
- **Smell Scan** (use case): source-to-report analysis. Spine: `Program.Main` -> `SourceTree.ReadSources` -> `Codebase.FindSmells` -> `CodeUnit.CollectFindings` -> `Report.Render`. Implemented.
