# Refactoring Plan: encapsulate (code-smell audit)

Workflow: /find-smells-and-plan-refactoring (Audit Mode: TODO comments only, no code/behavior changes)
Scope: all C# and project/solution files under Encapsulate/, Encapsulate.UnitTests/, and Encapsulate.slnx.
Baseline: 125/125 tests pass (`dotnet test Encapsulate.slnx`).

Severity thresholds applied: CRITICAL (architecture layer violations, domain external dependencies, circular dependencies); HIGH (long methods >20 lines, >3 params, Feature Envy, non-constant static members, duplicate code >2 occurrences, repeated discriminator / duplicate switch in >2 methods); MEDIUM (long methods 11-20 lines, method naming, message chains 3+ dots; other unlisted smells assigned MEDIUM by judgement and marked as such); LOW (comment clarity, style-only items).

Evidence sources: ReSharper InspectCode scoped run (`Docs/_Current/inspect.sarif`, candidate evidence only) plus manual source review and grep verification of every item below.

## 1. Inspection pre-pass (T0) triage

| ReSharper item | Location | Verdict |
|---|---|---|
| ConvertClosureToMethodGroup | Encapsulate/Data/SourceTree.cs:17 | Verified. LOW (readability). TODO added. |
| ConvertClosureToMethodGroup | Encapsulate/Domain/Codebase.cs:29 | Verified. LOW (readability). TODO added. |
| MemberCanBePrivate | Encapsulate/Domain/Receiver.cs:8, :10 | Verified. Covered by M-12 (speculative generality). |
| MemberCanBePrivate | Encapsulate.UnitTests/Support/Analysis.cs:77 | Verified. LOW. TODO added. |
| InconsistentNaming (underscore-prefixed private fields) | Codebase.cs, CodeUnit.cs, Receiver.cs, tests | Rejected as a smell. Project-wide camelCase private field convention is applied consistently; a naming-convention choice, not a code smell. |
| MergeIntoPattern | Encapsulate/Domain/CodeUnit.cs:42 | Not verified as a listed smell (style). Not reported. |
| MergeIntoPattern | Encapsulate.UnitTests/Domain/AnalysisUnitTests.cs:120 | Not verified (no nested pattern to merge). Not reported. |
| RedundantAlwaysMatchSubpattern | Encapsulate/Domain/CallSite.cs:59 | Unverified (needs Roslyn nullability annotation check on IConditionalAccessOperation.WhenNotNull). Not reported. Do not change without that check. |
| ZzProbeTests.cs (0 bytes, not inspected by ReSharper) | Encapsulate.UnitTests/ZzProbeTests.cs | Found by manual review. Reported as M-14. |

Out-of-scope observation (not reported): a stray file `_wtest` exists at the repository root. It is outside the review scope.

## 2. Findings

Line numbers are 1-based and refer to the current source. "LOC estimate" counts the lines the fix would touch; "not estimated" where the fix design is not yet known.

### CRITICAL (block refactoring)

| ID | Smell | File:line | Snippet | LOC estimate |
|---|---|---|---|---|
| C-1 | Architecture: Domain layer has external dependencies and I/O (Roslyn package; file I/O; environment read) | Encapsulate/Domain/Codebase.cs:55-59; also `using Microsoft.CodeAnalysis*` in CallSite.cs:1-4, CodeUnit.cs:1-3, Codebase.cs:1-3, MemberAccess.cs:1-5 | `((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? string.Empty).Split(Path.PathSeparator, ...).Select(path => (MetadataReference)MetadataReference.CreateFromFile(path))` | not estimated (needs a decision: a Data-layer reference provider or an accepted-domain exception) |
| C-2 | Architecture: Presentation layer owns domain logic (dedupe and ordering of findings) | Encapsulate/Presentation/Report.cs:8-11 | `findings.Distinct().OrderBy(f => f.FilePath, ...).ThenBy(f => f.Line).ThenBy(f => f.Column).ThenBy(f => f.Kind, ...)` | ~10 (move dedupe/order into Domain, e.g. Codebase.FindSmells returns ordered distinct findings; Report keeps grouping and formatting) |

### HIGH (refactor ASAP)

| ID | Smell | File:line | Snippet | LOC estimate |
|---|---|---|---|---|
| H-1 | Feature Envy: 5 external calls vs 2 internal | Encapsulate/Domain/CodeUnit.cs:50-58 | `.Where(access => access.IsCountable).GroupBy(access => access.ReceiverKey)...Where(receiver => receiver.IsEnvied).Select(receiver => receiver.CreateFinding(...))` | ~12 |
| H-2 | Feature Envy: 3 external calls (IsBefore, Line, Column) vs 1 internal | Encapsulate/Domain/Receiver.cs:16-20 | `MemberAccess first = occurrences.Aggregate((earliest, next) => next.IsBefore(earliest) ? next : earliest); return new SmellFinding(path, first.Line, first.Column, kind);` | ~6 |
| H-3 | Feature Envy (external Start/Aggregate, 0 internal) plus duplicate "earliest position to finding" algorithm with H-2 | Encapsulate/Domain/CallSite.cs:27-31 | `Counted first = group.Aggregate((earliest, next) => next.Start.CompareTo(earliest.Start) < 0 ? next : earliest); return new SmellFinding(path, first.Start.Line + 1, first.Start.Character + 1, ...);` | ~10 (single earliest-position helper shared with Receiver) |
| H-4 | Feature Envy: Render and FormatLine read 8 SmellFinding fields (FilePath x2, Line x3, Column x2, Kind x2) | Encapsulate/Presentation/Report.cs:8-19 | `.ThenBy(finding => finding.Line)` ... `FormattableString.Invariant($"  {finding.Kind} ({finding.Line}, {finding.Column})")` | ~8 (SmellFinding owns its sort key and its display text) |
| H-5 | Repeated discriminator / duplicate switch: the conditional-access instance type test is read in 4 methods (see table D-1) | Encapsulate/Domain/MemberAccess.cs:46-51 (ReceiverStart), :53-59 (ReceiverKeyOf), :61-67 (ExpressionKey), :69-72 (ExpressionStart) | `IConditionalAccessInstanceOperation => EnclosingConditionalAccess().Operation.Syntax` (and 3 other reads) | not estimated (design: normalise the receiver once into a single model, then read the discriminator in one place) |
| H-6 | Duplicate algorithm (4 occurrences): walk parents until a target type is found | Encapsulate/Domain/MemberAccess.cs:24-31 (IsInsideNameOf), :85-93 (ConditionalOf); Encapsulate/Domain/CallSite.cs:40-41 (IsWrittenArgument), :73-82 (Origin) | `for (IOperation? current = Member; current is not null; current = current.Parent)`, `while (current is not IConditionalAccessOperation)`, `while (current.Parent is ParenthesizedExpressionSyntax outermost)`, `syntax.Ancestors().SkipWhile(...).FirstOrDefault()` | ~12 (one extension method per tree type: IOperation and SyntaxNode) |
| H-7 | Duplicate code (4 occurrences): skipping parentheses (discriminator table D-2) | Encapsulate/Domain/MemberAccess.cs:97 (NormalizedTextOf); Encapsulate/Domain/CallSite.cs:58 (Unwrap, IParenthesizedOperation), :78 (Origin), :41 (IsWrittenArgument) | `ParenthesizedExpressionSyntax parenthesized => NormalizedTextOf(parenthesized.Expression)`; `IParenthesizedOperation parenthesized => Unwrap(parenthesized.Operand)` | ~8 |
| H-8 | Duplicate code (4 test sites): `Analysis.OfKind(...PrimitiveObsession)` and `Assert.Single/Empty(Analysis.OfKind(findings, FeatureEnvy))` repeated | Encapsulate.UnitTests/Domain/PrimitiveObsessionRuleTests.cs:66, :76, :86, :96; AnalysisUnitTests.cs:23, :49, :57, :65, :85, :95, :105; FeatureEnvyRuleTests.cs:87, :117, :159 | `var findings = Analysis.OfKind(Analysis.Scan(source), SmellFinding.PrimitiveObsession);` | ~8 (Analysis helper that returns the count or the single finding by kind) |
| H-9 | Duplicate code (3 occurrences): `new SourceFile("Prelude.cs", Analysis.Prelude)` plus Codebase construction | Encapsulate.UnitTests/Domain/AnalysisUnitTests.cs:111-116, :126-131; Encapsulate.UnitTests/Support/Analysis.cs:78 | `new SourceFile("Prelude.cs", Analysis.Prelude), new SourceFile(Analysis.TargetPath, ...)` | ~6 |
| H-10 | Static members (non-constant): static readonly computed state (environment-derived) and static helper methods in tests | Encapsulate.UnitTests/Integration/CorpusRegressionTests.cs:13 (`RepositoryRoot`), :15 (`ScanDirectory`), :46 (`FindRepositoryRoot`) | `private static readonly string RepositoryRoot = FindRepositoryRoot();` | ~6 (make the fixture instance-based, or compute the root per test) |
| H-11 | Static members (non-constant): static class with six static methods used as test helper utility | Encapsulate.UnitTests/Support/Analysis.cs:6 (class), :48 (Method), :59 (Scan), :62 (ScanMethod), :65 (Locate), :74 (OfKind), :77 (Run) | `internal static class Analysis { public static string Method(string body) => ... }` | not estimated (design: instance fixture or extension methods on IReadOnlyList<SmellFinding>; rule guideline prefers extension methods) |
| H-12 | Layer mixing in composition root: branching (usage error vs scan vs path-not-found) in the same class that performs console and directory I/O | Encapsulate/Program.cs:15-24 | `string root = args.Count == 0 ? Directory.GetCurrentDirectory() : args[0]; ... return tree.Exists ? Scan(tree, output) : Reject(error, ...)` | not estimated |

