# Research Context — `encapsulate` C# console smell detector

> **Authoritative clarification (2026-11-07):** Where earlier research below conflicts with this paragraph, the clarified requirements here take precedence. The cross-platform .NET 10 C# CLI accepts zero or one positional file/directory path, defaults to CWD, recursively scans `.cs` files only, and has no flags or configuration. Do not silently exclude generated files, `bin`, `obj`, or hidden directories merely because earlier sections suggested doing so. Feature Envy counts at least two field/property access occurrences on the same external receiver in a method or compound expression, including repeated access to the same member; exclude `this` and `base` receivers. Primitive Obsession counts fields/properties of the same receiver in at least two separate arguments of one invocation or constructor, including repeated members; the Feature Envy `this`/`base` exclusion does not automatically apply to this rule. Matching receiver expressions is sufficient; no alias analysis or proof of runtime object identity is required. A receiver that is itself an owned field/property is not automatically `this` or `base`. Cases such as nested argument expressions, initializer targets, lambdas, file extension casing, inaccessible paths, symlink cycles, output, and exit codes remain implementation details, not resolved requirements. Synthetic compilation may lack external references and therefore miss semantic matches; do not claim complete semantic coverage for arbitrary folders without a strategy for unresolved symbols.

**Workflow:** `/research-context` (session `864fae95a494496ca45126480930e169`)
**Role:** research agent (no recursive research delegation)
**Date:** 2026-11-06
**Repository:** `/workdir` (Digital-Worker-Demo; git worktree metadata is broken — see §2)

---

## 1. Task restatement and interpretation

Build a C# console application named/aliased **`encapsulate`** that inspects .NET source and reports two design smells:

1. **Feature Envy**
   - An external instance is accessed through **>= 2 of its properties/fields** within the **same method** or within the **same compound expression**.
   - Exclude receivers that are `this` or `base`.
2. **Primitive Obsession** (narrow project definition)
   - The **same object's properties/fields are passed as separate arguments** to an **invocation or constructor**.
   - Implied threshold in the prompt: >= 2 such arguments; otherwise no smell.

CLI contract from the prompt:

- Optional positional argument: a **file** or a **directory**.
- No argument → default to the **current working directory (CWD)**.
- Directory input → **recursive** scan of .NET source (`.cs`).
- File input → scan that file only.
- Research `/workdir` README conventions, .NET tooling, and semantic-language support.
- **Write only `Docs/_Current/context.md`.** No git writes; no modifications outside `/workdir` (and this workflow writes no other file).

This document records evidence and options for the later planning/design steps. It does not decide implementation details that belong to `/high-level-plan` or `/plan-and-design`.

---

## 2. Repository and environment ground truth (verified)

### 2.1 Toolchain

| Fact | Evidence |
|---|---|
| .NET SDK **10.0.302**, MSBuild 18.6.11 | `dotnet --info` |
| Runtime **Microsoft.NETCore.App 10.0.10** | `dotnet --list-runtimes` |
| OS: Debian 12, Linux x64 | `dotnet --info` |
| No MAUI workloads installed | `dotnet workload` output: "There are no installed workloads to display" |
| No `global.json` | `ls /workdir/global.json` → not found |
| NuGet source: nuget.org enabled | `dotnet nuget list source` |
| NuGet global-packages cache is **absent** (`/home/opencode-agent/.nuget/packages` does not exist) | `dotnet nuget locals global-packages --list` |
| Disk: ~496 GB free | `df -h /workdir` |
| `git` operations fail: `/workdir/.git` is a file containing `gitdir: ../../../.git/worktrees/feature-card-...` whose target is missing | `git -C /workdir status` → "fatal: not a git repository" |
| Network to nuget.org / learn.microsoft.com works via HTTP fetch | verified during research |

Consequences:

- The first build/restore will download NuGet packages; no offline cache is available. Package versions must be pinned.
- No git commit/branch verification is possible in this sandbox; do not attempt git writes.
- MAUI projects cannot build here (`NETSDK1147` expected), but `net10.0` library/console/test projects build and test normally.

### 2.2 Solution and project layout

- Root `Digital-Worker-Demo.slnx` aggregates 6 **workload-free** projects (MAUI app deliberately excluded):
  - `Calculator/Domain/SciCalc.Domain`
  - `Calculator/Domain/SciCalc.Domain.UnitTests`
  - `Calculator/Presentation/SciCalc.Maui.UnitTests`
  - `UrlShortener/Presentation/UrlShortener.Api`
  - `UrlShortener/Presentation/UrlShortener.Api.UnitTests`
  - `UrlShortener/Presentation/UrlShortener.Api.IntegrationTests`
- Subsolutions: `Calculator/SciCalc.slnx`, `Calculator/SciCalc.App.slnx`, `UrlShortener/UrlShortener.slnx`.
- Source inventory: 44 `.cs` files under `Calculator/` + `UrlShortener/` (excluding `bin`/`obj`), no existing CLI/tool project.
- The repo is a **demo/proof repository** and currently contains **two independently generated apps**, not a product codebase.

### 2.3 Build/test/package conventions (from README + project files)

| Convention | Evidence |
|---|---|
| `.slnx` is the solution format | `Digital-Worker-Demo.slnx`, `Calculator/SciCalc.slnx`, `UrlShortener/UrlShortener.slnx` |
| Prefer explicit solution/project paths for verification; bare `dotnet test` is ambiguous because two apps exist | `Calculator/Presentation/SciCalc.Maui/README.md:30` |
| Test framework: **xUnit v2 + VSTest** (`Microsoft.NET.Test.Sdk` 17.12.0, `xunit` 2.9.2, `xunit.runner.visualstudio` 2.8.2) | `Calculator/Directory.Packages.props`, `UrlShortener/Directory.Packages.props`, test csproj files |
| Central Package Management is enabled **per application directory** (`Calculator/`, `UrlShortener/`), not at repo root | `*/Directory.Packages.props` with `ManagePackageVersionsCentrally=true` |
| `Directory.Build.props` sets `TargetFramework net10.0`, `Nullable enable`, `ImplicitUsings enable` in both app trees | `Calculator/Directory.Build.props`, `UrlShortener/Directory.Build.props` |
| No repo-root `Directory.Build.props`, `Directory.Packages.props`, `nuget.config`, `.editorconfig`, analyzers, or StyleCop/Roslynator configuration | filesystem check; only generated `obj/*.GeneratedMSBuildEditorConfig.editorconfig` exist |
| Tests are co-located per layer (`Domain/*.UnitTests`, `Presentation/*.UnitTests`) | `Calculator/Presentation/SciCalc.Maui/README.md:11-16` |
| README documents verification commands, layout, and known environment caveats; README updates accompany behavior changes in prior cards | root `README.md:22-60`, `Docs/_Current/prompt.md` prior plans |
| `Docs/_Current/` is the workflow state channel; prior artifacts include `prompt.md`, `high-level-plan-review.md`; required docs may be absent and must not block the workflow | workflow header; directory listing |
| `Docs/refactoring-tools.md` records that the **RefactorMCP server is not connected** in this sandbox; refactorings would have to be manual if needed | `Docs/refactoring-tools.md:3-7` |

