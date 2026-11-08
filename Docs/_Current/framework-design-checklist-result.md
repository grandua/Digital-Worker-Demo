# Framework Design Checklist Result: encapsulate

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
