# Navigation Map: `encapsulate` smell detector (planned, all classes new)

Back-reference: `Docs/_Current/prompt.md` (High-Level Plan for `encapsulate`, §5 Architecture, §5a CRC cards and §7a Review amendments).
Status: PLAN ONLY. Nothing in this map is implemented. Semantic decisions live in the plan (§3, amended by §7a).

## As-Is Flow
None. Greenfield: no CLI, analyzer, file scanning or smell code exists (context.md §7.3, §9).

## To-Be Flow

Entry Point
Program.Main(string[] args)  (Presentation, new)

Calls
Program.Main
-> new SourceTree(string root)                 (Data, new; root = args[0] or Directory.GetCurrentDirectory())
   <- SourceTree  (throws/returns exit 2 when root does not exist)
-> SourceTree.ReadSources()                    (Data, new; no exclusions, hidden entries included, reparse points skipped, see plan §7a R9)
   <- IReadOnlyList<SourceFile>
-> new Codebase(IReadOnlyList<SourceFile>)     (Domain, new; builds one CSharpCompilation, C# 14, TRUSTED_PLATFORM_ASSEMBLIES refs)
   <- Codebase
-> Codebase.FindSmells()                       (Domain, new)
   <- IReadOnlyList<SmellFinding>  (ordinal sort: path, line, column, smell)
      internally, per CodeUnit:
      -> CodeUnit.Findings                     (Domain, new; body is CodeUnit state)
         -> MemberAccess (per property/field reference, IOperation -> MemberAccess)  (Domain, new)
         -> Receiver.Record(MemberAccess)      (Domain, new)   <- void
         -> CallSite.PassesObjectParts          (Domain, new)   <- bool
-> new Report(IReadOnlyList<SmellFinding>)     (Presentation, new)
-> Report.Render()
   <- IReadOnlyList<string> (header per file, then "  Feature Envy (L, C)" / "  Primitive Obsession (L, C)")
-> Console.Out / Console.Error                 (exit 0 on success, 2 on invalid path)

Data table (to-be, method level)

| Call | Parameters | Return |
|---|---|---|
| Program.Main | string[] args | int exit code |
| new SourceTree | string root | SourceTree |
| SourceTree.ReadSources | none | IReadOnlyList<SourceFile> |
| new Codebase | IReadOnlyList<SourceFile> | Codebase |
| Codebase.FindSmells | none | IReadOnlyList<SmellFinding> |
| CodeUnit.Findings | none | IEnumerable<SmellFinding> |
| Receiver.Record | MemberAccess access | void (occurrence count per receiver key) |
| Receiver.IsEnvied | none | bool (occurrences >= 2, excludes this/base) |
| CallSite.PassesObjectParts | none | bool (>= 2 counted arguments with the same receiver key) |
| new Report | IReadOnlyList<SmellFinding> | Report |
| Report.Render | none | IReadOnlyList<string> |

## Classes (all new)

| Class | Layer | State | Behaviour |
|---|---|---|---|
| Program | Presentation | none (composition root) | Main: resolves path, composes objects, writes output |
| Report | Presentation | findings list | Render: groups by file, formats lines |
| SourceTree | Data | root path | ReadSources: unfiltered recursive enumeration (plan §7a R9) and reading (IO only) |
| SourceFile | Domain | header path, text | value holder (Domain per plan §7a R14) |
| Codebase | Domain | SourceFile list, one compilation | FindSmells: runs per-unit analysis |
| CodeUnit | Domain | semantic model, body | Findings: walks operations of one body |
| Receiver | Domain | receiver key (normalized text), occurrence count | Record, IsEnvied (computed) |
| MemberAccess | Domain | receiver key, location | value for one access (no member symbol, plan §7a R17) |
| CallSite | Domain | counted arguments (direct receiver key, location; plan §7a R5) | PassesObjectParts (computed) |
| SmellFinding | Domain | file, line, column, kind | immutable value (identical findings collapse, plan §7a R4) |

## Rules

- Layers: Presentation and Data depend on Domain; Domain never depends on Presentation or Data. Presentation composes with `new`.
- No static members except constants and the required `Program.Main` entry point. No DI container, no singletons, so no registration is needed.
- No interfaces between Domain and Data.
- Roslyn types (`IOperation`, `SemanticModel`) stay inside CodeUnit, MemberAccess and CallSite.
- Derived values are computed (IsEnvied, PassesObjectParts), never populated by a separate step.
- Receiver keys are normalized receiver expression text; no alias or identity analysis. PO includes this/base receivers; FE excludes them.
- No Long Parameter List: every method takes at most one parameter.
- No tuples and no out parameters.

## Testability

- Codebase, CodeUnit, Receiver, MemberAccess, CallSite: tested with in-memory SourceFile text.
- SourceTree: tested with temp directories outside the repository.
- Report: tested with fixed SmellFinding lists.
- Program: tested with argument arrays and temp directories.

## Back-reference from the plan

The plan estimates about 600 LOC (about 300 production, about 300 tests), which is above the 200 LOC threshold, so this map is required and referenced by the high-level plan.
