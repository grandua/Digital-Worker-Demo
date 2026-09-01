# Full Correctness and Standards Audit (2026-09-01)

Workflow: `review-for-correctness-and-standards`
Session: `c934847e6cf940179a67c9cb70764b05`
Scope: all committed code under `src/SciCalc.Domain/**`, `src/SciCalc/**`, and `tests/SciCalc.Tests/**`.

## Correctness Findings

- [x] FIXED [Medium] `src/SciCalc/Components/CalculatorPage.razor:57-62,178-199`: history rows are rendered through `DisplayText`, whose `RenderNumber` reads `Calc.Buffer.EditingNumber` whenever the supplied token is last. While the user edits a current number, every history expression ending in a number can therefore display that current edit text instead of its stored final number. Render history snapshots without current-buffer edit state. A TODO is present at line 177.
  - Fixed: `HistoryText` renders snapshots with `isLive: false`; live-edit substitution now applies only to the current expression line.
- [x] FIXED [Medium] `src/SciCalc.Domain/Calculator.cs:89-91,170-173`: `StoreMemory` stores only `Preview`. With an earlier answer and a non-empty malformed/incomplete buffer, `Preview` is null and STO does nothing instead of falling back to the latest answer as required by `Docs/_Current/prompt.md:10`. Add the missing fallback outcome and regression coverage. A TODO is present at line 169.
  - Fixed: `StoreMemory` falls back to `LastAnswer` when `Preview` is null; regression tests `StoreWithIncompleteBufferFallsBackToLastAnswer` (all 3 slots) added in `MemoryTests`.
- [x] FIXED [Low] `src/SciCalc/README.md:7,12`: the solution-layout list says `SciCalc.sln` contains the MAUI project, then the next section says the project is intentionally absent. The actual solution contains `src/SciCalc/SciCalc.csproj`; remove the contradictory statement and document the real workload behavior.
  - Fixed: README now documents that the MAUI project is in the solution and explains the NETSDK1147 workload gate.

## Architecture and Domain Findings

- [Medium] `src/SciCalc.Domain/InputBuffer.cs:9-44,84-104` and `src/SciCalc/Components/CalculatorPage.razor:72-107,178-199`: Domain retains operator/function/constant display mappings while Presentation owns a second glyph mapping. This mixes presentation concerns into `InputBuffer` and duplicates the mapping source. Keep expression/edit semantics in Domain and UI glyph rendering in Presentation.
- [Low] `tests/SciCalc.Tests/TestTokens.cs:7-118`: `TestTokens` is a static procedural tokenizer with static lookup state, not an extension host or a state-and-behavior object. It also duplicates symbol/function mappings represented elsewhere. Replace it with a non-static test expression object or use the public calculator outcomes directly.
- [Low] `src/SciCalc.Domain/CalculationResult.cs:3-19`: the default value of this public struct has neither `Value` nor `Error`, but `HasError` reports false and callers then dereference `Value`. The type does not enforce its success/failure invariant; use a shape whose default cannot masquerade as success.

No Domain I/O, outward Domain package dependency, circular dependency, Data-layer inversion, anemic production service, or presentation-owned calculation was found. `Calculator` remains the aggregate coordinating session behavior. Derived properties `LastAnswer`, `Locked`, `Preview`, and `HasLiteralOverflow` are pull-based and have no mutable caches.

## Strict Standards Findings

