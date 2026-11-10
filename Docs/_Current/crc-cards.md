# CRC Cards: `encapsulate`

Status: implemented (Encapsulate/, Encapsulate.UnitTests/). Source: `prompt.md` section 5a as amended by section 7a, and `navigation-map.md`. The user confirmed approval of the plan.

## As-Is CRC Cards

None: this was a greenfield CLI. No class implemented the smell scan before this change.

## To-Be CRC Cards (as built)

All classes, types, and members in this table are new. There are no to-change classes or members in this greenfield implementation.

| Class (layer) | Responsibilities | Collaborators | Status |
|---|---|---|---|
| `Program` (Presentation) | `Main` (entry point, plan R12) calls `Run(args, output, error)`: resolves the 0/1 path argument (CWD default), composes objects with `new`, writes output, returns exit code 0 or 2. | `SourceTree`, `Codebase`, `Report` | Implemented. Tests: `ProgramTests` (7 facts). |
| `Report` (Presentation) | Knows findings; `Render()` collapses duplicates (R4), orders them, renders one header per file plus smell lines. | `SmellFinding` | Implemented. Tests: `ReportTests` (5). |
| `SourceTree` (Data) | Knows root; `Exists`; `ReadSources()` enumerates `*.cs` recursively (R9), reads text, forms header paths (R10). | `SourceFile` | Implemented. Tests: `SourceTreeTests` (7). |
| `SourceFile` (Domain) | Immutable record: header path and source text (R14). | none | Implemented (record). |
| `Codebase` (Domain) | Knows sources; builds one compilation with C# 14 parse options, trusted references and the SDK implicit global usings (R11); `FindSmells()` runs every analysis unit. | `SourceFile`, `CodeUnit`, `SmellFinding` | Implemented. Tests: `AnalysisUnitTests`, `CorpusRegressionTests`. |
| `CodeUnit` (Domain) | Knows one body and its semantic model; `CollectFindings()` walks operations (lambdas included, local functions skipped), groups FE accesses by receiver and collects PO call sites. | `MemberAccess`, `Receiver`, `CallSite`, `SmellFinding` | Implemented. Renamed from the planned `Findings`. |
| `MemberAccess` (Domain) | Wraps one Roslyn field/property operation; `IsCountable` (R7, R3, nameof), `ReceiverKey` (R1, R2), `Line`, `Column` (R15), `IsBefore`. | none (wraps `IOperation`) | Implemented. Tests: rule test classes. |
| `Receiver` (Domain) | Knows key and its accesses (constructor); `HasRepeatedAccess`; `IsEnvied` (count >= 2, excludes `this`/`base`); `CreateFinding(path, kind)` reports at the earliest access. | `MemberAccess`, `SmellFinding` | Implemented. `Record` was not needed: grouping happens in the constructor. |
| `CallSite` (Domain) | Knows separate syntactic arguments (R5, R6); `CollectFindings(path)` groups counted argument accesses by receiver key and reports keys with two or more. | `MemberAccess`, `Receiver`, `SmellFinding` | Implemented. `PassesObjectParts` is not a separate member; the same rule is `Receiver.HasRepeatedAccess`. |
| `SmellFinding` (Domain) | Immutable record: file, 1-based line, column and kind (R13); constants `FeatureEnvy` and `PrimitiveObsession`; value equality for R4. | none | Implemented. |

Totals at the time of this update: 111 tests passing before the corpus addition, 3 corpus regression tests added (114 total, see the final test run in the report).

## Cross-Class Concepts Glossary

- **Receiver Key** (identity key): normalized receiver expression text. Canonical owner `MemberAccess` (Domain); participants `Receiver`, `CallSite` (Domain). No runtime identity or alias analysis.
- **Smell Scan** (use case): source-to-report analysis. Spine: `Program.Main` -> `SourceTree.ReadSources` -> `Codebase.FindSmells` -> `CodeUnit.CollectFindings` -> `Report.Render`. Implemented.
