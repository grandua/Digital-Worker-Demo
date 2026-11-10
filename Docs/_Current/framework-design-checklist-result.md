# Framework Design Checklist Result: encapsulate

## Fix Round Update (2026-11-10)

Resolution of the audit follow-ups below (see issues.md "Fix Round"):

- [x] Naming field prefix: private fields reverted to the canonical no-underscore checklist naming. The remaining InspectCode `InconsistentNaming` results are the tool's default profile (which prefers `_camelCase`), not a checklist violation; the canonical checklist is authoritative for this project.
- [x] Method verb names: `RootOperations` is a computed property; parameterized operations renamed to verb phrases (`Body`, `TokenTextOf`, `NormalizedTextOf`, `ValuesOf`, `ExpandedElementsOf`).
- [x] Readonly mutable values: visitor accumulator exception documented in code (mutable by design, confined to the collector instance).
- [x] Snapshot collection property: `CallSite.CountedArguments` is a lazy projection.
- [x] Static rule: `IsConditionalBinding` is an instance method.
- [x] Tuple result: `ProgramTests.Run` returns a named record.
- [~] Public validation and standard argument exceptions (S3): unchanged; internal-only usage, documented follow-up.
- [~] Sealing (S2) and string smell kinds (S8): deliberate plan decisions for this leaf console app; no behavior change.
- [~] NullReferenceException contract (S3): unchanged; internal-only usage, documented follow-up.

## Audit Update (Session 6ba9feebd142484cbcad0c400101b445)

Scope: user-named Encapsulate production/test files and solution, current worktree at HEAD 117d01a. Profile `all`; canonical resource read again without modification. Prior review below is historical and has stale symbol/conformance statements.

Selected sections: Naming Guidelines; Type Design Guidelines; Member Design Guidelines; Designing for Extensibility; Design Guidelines for Exceptions; Usage Guidelines; Common Design Patterns.

- [~] Naming field prefix: Codebase._parseOptions/_sourceTrees/_compilation, Receiver._occurrences, CodeUnit.AccessCollector._accesses/_callSites, ProgramTests._root, SourceTreeTests._root violate no-underscore rule (S1).
- [~] Method verb names: MemberAccess.TokenText/NormalizedText, CodeUnit.RootOperations/BodyOf, CallSite.ValueOperations/ExpandedElements, SourceTree.HeaderPath violate verb-phrase names (S4).
- [~] Enum rule: SmellFinding.Kind uses arbitrary string plus constants for closed smell set; approved design exception needs reconciliation (S8).
- [~] Public validation and standard argument exceptions: Codebase constructor, SourceTree constructor, CodeUnit constructor, MemberAccess constructor, CallSite constructor, Report constructor, Receiver constructor/CreateFinding do not establish their required argument invariants (S3).
- [~] Sealing: Codebase, CodeUnit, MemberAccess, Receiver, CallSite, SourceFile, SmellFinding, SourceTree, Report, Program and the in-scope sealed test fixtures have no documented concrete sealing reason (S2).
- [~] Readonly mutable values: CodeUnit.AccessCollector._accesses/_callSites are mutable List values assigned to readonly fields; deliberate visitor state still needs an exception (S7).
- [~] Snapshot collection property: CallSite.CountedArguments materializes ToList per access rather than live/lazy collection (S6).
- [?] NullReferenceException contract: unsupported null constructor inputs can produce later null dereferences; decide internal-only API versus validated public contract (S3).
- [x] Cohesive type responsibilities, file-scoped namespaces, immutable record values, affirmative Boolean properties, get-only derived properties, private nested visitor, no custom 1:1 interfaces, no public nested grouping types, no out/ref APIs, collections returned non-null, no public List/Dictionary/weak collection API, invariant numeric formatting, typed invariant exception and scoped recovery catch.
- [N/A] Abstract type/interface/custom struct/flag-enum design: none introduced by the feature.
- [N/A] Overloads/indexers/extension-method APIs: none in feature implementation; snippet declarations are analyzer inputs.
- [N/A] Disposable-owner pattern: fixtures own temporary directory paths, not native/disposable fields; using declarations dispose actual writers/streams.
- [N/A] Error-code library prohibition at CLI: exit 0/2 is explicitly approved process behavior.
- [N/A] Virtual/protected members and static constructors: no relevant custom members.

Summary of recorded review outcomes: 7 violated guideline groups, 1 uncertain group, 15 passing checks summarized, 5 N/A groups. Findings, exact follow-ups and TODO locations are tracked in issues.md; none fixed in audit mode. Tool naming defaults conflict with the canonical checklist, so a clean InspectCode report is not standards approval. Public Run now takes IReadOnlyList (not array); Program.Main's array is the required CLI entry signature. Historical claims of no tuple/static helper/nested type below no longer describe the snapshot.

## Historical Review

Profile: `all` (every section relevant to in-scope code). Scope: all types under `Encapsulate/` (production) and `Encapsulate.UnitTests/` (tests), reviewed as full files. Source: `docs://framework-design-guidelines` (read-only, not modified).