- [Medium] Methods over the 10-line limit: `src/SciCalc.Domain/InputBuffer.cs:53-67` (`Add`), `src/SciCalc.Domain/MathExpression.cs:5-15` (`Evaluate`), `src/SciCalc.Domain/MathExpression.cs:103-114` (`ParsePrimary`), `src/SciCalc.Domain/Nodes/FunctionNode.cs:35-45` (`Inverse`), and `src/SciCalc/Components/CalculatorPage.razor:181-191` (`RenderToken`). Test/helper violations include `tests/SciCalc.Tests/TestTokens.cs:19-32,43-53`, `MemoryTests.cs:24-35,41-55,58-72`, `HistoryTests.cs:9-19,22-34,37-47`, and `CalculatorTests.cs:123-134,157-171,193-209,260-273,287-297,310-321,353-375`.
- [Medium] `tests/SciCalc.Tests/MemoryTests.cs:41-42`: `StoreRecallClearRoundTripPerSlot` has four parameters. Constructors alone are exempt from the maximum of three.
- [Low] Loops prohibited by the workflow's LINQ/pipeline rule remain at `src/SciCalc.Domain/Calculator.cs:108,165`, `src/SciCalc/Components/CalculatorPage.razor:23,36,42,57`, `tests/SciCalc.Tests/TestTokens.cs:22`, `InputBufferTests.cs:34`, `HistoryTests.cs:13`, and `TestKeys.cs:9`.
- [Low] `src/SciCalc.Domain/InputBuffer.cs:8,45`: `_tokens` and `_numberText` use the prohibited underscore field prefix. The existing TODO at line 7 tracks this.
- [Low] `src/SciCalc.Domain/MathExpression.cs:29`: `ParseFailure` derives from `Exception` without the framework-standard `Exception` suffix.
- [Low] `src/SciCalc/Components/CalculatorPage.razor:171-172`: `NewestFirst` returns an `IEnumerable` of tuples instead of a named record/class/struct.
- [Low] Duplicate closeness/preview assertion helpers remain at `tests/SciCalc.Tests/ParserTests.cs:49-56`, `EvaluatorTests.cs:58-65`, `CalculatorTests.cs:386-390`, `MemoryTests.cs:132-136`, and `TestTokens.cs:11-17`. The non-extension static helpers also violate the workflow's static-member rule. `TestKeys`, `AngleModeConversions`, framework entry points, and value-object factories are exempt.
- [Low] Null-forgiving value/error chains obscure invariants at `src/SciCalc.Domain/Calculator.cs:183-185`, `InputBuffer.cs:58,74,89-94,104`, `MathExpression.cs:23`, `Nodes/BinaryNode.cs:11`, `Nodes/FunctionNode.cs:11`, `Nodes/PercentNode.cs:14-15,21`, and `src/SciCalc/Components/CalculatorPage.razor:14,183-196`.
- [Low] Stepwise/void mutation remains in `Calculator.RestoreHistory` and `WrapBufferInFunction` (`Calculator.cs:104-109,158-166`), `InputBuffer` edit helpers (`InputBuffer.cs:106-133`), parser AST aggregation (`MathExpression.cs:51-60`), and the test tokenizer (`TestTokens.cs:19-31`). Return transformations and assign them at aggregate command boundaries.

## Test Gaps

- [Medium] `src/SciCalc/Components/CalculatorPage.razor:5-224`, `CalculatorPage.razor.css:1-253`, and `MauiProgram.cs:8-25`: no automated component/startup harness verifies keypad routes, two-line/error rendering, history rendering/restoration, mode changes, memory indicators, DI composition, or selector alignment. The TODO at `CalculatorPage.razor:71` tracks this; the README records only a temporary compile harness.
- [Low] Mechanism-targeted empty-token assertions remain at `tests/SciCalc.Tests/CalculatorTests.cs:129,170,231,241,320` and `MemoryTests.cs:84`; prefer displayed buffer/session outcomes. `HistoryTests.cs:50-58` legitimately tests snapshot encapsulation.
- [Low] No focused tests cover the two correctness findings above: malformed-current-buffer STO fallback and history rendering while current numeric editing is active.

## Requirements Audit

Active source: `Docs/_Current/refactoring-plan.md` (newest pre-workflow timestamp). Completion score: **13.0/15 = 86.7%**. Fully complete: A1, A2, A3, B1a, B5, B1b, B9, B10, B2, B8, and resolved-TODO cleanup. Partial: B3 static/test cleanup, B4 presentation mapping, B6 method/loop cleanup, and B7 naming polish. No item scored None.

## Verification

- `dotnet test tests/SciCalc.Tests`: passed, 227 succeeded, 0 failed, 0 skipped.

---

# SciCalc Correctness and Standards Review (Historical)

## Final verification iteration 6 findings — fixed

