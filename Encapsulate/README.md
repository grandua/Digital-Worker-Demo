# encapsulate

A zero-configuration .NET 10 console tool that reports two encapsulation smells in C# code:

- **Feature Envy (FE)**: a method, accessor or initializer reads two or more field/property occurrences on the same external receiver (for example `order.Total` and `order.Tax`). Accesses through `this`, `base` or implicit `this` are not counted.
- **Primitive Obsession (PO)**: a single invocation or object creation passes two or more separate arguments that are fields/properties of the same receiver (for example `new Pair(order.Total, order.Tax)`).

## Usage

```
encapsulate [path]
```

| Invocation | Behaviour |
|---|---|
| `encapsulate` | Scans the current working directory recursively. |
| `encapsulate <directory>` | Scans every `.cs` file under the directory, recursively. |
| `encapsulate <file.cs>` | Scans only that file. |
| `encapsulate <non-.cs file>` | Prints nothing, exit 0. |
| `encapsulate <missing path>` | Writes `encapsulate: path not found: <path>` to stderr, exit 2. |
| `encapsulate a b` | Writes `usage: encapsulate [path]` to stderr, exit 2. |

Output is grouped by file. Each file with findings has one header line followed by one line per finding, sorted by line, then column, then smell name:

```
Api/LinkEndpoints.cs:
  Feature Envy (50, 13)
  Primitive Obsession (50, 31)
```

- Headers are paths relative to the scanned directory (or the file name for a file argument), using `/` separators.
- `(line, column)` is 1-based. A tab counts as one column.
- Clean files are omitted. A scan with no findings prints nothing.
- Exit code 0 means the scan completed (with or without findings). Exit code 2 means the arguments were invalid.

## Rules

- **Receiver key**: the receiver expression text with trivia, null-forgiving `!`, redundant parentheses and a leading `this.` removed. `this.X` and `X` both use the key `this`; `base.X` uses `base`. `a?.X` uses the key `a`.
- **FE**: per method-level unit and per receiver key, two or more occurrences of field/property accesses (repeated accesses count). The finding is reported at the start of the first occurrence. `this` and `base` keys are never reported. Static members, `nameof`, object/collection initializer targets, `with` expressions and property sub-patterns are not counted.
- **PO**: per invocation or object creation, two or more separate arguments whose outermost member reference has the same receiver key. Named and reordered arguments count. Expanded `params` elements count separately. The receiver of a reduced extension call, default-value arguments and method-call arguments are not counted. `this`/implicit receivers do count for PO.
- **Indexers** are not field/property accesses for either rule.
- **Units**: each method, constructor, accessor, operator, local function, field or property initializer, expression-bodied member and primary-constructor base call is one unit. Lambdas belong to their enclosing unit. Local function bodies are their own unit. All top-level statements in one file form one unit.
- Duplicate findings at the same position and kind are printed once. A field-access finding and a primitive-obsession finding at the same position are both printed, with Feature Envy first.

## Limitations

- **Semantic coverage is best-effort.** The compilation references only the assemblies in the runtime's trusted platform list (plus the scanned sources and the SDK implicit global usings). Types from other packages, such as ASP.NET Core, do not bind. Accesses on unbound types are skipped silently, so a smell can be missed. For example, scanning `UrlShortener/Presentation/UrlShortener.Api/Api/LinkEndpoints.cs` alone reports nothing, but scanning its directory reports the findings at lines 50, because the sibling sources bind the `ShortLink` type.
- **Text-based receiver grouping.** Receivers are compared by normalized text, not by runtime identity or alias analysis. Two accesses share a key when their receiver text matches, even if they refer to different variables in different scopes. Aliasing and reassignment are not tracked.
- **Deterministic rules, not a design judgement.** The tool reports structural patterns using the rules above, without architectural heuristics. It does not decide whether a finding is a real design problem.
- **Discovery** enumerates every `.cs` file, including hidden, `bin/` and `obj/` folders. Symlinks and other reparse points are not followed. Files and folders that cannot be enumerated or read during a directory scan are skipped. A file argument that cannot be read is not handled: the tool fails with an unhandled exception. Extension matching is case-insensitive.
- **Malformed source** is still analysed with Roslyn error recovery; unresolved symbols are skipped.
- **Single-threaded**, no flags, no configuration, no output formats other than the text report, no auto-fix.
- Only `.cs` sources are analysed. `.vb` and `.fs` are not.

## Build and test

From the repository root:

```
dotnet build Encapsulate.slnx
dotnet test Encapsulate.slnx
```

Run it from source:

```
dotnet run --project Encapsulate -- <path>
```

The project targets `net10.0` and depends on `Microsoft.CodeAnalysis.CSharp` 5.9.0.