### MEDIUM (refactor when convenient)

| ID | Smell | File:line | Snippet | LOC estimate |
|---|---|---|---|---|
| M-1 | Cyclomatic complexity > 3 (switch with 6 arms, CC 7) | Encapsulate/Domain/Codebase.cs:44-53 | `IsUnitScope(SyntaxNode node) => node switch { ... 6 arms }` | ~8 |
| M-2 | Cyclomatic complexity > 3 (switch with 4 arms plus `??` chain, CC 5) | Encapsulate/Domain/CodeUnit.cs:33-39 | `InitializerExpression(SyntaxNode node) => node switch { PropertyDeclarationSyntax ... }` | ~6 |
| M-3 | Cyclomatic complexity > 3 (switch with 4 arms, CC 4) | Encapsulate/Domain/CallSite.cs:55-61 | `Unwrap(IOperation value) => value switch { ... }` | ~4 |
| M-4 | Cyclomatic complexity > 3 (switch with 3 arms, nested pattern, CC 4) | Encapsulate/Domain/MemberAccess.cs:15-20 | `IsCountable => operation switch { ... }` | ~6 |
| M-5 | Cyclomatic complexity > 3 (switch with 4 arms, CC 4) | Encapsulate/Domain/MemberAccess.cs:53-59 | `ReceiverKeyOf(IMemberReferenceOperation member) => member.Instance switch { ... }` | ~6 |
| M-6 | Cyclomatic complexity > 3 (switch with 4 arms, guard arm, CC 4) | Encapsulate/Domain/MemberAccess.cs:61-67 | `ExpressionKey(IMemberReferenceOperation member) => member switch { ... }` | ~6 |
| M-7 | Cyclomatic complexity > 3 (switch with 5 arms, CC 5) | Encapsulate/Domain/MemberAccess.cs:95-102 | `NormalizedTextOf(SyntaxNode node) => node switch { ... }` | ~12 (combined with H-7) |
| M-8 | Message chain (3 dots): `GetLocation().GetLineSpan().StartLinePosition` (2 occurrences) | Encapsulate/Domain/MemberAccess.cs:44; Encapsulate/Domain/CallSite.cs:71 | `ReceiverStart.GetLocation().GetLineSpan().StartLinePosition` / `Origin.GetLocation().GetLineSpan().StartLinePosition` | ~6 (one shared start-position helper) |
| M-9 | Primitive obsession / magic strings: "this" and "base" compared across two classes | Encapsulate/Domain/MemberAccess.cs:81; Encapsulate/Domain/Receiver.cs:14 | `self.Syntax is BaseExpressionSyntax ? "base" : "this"` and `Key is "this" or "base"` | ~8 (a single containing-instance type or constants) |
| M-10 | Magic number duplicated for one rule ("repeated at least twice"), 2 occurrences | Encapsulate/Domain/Receiver.cs:10; Encapsulate/Domain/CallSite.cs:17 | `occurrences.Count >= 2` / `.Where(group => group.Count() >= 2)` | ~4 |
| M-11 | Primitive obsession: finding kind is a string; the closed set is not an enum. No behavior branches on it in production (see D-4) | Encapsulate/Domain/SmellFinding.cs:4-7 | `public sealed record SmellFinding(string FilePath, int Line, int Column, string Kind)` | ~10 (SmellKind enum with display mapping) |
| M-12 | Speculative generality: members public but used only inside their own class | Encapsulate/Domain/Receiver.cs:8 (`Key`); Encapsulate/Domain/MemberAccess.cs:83 (`EnclosingConditionalAccess`, one-line alias used once) | `public string Key { get; } = key;` / `private IConditionalAccessOperation EnclosingConditionalAccess() => ConditionalOf(Member);` | ~3 |
| M-13 | Method naming: `Body` does not say it unwraps a local function; `Finding` does not say it builds a finding; mixed verbs for one concept (Collect/Report/Create/Find) | Encapsulate/Domain/CodeUnit.cs:42 (`Body`), :50 (`ReportFindings`); Encapsulate/Domain/CallSite.cs:27 (`Finding`) | `private IOperation Body(IOperation operation) =>` | ~4 |
| M-14 | Speculative generality / dead file: empty test file | Encapsulate.UnitTests/ZzProbeTests.cs (0 bytes) | (empty file) | ~1 (delete file) |
| M-15 | Duplicate test fixture code (2 occurrences): temp root creation, IDisposable cleanup and write helper | Encapsulate.UnitTests/Integration/ProgramTests.cs:9-21; Encapsulate.UnitTests/Integration/SourceTreeTests.cs:7-18 | `private readonly string root = Path.Combine(Path.GetTempPath(), "encapsulate-program-tests", Guid...)` / `"encapsulate-tests"` | ~12 (shared temp-directory fixture) |