- [Fixed] `src/SciCalc.Domain/InputBuffer.cs`: `HasLiteralOverflow` was mutable state copied from `_numberText`. **Fix:** replaced it with an expression-bodied JIT query and removed imperative assignments.
- [Fixed] `src/SciCalc.Domain/Calculator.cs`: memory store preferred `LastAnswer` whenever history existed, so `STO` ignored a valid current-buffer preview. **Fix:** store the pull-based `Preview` value and cover the mixed history/current-buffer case.
- Razor wiring review: all keypad, mode, memory, and history buttons route to the intended aggregate command. No explicit `StateHasChanged` is needed for these Blazor event callbacks.

> Consolidated findings index (full detail below): blocking fixes required — (1) InputBuffer long-literal OverflowException must route to CalcError.Overflow + AC-only lockout; (2) DEL must preserve numeric editing state; (3) FunctionKind.Abs must join IsPostfixWrapKey; refactor backlog — EvaluationContext anemic/unused-Ans, Calculator derived-state → smart properties, presentation glyphs out of InputBuffer, mutable-list exposure, static BinaryNode member, >10-line methods, underscore field names, KeyDef/MemSlot/OperatorKind abbreviations, contiguous-enum memory routing, YAGNI (HistoryEntry timestamp), Razor @key/StateHasChanged/M+ label.

> **Refactoring plan status:** `Docs/_Current/refactoring-plan.md` created by `/find-smells-and-plan-refactoring` session `88809b08542048caab1bd58682e8d56b`. Sequence **A** (correctness) → **B** (structural) → **C** (gates). Smell counts: CRITICAL 0, HIGH 8, MEDIUM 18, LOW 14. No new classes required beyond optional nested private records; prefer existing Calculator/AngleMode/InputBuffer hosts.

Workflow session: `review-for-correctness-and-standards` / `b0d32a766ae348d3ae8ceb0eefe7c73d`

## Correctness Findings

- [High] `src/SciCalc.Domain/InputBuffer.cs:124-129`: numeric entry uses unguarded `double.Parse`. A sufficiently long literal throws `OverflowException` during a key press instead of producing the required `Error` / `Overflow` state and AC-only lockout. Add literal-overflow coverage and route the failure through the calculator error state.
- [Medium] `src/SciCalc.Domain/InputBuffer.cs:65-70,109-129`: DEL discards number-entry state. After entering `12+`, deleting `+`, and entering `3`, the display reads `123` but the buffer contains adjacent `Number(12)` and `Number(3)` tokens, leaving preview/evaluation malformed. Preserve or reconstruct editable numeric state and add a continuation-after-DEL test.
- [Medium] `src/SciCalc.Domain/Calculator.cs:147-149`: the active plan classifies `|x|` as a postfix/wrap key, but `FunctionKind.Abs` is omitted from `IsPostfixWrapKey`. Pressing `|x|` after an operand appends `abs(` after that operand and produces a malformed expression. Add the missing keypad-route outcome test.

## Architecture and Standards Findings