No README currently documents an `encapsulate` tool, CLI conventions, or a tools folder. No `.editorconfig` means no repo-wide style gate; follow existing C# style (file-scoped namespaces, primary constructors, collection expressions, target-typed `new`, `sealed`, `Nullable enable`).

### 2.4 Placement / packaging constraints to carry into planning

- A new project at **repo root** (e.g. `/workdir/Encapsulate/`) inherits **no** `Directory.Build.props`/`Directory.Packages.props`; it must set `TargetFramework`, `Nullable`, `ImplicitUsings`, and package versions explicitly. This avoids polluting the apps' CPM.
- A new project under `Calculator/` or `UrlShortener/` would inherit that app's CPM and TFM/nullable settings and require version entries in its `Directory.Packages.props`; this couples a generic tool to a demo app. Not recommended unless deliberately colocated.
- If a solution is added, `.slnx` matches repo convention. Adding the tool to `Digital-Worker-Demo.slnx` would change the root build graph; keeping a separate `Encapsulate.slnx` or just `dotnet test Encapsulate/...csproj` is lower risk.
- Packaging as a .NET tool requires `PackAsTool=true`, `ToolCommandName=encapsulate`, `OutputType=Exe`, `PackageOutputPath`; then `dotnet pack` and `dotnet tool install --global --add-source <nupkg dir> encapsulate` (official tutorial in §6). Not required for running via `dotnet run --project`.

---

## 3. Detector semantics — research and ambiguity log

Both are Fowler/refactoring.guru design smells; neither has a canonical numeric threshold. The prompt's thresholds and the narrow Primitive Obsession definition are **project-specific heuristics** and must be encoded explicitly in tests.

### 3.1 Feature Envy