### LOW (nice to have)

| ID | Smell | File:line | Snippet | LOC estimate |
|---|---|---|---|---|
| L-1 | Comment clarity: XML doc references to an unexplained plan rule numbering (R1-R15, plan section 6) | Encapsulate/Domain/CallSite.cs:10-11 (also CodeUnit.cs:8 n/a, Codebase.cs:8, MemberAccess.cs:11, SourceTree.cs:42) | `/// Each argument contributes its outermost member access (plan R5); ...` | ~4 |
| L-2 | Readability: method group conversion suggested by inspection (verified) | Encapsulate/Data/SourceTree.cs:17; Encapsulate/Domain/Codebase.cs:29 | `.Select(path => TryRead(path))` / `.Select(source => ParseSource(source))` | ~1 each |
| L-3 | Member visibility: `Run` is public on an internal test helper, used only by `Scan` | Encapsulate.UnitTests/Support/Analysis.cs:77 | `public static IReadOnlyList<SmellFinding> Run(string targetSource) =>` | ~1 |

## 3. Repeated discriminator table (step 1.l)

Counted by searching the production code (Encapsulate/) and the test code (Encapsulate.UnitTests/), excluding comment lines. A read is one direct test of the discriminator in a method body.

### D-1: Conditional-access instance (IConditionalAccessInstanceOperation), repeated in more than 2 methods

| Site (file, method) | Reads | Outcome chosen at that site |
|---|---|---|
| MemberAccess.cs, ReceiverStart | 1 | Uses the enclosing conditional access syntax as the receiver start |
| MemberAccess.cs, ReceiverKeyOf | 1 | Uses the enclosing conditional access text as the receiver key |
| MemberAccess.cs, ExpressionKey | 1 | Adds `?.` to the receiver key |
| MemberAccess.cs, ExpressionStart | 1 | Uses the enclosing conditional access syntax as the expression start |
| Totals | 4 reads in 4 methods | Repeated (> 2). Finding H-5 |

### D-2: Parenthesis discriminator (ParenthesizedExpressionSyntax and IParenthesizedOperation), repeated in more than 2 methods

| Site (file, method) | Reads | Outcome chosen at that site |
|---|---|---|
| MemberAccess.cs, NormalizedTextOf | 1 | Recurses into the parenthesized expression |
| CallSite.cs, Unwrap (IParenthesizedOperation) | 1 | Recurses into the operand |
| CallSite.cs, Origin | 1 | Climbs to the outermost parenthesis |
| CallSite.cs, IsWrittenArgument | 1 | Skips parentheses before testing for ArgumentSyntax |
| Totals | 4 reads in 4 methods | Repeated (> 2). Finding H-7 |

### D-3: Other discriminators checked (no finding)

| Discriminator | Sites | Verdict |
|---|---|---|
| IPropertyReferenceOperation.Property.IsIndexer | MemberAccess.IsCountable, ExpressionKey (2) | Below threshold (2 methods). Recorded only. |
| InstanceReferenceKind / IInstanceReferenceOperation | MemberAccess.IsCountable, ReceiverKeyOf (2) | Below threshold. Recorded only. |
| IConditionalAccessOperation (type test) | CallSite.Unwrap, MemberAccess.ConditionalOf (2) | Below threshold. Recorded only. |
| ArgumentKind | CallSite.IsSeparateArgument, IsExpandedParams (2) | Below threshold. Recorded only. |
| MemberAccessExpressionSyntax | MemberAccess.NormalizedTextOf, IsConditionalBinding (2) | Below threshold. Recorded only. |
| Declaration kind: PropertyDeclarationSyntax, IndexerDeclarationSyntax, VariableDeclaratorSyntax | Codebase.IsUnitScope, CodeUnit.InitializerExpression (2 each) | Below threshold (same switch structure duplicated across two classes). Recorded only; revisit if a third site appears. |
| CompilationUnitSyntax | Codebase.IsUnitScope, CodeUnit.RootOperations (2) | Below threshold. Recorded only. |

### D-4: Finding kind string (SmellFinding.Kind)

| Site (file, method) | Reads | Outcome chosen |
|---|---|---|
| Encapsulate (production) | 0 | Kind is passed through and sorted; no production method branches on it. Enum suggestion (M-11) is therefore not blocked by step 1.l. |
| Encapsulate.UnitTests/Domain/AnalysisUnitTests.cs, DuplicateTypeNamesInTwoFilesDoNotStopAnalysis | 1 | Assertion filter |
| Encapsulate.UnitTests/Domain/AnalysisUnitTests.cs, TopLevelStatementsInTwoFilesEachFormTheirOwnUnit | 1 | Assertion filter |
| Encapsulate.UnitTests/Support/Analysis.cs, OfKind (helper) | excluded | Test helper, not a call site |

## 4. Checked and clean (no finding)

- Static members (production): only `Program.Main` is static. It is the required .NET entry point and is exempt.
- AddSingleton / DI singletons: none present (no DI container in the project).
- Long methods > 20 lines or 11-20 lines: none (longest production body is about 9 lines).
- Long parameter lists (> 3 params in methods; constructors and records excluded): none in production. `Util.Four` (4 params) exists only inside the Prelude string literal in Analysis.cs:36, which is test input, not code.
- Circular dependencies: none. Dependency direction is Program -> Data/Presentation -> Domain; SourceTree depends on Domain.SourceFile; Domain has no reference back to Data or Presentation.
- Existing TODO in scope: Encapsulate.UnitTests/Integration/CorpusRegressionTests.cs:7 (`//TODO: Framework Design -- Sealing: ...`). It has no "Ignore" instruction, so it stays active and no duplicate was added for it.
- Same-shape classes or identical constructor duplication: none found beyond H-3 (the same SmellFinding construction in CallSite and Receiver).
- ImplicitGlobalUsings (Codebase.cs:13-21) duplicates the SDK implicit usings list on purpose (plan R11, documented). Accepted; not reported.
- Language version duplicated in config and code (`<LangVersion>14</LangVersion>` in Encapsulate/Encapsulate.csproj:10 and `new(LanguageVersion.CSharp14)` in Codebase.cs:23). Accepted as a minor configuration duplicate; not reported with a TODO (csproj); noted here for the record.

## 5. Refactoring order (suggested)

1. C-1 and C-2 (CRITICAL): decide the Domain/Roslyn boundary and move ordering/dedupe out of Presentation. Blocks further refactoring.
2. H-5 and H-6 and H-7 (repeated discriminator and duplicate parent-walk / parenthesis logic): introduce one receiver model and shared extension helpers.
3. H-1, H-2, H-3, H-4 (Feature Envy): move "earliest finding" and "sort/format" onto the owning types.
4. H-8, H-9, H-11 (test duplication and test static helpers) and H-10 (static state in corpus tests).
5. H-12 (composition root branching).
6. MEDIUM and LOW items as convenient.