- [Medium] `src/SciCalc.Domain/InputBuffer.cs:8-44,78-101`: Domain owns UI glyph/name rendering and duplicates mappings in `CalculatorPage.razor:71-123`. Move display mapping to Presentation and keep Domain dependency-free and presentation-agnostic.
- [Medium] `src/SciCalc.Domain/Calculator.cs:59-69,99-105,177-205`: `Locked`, `LastAnswer`, and `Preview` are mutable values derivable from existing error/history/buffer state. Replace mutable caches with smart properties or a single coherent session state; retain aggregate commands as mutation boundaries.
- [Medium] `src/SciCalc.Domain/Calculator.cs:59` and `src/SciCalc.Domain/InputBuffer.cs:47`: mutable `List` instances are exposed behind `IReadOnlyList` and remain castable/mutable. `HistoryEntry.cs:3-7` also retains a caller-supplied token collection. Return immutable snapshots/read-only wrappers.
- [Low] `src/SciCalc.Domain/EvaluationContext.cs:3`: `EvaluationContext` is an anemic Context-suffixed data class under the workflow naming rules; its `Ans` value is unused. Remove unused state and place angle conversion behavior on an appropriate domain concept.
- [Low] `src/SciCalc/Components/CalculatorPage.razor:167-169`: `KeyDef` and `MemSlot` use prohibited abbreviations; use complete domain/presentation names.
- [Low] Field names beginning with `_` violate the active naming workflow: `Calculator.cs:5-6,31`, `InputBuffer.cs:7-45`, `MathExpression.cs:31`, `MemoryBank.cs:5-7`, and `CalculatorPage.razor:71,97,125`.
- [Low] `src/SciCalc.Domain/Calculator.cs:118-130`: memory routing relies on contiguous enum ordinals, making behavior silently dependent on declaration order. Use explicit key-to-command/slot mapping.
- [Low] `src/SciCalc/Components/CalculatorPage.razor:23,36,42,57`: rendered loops have no `@key`; component reconciliation is not explicitly stabilized.
- [Low] `src/SciCalc/Components/CalculatorPage.razor:142-152`: explicit `StateHasChanged` calls are redundant for Blazor event callbacks.
- [Low] `src/SciCalc/Components/CalculatorPage.razor:27`: the memory store action is labeled `M+`, conventionally meaning add, while Domain overwrites the slot. Label it as store without changing the required store behavior.
- [Low] `src/SciCalc.Domain/HistoryEntry.cs:7` and `tests/SciCalc.Tests/HistoryTests.cs:50-58`: timestamp state and its test are unused by every MVP behavior and UI (YAGNI).
- [Low] `src/SciCalc.Domain/EvaluationContext.cs:3`: `Ans` is unused because ANS is inserted as a number token (YAGNI).

## Strict Workflow Rule Findings

- Replace imperative `for`/`foreach` loops per workflow: `Calculator.cs:89,157`, `InputBuffer.cs:81`, `MathExpression.cs:135`, `FunctionNode.cs:70`, `CalculatorPage.razor:23,36,42,57`, `TestTokens.cs:22`, `HistoryTests.cs:13`, and `TestKeys.cs:9`.
- Static non-factory/non-extension members violate the supplied rule. Production: `BinaryNode.cs:24`. Tests: assertion/tokenizer helpers and static state in `ParserTests.cs:49`, `EvaluatorTests.cs:56`, `CalculatorTests.cs:263`, `MemoryTests.cs:120`, and `TestTokens.cs:7-118`. Framework entry points/factories and Token/CalculationResult factories are exempt.
- Methods over 10 lines include `MathExpression.Evaluate` (`MathExpression.cs:5-16`), `Parser.ParsePrimary` (`101-113`), `Parser.TryTakeAny` (`133-144`), `InputBuffer.Add` (`InputBuffer.cs:49-63`), `FunctionNode.Inverse` (`FunctionNode.cs:35-45`), `MauiProgram.CreateMauiApp` (`MauiProgram.cs:8-20`), and multiple test methods/helpers. Decompose without changing behavior.
- Constructor parameter limit is exempt. The private presentation record `MemSlot` has five constructor parameters, so it does not violate the method-parameter rule; no non-constructor method with more than three parameters was found.
- No 1-to-1 interfaces, IO in Domain, outward Domain package dependency, Data-layer inversion issue, or IO orchestrator was found. `Calculator` is a domain aggregate coordinating owned session behavior.

## Test Gaps and Risks

- No automated component/UI harness verifies keypad routes, two-line/error rendering, mode visibility, memory indicators, or history clicks. MAUI cannot build in this sandbox because workloads are absent.
- Mechanism-focused assertions couple tests to token representation in `CalculatorTests.cs:81-104,127-161`, `HistoryTests.cs:21-35`, and `MemoryTests.cs:37-54`; prefer observable expression/result/session outcomes.
- Requirements completion score: 12.5 / 14 = 89.3%. Features 3, 12, and 13 are partial due to the three correctness findings above; all other MVP features have implementation and test evidence.

## Verification State

- Reused parent verification as requested: Domain/tests build clean with zero warnings/errors; 205/205 tests pass through the xUnit v3 MTP executable.
- MAUI build is blocked by missing Linux MAUI workloads (`NETSDK1147`), accepted and not a defect.