Canonical definition: "A method accesses the data of another object more than its own data." Treatments: Move Method / Extract Method. (refactoring.guru, Fowler's *Refactoring*.)

Project rule interpretation:

- **Unit of analysis:** a method body (including constructors, accessors, local functions if treated as methods, and expression-bodied members).
- **Access:** a reference to an instance **property** (`IPropertyReferenceOperation`) or **field** (`IFieldReferenceOperation`).
- **External instance:** the receiver of the member access, resolved to a symbol, is neither the containing instance nor `base`.
  - Explicit/implicit `this` and `base` are both represented by `IInstanceReferenceOperation` with `ReferenceKind == InstanceReferenceKind.ContainingTypeInstance` — exclude these.
  - Exclude static member references (`Instance == null`).
  - Exclude `ImplicitReceiver` (object/collection initializer targets, `with` expressions) and implicit/compiler-generated operations (`IsImplicit`).
- **Threshold:** >= 2 field/property access occurrences on the same receiver expression; `a.X + a.X` qualifies.
- **Grouping key:** `(receiver identity, containing type of member)`; receiver identity should be derived from the bound symbol (`IParameterSymbol`, `ILocalSymbol`, `IFieldSymbol`, `IPropertySymbol`) or, when binding is unavailable, normalized receiver text.
  - `order.Total` and `order.Tax` → same receiver `order` → count 2.
  - `this._repo.Total` and `_repo.Tax` → same receiver field `_repo` (normalize `this.`).
  - `a.B.X` and `a.B.Y` → receiver is property `B` on `a`; count 2 on `B` (nested receiver). This is a deliberate design choice to document.
  - `GetOrder().Total` and `GetOrder().Tax` have matching receiver expressions; matching is sufficient without proving runtime identity.
- **Scope:** "same method **or** compound expression".
  - Method-wide grouping subsumes expression-level grouping. Implement method-wide grouped counts, and additionally record the smallest shared expression (lowest common ancestor) as evidence; a compound-expression finding is a method finding with a tighter location.
  - Method-wide and compound-expression scopes are both explicitly requested; expression-only counting is insufficient.
- **Candidate evidence in this repo:**
  - `UrlShortener/Presentation/UrlShortener.Api/Api/LinkEndpoints.cs:49-50` — `ToLinkResponse(this ShortLink link, ...)` accesses `link.Code` (twice), `link.OriginalUrl`, `link.ClickCount`: 4 property accesses (3 distinct: `Code`, `OriginalUrl`, `ClickCount`) on the same external `link` in one expression-bodied method → **Feature Envy under the rule** (though a mapper intentionally does this).
  - `LinkEndpoints.cs:43` — `http.Scheme` + `http.Host` in the same conditional expression → **2 distinct properties of external `http` in one compound expression** → Feature Envy candidate.
  - `Calculator/Domain/SciCalc.Domain/Calculator.cs:93,102,111-...` — many `Buffer.*` accesses. `Buffer` is `this.Buffer` (own property) but the **receiver is the `Buffer` instance**, not `this`. Whether nested receivers count as "external" is an ambiguity; literal reading of "external instance" (not `this`/`base`) suggests yes, which would flag aggregate-internal delegation heavily. Plan must decide whether to exclude receivers that are fields/properties of the containing type (recommended precision control; document deviation from literal rule).
- **Exclusion controls to document/test:** `this`/`base`, implicit receiver, static members, `nameof`, `typeof`, attribute arguments, default parameter values, compiler-generated code, generated files (`*.g.cs`, `*.Designer.cs`, `obj/`, `bin/`), and possibly members of the containing type.

### 3.2 Primitive Obsession (narrow definition)

Canonical definition: primitives used instead of small objects (currency, ranges, phone numbers, type codes); treatment Replace Data Value with Object / Introduce Parameter Object / Preserve Whole Object. The prompt narrows this to **passing multiple members of the same object as separate arguments**, i.e. "Preserve Whole Object" territory, not general primitive-typed fields.

Project rule interpretation:

- **Sites:** method invocations (`IInvocationOperation`) and object constructions (`IObjectCreationOperation`). Constructors include `new T(...)` (including target-typed `new(...)`) and may include `this(...)`/`base(...)` initializers if treated as constructor invocations; document.
- **Arguments:** use `IArgumentOperation.Value` (unwrapping conversions and parentheses) and `IArgumentOperation.ArgumentKind`; count only explicitly supplied values with a resolved property/field reference.
- **Match:** >= 2 arguments are **direct** property/field references whose receivers resolve to the **same instance symbol**.
  - `new LinkResponse(link.Code!.Value, link.OriginalUrl, ..., link.ClickCount)` → `link.OriginalUrl` and `link.ClickCount` are direct members of the same `link` receiver; `link.Code.Value` is a nested property chain. Under a "direct receiver member" reading this yields 3 distinct direct members (`Code`, `OriginalUrl`, `ClickCount`) if the nested `Code` access counts at its outer level; likely report. Under a "only top-level argument member" reading it yields 2 (`OriginalUrl`, `ClickCount`) → still report.
  - `M(p.X, p.Y)` → report.
  - `M(p.X, q.Y)` → different receivers, no report.
  - `M(p.X + p.Y)` → not separate arguments, no report for this rule.
  - `M(p)` → whole object already passed, no report.
  - `M(p.X, p.X)` → report: repeated members in separate arguments count.
- **Exclusions:** static members (no instance), method calls as arguments (`p.GetX()` is not a property/field), literals. The explicit `this`/`base` exclusion applies to Feature Envy, not Primitive Obsession.
- **Candidate evidence:** `LinkEndpoints.cs:50` is a strong real example. `Calculator.cs:225` (`new HistoryEntry(Buffer.Text(), value, Buffer.Tokens)`) has only **one** property (`Buffer.Tokens`) plus a method call → not a hit, a good negative test. `Calculator.cs:237` (`new MathExpression(Buffer.Tokens)`) one property → negative.
- **Not detected by this narrow rule:** primitive-typed fields/parameters generally (`string originalUrl`, `double value`), type-code enums, and primitives in unrelated argument positions. Document this explicitly so tests do not expect generic Primitive Obsession.

### 3.3 Edge cases the implementation must consciously handle

| Case | Feature Envy | Primitive Obsession | Suggested handling |
|---|---|---|---|
| `this.X` / `base.X` | exclude | eligible | exclude containing instance only for FE |
| Implicit `this` (`X` inside class) | exclude | eligible | recognize implicit containing instance for PO |
| Static property/field | exclude (no instance) | exclude (no instance) | require `Instance != null` |
| Object/collection initializer (`new T { X = ... }`) | exclude (`ImplicitReceiver`) | not an invocation/constructor arg | skip |
| Indexer (`obj[i]`) | property reference with arguments; count as property | as argument, direct member? | document; likely count but record indexer |
| Null-forgiving `p.X!` | unwrap | unwrap | no `IOperation` for `!`, safe |
| Conversions / parentheses | unwrap | unwrap | walk `IConversionOperation` |
| `nameof(p.X)` / `typeof` | exclude (no runtime access) | n/a | skip nodes under nameof/typeof |
| Attributes / default parameter values | not method body → skip | skip | restrict to method bodies / explicit args |
| Partial classes / partial methods | group by method symbol, merge | same | use symbol keys |
| Records / primary constructors | constructor is a method; property init accesses may appear | primary constructor parameters are arguments? | test explicitly |
| Local functions / lambdas | method-like; decide whether to treat as separate units | arguments within them | prefer separate analysis unit with own method symbol; document |
| Extension method `x.M()` | invocation receiver is not property access; members inside count | n/a | no special case |
| `dynamic` / unresolved code | semantic binding null → skip or syntax fallback | same | skip with optional diagnostic; do not crash |
| Duplicate member (`p.X, p.X`) | count both accesses | count separate arguments | repeated members qualify |
| Nested receiver `a.B.X`, `a.B.Y` | count on `B` | count on `B` | document nested-receiver policy |
| `Get()` returned instance | matching expressions suffice | matching expressions suffice | no alias or runtime identity analysis required |

---

## 4. Console/CLI and file-system research

### 4.1 Argument parsing options

- **System.CommandLine 2.0.14** is the current stable GA line (3.0 is preview at time of research). Official API shape:
  - `RootCommand`, `Argument<string?>` / `Argument<FileSystemInfo?>`, `Option<T>`
  - `ParseResult.GetValue(argument)`, `command.SetAction(parseResult => ...)`, `rootCommand.Parse(args).Invoke()`
  - Built-in `--help`, `--version`, tab completion, response files, `[diagram]` directive.
  - Docs: `https://learn.microsoft.com/en-us/dotnet/standard/commandline/` and `/syntax`.
- **Manual `args` parsing** is viable for one optional positional path and avoids a dependency; current repo has no CLI precedent. A `System.CommandLine` dependency is justified if the tool grows flags (`--format`, `--exclude`, `--severity`), but it adds a package plus its transitive dependencies and version pinning.
- A zero-dependency implementation can use `Environment.CurrentDirectory`, `Path.GetFullPath`, `File.Exists`/`Directory.Exists`, and `Directory.EnumerateFiles`.

### 4.2 File discovery

- Use `Directory.EnumerateFiles(root, "*.cs", new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true, AttributesToSkip = FileAttributes.Hidden | FileAttributes.System })`.
- Do not impose default generated-file, directory, or hidden-file exclusions: the requested recursive `.cs` scan includes them. Handle symlink cycles and access failures explicitly without silently omitting ordinary source files.
- Default CWD behavior: `Directory.GetCurrentDirectory()`; if the CWD is a file path? No — file handling is explicit only when the positional argument points to a file.
- Symlinks/reparse points: `EnumerationOptions.AttributesToSkip` does not exclude reparse points; guard against cycles if following links (default: do not follow directory symlinks).
- Encoding: read with `File.ReadAllText` (UTF-8 detection with BOM); Roslyn `CSharpSyntaxTree.ParseText` accepts `SourceText` and an encoding for diagnostics.
- Large repos: parse in parallel (`Parallel.ForEach` / PLINQ) but keep output ordering deterministic (sort findings by path, line, column, smell).

### 4.3 Output and exit codes (options to decide in planning)

- Minimal: one line per finding, machine-friendly and stable:
  `relative/path.cs:line:col: feature-envy [receiverType receiverName] members: A, B`
  `relative/path.cs:line:col: primitive-obsession [target] args: A, B`
- Summary line with file/method/finding counts.
- Alternative structured output: JSON or **SARIF 2.1.0** for IDE/CI ingestion (`https://docs.oasis-open.org/sarif/sarif/v2.1.0/sarif-v2.1.0.html`). SARIF adds schema work; not required by the prompt.
- Exit codes: propose `0` = scan completed (no findings), `1` = findings (CI gate), `2` = usage/IO error. Alternatively `0` always unless error; decide and test. The prompt does not specify.

### 4.4 Diagnostics and failure behavior

- Unreadable file / invalid path → clear stderr message, non-zero exit, no stack trace.
- Syntax errors in one file must not abort the scan; report per-file diagnostics only in verbose mode.
- Missing semantic references (see §5) must degrade gracefully, not crash.

---

## 5. Semantic language support — options, APIs, and trade-offs

### 5.1 What semantic analysis buys

Both rules need to distinguish:
- property/field vs method,
- instance vs static,
- `this`/`base` vs external receiver,
- same receiver identity (symbol equality, not text),
- argument values vs other expressions.

Syntax-only matching can approximate this but will misclassify (`p.GetX` vs `p.X`, static members, `this`, aliases, `nameof`, nested receivers). Roslyn semantic binding is the robust route.

### 5.2 Option A — syntax-only (`Microsoft.CodeAnalysis.CSharp`, no compilation)

```csharp
SyntaxTree tree = CSharpSyntaxTree.ParseText(
    sourceText,
    new CSharpParseOptions(LanguageVersion.CSharp14)); // or Latest/Preview
CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
```

- `SyntaxNode.DescendantNodes()`, `OfType<MemberAccessExpressionSyntax>()`, `InvocationExpressionSyntax`, `ObjectCreationExpressionSyntax`, `ArgumentListSyntax`.
- Can identify `this`/`base` textually and member names, but cannot reliably distinguish fields/properties from methods, or resolve receiver types/symbols.
- Cheapest, no reference resolution, works on broken code. Highest false-positive/negative risk.
- Docs: syntax analysis tutorial (link in §6).

### 5.3 Option B — per-file semantic model with a synthetic compilation (recommended primary)

```csharp
var parseOptions = new CSharpParseOptions(LanguageVersion.CSharp14);
SyntaxTree[] trees = files.Select(f =>
    CSharpSyntaxTree.ParseText(File.ReadAllText(f), parseOptions, path: f)).ToArray();

var references = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? "")
    .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
    .Select(p => MetadataReference.CreateFromFile(p))
    .ToArray();

var compilation = CSharpCompilation.Create(
    "Encapsulate.Analysis",
    trees,
    references,
    new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

foreach (SyntaxTree tree in trees)
{
    SemanticModel model = compilation.GetSemanticModel(tree);
    // per node: model.GetOperation(node) / model.GetSymbolInfo(node)
}
```

- Gives full `IOperation` tree for method bodies, so member/instance classification is precise.
- `AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")` supplies the runtime's reference set on .NET Core/.NET 5+; it is an implementation detail but widely used. A more deterministic alternative is the `Basic.Reference.Assemblies.Net100` package (1.8.14) which ships reference assemblies for net10.0.
- Project references are absent: types from the scanned project's other files are in the same compilation; types from NuGet/other projects are unresolved unless their DLLs are referenced. For smell detection that is acceptable; unresolved symbols simply reduce findings.
- Analyzer/compiler-generated code is not present unless generated files are included.
- Performance: one compilation over all files (better cross-file symbols) vs per-file compilations (faster, less memory, no cross-file type resolution). Recommend one compilation for a directory scan, batching if needed.
- `model.GetOperation(node)` can return `null` for invalid/unsupported nodes; skip.

### 5.4 Option C — `MSBuildWorkspace` (full project fidelity, heavy)

```csharp
MSBuildLocator.RegisterDefaults(); // Microsoft.Build.Locator
using var workspace = MSBuildWorkspace.Create();
Solution solution = await workspace.OpenSolutionAsync("path/to/App.slnx");
foreach (Project project in solution.Projects)
foreach (Document document in project.Documents)
{
    SemanticModel? model = await document.GetSemanticModelAsync();
}
```

- Full project references, `DefineConstants`, `LangVersion`, generated sources, correct symbols.
- Requires `Microsoft.CodeAnalysis.Workspaces.MSBuild` (5.9.0) + `Microsoft.Build.Locator` (1.11.2) + MSBuild assemblies; on this Linux box it must locate the SDK's MSBuild. Loading MAUI projects will fail (`NETTARGET`/workload), and `.slnx` support depends on the MSBuild/Roslyn version.
- Much slower, more failure modes, and overkill for a path-in/path-out console scanner that must work on arbitrary folders (not necessarily valid solutions).
- Workspace docs: `https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/work-with-workspace`.

### 5.5 Recommendation for planning (not a decision)

Use **Option B as the default**, with:
- a single `CSharpCompilation` per scan batch, references from `TRUSTED_PLATFORM_ASSEMBLIES` (fallback: `AppDomain.CurrentDomain.GetAssemblies()` locations, then `Basic.Reference.Assemblies.Net100`);
- `CSharpParseOptions(LanguageVersion.CSharp14)` (or `Latest`), explicitly compatible with the repo's C# 12/13/14 syntax (file-scoped namespaces, primary constructors, collection expressions, target-typed `new`, records);
- graceful skip when `GetOperation`/`GetSymbolInfo` returns null;
- no MSBuildWorkspace in v1; keep it behind a future flag if project-aware analysis is needed.

### 5.6 `IOperation` API facts (official signatures)

| Interface | Key members relevant to the rules |
|---|---|
| `IPropertyReferenceOperation` : `IMemberReferenceOperation` | `Property`, inherited `Member`, `Instance` (null for static), `Arguments` (indexer args) |
| `IFieldReferenceOperation` : `IMemberReferenceOperation` | `Field`, inherited `Member`, `Instance`, `IsDeclaration` |
| `IInvocationOperation` | `TargetMethod`, `Instance`, `Arguments` (evaluation order, excluding instance) |
| `IObjectCreationOperation` | `Constructor`, `Arguments`, `Initializer` |
| `IArgumentOperation` | `Value`, `Parameter`, `ArgumentKind` |
| `IInstanceReferenceOperation` | `ReferenceKind` — `ContainingTypeInstance` covers **both `this` and `base`** in C#; also `ImplicitReceiver`, `PatternInput`, `InterpolatedStringHandler` |
| `IMemberReferenceOperation` | `Member`, `Instance`, `ConstrainedToType` |
| Extensions | `IOperation.Descendants()`, `DescendantsAndSelf()` (evaluation order) |

Reference exclusion snippet:

```csharp
static bool IsContainingInstance(IOperation? instance) =>
    instance is IInstanceReferenceOperation { ReferenceKind: InstanceReferenceKind.ContainingTypeInstance };

static bool IsCountableMember(IOperation op) => op switch
{
    IPropertyReferenceOperation { IsImplicit: false, Instance: not null } => true,
    IFieldReferenceOperation    { IsImplicit: false, Instance: not null } => true,
    _ => false
};
```

Note: `InstanceReferenceKind` has no separate `This`/`Base` values; `ContainingTypeInstance` must be excluded as a unit. `ImplicitReceiver` covers initializer targets and should also be excluded.

### 5.7 Language/version and syntax features observed in the repo

The detector must parse modern C# correctly. Examples in `/workdir`:

- File-scoped namespaces: every `.cs` file.
- Primary constructors: `MathExpression(IReadOnlyList<Token> tokens)`, `Parser(...)`, `BinaryNode(...)`, `FunctionNode(...)`.
- Collection expressions: `private readonly List<HistoryEntry> history = [];`, `Token[] inner = [.. Buffer.Tokens];`.
- Target-typed `new`: `new(...)` in `LinkEndpoints.cs:50`, `new Calculator()` etc.
- Records/readonly record structs: `HistoryEntry`, `Token`.
- `ref` returns and switch expressions: `MemoryBank.Slot`.
- Nullable enabled in both app trees.
- `nameof`/`typeof` not prominent, but tests may add them.

Parse with C# 14 (SDK 10 default) or `Latest`; do not rely on `LangVersion=latest` in project files (official guidance warns it is machine-dependent — see §6). If a scanned file uses a newer preview feature, fall back to `Preview` parse options and surface a diagnostic.

---

## 6. Official documentation and package versions (verified)

### 6.1 Roslyn / semantic analysis

| Topic | Link | Relevant APIs |
|---|---|---|
| Syntax analysis tutorial | https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/get-started/syntax-analysis | `CSharpSyntaxTree.ParseText`, `CompilationUnitSyntax`, `SyntaxNode.DescendantNodes`, `CSharpSyntaxWalker` |
| Semantic analysis tutorial | https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/get-started/semantic-analysis | `CSharpCompilation.Create`, `AddReferences`, `MetadataReference.CreateFromFile`, `GetSemanticModel`, `GetSymbolInfo`, `GetTypeInfo` |
| Workspace model | https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/work-with-workspace | `Workspace`, `Solution`, `Project`, `Document` |
| `IOperation` family | https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.operations.ioperation | base interface; `GetOperation` returns null if unavailable |
| `IPropertyReferenceOperation` | https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.operations.ipropertyreferenceoperation | `Property`, `Instance`, `Arguments` |
| `IFieldReferenceOperation` | https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.operations.ifieldreferenceoperation | `Field`, `Instance`, `IsDeclaration` |
| `IInvocationOperation` | https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.operations.iinvocationoperation | `TargetMethod`, `Instance`, `Arguments` |
| `IObjectCreationOperation` | https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.operations.iobjectcreationoperation | `Constructor`, `Arguments`, `Initializer` |
| `IArgumentOperation` | https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.operations.iargumentoperation | `Value`, `Parameter`, `ArgumentKind` |
| `IInstanceReferenceOperation` | https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.operations.iinstancereferenceoperation | `ReferenceKind` |
| `InstanceReferenceKind` | https://learn.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.operations.instancereferencekind | `ContainingTypeInstance`, `ImplicitReceiver`, `PatternInput`, `InterpolatedStringHandler` |
| C# language versioning | https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/configure-language-version | C# 14 default for .NET 10; avoid `latest` in project files |
| File enumeration | https://learn.microsoft.com/en-us/dotnet/api/system.io.enumerationoptions?view=net-10.0 | `RecurseSubdirectories`, `AttributesToSkip`, `IgnoreInaccessible`, `MaxRecursionDepth` |

### 6.2 CLI / packaging

| Topic | Link |
|---|---|
| System.CommandLine overview | https://learn.microsoft.com/en-us/dotnet/standard/commandline/ |
| System.CommandLine syntax (commands/arguments/options/arity) | https://learn.microsoft.com/en-us/dotnet/standard/commandline/syntax |
| Create a .NET tool (PackAsTool/ToolCommandName/PackageOutputPath) | https://learn.microsoft.com/en-us/dotnet/core/tools/global-tools-how-to-create |

### 6.3 Package versions observed on nuget.org (2026-11-06)

| Package | Latest stable observed | Notes |
|---|---|---|
| `Microsoft.CodeAnalysis.CSharp` | **5.9.0** | parser + semantic model; net10.0-compatible |
| `Microsoft.CodeAnalysis.Workspaces.MSBuild` | **5.9.0** | only if Option C |
| `Microsoft.Build.Locator` | **1.11.2** | required by MSBuildWorkspace |
| `System.CommandLine` | **2.0.14** | stable 2.0.x (3.0 preview exists) |
| `Basic.Reference.Assemblies.Net100` | **1.8.14** | deterministic net10.0 reference set |

No package cache exists locally; restore requires network. Pin versions explicitly if the new project sits outside a CPM directory.

### 6.4 Smell definitions

| Smell | Source |
|---|---|
| Feature Envy | https://refactoring.guru/smells/feature-envy — "a method accesses the data of another object more than its own data"; treatments Move Method / Extract Method; no numeric threshold |
| Primitive Obsession | https://refactoring.guru/smells/primitive-obsession — primitives instead of small objects; treatments Replace Data Value with Object, Introduce Parameter Object, Preserve Whole Object; no numeric threshold |

---

## 7. Relevant files and 2-level dependency map

### 7.1 Calculator domain (best corpus for semantic tests)

`Calculator/Domain/SciCalc.Domain/Calculator.cs` (238 lines, aggregate root) direct dependencies:

- `InputBuffer` (`Calculator.cs:75`) — `Token`, `TokenKind`, `OperatorKind`, `FunctionKind`, `ConstantKind`, `CultureInfo`.
- `MemoryBank` (`:79`) — `MemorySlotId`, `double?` slots.
- `HistoryEntry` (`:81,225`) — `Token` list, expression text, result.
- `CalculationResult` (`:85,190,232,236`) — `CalcError`, `double?`.
- `MathExpression` (`:237`) — `Token` list, `AngleMode`, `CalcError`, nested `Parser` (`MathExpression.cs:31`), `Node` tree.
- `Token` (`:7-31`) — `TokenKind`, `OperatorKind`, `FunctionKind`, `ConstantKind`.
- `CalcError`, `AngleMode`, `InputKey`, `MemorySlotId`, `FunctionKind` (enums).
- `Preview`, `Locked`, `ActiveError`, `LastAnswer` public state.

Second level (dependency of dependency):

- `InputBuffer` → `Token`, `TokenKind`, `OperatorKind`, `FunctionKind`, `ConstantKind`, `CultureInfo`; no further domain types.
- `MathExpression` → `Parser` (private nested), `Node` (`Nodes/Node.cs`), `BinaryNode`, `FunctionNode`, `NumberNode`, `PercentNode`, `UnaryMinusNode`, `CalculationResult`, `CalcError`, `AngleMode`, `Token`, `FunctionKind`, `OperatorKind`.
- `Node` → `CalculationResult`, `AngleMode`.
- `HistoryEntry` → `Token`, `IEnumerable<Token>`.
- `MemoryBank` → `MemorySlotId`.
- `Token` → `TokenKind`, `OperatorKind`, `FunctionKind`, `ConstantKind`.

Test project: `Calculator/Domain/SciCalc.Domain.UnitTests/SciCalc.Domain.UnitTests.csproj` — `net10.0`, `IsPackable=false`, xUnit + VSTest, ProjectReference to Domain. `ContinuationTests.cs` shows the repo's test style (`[Fact]`, `[Theory]`, `[InlineData]`, `Assert.*`, file-scoped namespace).

### 7.2 UrlShortener (best corpus for Primitive Obsession/Feature Envy)

- `UrlShortener/Presentation/UrlShortener.Api/Domain/ShortLink.cs` → `ShortCode`, `DateTime`, `int`.
- `ShortCode.cs` → `Alphabet`, `CodeBase`, `BufferLength`, `string`, `IEquatable<ShortCode>`.
- `UrlShortener/Presentation/UrlShortener.Api/Api/LinkEndpoints.cs` → `LinkRegistry`, `ShortLink`, `CreateLinkRequest`, `LinkResponse`, `HttpRequest`, `IConfiguration`, `Uri`, ASP.NET `IResult`.
- `Api/Dtos.cs` → records `CreateLinkRequest(string Url)`, `LinkResponse(string Code, string OriginalUrl, string ShortUrl, int ClickCount)`.
- `Api/LinkRegistry.cs` → `ShortenerDbContext`, `ShortLink`, `CreateLinkRequest`, EF Core (`db.Links`, `SaveChangesAsync`, `ExecuteUpdateAsync`).
- `Data/ShortenerDbContext.cs` → `DbContext`, `DbSet<ShortLink>`.
- `Program.cs` → configuration, DI, rate limiting, EF Core; top-level statements.

Second level:

- `LinkRegistry` → `ShortenerDbContext` → `ShortLink` (`DbSet<ShortLink>`), EF Core.
- `LinkEndpoints` → `LinkRegistry` → `ShortenerDbContext`/`ShortLink`; `ToLinkResponse` extension → `LinkResponse` + `ShortLink`.
- `ShortLink` → `ShortCode` → `string`, `long`.
- Tests: `UrlShortener/Presentation/UrlShortener.Api.UnitTests/*` (ShortLink/ShortCode), `IntegrationTests/*` (`TestServerFixture`, `LinkApiTests`, `RateLimitTests`, `ConcurrencyTests`).

### 7.3 Files with no existing equivalent functionality

- No `encapsulate` project/namespace/command anywhere (`grep -ri encapsulate` only found prose in `Docs/refactoring-tools.md`).
- No existing analyzer package or `Analyzer` item in any csproj/props.
- No `.editorconfig`, no `stylecop.json`, no Roslynator, no NDepend.
- `Docs/refactoring-tools.md` lists RefactorMCP tools, but the server is **not connected** here, so no refactoring API can be called; implementation must be plain Roslyn code.

---

## 8. Existing code patterns and snippets (positive/negative corpus)

### 8.1 Strong Feature Envy examples

`UrlShortener/Presentation/UrlShortener.Api/Api/LinkEndpoints.cs:49-50`

```csharp
public static LinkResponse ToLinkResponse(this ShortLink link, Uri baseUri) =>
    new(link.Code!.Value, link.OriginalUrl, new Uri(baseUri, link.Code.Value).ToString(), link.ClickCount);
```

- Same external instance `link`; distinct members `Code`, `OriginalUrl`, `ClickCount` (3) in one expression-bodied method.
- Also the same site is a **Primitive Obsession** example: `OriginalUrl` and `ClickCount` (and `Code` at the nested level) are passed as separate constructor arguments instead of the whole `ShortLink`.

`UrlShortener/Presentation/UrlShortener.Api/Api/LinkEndpoints.cs:41-44`

```csharp
private static Uri GetBaseUri(HttpRequest http, IConfiguration configuration)
{
    var origin = configuration["Shortener:PublicOrigin"];
    return string.IsNullOrWhiteSpace(origin) ? new Uri($"{http.Scheme}://{http.Host}") : new Uri(origin);
}
```

- `http.Scheme` + `http.Host`: 2 distinct properties of the same external instance `http` in one compound expression → Feature Envy candidate. (The `configuration` indexer is not a property/field; not counted.)

### 8.2 Ambiguous / negative examples

`Calculator/Domain/SciCalc.Domain/Calculator.cs`

- Many `Buffer.*` accesses (`:93`, `:102`, `:111`, `:113`, `:133`, `:148`, `:155-156`, `:166-170`, `:173`, `:185`, `:193`, `:198-199`, `:210-214`, `:225`, `:237`). If nested receivers count as external, this method-rich aggregate will produce many Feature Envy findings on its own `Buffer`. This is the key precision decision.
- `:225` `new HistoryEntry(Buffer.Text(), value, Buffer.Tokens)` — only one property (`Tokens`) plus a method call → **not** a Primitive Obsession hit.
- `:237` `new MathExpression(Buffer.Tokens)` — one property → not a hit.

`UrlShortener/Presentation/UrlShortener.Api/Domain/ShortLink.cs`

- `AssignCode` reads `Id`, `Code`, calls `ShortCode.FromId(Id)` — own members, `this` excluded → no Feature Envy.
- `IsAbsoluteHttpUrl` uses `Uri.TryCreate`/`uri.Scheme`/`Uri.UriSchemeHttp` — only static/local, no external instance grouping → no hit.

`UrlShortener/Presentation/UrlShortener.Api.IntegrationTests/TestServerFixture.cs:29-36`

- `DatabasePath + "-shm"` etc.; no property/field clustering → no hit. Good regression negative.

### 8.3 Modern C# syntax the parser must accept

| Feature | Example |
|---|---|
| File-scoped namespace | `namespace SciCalc.Domain;` (every file) |
| Primary constructor | `public sealed class MathExpression(IReadOnlyList<Token> tokens)` |
| Collection expression | `private readonly List<HistoryEntry> history = [];`, `Token[] inner = [.. Buffer.Tokens];` |
| Target-typed new | `new(link.Code!.Value, ...)` |
| Record / readonly record struct | `HistoryEntry`, `Token` |
| Switch expression / ref return | `MemoryBank.Slot` |
| Nullable annotations / null-forgiving | `Double?`, `link.Code!.Value` |
| Top-level statements | `UrlShortener/.../Program.cs` |

### 8.4 Test-style conventions

- xUnit `[Fact]` / `[Theory]` + `[InlineData]`, `Assert.Equal/True/False/Null/NotNull`, precision for doubles.
- Tests are plain classes with file-scoped namespaces, no custom base class (except integration fixture).
- Test project uses `IsPackable=false` and CPM-provided package versions.
- For the detector, rule tests should parse **source strings** with Roslyn (fast, no temp files); CLI/file-discovery tests should use temp directories outside the repo or an injected file system abstraction to avoid writing to `/workdir`.

---

## 9. Prior art and existing similar functionality in the repo

- **No smell detector exists.** `grep -ri "feature envy|primitive obsession|encapsulate"` found only `Docs/refactoring-tools.md` prose.
- The repo's generated apps are *subjects* for the detector, not implementations of it.
- `Docs/refactoring-tools.md` is a reference list for the mcp-router RefactorMCP server, which is **absent at runtime** in this sandbox; it documents intent, not available APIs. Do not plan on calling `extract-method`, `rename-symbol`, etc.
- Prior `Docs/_Current/prompt.md` content is a series of unrelated cards (calculator defect fixes, solution aggregation, Windows build, global.json/SQLite) and shows the documentation style: "Ground truth (verified in this worktree)", "Assumptions", "Decisions / trade-offs", "Scope", "Acceptance criteria", "Test cases", "Verdict". Reuse this style in the plan.

---

## 10. Risks, unknowns, and open questions for the next step

### Must resolve in planning/design

1. **Feature Envy scope:** method-wide vs compound-expression-only. Recommendation: method-wide grouped count, with compound-expression evidence; document that a single expression with 2 members is also a hit.
2. **Nested receivers:** does `this.Buffer.Tokens` + `this.Buffer.Text()` count as Feature Envy on `Buffer`? Literal rule says yes; precision/UX likely says exclude receivers that are own fields/properties. Decide and encode in tests.
3. **Distinct members vs accesses:** require ≥2 distinct members (recommended) or count repeated accesses.
4. **Primitive Obsession receiver:** exclude `this`/`base` (recommended for consistency) or follow the prompt literally (only Feature Envy mentions the exclusion).
5. **Nested property chains as args:** `link.Code.Value` — count the outer member `Code`, the inner `Value`, both, or only direct terminal members? Recommendation: count the direct member on the shared receiver (`Code`, `OriginalUrl`, `ClickCount`) and treat the nested chain as one argument member; document.
6. **Constructors:** include `this(...)`/`base(...)` initializers? Include attribute constructors? Recommendation: include object creation only; exclude attributes.
7. **Local functions/lambdas:** separate method units or part of the containing method? Recommendation: separate units keyed by symbol; document.
8. **Generated code / exclusions:** default exclude `bin`, `obj`, `.git`, `.vs`, `TestResults`, `*.g.cs`, `*.Designer.cs`, `*.Generated.cs`, `AssemblyInfo.cs`; expose `--exclude` later.
9. **Output format and exit codes:** plain text vs JSON/SARIF; 0/1/2 semantics. Recommendation: plain text first, stable sort, `0` clean / `1` findings / `2` error.
10. **Project placement/name:** root `Encapsulate/` with explicit package versions (recommended) vs app-tree placement with CPM; solution inclusion vs standalone test command.
11. **CLI library:** System.CommandLine 2.0.14 vs manual args. Recommendation: manual parsing for one optional path; add System.CommandLine only if flags are required.
12. **Semantic references:** `TRUSTED_PLATFORM_ASSEMBLIES` vs `Basic.Reference.Assemblies.Net100`; behavior when symbols are unresolved.

### Technical risks

| Risk | Impact | Mitigation |
|---|---|---|
| No NuGet cache; first restore needs network | build blocked offline | pin versions; verify restore early; document network prerequisite |
| Roslyn package/version conflicts with SDK 10.0.302 | build/analyzer load errors | use `Microsoft.CodeAnalysis.CSharp` 5.9.0 only; do not reference SDK internals |
| `GetOperation` null on incomplete code | missed findings | skip gracefully; optionally syntax fallback; emit verbose diagnostics |
| False positives on aggregate-internal delegation (`Calculator.Buffer`) | noisy output | exclude own-field/property receivers or document finding; regression tests |
| False positives on mappers/DTO construction (`ToLinkResponse`) | intended pattern flagged | document that smells are advisory; tests assert expected flags |
| C# 14 syntax features | parse failures | parse with `LanguageVersion.CSharp14`; fallback `Preview`; test repo files |
| Multi-target/MAUI files in scan | unresolved symbols | semantic analysis is best-effort; no project-loading |
| Path traversal/symlinks/large repos | hangs/cycles | `IgnoreInaccessible`, no symlink following, max depth option |
| Determinism across file enumeration order | flaky output/tests | sort findings deterministically; stable group keys |
| Output line/column 1-based vs 0-based | test mismatches | use Roslyn `LinePosition` (0-based) then convert to 1-based for display; document |

---

## 11. Suggested verification corpus (for the plan/test cases)

Use the real repo plus synthetic fixtures:

**Positive Feature Envy**
- `LinkEndpoints.ToLinkResponse` (`link.Code`, `link.OriginalUrl`, `link.ClickCount`).
- `GetBaseUri` (`http.Scheme`, `http.Host`) in one conditional expression.
- Synthetic: `void M(Order o) { var x = o.Total + o.Tax; }` (method and compound expression).
- Synthetic: `void M(Order o) { Use(o.Total); Use(o.Tax); }` (method-wide, not same expression).
- Synthetic: `void M(Order o) { var a = o.Total; }` (one member → no finding).

**Negative Feature Envy**
- `this.X` + `this.Y` in one method.
- `base.X` + `base.Y`.
- Static members `T.X` + `T.Y` (no instance).
- `p.X` + `q.Y` (different receivers).
- `p.X` twice (same member, if distinct-member rule).
- `nameof(p.X)` + `nameof(p.Y)` (no runtime access).
- `new T { X = 1, Y = 2 }` (object initializer implicit receiver).

**Positive Primitive Obsession**
- `new LinkResponse(link.Code!.Value, link.OriginalUrl, url, link.ClickCount)`.
- `M(p.X, p.Y)`.
- `new Point(p.X, p.Y)`.
- Named args: `M(y: p.Y, x: p.X)`.

**Negative Primitive Obsession**
- `M(p.X + p.Y)` (one argument).
- `M(p)` (whole object).
- `M(p.X, q.Y)` (different receivers).
- `M(p.X, p.X)` (same member twice, if distinct-member rule).
- `new HistoryEntry(Buffer.Text(), value, Buffer.Tokens)` (one property + method call).
- `new MathExpression(Buffer.Tokens)` (one property).
- `M(p.GetX(), p.GetY())` (method calls, not properties/fields).

**CLI / file discovery**
- No args → scans CWD.
- File path → scans only that file.
- Directory path → recursive, skips `bin`/`obj`/`.git`.
- Invalid path / unreadable file → clear error, non-zero exit.
- Empty directory / no `.cs` → clean output, exit 0.
- Deterministic ordering of multiple findings.
- Relative paths in output relative to the input root (or CWD), not absolute.

**Syntax robustness**
- All repo files parse with 0 syntax errors.
- Primary constructors, collection expressions, target-typed `new`, records, top-level statements.
- Malformed file → other files still analyzed; diagnostics only in verbose mode.

---

## 12. Key source excerpts (for the plan to reference)

`LinkEndpoints.cs:49-50` — positive FE + PO:

```csharp
public static LinkResponse ToLinkResponse(this ShortLink link, Uri baseUri) =>
    new(link.Code!.Value, link.OriginalUrl, new Uri(baseUri, link.Code.Value).ToString(), link.ClickCount);
```

`LinkEndpoints.cs:41-44` — positive FE in a compound expression:

```csharp
private static Uri GetBaseUri(HttpRequest http, IConfiguration configuration)
{
    var origin = configuration["Shortener:PublicOrigin"];
    return string.IsNullOrWhiteSpace(origin) ? new Uri($"{http.Scheme}://{http.Host}") : new Uri(origin);
}
```

`Calculator.cs:223-228` — negative PO (one property + method):

```csharp
private void PushHistory(double value)
{
    HistoryEntry entry = new(Buffer.Text(), value, Buffer.Tokens);
    history.Add(entry);
    if (history.Count > MaxHistoryEntries) history.RemoveAt(0);
}
```

`MathExpression.cs:1-27` — C# 12 primary constructor, collection expression, target-typed `new`, nested parser:

```csharp
public sealed class MathExpression(IReadOnlyList<Token> tokens)
{
    public CalculationResult Evaluate(AngleMode mode) { ... }
    private CalculationResult EvaluateParsed(AngleMode mode) =>
        Normalize(new Parser(tokens).Parse().EvaluateNode(mode));
    ...
    private sealed class Parser(IReadOnlyList<Token> tokens) { ... }
}
```

---

## 13. Status

Research complete for `/research-context`. No code or other documentation was changed; this file is the only artifact written. The next workflow step should verify this context and then hand off to `/high-level-plan` (or `/plan-and-design` if the planning step judges the detector complex enough). Open semantic decisions are listed in §10 for that step.

---

## 14. Research update — fallback run on the current worktree (preserves §1-§13 above)

**Workflow:** `/research-context` (WorkflowsMcp-http), session `012f5a85dda74739bf35ee028ca1bb55`.
**Status:** complete — 9/9 steps, workflow reported completed successfully.
**Why this section exists:** the configured research sub-agent model `openai/gpt-6-luna` was unavailable, so this run was executed as the research-agent fallback. The prior research (§1-§13) was produced against `/workdir`; this section re-verifies it against the actual current worktree and records only deltas. No implementation was performed.

### 14.1 Current worktree ground truth (verified this run)

| Fact | Verified value |
|---|---|
| CWD / repo root | `/workdir/.DigitalWorker/worktrees/feature-card-issue-1237211400-27-20261106145344234` |
| Branch | `feature-card-issue-1237211400-27-20261106145344234` |
| Git state | `git status` works here (untracked `Docs/_Current/context.md`, `Docs/_Current/session.md`). This contradicts §2.1's "git worktree metadata broken" — that applied to `/workdir`, not this worktree. Still: no git writes performed. |
| SDK / runtime | .NET SDK `10.0.302`; `Microsoft.NETCore.App` / `Microsoft.AspNetCore.App` `10.0.10`; Linux x64 |
| MAUI workloads | none installed (`dotnet workload list` empty) |
| `global.json` / `.editorconfig` | absent |
| Source inventory (excl. `bin`/`obj`) | 61 `.cs` files: `Calculator/` 48, `UrlShortener/` 13 (prior §2.2 "44" is stale — now 61) |
| Solution files | root `Digital-Worker-Demo.slnx` (6 workload-free projects), `Calculator/SciCalc.slnx`, `Calculator/SciCalc.App.slnx`, `UrlShortener/UrlShortener.slnx` |
| Existing detector | none. Repo-wide grep for `encapsulate`, `feature envy`, `primitive obsession` in `.cs`/`.csproj`/`.props` returns no implementation. |
| `Directory.Build.props` | `Calculator/`: sets `Nullable`/`ImplicitUsings` only (TFM `net10.0` is set per-csproj). `UrlShortener/`: sets `net10.0` + nullable + implicit usings. Prior §2.3 wording adjusted. |
| CPM | per-app `Directory.Packages.props` (`Calculator/`, `UrlShortener/`), `ManagePackageVersionsCentrally=true` |
| Test stack | xUnit v2 `2.9.2` + `xunit.runner.visualstudio` `2.8.2` + `Microsoft.NET.Test.Sdk` `17.12.0` (VSTest) |

### 14.2 README / project / tests inspected

- Root `README.md` (63 lines): documents the two generated demo apps, the MAUI-on-Linux caveat (`NETSDK1147` without workloads), and the authoritative verification commands (`dotnet test Calculator/SciCalc.slnx`, `dotnet test UrlShortener/UrlShortener.slnx`). No `encapsulate` tool documented yet; adding a root-level project must not be assumed to inherit app CPM.
- `Calculator/Presentation/SciCalc.Maui/README.md` (55 lines): solution layout, "always name the solution explicitly", workload-free gate, behavior/accessibility notes.
- `Docs/refactoring-tools.md`: records that the RefactorMCP server is not connected in this sandbox (only WorkflowsMcp-http is available); no refactoring API may be assumed.
- `Docs/_Current/*`: `context.md` (this file, prior research), `session.md` (previous session export), `prompt.md` + `high-level-plan-review.md` (unrelated prior cards — preserved).
- Test style confirmed in `Calculator/Domain/SciCalc.Domain.UnitTests/ContinuationTests.cs`: file-scoped namespace, `[Fact]`/`[Theory]`/`[InlineData]`, `Assert.*`, no custom base class.
- Csproj conventions confirmed: `Calculator/Domain/SciCalc.Domain/SciCalc.Domain.csproj` and its test project are plain `net10.0`; `UrlShortener.Api` is `Microsoft.NET.Sdk.Web`.

### 14.3 Detector semantics — confirmed, unchanged from §3

- **Feature Envy:** >= 2 field/property access occurrences on a matching external receiver in the same method or compound expression, including repetitions; exclude `this`/`base`.
- **Primitive Obsession:** fields/properties of a matching receiver in >= 2 separate arguments of one invocation or constructor, including repetitions. Do not apply the FE-only `this`/`base` exclusion.
- Both are project-specific deterministic heuristics; the canonical smells carry no numeric threshold. Official confirmations re-fetched this run:
  - https://refactoring.guru/smells/feature-envy — "A method accesses the data of another object more than its own data"; treatments Move Method / Extract Method.
  - https://refactoring.guru/smells/primitive-obsession — primitives instead of small objects; treatments Replace Data Value with Object / Introduce Parameter Object / Preserve Whole Object.

### 14.4 Corpus examples re-verified in this worktree

- `UrlShortener/Presentation/UrlShortener.Api/Api/LinkEndpoints.cs:49-50`:
  `new(link.Code!.Value, link.OriginalUrl, new Uri(baseUri, link.Code.Value).ToString(), link.ClickCount)` — same external `link`; distinct members `Code`, `OriginalUrl`, `ClickCount` (Feature Envy) and separate `link` member arguments to the constructor (Primitive Obsession).
- `UrlShortener/Presentation/UrlShortener.Api/Api/LinkEndpoints.cs:40-44` (`GetBaseUri`): `http.Scheme` + `http.Host` in one conditional expression → compound-expression Feature Envy.
- `Calculator/Domain/SciCalc.Domain/Calculator.cs`: `Buffer.*` accesses at lines 93, 102, 111, 113, 133, 148, 155-156, 166-170, 173, 185, 193, 198-199, 210-214, 225, 237 — nested own-field receiver ambiguity (see §10 item 2).
- `Calculator.cs:225` `new HistoryEntry(Buffer.Text(), value, Buffer.Tokens)` — one property + one method call → negative Primitive Obsession case.
- `Calculator.cs:237` `new MathExpression(Buffer.Tokens)` — one property → negative.
- `UrlShortener/Presentation/UrlShortener.Api/Domain/ShortLink.cs`: own-member accesses (`Id`, `Code`) → no Feature Envy; a clean negative.

### 14.5 CLI / output contract (restated for planning)

- Optional single positional path: file → that file only; directory or omitted → recursive scan of `.cs` under CWD.
- Output grouped per file; print `path:` header once, then one line per finding: `  <Smell Name> (<line>, <column>)` with 1-based line/column (Roslyn `LinePosition` is 0-based — convert for display).
- Deterministic ordering (path, line, column, smell). Zero configuration; no flags.
- `GetOperation` may return `null` on invalid/incomplete code — skip gracefully; never abort the scan.

### 14.6 Remaining planning decisions (unchanged from §10)

Nested chains (`link.Code.Value`), local functions/lambdas, output and exit codes, project placement, semantic reference source and unresolved symbols remain design considerations. Method/compound-expression scope, repeated occurrences, receiver matching, recursive `.cs` scanning, and zero flags/configuration are clarified above and are not open decisions.

### 14.7 Status of this run

Research complete. Only `Docs/_Current/context.md` was written (this appended section); all prior content in §1-§13 is preserved. No code changes, no git writes, no files outside CWD. No blocker encountered; the model-unavailability was handled exactly as instructed by acting as the research-agent fallback.

## 15. Current approval and research delta

- `Docs/_Current/prompt.md:69-91` and `Docs/_Current/encapsulate-plan-review.md:34-38` explicitly retain four human checkpoints before implementation: C1 approval of the amended plan, C2 direct-receiver treatment of nested PO arguments, C3 exclusion of indexers, and C4 inclusion of `this`/implicit-`this` in PO. The review verdict is **approve with changes**, not implementation approval. The separate `high-level-plan-review.md` concerns an unrelated calculator task.
- The clarification at the beginning of this document supersedes historical contradictory recommendations and examples, including distinct-member thresholds, excluding owned receivers, PO automatically excluding `this`/`base`, and default generated/hidden-directory exclusions. Repeated occurrences count; matching receiver expressions suffice without runtime identity or alias analysis. The C2-C4 choices remain plan approval checkpoints, not additional clarified requirements.
- Existing sections 7 and 12 contain the two-level repository dependency map and relevant source examples; no existing detector was found. Microsoft's syntax model documentation confirms syntax trees preserve source structure even for malformed code: https://learn.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/work-with-syntax. Semantic binding remains best-effort when references or symbols are unavailable.
- The plan's verification command at `prompt.md:66` names `Encapsulate/Encapsulate.slnx`, whereas its placement at `prompt.md:19` and `:67` names root `Encapsulate.slnx`. Reconcile the path before verification; this is a plan inconsistency, not a separate human approval checkpoint.