## Sections in scope
- Naming Guidelines: General Naming Conventions; Names of Classes, Structs, and Interfaces; Names of Type Members; Names of Fields
- Type Design Guidelines: Type Design; Choosing Between Class and Struct; Enum (none used); Nested Types (none)
- Member Design Guidelines: Member Overloading (none); Property Design; Constructor Design; Field Design; Extension Methods (none); Parameter Design
- Designing for Extensibility: Unsealed Classes; Sealing; Abstractions (none)
- Design Guidelines for Exceptions: Exception Throwing; Using Standard Exception Types; Exceptions and Performance
- Usage Guidelines: Arrays; Collections
- Common Design Patterns: Dispose Pattern (test fixtures only)

## Findings (non-passing)
- [~] `Encapsulate/Domain/*.cs`, `Encapsulate/Data/SourceTree.cs`, `Encapsulate/Presentation/Report.cs`, `Encapsulate/Program.cs`: every production class is `sealed`. Guideline: DO NOT seal classes without a good reason. Repo convention, applied deliberately; no extensibility is planned. Not changed. Next action: decide project-wide.
- [~] `Encapsulate.UnitTests/Integration/CorpusRegressionTests.cs:8` (and `ProgramTests`, `SourceTreeTests`): sealed test fixtures, same convention as above.
- [~] Public constructors with no argument validation: `SourceTree(string root)`, `Codebase(IReadOnlyList<SourceFile> sources)`, `Report(IReadOnlyList<SmellFinding> findings)`, `CallSite(IEnumerable<IArgumentOperation> arguments)`, `MemberAccess(IOperation operation)`, `CodeUnit(SemanticModel model, SyntaxNode scope)`. Guideline: DO validate arguments passed to public members and throw `ArgumentNullException`. Risk is low (internal composition root only). Not changed. Next action: add guard clauses if these types are exposed beyond the console app.
- [~] `Encapsulate/Data/SourceTree.cs` `TryRead`: `catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)`. Guideline: AVOID catching `System.Exception`. Behaviour is correct (filter limits it), but two typed catch clauses would match the guideline exactly. Not changed.
- [~] `Encapsulate/Program.cs` `Run(string[] args, ...)`: public method takes an array. Guideline: DO prefer collections over arrays in public APIs. Low risk: it is the console entry-point signature.
- [~] Private helper methods named with nouns (`TokenText`, `NormalizedText`, `RootOperations`, `ExpandedElements`, `ValueOperations`, `InitializerExpression`, `BodyOf`, `HeaderPath`). Guideline: method names are verbs or verb phrases. Private scope, low risk. Not changed.
- [~] ReSharper hint "private fields should start with `_`" (`Codebase.parseOptions`, `CodeUnit.accesses`, `Receiver.occurrences`, `SourceTree`/test `root`). Conflict: guideline says DO NOT prefix field names with an underscore. The camelCase fields in this codebase follow the guideline and match the Calculator domain code, so the ReSharper hint is the rule that conflicts. Not changed; recorded as a tool-rule conflict.

## Passing items (summary)
- [x] Names: no underscore or prefix on production fields; no abbreviations in public identifiers; boolean properties are affirmative (`IsCountable`, `IsEnvied`, `HasRepeatedAccess`, `Exists`); properties are nouns (`ReceiverKey`, `Line`, `Column`, `Start`); collection properties are plural (`CountedArguments`); no `Enum`/`Flags` suffixes (no enums).
- [x] Type design: each class has one cohesive responsibility (see navigation map); value types are `record` (immutable, value equality: `SmellFinding`, `SourceFile`); no mutable struct; no struct used; no marker or 1-to-1 interfaces; no public nested types; no static bucket classes (only `Program.Main`, the documented entry point, plus `const` display names).
- [x] Member design: properties are get-only; no set-only properties; no indexers; no `ref`/`out` parameters (no tuples); parameters at most 3 per method; no optional-argument overloads.
- [x] Exceptions: no `System.Exception` thrown; `InvalidOperationException` for the internal invariant in `MemberAccess.EnclosingConditionalAccess`; no exception filters that throw; no exceptions from `finally`; no `NullReferenceException` as the contract.
- [x] CLI exit codes (`Program.Run` returns 0/2) are a required process contract from the approved requirements, not an error-code API inside the library. [N/A for the library rule.]
- [x] Collections: public returns are `IReadOnlyList<T>` (not `List<T>`), never `null`; no snapshot-property violations (`CountedArguments` is computed per access).
- [x] Dispose pattern: `ProgramTests`/`SourceTreeTests` hold only strings and temp directories (no disposable members); the `IDisposable` on these fixtures removes a temp folder only. [N/A for the disposable-holder rule.]

## Summary counts
- Passed (summarized): 19
- Failed: 0
- Uncertain: 0
- Violated (recorded, not fixed): 7 (`[~]`: sealing x2, argument validation, catch scope, array parameter, noun-named private methods, underscore-hint conflict)
- N/A: 6 (no enums, no interfaces, no extension methods, no nested types, no overloads, no disposable members)