---
## Requirements-source last-write log (requirements-audit entry point)
- prompt.md (user prompt + high-level plan): 2026-09-01 01:53 UTC
- plan-and-design.md: ABSENT (task ran on high-level plan only — no plan-and-design workflow invoked)
- refactoring-plan.md: ABSENT (no refactor pass yet)
- issues.md (correctness/standards review report): 2026-09-01 02:16 UTC

---
## Class-name verification

Workflow session: `verify-class-names` / `9eea79b8baf84a5d86bf727137ea7248`
Scope: all types in `src/SciCalc.Domain`, `src/SciCalc`, `tests/SciCalc.Tests` (31 types total).

### Classes that PASS OOP/DDD naming rules

| # | Class | File | Justification |
|---|-------|------|---------------|
| 1 | Calculator | `src/SciCalc.Domain/Calculator.cs:3` | Real-world domain concept. Rich aggregate with state + behavior. |
| 2 | InputBuffer | `src/SciCalc.Domain/InputBuffer.cs:5` | Real-world concept (calculator input buffer). State + behavior. |
| 3 | MemoryBank | `src/SciCalc.Domain/MemoryBank.cs:3` | Real-world concept. State (_m1-_m3) + behavior (Store/Recall/Clear). |
| 4 | HistoryEntry | `src/SciCalc.Domain/HistoryEntry.cs:3` | Real-world concept. Immutable value object (record). |
| 5 | Token | `src/SciCalc.Domain/Token.cs:3` | Real-world concept (lexical token). Record struct + factory methods. |
| 6 | MathExpression | `src/SciCalc.Domain/MathExpression.cs:3` | Real-world concept. State (tokens) + behavior (Evaluate/Parse). |
| 7 | CalculationResult | `src/SciCalc.Domain/CalculationResult.cs:3` | Domain concept + Result/Value Object pattern. |
| 8 | Node | `src/SciCalc.Domain/Nodes/Node.cs:3` | Interpreter/Composite pattern. Abstract with EvaluateNode behavior. |
| 9 | FunctionNode | `src/SciCalc.Domain/Nodes/FunctionNode.cs:6` | Interpreter pattern. Rich behavior (Apply, trig, log, etc.). |
| 10 | NumberNode | `src/SciCalc.Domain/Nodes/NumberNode.cs:3` | Interpreter pattern terminal node. |
| 11 | BinaryNode | `src/SciCalc.Domain/Nodes/BinaryNode.cs:3` | Interpreter pattern for binary operators. |
| 12 | UnaryMinusNode | `src/SciCalc.Domain/Nodes/UnaryMinusNode.cs:3` | Interpreter pattern for unary negation. |
| 13 | PercentNode | `src/SciCalc.Domain/Nodes/PercentNode.cs:3` | Interpreter pattern with relative-percent semantics. |
| 14 | App | `src/SciCalc/App.cs:3` | MAUI framework convention. |
| 15 | MauiProgram | `src/SciCalc/MauiProgram.cs:6` | MAUI framework convention (static entry point). |
| 16 | MainPage | `src/SciCalc/MainPage.razor` | MAUI framework convention. |
| 17 | CalculatorPage | `src/SciCalc/Components/CalculatorPage.razor` | Blazor page component, real concept. |

All enums pass: `CalcError`, `TokenKind`, `OperatorKind`, `FunctionKind`, `ConstantKind`, `AngleMode`, `InputKey`, `MemorySlotId` — PascalCase, real-world concepts.
All test classes pass xUnit naming convention: `CalculatorTests`, `ParserTests`, `EvaluatorTests`, `MemoryTests`, `FunctionTests`, `HistoryTests`, `PercentTests`, `TestTokens` (static helper), `TestKeys` (static extension methods).

### Violations found

- [Medium] `src/SciCalc.Domain/EvaluationContext.cs:3`: **"Context" suffix — Data Class code smell.** `EvaluationContext` is a `readonly record struct` ending in "Context". It carries `AngleMode Mode` and `double? Ans` (Ans is unused — see YAGNI finding in Correctness section above). The `ToRadians`/`ToDegrees` methods are angle-conversion utilities that belong on the domain concept they serve, not on a generic "context" bag. Consider: (a) passing `AngleMode` directly into `Evaluate`/`EvaluateNode` and placing conversion methods on `AngleMode` or a dedicated type; or (b) renaming to a domain concept like `AngleSettings` if a parameter object is still needed. Remove the unused `Ans` field. *(Confirms existing finding at issues.md line 16.)*