## 6. TODO index (comments added to code in this audit)

Each TODO uses the form `//TODO: <Category> -- <SEVERITY>: <description>. LOC estimate: <n> | not estimated.`
Comments only; no code, test or behavior changed. Verification: `dotnet test Encapsulate.slnx` = 125/125 passed after the change.

| File | TODO count | Finding IDs |
|---|---|---|
| Encapsulate/Domain/Codebase.cs | 4 | C-1 (x2), M-1, L (ParseSource method group) |
| Encapsulate/Domain/CallSite.cs | 11 | C-1, L-1, M-10, H-3, M-13, H-6, H-7, M-3, M-8 |
| Encapsulate/Domain/CodeUnit.cs | 5 | C-1, M-2, M-13 (Body), H-1, M-13 (ReportFindings) |
| Encapsulate/Domain/MemberAccess.cs | 15 | C-1, M-4, H-6 (x3), M-8, H-5 (x4 sites), M-5, M-6, M-9, M-12, H-7, M-7 |
| Encapsulate/Domain/Receiver.cs | 4 | M-12, M-10, M-9, H-2 |
| Encapsulate/Domain/SmellFinding.cs | 1 | M-11 |
| Encapsulate/Presentation/Report.cs | 2 | C-2, H-4 |
| Encapsulate/Program.cs | 1 | H-12 |
| Encapsulate/Data/SourceTree.cs | 1 | L (TryRead method group) |
| Encapsulate.UnitTests/Support/Analysis.cs | 9 | H-11 (class and 6 static members), H-9, L-3 |
| Encapsulate.UnitTests/Integration/CorpusRegressionTests.cs | 4 | H-10 (x3); existing sealing TODO at line 7 kept unchanged |
| Encapsulate.UnitTests/Integration/ProgramTests.cs | 1 | M-15 |
| Encapsulate.UnitTests/Integration/SourceTreeTests.cs | 1 | M-15 |
| Encapsulate.UnitTests/Domain/AnalysisUnitTests.cs | 2 | H-9 (x2) |
| Encapsulate.UnitTests/Domain/PrimitiveObsessionRuleTests.cs | 1 | H-8 |
| Encapsulate.UnitTests/Domain/FeatureEnvyRuleTests.cs | 1 | H-8 |
| Encapsulate.UnitTests/ZzProbeTests.cs | 1 | M-14 |

Not commented (plan-only): C-1 csproj language-version duplication (section 4, accepted); Program.Main static (exempt).

## 7. Refactoring plan (standard named refactorings)