- [Low] `src/SciCalc/Components/CalculatorPage.razor:168`: **Abbreviation in type name.** `KeyDef` uses abbreviation "Def". Rename to `KeyDefinition` or a domain-meaningful name like `KeypadButton`. *(Confirms existing finding at issues.md line 17.)*

- [Low] `src/SciCalc/Components/CalculatorPage.razor:170`: **Abbreviation in type name.** `MemSlot` uses abbreviation "Mem". Rename to `MemorySlot`. *(Confirms existing finding at issues.md line 17.)*

- [Low] **Field names starting with `_` (underscore prefix).** Violates the active naming rule "do NOT start field names with `_`":
  - `Calculator.cs:5` `_history`, `:6` `_keyTokens`, `:31` `_functionKeys`
  - `InputBuffer.cs:7` `_tokens`, `:8` `_operatorNames`, `:17` `_functionNames`, `:40` `_constantNames`, `:45` `_numberText`
  - `MemoryBank.cs:5-7` `_m1`, `_m2`, `_m3`
  - `MathExpression.cs:31` `_position` (nested Parser)
  - `CalculatorPage.razor:72` `_functionKeys`, `:98` `_mainKeys`, `:126` `_memorySlots`
  *(Confirms existing finding at issues.md line 18.)*

- [Low] **Abbreviated enum members in `OperatorKind`.** `src/SciCalc.Domain/OperatorKind.cs:5-10`: members `Sub`, `Mul`, `Div`, `Mod`, `Pow` are abbreviations. While these are widely recognized mathematical shorthand, they strictly violate the "no abbreviations" rule. Consider `Subtract`, `Multiply`, `Divide`, `Modulo`, `Power` for full compliance. *(New finding.)*

### No violations (confirmed clean)

- No anemic/procedural stateless service classes found. All domain classes combine state and behavior.
- No prohibited class-name prefixes found.
- No acronyms in class names.
- All class names are 1-3 words, PascalCase.
- State and logic are maximized in Domain layer — only CalculatorPage is outside Domain and it is a thin UI shell.

---
## Requirements audit (traceability matrix) — coverage 12/14 = 85.7%
| # | Feature | Score | Evidence |
|---|---------|-------|----------|
| 1 | +,-,*,/ precedence | 1.0 | ParserTests precedence Theory, EvaluatorTests |
| 2 | Nested parens + unmatched detection | 1.0 | ParserTests nesting/Malformed |
| 3 | Scientific functions (all 20) | 0.5 | FunctionTests both modes; PARTIAL: Abs not in IsPostfixWrapKey (Calculator.cs:147-149); long-literal OverflowException (InputBuffer.cs:124-129) |
| 4 | π, e constants | 1.0 | EvaluatorTests |
| 5 | DEG/RAD + always-visible mode | 1.0 | FunctionTests both modes; CalculatorPage mode-toggle |
| 6 | Unary minus by context | 1.0 | ParserTests unary |
| 7 | Right-assoc ^ (2^3^2=512) | 1.0 | EvaluatorTests |
| 8 | Percent semantics | 1.0 | 50%=0.5; 200+10%=220; 200-10%=180; 200*10%=20; 200/10%=2000 |
| 9 | History ≤10 tap re-insert | 1.0 | HistoryTests FIFO cap + restore; UI panel |
| 10 | Memory M1-M3 + indicators | 1.0 | MemoryTests independence + IsNonEmpty; UI badges |
| 11 | ANS key | 1.0 | CalculatorTests (LastAnswer ?? 0) |
| 12 | AC + DEL last token | 0.5 | CalculatorTests; PARTIAL: DEL loses numeric entry state (adjacent Number tokens) |
| 13 | Errors + reason + lockout until AC | 0.5 | ErrorTests taxonomy; PARTIAL: long literal throws raw OverflowException instead of routed CalcError.Overflow |
| 14 | Two-line display | 0.5 | CalculatorPage.razor display/error rendering; PARTIAL: no automated UI harness (MAUI workloads absent in sandbox) |

Required follow-ups (fix-issues pass):
1. Route numeric-literal overflow through CalcError.Overflow + lockout (InputBuffer.Add — guard double.TryParse / cap literal length).
2. Preserve numeric editing state across DEL (merge/re-lex adjacent Number tokens or track editable last token).
3. Add FunctionKind.Abs to postfix-wrap key set (Calculator.IsPostfixWrapKey) + keypad-route test.
4. UI harness gap accepted: MAUI unavailable in sandbox; documented in src/SciCalc/README.md.

---
## Class-name re-check (find-smells-and-plan-refactoring)

Workflow session: `find-smells-and-plan-refactoring` / `88809b08542048caab1bd58682e8d56b` → nested `/verify-class-names`.

Reconfirmed prior violations (no new class-name defects):
- EvaluationContext (Context suffix / data class) — plan B5
- KeyDef / MemSlot abbreviations — plan B7
- Underscore fields — plan B7
- OperatorKind abbreviated members — plan B7

No new anemic service classes; Node* Interpreter pattern names justified.

---
## Current unstaged-change audit

Workflow session: `review-for-correctness-and-standards` / `a898371a4ed14856b0763ea98d2f22bf`

### New findings

- [Medium] `src/SciCalc/Components/CalculatorPage.razor.css:36-251` renames selectors to `display-expression`, `display-result`, `error-text`, `display-error-reason`, `memory-row`, `fn-grid`, `main-grid`, `key.fn`, `key.ac`, `key.del`, `key.eq`, and `span-all`, while `CalculatorPage.razor:8-44` still emits the prior classes. The changed rules therefore do not match the rendered component, removing display/error typography, memory-grid layout, keypad grids, and key variants. Align the markup and stylesheet and add component-level selector/render coverage.
- [Medium] `src/SciCalc.Domain/InputBuffer.cs:53,71-78,134-144` adds `HasLiteralOverflow` as mutable cached state even though it is derivable from `_numberText`; `RemoveLastToken` changes the source state without resetting the flag, so the public buffer can continue reporting overflow after its overflowing edit is removed. Make the state a derived smart property or synchronize every mutation path, with direct `InputBuffer` coverage.
- [Low] `src/SciCalc/README.md:10` says shared Razor usings come from `Components/_Imports.razor`, but the reviewed untracked file is `src/SciCalc/_Imports.razor`; no `Components/_Imports.razor` exists. Correct the documented path.
- [Low] `src/SciCalc/Components/CalculatorPage.razor.css:42-46` repeats the base `.mode-toggle` color, border-color, and background declarations verbatim in `.mode-toggle.mode-rad`. Keep the base as the RAD default or centralize the variant values.

### Requirements-audit completion matrix

Active source: `Docs/_Current/refactoring-plan.md` (latest pre-workflow timestamp). A1, A2, and A3 are fully implemented with focused tests. B1-B8 remain pending as the plan's unchecked checklist states. Score: 3.0/11 = 27.3%; the low percentage reflects intentionally unexecuted Phase B work rather than a claim of regression.

### Documentation consistency risks

- Earlier sections in this file still describe A1-A3 as open and report 205 tests / prior requirements scores. The current diff implements A1-A3, while the user-provided current baseline is 214 passing tests. Treat those earlier snapshots as historical, not current status.

### Current verification

- `dotnet build tests/SciCalc.Tests/SciCalc.Tests.csproj`: passed, 0 warnings / 0 errors.
- `dotnet test tests/SciCalc.Tests/SciCalc.Tests.csproj`: passed, 223/223 tests.
- Coverage collection with `--collect:"XPlat Code Coverage"`: unavailable; the SDK/xUnit v3 invocation selected the incompatible zero-test path and exited 5, so no coverage percentage was produced.

---
## Class-name verification (current unstaged diff)