Reference: Rider "Main Set of refactorings" (https://www.jetbrains.com/help/rider/Main_Set_of_refactorings.html) returned HTTP 404 during this run; the names below follow the standard Fowler catalogue that the Rider list uses. Each step names the mechanism, not the result.

Rules applied to every new class: (a) reuse-first: the existing host is tried first (MemberAccess, Receiver, SmellFinding, SourceTree, SourceFile, LinePosition from Roslyn); (b) no banned justification (no mock-driven interfaces, no seal-for-safety, no intermediate factories for testability); (c) every new class must get a CRC card (section 8 below, cross-referenced to crc-cards.md); (d) size is "not estimated" until the class list is fixed.

Global order: P0 (decision) -> P1 (characterization) -> P2 (discriminator) -> P3 (duplicates) -> P4 (feature envy) -> P5 (tests) -> P6 (remaining medium/low). Run `dotnet test Encapsulate.slnx` (125/125) after every step.

### 7.1 Blocking decision (C-1, C-2)

- **P0-a Decide the Domain/Roslyn boundary (C-1).** Options: (A, recommended) Move Class of the Roslyn-facing types (MemberAccess, CallSite, CodeUnit, Codebase) to the Data layer; Domain keeps SmellFinding, Receiver, SourceFile and works on plain access facts (new record `ReceiverAccess(string Key, LinePosition Start, bool IsContainingInstance)`, see N-4). (B) Keep Roslyn in Domain and accept the exception in writing. Decision required from the user before P2 is executed under option A.
- **P0-b Move Method for TrustedReferences (C-1 I/O part).** Move Method `Codebase.TrustedReferences` to the Data layer and pass the references in: `new Codebase(sources, references)`; Program composes them. Removes AppContext and file I/O from Domain. LOC: not estimated (follows P0-a).
- **P0-c Move Method for ordering and de-duplication (C-2).** Move Method `Report.Render`'s `Distinct()` and `OrderBy/ThenBy` into the domain: `SmellFinding` implements `IComparable<SmellFinding>` (Move Method of the sort key), and `Codebase.FindSmells()` returns `findings.Distinct().Order()`. Report keeps only `GroupBy` and line formatting. Tests: move the dedupe and order cases from ReportTests to a domain test class (test churn only).

### 7.2 Characterization (before any change to the discriminator)

- **P1-a Add characterization cases where the existing tests do not pin the behavior.** Required cases (each a single InlineData or fact): `base.Total` as countable receiver key "base" in a Feature Envy count (already partly in FeatureEnvyRuleTests line 18; add an explicit ReceiverKey assertion); conditional binding `o?.B.Total` root start (exists, FeatureEnvyRuleTests line 85/91); `nameof` inside a conditional access (add one); parenthesized conditional `(o?.B).Total` (add one). Tests only; no production change.

### 7.3 Repeated discriminator (H-5, D-1) and parenthesis discriminator (H-7, D-2)

**Before/After for D-1 (IConditionalAccessInstanceOperation, 4 read sites, counted from the code):**

| Site (method) | Reads now | Value chosen now |
|---|---|---|
| MemberAccess.ReceiverStart | 1 | conditional: enclosing conditional syntax; otherwise instance syntax |
| MemberAccess.ReceiverKeyOf | 1 | conditional: normalized conditional text; this/base; member; otherwise instance text |
| MemberAccess.ExpressionKey | 1 | conditional: "key?.name"; otherwise by binding and syntax |
| MemberAccess.ExpressionStart | 1 | conditional or binding: conditional syntax; otherwise member syntax |
| **Total** | **4 in 4 methods** | |

- **P2-a Replace Conditional with Polymorphism (host: MemberAccess, existing).** Base class: `MemberAccess` (existing, reused as the host; no new base class). Subclasses, one per value including the default (N-1): `ConditionalMemberAccess` (instance is IConditionalAccessInstanceOperation), `ContainingInstanceMemberAccess` (instance is the containing-type self reference; overrides only the receiver key, this/base), `ChainedMemberAccess` (instance is IMemberReferenceOperation), `PlainMemberAccess` (default: every other instance kind, including invocation receivers such as `Make().Total`). Each site's outcome becomes a virtual member overridden only where the value differs. Selection point: one static factory `MemberAccess.For(IOperation operation)` (factory methods are exempt from the static rule), called from CodeUnit.AccessCollector.VisitPropertyReference and VisitFieldReference, which currently call `new MemberAccess(operation)`.
- **After state:** 0 reads of IConditionalAccessInstanceOperation at the four sites; the only read is in the factory, which needs only the IOperation instance. Adding a value adds one subclass and changes no site.
- **Order-of-checks dependency (stated):** ExpressionKey checks the indexer discriminator (IPropertyReferenceOperation.Property.IsIndexer) before the instance subclass dispatch. Keep that indexer check in the base (it is a read of a different discriminator, 2 sites, below threshold) and call the virtual member only for non-indexer accesses. Type tests in ReceiverKeyOf are disjoint (IInstanceReferenceOperation, IConditionalAccessInstanceOperation, IMemberReferenceOperation), so their order does not matter.
- **Coverage:** existing tests cover every value: conditional (`o?.Total`, `o?.B.Total` in FeatureEnvyRuleTests and PrimitiveObsessionRuleTests), containing (`this.Total`, `base.Total`, implicit `Tax`), chained (`o.B.Total`), default (`Make().Total`, `(o).Total`). No new characterization needed beyond P1-a.
- **Rejected options:** (1) an enum `ReceiverKind` plus a switch at each site: leaves 4 reads of the enum (one per site) and adds a fifth read in the factory; rejected. (2) A lookup dictionary keyed by the instance Type: leaves 4 keyed reads plus the key construction; rejected. (3) A new strategy interface with a mock per value: banned justification; rejected.
- **New classes:** N-1 (four subclasses of MemberAccess). CRC card needed (section 8). Size: not estimated.

**Before/After for D-2 (parenthesis skipping, 4 sites: NormalizedTextOf, CallSite.Unwrap, CallSite.Origin, CallSite.IsWrittenArgument):**

- **P2-b Replace Algorithm with library call (Roslyn).** Syntax-side sites (NormalizedTextOf, Origin, IsWrittenArgument) use the Roslyn method that walks down parentheses (verify the exact public API in Microsoft.CodeAnalysis.CSharp 5.9.0 before applying; if absent, keep one Extract Method on the owner). The operation-side site (Unwrap, IParenthesizedOperation) stays the only operation-level read.
- **After state:** 0 reads of ParenthesizedExpressionSyntax in the three syntax sites; 1 operation-level read.
- **Coverage:** `Util.Take((o.Total), o.Tax)`, `(long)o.Total`, `(o).Total + o!.Total` in existing tests.
- **Rejected:** a parenthesis-skipping lookup or enum: no behavior selection, so no enum; rejected as not needed.

### 7.4 Duplicated algorithms (H-6, H-2, H-3)

- **P3-a Replace Algorithm with library call (ancestor walk, H-6).** Operation-level walks in MemberAccess.IsInsideNameOf and ConditionalOf become one syntax call (Roslyn `Ancestors().OfType<...>().FirstOrDefault()` on `Member.Syntax`) and are verified by the nameof and conditional-access characterization cases (P1-a). IsWrittenArgument and Origin already use syntax; keep them as they are.
- **P3-b Replace Algorithm with MinBy (earliest position, H-2 and H-3).** Both `Receiver.CreateFinding` and `CallSite.Finding` use an Aggregate that picks the earliest start. Replace each with `MinBy(x => x.Start)` (`LinePosition` already implements comparison and is the existing concept used by both classes; no new position class, rejected: `SourcePosition`). Then Move Method: the finding construction moves to a factory on the existing record `SmellFinding.At(path, LinePosition start, kind)`, which owns the one-based +1 conversion. This removes the duplicated +1 conversions (M-8 partial).
- **P3-c Decompose Conditional (Extract Method, every compound condition).** `CallSite.IsSeparateArgument` (`&&` and `||`): Extract Method `IsExpandedParams`-style named predicate for the `||` operand, so the `&&` line reads as named predicates only. `CallSite.IsWrittenArgument` (`||`): Extract Method `IsWrittenOrAncestorArgument(syntax)`. `MemberAccess.ExpressionStart` (`||`) -> Extract Method `IsConditionalMember(member)` (also shares the discriminator read with P2-a; applied in the polymorphism step). `Receiver.IsEnvied` (`&&`) already reads two named properties; no further change.

### 7.5 Feature Envy (H-1, H-4)

- **P4-a Move Method (grouping into the owner).** `CodeUnit.AccessCollector.ReportFindings` makes 5 external calls. Move the receiver grouping into a factory on `Receiver` (`Receiver.EnviedFrom(IEnumerable<MemberAccess>)`, a factory method, exempt from the static rule) and Extract Method the pipeline into `CollectFindings` so the collector only accumulates. Expected: external calls drop to 2.
- **P4-b Move Method (report ordering, H-4).** Covered by P0-c (sort key moves to SmellFinding). FormatLine's remaining reads (Kind, Line, Column) are presentation of a data record; accepted and recorded as a decision. Render stops being an envious method once P0-c lands.
- **P4-c Rename Method (naming, M-13).** Rename `Body` to `LocalFunctionBodyOrSelf`, `Finding` to `ToFinding` (or inline it into P3-b's factory), and `ReportFindings` to `CollectFindings` (one verb for one concept).

### 7.6 Primitive obsession and magic values (M-9, M-10, M-11)

- **P5-a Replace Magic String with Symbolic Constant + Move Method (M-9).** Introduce constants for "this" and "base" in MemberAccess (owner of the receiver-key rules). Move `Receiver.IsContainingInstanceKey` into MemberAccess as a predicate on the key (Tell, Don't Ask: Receiver asks the key, MemberAccess decides). Receiver then calls the predicate.
- **P5-b Replace Magic Number with Symbolic Constant (M-10).** One `public const int RepetitionThreshold = 2;` in Receiver (const is allowed by the static rule); CallSite.CollectFindings uses it. Both rules name the same concept.
- **P5-c Replace Type Code with Enum (M-11).** New type N-2 `SmellKind` enum (`FeatureEnvy`, `PrimitiveObsession`) replacing the string constants. Allowed: step 1.l shows zero production reads that select behavior (D-4); the enum does not replace a discriminator with more than 2 read sites. Display text: one Extract Method in Report (`DisplayName(SmellKind)` switch with 2 arms). Test churn: about 40 assertions compare `Kind` to strings; mechanical rename.
- **Size:** N-2 not estimated until crc-cards fixes the class list.

### 7.7 Complexity (M-1 to M-7)

- **P6-a Merge (duplicate switch, M-1 and M-2).** IsUnitScope and InitializerExpression both switch on declaration kind. Merge: IsUnitScope keeps its parent check for VariableDeclarator (FieldDeclaration or EventFieldDeclaration parent, otherwise local declarators would become unit scopes; this semantic difference must be preserved) and delegates the initializer test to InitializerExpression for the other kinds. CC drops for both.
- **P6-b Decompose Conditional (M-4).** IsCountable: Extract Method `HasExplicitInstance(instance)` for the negated pattern; the switch arms stay (CC 3 after extraction).
- **P6-c Extract Method (M-3, M-5, M-6, M-7).** After P2-a, ReceiverKeyOf, ExpressionKey, and ExpressionStart become one-liners that call the subclass members (CC 1 each). NormalizedTextOf and TokenTextOf use no instance state: Move Method to a Data-layer syntax helper (no static utility class: the static rule allows only constants and extension methods on primitives; so keep them as private instance members of the Data-layer type that needs them, or inline into one owner).
- **P6-d Extract Method (M-2).** InitializerExpression: one Extract Method per declaration kind (three small methods) to remove the null-coalescing chain.

### 7.8 Program and composition root (H-12)

- **P7-a Move Method (root resolution and existence check into the existing host).** `SourceTree` already owns root and Exists. Move the CWD default into a factory on SourceTree: `SourceTree.FromArguments(IReadOnlyList<string> args)` (factory, exempt from the static rule). Program keeps only the argument-count check (a simple `if` on a primitive, allowed in Presentation), the console writes, and the exit code.
- **P7-b Reuse-first decision.** A new class (`ScanCommand`) for the branch was considered and rejected because SourceTree already hosts the decision data (root, Exists); see section 8, N-3 rejected.

### 7.9 Static members and test helpers (H-10, H-11, H-9, H-8, M-15, M-14, L-3)

- **P8-a Change Static Member to Instance (H-10).** CorpusRegressionTests: `RepositoryRoot` becomes an instance readonly field set in the constructor; ScanDirectory and FindRepositoryRoot become instance methods. xUnit creates one instance per test, so cost is negligible.
- **P8-b Change Static Member to Instance (H-11).** Analysis (test helper) becomes an instance fixture: `internal sealed class Analysis` with instance methods; test call sites change from `Analysis.Scan(...)` to a field (`private readonly Analysis analysis = new();`). Churn is about 80 call sites across five test files; mechanical. The alternative (keeping the static class) violates the static rule and is rejected.
- **P8-c Extract Method (H-9).** `Analysis.WithPrelude(params (string path, string text)[] files)` builds the Codebase input; AnalysisUnitTests and Analysis.Run use it.
- **P8-d Extract Method (H-8).** `Analysis.CountOf(findings, kind)` and `Analysis.SingleOf(findings, kind)` replace the repeated `Assert.Single(OfKind(...))` and `Assert.Empty(OfKind(...))` pairs.
- **P8-e Extract Superclass (M-15).** Abstract test base `TempDirectoryTests : IDisposable` holding the temp root and cleanup; ProgramTests and SourceTreeTests inherit it. New class N-5 (test-only base class; no mock, no seal-for-safety reason); CRC card needed.
- **P8-f Remove Dead Code (M-14).** Delete `Encapsulate.UnitTests/ZzProbeTests.cs`.
- **P8-g Hide Method (L-3).** Make `Analysis.Run` private (or inline into `Scan` if P8-c lands first).

### 7.10 Medium and low items

- **P9-a Inline Method + Hide Method (M-12).** Inline `EnclosingConditionalAccess` into its single caller; `Receiver.Key` and `HasRepeatedAccess` become private (inspection MemberCanBePrivate).
- **P9-b Replace Lambda with Method Group (L-2).** SourceTree.cs:17 and Codebase.cs:29 (verified inspection suggestions; behavior identical).
- **P9-c Extract Method (comment clarity, L-1).** Replace references to plan rule numbers (R5, R6, R15) in the CallSite XML documentation with plain statements of each rule. Documentation only.

### 7.11 Explicitly rejected proposals

- Enum or lookup map for the conditional-access discriminator (see P2-a).
- Static utility class for NormalizedTextOf or extension methods on Roslyn types: rejected by the static rule (extension methods are allowed only on primitives).
- New `SourcePosition` value type: rejected; Roslyn `LinePosition` already fills this role.
- New `ScanCommand` class: rejected; SourceTree is the existing host (P7-b).
- Parameter Object: not applicable; the audit found no long parameter lists and no data clumps.

## 8. CRC cross-reference (new classes proposed by this plan)

| ID | Class (host / layer) | Responsibilities | Collaborators | Reuse check | Status |
|---|---|---|---|---|---|
| N-1 | `ConditionalMemberAccess`, `ContainingInstanceMemberAccess`, `ChainedMemberAccess`, `PlainMemberAccess` (subclasses of existing `MemberAccess`, Domain or Data per P0-a) | Each owns one receiver-kind value of the conditional-access discriminator | `MemberAccess` (base host) | `MemberAccess` already holds ReceiverKey and Start; reused as base | Proposed. Size: not estimated |
| N-2 | `SmellKind` enum (Domain) | Closed set of smell kinds | `SmellFinding` | None: replaces string constants `SmellFinding.FeatureEnvy/PrimitiveObsession` | Proposed. Size: not estimated |
| N-3 | none (rejected) | SourceTree hosts root resolution (P7-a) | `SourceTree` | Reused | Rejected |
| N-4 | `ReceiverAccess` record (Domain), only if P0-a option A | Plain access fact: key, start position, containing flag | `Receiver`, `CallSite` | `MemberAccess` holds the same four facts but wraps IOperation, which Domain may not reference; the record is the Domain-side view | Proposed, blocked by P0-a. Size: not estimated |
| N-5 | `TempDirectoryTests` (abstract test base) | Temp root and cleanup for file-system tests | `ProgramTests`, `SourceTreeTests` | None | Proposed. Size: not estimated |

Cross-check: `crc-cards.md` lists the as-built classes (Program, Report, SourceTree, SourceFile, Codebase, CodeUnit, MemberAccess, Receiver, CallSite, SmellFinding). None of N-1..N-5 duplicates an existing entity. `Receiver` and `CallSite` keep their roles.

## 9. Execution log (/refactor)

Each entry: plan step applied, standard refactoring used, verification (`dotnet build` + `dotnet test Encapsulate.slnx`), TODO count after the step, and follow-up tasks added.

| Step | Refactoring | Result / verification | TODOs remaining |
|---|---|---|---|
| P0-a | Move Class (Codebase, CodeUnit, MemberAccess, CallSite) Domain -> Data; new Domain record `ReceiverAccess(int Line, int Column)` with `IsBefore`; `MemberAccess.ToReceiverAccess()`; `Receiver` refactored to consume `ReceiverAccess` (deviation from N-4's `LinePosition`: `int` pair keeps Domain free of Microsoft.CodeAnalysis, same role the plan assigns LinePosition) | Build succeeded; 125/125 pass. C-1 TODOs removed (4). Discriminator table unchanged (no discriminator step in P0-a) | 58 |
| P0-b | Resolved by P0-a: `Codebase.TrustedReferences` (AppContext + file I/O) now lives in the Data layer, where infrastructure I/O is allowed; Domain performs no I/O. Not injected from Program — Data layer is the owner of infrastructure per the plan's own layering | n/a (no behavior change) | 58 |
| P0-c | Move Method (dedupe + ordering into Domain): `SmellFinding : IComparable<SmellFinding>` (sort key path/line/column/kind); `Codebase.FindSmells` returns `Distinct().Order()`; `Report.Render` keeps only grouping + formatting. Planned test churn: dedupe/order cases moved from ReportTests to new `SmellFindingTests` (Domain folder); ReportTests keep grouping cases | Build succeeded; 124/124 pass (ReportTests 5→2, +2 SmellFindingTests). C-2 TODO removed; H-4 TODO trimmed to the FormatLine remainder (decided at P4-b) | 57 |
| P1-a | Characterization tests only (no production change): `BaseReceiverArgumentsReportAtTheBaseAccessStart` (explicit base receiver-key position, PO), `NameofAccessDoesNotMergeIntoAConditionalReceiverKey` (FE+PO), `ParenthesizedConditionalReceiverGroupsAndReportsAtTheConditionalAccessStart` (pins actual behavior: parenthesized conditional receiver groups under its own key and reports at the inner `o?.B` start, not the parenthesis) | Build succeeded; 127/127 pass (+3 tests) | 57 |
| P2-a | Replace Conditional with Polymorphism (D-1/H-5): `MemberAccess` is now an abstract base; static factory `MemberAccess.For(IOperation)` is the ONLY read of `IConditionalAccessInstanceOperation`; subclasses `ConditionalMemberAccess`, `ContainingInstanceMemberAccess`, `ChainedMemberAccess`, `PlainMemberAccess` (N-1) override `ReceiverKey`/`ReceiverStart`/`JoinsConditionally` only where the kind differs. Faithful mapping note: the original `ReceiverKeyOf` arm 3 passed the *instance* to `ExpressionKey`, so a chained access's key is its receiver's key (`o.B`); the dead arm `member.Instance is IConditionalAccessInstanceOperation` in `ExpressionKey` (unreachable: all callers guarantee an IMemberReferenceOperation instance) was removed; `EnclosingConditionalAccess` alias inlined (part of P9-a) | Build succeeded; 127/127 pass. D-1 recount: 1 read in 1 method (factory) — was 4 reads in 4 methods; matches the plan's after-state. H-5 TODOs (4) removed; TODO count 50. New classes recorded in crc-cards.md | 50 |
| P2-b | Replace Algorithm with library call — fallback branch applied: verified that Microsoft.CodeAnalysis.CSharp 5.9.0 has NO public parenthesis-walking API on SyntaxNode (only internal `WalkDownParentheses`), so per the plan's fallback one Extract Method per owner: `MemberAccess.UnwrapParentheses` (syntax walk-down, used by NormalizedTextOf) and `CallSite.OutermostParenthesisOrSelf` (syntax walk-up, used by `Origin` and `IsWrittenArgument` — the SkipWhile-ancestors form rewritten into the equivalent climb loop); operation-side `Unwrap` (IParenthesizedOperation) kept as the single operation-level read | Build succeeded; 127/127 pass. D-2 recount: 3 paren-discriminator reads in 3 methods (walk-down helper, walk-up helper, operation-level Unwrap) — was 4 reads in 4 methods; deviation from the plan's ideal 0-syntax-reads recorded (no public API). 4 H-7 duplicate-code TODOs removed; TODO count 47 | 47 |
| P3-a | Replace Algorithm with library call (ancestor walk, H-6) — ATTEMPTED AND REVERTED for `ConditionalOf`: the syntax-ancestor lookup (`owner.Syntax.Ancestors().OfType<ConditionalAccessExpressionSyntax>().First()`) changed behavior for nested conditional accesses (`o?.B.Total` etc., 4 test failures — Roslyn nests IConditionalAccessOperations without mirroring the nesting in ConditionalAccessExpressionSyntax spans, so the syntax ancestor differs from the operation ancestor). `ConditionalOf` operation walk restored; `IsInsideNameOf` operation walk kept on purpose (a syntax-level nameof test would misclassify bare verbatim `@nameof` user methods). Recorded as a rejected approach; walks that remain are single-site, so the 4-site duplication the H-6 TODO described is resolved (CallSite sites share `OutermostParenthesisOrSelf` since P2-b) | Build succeeded; 127/127 pass after revert. H-6/ancestor-walk TODOs removed (3); TODO count 44 (M-14 probe file emptied — see P8-f note) | 44 |
| P3-b | Replace Algorithm with MinBy (H-2, H-3) + Move Method (+1 conversion into the owner): `SmellFinding.At(filePath, (Line, Column) zeroBased, kind)` owns the one-based conversion; `Receiver.CreateFinding` uses `MinBy(access => (Line, Column))` over the now zero-based `ReceiverAccess` (deviation from the plan's `LinePosition`: Domain must not reference Roslyn, so the zero-based int pair plays the same role); `CallSite.Finding` renamed `ToFinding` (M-13 partial) and delegates to `At` with `MinBy(counted => counted.Start)`; dead `ReceiverAccess.IsBefore` and `MemberAccess.IsBefore` removed (both aggregations replaced) | Build succeeded; 127/127 pass. H-2 + H-3 TODOs and the M-13 Finding-rename TODO removed; TODO count 41 | 41 |
| P3-c | Decompose Conditional / Extract Method (CallSite): `IsSeparateArgument` now reads two named predicates — `IsExplicitArgument(argument) && AppearsInSource(argument)`; `IsWrittenArgument` delegates to `IsWrittenOrAncestorArgument(OutermostParenthesisOrSelf(syntax))`. The MemberAccess.ExpressionStart `||` was already dissolved by P2-a polymorphism; Receiver.IsEnvied already reads two named properties (no change, per plan). Also removed the two stale H-6 TODOs in CallSite (the ancestor walks they cite are single-site on purpose after P3-a/P2-b) | Build succeeded (duplicate `using Microsoft.CodeAnalysis.Text` in MemberAccess removed, CS0105 gone); 127/127 pass | 37 |
| P4-a | Move Method (grouping into the owner): new static factory `Receiver.EnviedFrom(IEnumerable<IGrouping<string, ReceiverAccess>>, path)` owns the envy selection (`IsEnvied` + `CreateFinding`); `CodeUnit` exposes the collector's accumulated `Accesses`/`CallSites` and its private `EnviedAccessFindings` filters countable, groups by `ReceiverKey` and projects `ToReceiverAccess()` (deviation from the plan's `IEnumerable<MemberAccess>` signature: Domain must not reference the Data type, so the grouping/element projection happens in Data and Receiver receives `IGrouping<string, ReceiverAccess>`). The collector now only accumulates | Build succeeded; 127/127 pass. H-1 TODO removed. Remaining external reads in Data: `IsCountable`, `ReceiverKey` (Data concepts) | 37 |
| P4-b | Decision recorded (no code): `Report.FormatLine`'s reads of `Kind`, `Line`, `Column` are presentation of a data record — accepted per plan; the sort key already lives on SmellFinding (P0-c) | n/a | 37 |
| P4-c | Rename Method (M-13): `Body` → `LocalFunctionBodyOrSelf`; the collector's `ReportFindings` dissolved into `CodeUnit.CollectFindings` + `EnviedAccessFindings` (one collection verb); `Finding` → `ToFinding` was already applied at P3-b | Build succeeded; 127/127 pass. Both M-13 TODOs removed | 37 |
| P5-a | Replace Magic String with Symbolic Constant + Tell-Don't-Ask (M-9) — plan deviation required by the layer rule: the plan placed the constants and the containing-instance predicate on Data-layer `MemberAccess`, but Domain (`Receiver`) must not reference Data. Moved instead into Domain: constants `Receiver.ThisKey`/`Receiver.BaseKey` and new extension `ReceiverKeyExtensions.IsContainingInstance(this string)` (N-6, extension on a primitive, the static-rule-allowed shape); `Receiver.IsEnvied` asks the key predicate; `ContainingInstanceMemberAccess.ReceiverKey` uses the Domain constants | Build succeeded; 127/127 pass. Both M-9 TODOs removed | 34 |
| P5-b | Replace Magic Number with Symbolic Constant (M-10): `public const int RepetitionThreshold = 2;` on Receiver; `Receiver.HasRepeatedAccess` and `CallSite.CollectFindings` both use it | Build succeeded; 127/127 pass. Both M-10 TODOs removed | 34 |
| P5-c | Replace Type Code with Enum (M-11): new Domain enum `SmellKind { FeatureEnvy = 0, PrimitiveObsession }` (N-2, no `None` per naming review); `SmellFinding.Kind` becomes `SmellKind`; `SmellFinding.At`/`Receiver.CreateFinding`/`Analysis.Locate`/`Analysis.OfKind` signatures updated; `Report.DisplayName` switch (2 arms) owns the display text; ~40 test literals mechanically renamed `SmellFinding.FeatureEnvy/PrimitiveObsession` → `SmellKind.*` | Build succeeded; 127/127 pass. M-11 TODO removed | 33 |
| P6-a | Merge duplicate switch (M-1 + M-2): the unit-scope rule moved into `CodeUnit` — static factory `CodeUnit.For(model, node)` (factory exempt from the static rule) returns the unit when instance predicate `IsAnalysisUnit` holds; the declarator arm keeps the Field/EventField parent check (semantic difference preserved; a local declarator initializer stays inside its method's unit), and the remaining kinds delegate to `InitializerExpression`. `Codebase.UnitScopes/IsUnitScope` replaced by `Units(tree)` selecting `CodeUnit.For(...)` | Build succeeded; 127/127 pass. M-1 TODO removed (count measured 31 together with the same-cycle P6-d edit, from 33) | 32 |
| P6-b | Decompose Conditional / Extract Method (M-4): `MemberAccess.IsCountable` reads the named predicate `HasExplicitInstance(member.Instance) && !IsInsideNameOf`; the negated nested instance pattern moved into `HasExplicitInstance` | Build succeeded; 127/127 pass. M-4 TODO removed | 31 |
| P6-c | Decision recorded (M-7/M-8 helper location): `NormalizedTextOf`/`TokenTextOf` stay as private instance members of `MemberAccess`, their only Data-layer caller (plan P6-c's fallback: Roslyn has no public parenthesis-walking API and a static syntax utility class is banned); the duplicate `using Microsoft.CodeAnalysis.Text` was already removed at P3-c | Build succeeded; 127/127 pass. M-7 TODO removed (count measured 29 together with the same-cycle P6-b edit) | 31 |
| P6-d | Extract Method (M-2 remainder): `InitializerExpression` switch arms are one-liners; the property `??` chain extracted into `PropertyInitializer(property)` | Build succeeded; 127/127 pass. M-2 TODO removed | 29 |
| P7-a | Move Method (H-12): root resolution moved into the existing host `SourceTree` — static factory `SourceTree.FromArguments(args)` (CWD default when no argument) plus exposed `Root`; `Program.Run` keeps only the usage check, console writes and exit code; the missing-path message reads `tree.Root` (behavior identical). Applied P9-b for `SourceTree.TryRead` in the same pass (`Select(TryRead)` method group) | Build succeeded; 127/127 pass. H-12 and the TryRead L-2 TODO removed | 27 |
| P8-a | Change Static Member to Instance (H-10): `CorpusRegressionTests` — `repositoryRoot` instance readonly field set in the constructor; `ScanDirectory`/`FindRepositoryRoot` instance members | Build succeeded; 127/127 pass. 3 static TODOs removed | 24 |
| P8-b/c/d/g | Change Static Member to Instance (H-11) + Extract Method (H-9, H-8) + Hide/Inline (L-3): `Analysis` is now an instance fixture (`internal sealed class`, one instance per test class); `Run` inlined into `Scan` via the new `WithPrelude(params (string Path, string Text)[])`; `CountOf`/`SingleOf` replace the repeated `Assert.Single/Empty(OfKind(...))` pairs; `Locate`/`Method` instance; `TargetPath`/`Prelude` remain const (allowed by the static rule). `AnalysisUnitTests`' two multi-file tests now call `WithPrelude` (H-9 sites); rule-test scan-then-OfKind single-finding asserts use `SingleOf` | Build succeeded; 127/127 pass. 9 static + 3 duplication TODOs removed | 20 |
| P8-e | Extract Superclass (M-15): `TempDirectoryTests : IDisposable` (UnitTests/Support, N-5) owns the temp root, cleanup and `WriteFile`; `ProgramTests` and `SourceTreeTests` inherit it (their duplicate fixtures deleted) | Build succeeded; 127/127 pass. 2 M-15 TODOs removed | 18 |
| P8-f | Remove Dead Code (M-14): `Encapsulate.UnitTests/ZzProbeTests.cs` is 0 bytes and inert (no compiled type). The file itself could not be deleted: this sandbox denies shell file-deletion commands. Environment limitation recorded; the empty file should be removed by the delivery step | n/a | 18 |
| P9-a | Inline + Hide Method (M-12): `Receiver.Key` property removed (the constructor parameter is used directly); `HasRepeatedAccess` now private | Build succeeded; 127/127 pass. 2 visibility TODOs removed | 16 |
| P9-b | Replace Lambda with Method Group (L-2): `Codebase` uses `Select(ParseSource)` (the `SourceTree.TryRead` site was already applied at P7-a) | Build succeeded; 127/127 pass. 1 TODO removed | 15 |
| P9-c | Comment clarity (L-1): the `CallSite` XML doc now states the separate-argument rules in plain words (no plan-rule references) | Build succeeded; 127/127 pass. 1 TODO removed | 14 |
| H-8 follow-up | The remaining rule-test `scan-then-OfKind` single-finding pattern dissolved (`analysis.Scan` + `SingleOf` inside the assertion); both stale H-8 TODOs removed | Build succeeded; 127/127 pass. 2 TODOs removed | 12 |
| M-3 decision | `CallSite.Unwrap` stays a single 4-arm switch (recorded in code): it is the one operation-level wrapper-peeling point kept by P2-b, its arms are disjoint wrap kinds, and extraction would only relocate the same three branches. TODO removed with justification | n/a | 12 |
| M-8 decision | The `GetLocation().GetLineSpan().StartLinePosition` chain is the canonical Roslyn position idiom (2 sites, no logic between the links); a helper would only wrap a framework call. Accepted; both TODOs removed with justification in code | n/a | 12 |
| H-4 decision | The `Report` remainder was accepted at P4-b; the decision is now recorded in code. TODO removed | n/a | 12 |
| Final state | TODO count down to 1 — the pre-existing Framework-Design sealing decision TODO in `CorpusRegressionTests`, kept on purpose (it asks for a project-wide convention decision, not a refactoring). Final verification: `dotnet build Encapsulate.slnx` zero errors and no new warnings introduced by this pass; `dotnet test Encapsulate.slnx` 127/127 passed | Build + tests green | 1 |