Workflow session: `verify-class-names` / `85b1f527cc0d4cf790695c8ff7cf0092`
Scope: only NEWLY ADDED or CHANGED types in the current unstaged diff (`git --no-pager diff`) plus new untracked file `src/SciCalc/_Imports.razor`. Cross-referenced against prior findings in sections "Class-name verification" and "Class-name re-check" above.

### Fixed prior violations

| Prior violation | Status | Evidence |
|----------------|--------|----------|
| `EvaluationContext` ("Context" suffix / anemic data class) | **FIXED** | `src/SciCalc.Domain/EvaluationContext.cs` deleted. `AngleMode` passed directly into `Evaluate`/`EvaluateNode`. Conversion methods moved to `AngleModeConversions` extension class on `AngleMode`. |

### New types that PASS naming rules

| # | Type | File | Justification |
|---|------|------|---------------|
| 1 | `AngleModeConversions` | `src/SciCalc.Domain/AngleMode.cs:9` | NEW static extension-method class. Extension-method containers are exempt from OOP/DDD class design rule. PascalCase, no abbreviations, 2 words, describes domain concept. |
| 2 | `HasLiteralOverflow` (property) | `src/SciCalc.Domain/InputBuffer.cs:53` | NEW `bool` property. PascalCase, no abbreviations, describes overflow state. (Stale-state concern is a correctness issue, not naming.) |

### Prior violations NOT FIXED (still present in current diff)

| Prior violation | Status | Evidence |
|----------------|--------|----------|
| `KeyDef` abbreviation | **NOT FIXED** | `CalculatorPage.razor:169` — still `KeyDef`. Diff only adds a TODO comment at line 72. Should be `KeyDefinition` or `KeypadButton`. |
| `MemSlot` abbreviation | **NOT FIXED** | `CalculatorPage.razor:171` — still `MemSlot`. Should be `MemorySlot`. |
| `OperatorKind` abbreviated members (`Sub`/`Mul`/`Div`/`Mod`/`Pow`) | **NOT FIXED** | `OperatorKind.cs:7-11` — still abbreviated. Diff only adds a TODO comment at line 3. Should be `Subtract`/`Multiply`/`Divide`/`Modulo`/`Power`. |
| Underscore-prefixed fields (`_history`, `_keyTokens`, `_functionKeys`, `_tokens`, `_operatorNames`, `_functionNames`, `_constantNames`, `_numberText`, `_m1`/`_m2`/`_m3`, `_position`, `_functionKeys`/`_mainKeys`/`_memorySlots` in Razor) | **NOT FIXED** | All files retain underscore fields. Diff adds TODO comments acknowledging violations but does not rename. |

### New violations introduced

None. The diff introduces no new class, record, enum, or field names that violate the naming rules. All new names (`AngleModeConversions`, `HasLiteralOverflow`, `TryParseFinite`) follow PascalCase, use no abbreviations, and are within 1-3 words.

### Types confirmed absent

`MemoryGroup` and `PadKey` (mentioned in the review scope as potential nested records) do not exist in the current codebase. Only `KeyDef` and `MemSlot` are declared as nested records in `CalculatorPage.razor`.

### Notes

- `Docs/framework-design-guidelines.md` does not exist in the repository. The workflow rule references it as a centralized guidance doc, but no such file was found.
- `src/SciCalc/_Imports.razor` (new untracked file) contains only `@using` directives — no type declarations, no naming issues.

---
## Class-name re-check (find-smells-and-plan-refactoring re-audit)

Workflow session: `find-smells-and-plan-refactoring` / `7923ea4ed5774f18b12f3bb26924e0e0` → nested `/verify-class-names`.
Scope: unstaged Domain/Presentation types only.

### Resolved since prior plan
- `EvaluationContext` ("Context" data class) — **GONE** (file deleted; AngleMode + `AngleModeConversions`).

### Still open (no new class-name defects)
- `KeyDef` → `KeyDefinition`; `MemSlot` → `MemorySlot` (B7)
- Underscore-prefixed fields Domain + Razor (B7)
- `OperatorKind` / mirrored `InputKey` abbreviated members (B7)
- `AngleModeConversions` extension host — **PASS** (extensions exempt)

No new anemic service classes; Node* Interpreter names justified.
