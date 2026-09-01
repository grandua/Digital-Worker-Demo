# User Prompt

Build a scientific calculator app "SciCalc" that runs natively on Windows, macOS, iOS, and Android from a single .NET MAUI Blazor Hybrid project. Keypad-driven scientific calculator: +, -, *, / with precedence; parentheses (unlimited depth, unmatched detection); scientific functions (sin, cos, tan, asin, acos, atan, sinh, cosh, tanh, log10, ln, e^x, 10^x, x², x³, √, ∛, x^y, n!, |x|, 1/x, mod); constants π, e; DEG/RAD toggle visible in UI; unary minus context; right-associative `^`; percent semantics (50% → 0.5, "200 + 10%" → 220); history of last 10 evaluated expressions (tap to re-insert); memory M1/M2/M3 with store/recall/clear + non-empty indicator; ANS key; AC/DEL; error handling (div-zero, sqrt of negative, ln of non-positive, bad factorial, asin/acos |x|>1, overflow, malformed) with "Error" + reason, lockout until AC; two-line display (input line + last result / live preview). Tech: .NET 10, .NET MAUI Blazor Hybrid (BlazorWebView + Razor), xUnit, NO external NuGets — hand-written parser/evaluator in Domain layer; no DataTable.Compute / Roslyn. Windows primary target; mobile/mac must compile but need not be device-tested.

# High-Level Plan: Final Verification Iteration 6

- Scope: fix only the confirmed pull-state violation in `InputBuffer` and stale memory-store selection in `Calculator`; retain existing Domain/Presentation boundaries and all Razor routes.
- Physical components: `SciCalc.Domain` owns both fixes; `SciCalc.Tests` adds one session regression test; audit documents record the review. Presentation remains unchanged.
- New classes/state/associations/methods: none.
- Data flow: `STO` evaluates a non-empty current buffer before falling back to the latest history answer; `HasLiteralOverflow` is queried directly from current number-edit text.
- Test-first: prove an older answer does not override the current expression during memory store, then apply the minimal behavior fix. Existing overflow tests verify the pull property without a behavior change.
- Assumptions: Blazor event callbacks rerender automatically; MAUI workloads/component harness remain unavailable; parser/evaluator behavior outside confirmed defects is unchanged.
- Trade-off: recompute finite parsing on property access rather than retain a mutable cache, favoring correctness and pull-based domain state over an unmeasured micro-optimization.
- Out of scope: naming, display-map deduplication, method extraction, test-helper cleanup, and all other refactoring backlog.
- Acceptance: all derived session properties are pull-based; `STO` stores the current valid expression when present; all Razor mappings remain correct; `dotnet test tests/SciCalc.Tests` passes.

# High-Level Plan: SciCalc (.NET MAUI Blazor Hybrid) — Resume Pass 2: UI, Build Hygiene, Verification

> Applies to commit `de4cef8` (TDD iterations 1-3 committed). Domain logic was re-verified against the spec; plan covers **remaining work only**.

## 1. Architecture / Approach Overview

The committed three-project structure is retained (unchanged):

- **`SciCalc.Domain`** (net10.0 class lib, no deps): complete engine — verified: parser (recursive descent into AST: precedence `+ - * / mod`, right-assoc `^`, unary minus, `n!`/`%` postfix, function calls, unmatched-paren & malformed detection), percent semantics (bare `p/100` vs baseline-scaled on `+`/`-`), all 20 scientific functions with DEG→RAD and RAD→DEG conversions on/in inverse trig, full error taxonomy, `Calculator` aggregate session (`InputBuffer`, `MemoryBank`, `HistoryEntry`, ANS, preview, lockout). **No changes planned** — only a spec-vs-code gap note (§2).
- **`SciCalc`** (MAUI Blazor Hybrid, references Domain): **the only new presentation work** — implement `Components/CalculatorPage.razor` (+ scoped CSS) as the keypad-driven UI over the `Calculator` aggregate; register the aggregate as a DI singleton in `MauiProgram`. No separate Application layer needed — Razor binds straight to the aggregate (thin glue; avoids anemic pass-through services).
- **`SciCalc.Tests`** (xUnit v3, references Domain): **205 tests, all passing** when scoped to the test project. Verification must still pass on `net10.0`.

**Environment constraint (confirmed on this machine):** .NET 10 SDK 10.0.302 present; **zero MAUI workloads installed** (no sudo/workload-install able). Plain `net10.0` TFM in the MAUI csproj pulls the `maui-tizen` workload → the whole-solution `dotnet test` currently fails building `SciCalc.csproj`. Build hygiene must be fixed so root-level `dotnet test` passes (§4, §5).

## 2. Existing State vs. Gap Analysis (at `de4cef8`)

| Exists & verified | Gap to close |
|---|---|
| All enums/VOs (`Token`, `CalculationResult`, `EvaluationContext`, `CalcError` 7 codes, `AngleMode`, `InputKey` full key surface, `MemorySlotId`) | None |
| Parser/evaluator across precedence, parens, unary minus, right-assoc `^`, postfix `!`/`%`, functions, percent — covered by ParserTests/EvaluatorTests/FunctionTests/PercentTests | None (re-verified green) |
| `Calculator` aggregate: buffer render/DEL/AC, preview, Eq→history+ANS, lockout-until-AC, empty-eq→Malformed, memory store/recall/clear independence, angle toggle, ANS-before-answer→0 — covered by CalculatorTests/HistoryTests/MemoryTests | None |
| MAUI shell: `MauiProgram`, `App`, `MainPage` + `BlazorWebView`, `index.html` | **`Components/CalculatorPage.razor` is a 2-line placeholder** → UI implementation |
| `SciCalc.sln` includes Domain, Tests, MAUI | Root `dotnet test` fails on MAUI workload (`maui-tizen` from plain `net10.0` TFM) → fix solution/build hygiene |
| MAUI csproj multi-targeting (`net10.0`, `net10.0-android`, `net10.0-ios`, `net10.0-maccatalyst`, + `net10.0-windows10.0.19041.0` on Windows) | Drop plain `net10.0` TF (Tizen fallback); keep 4 platform TFMs; confirm on a machine with workloads |

## 3. New/Extended Classes (presentation only)

| Class | Responsibilities | New members | Methods (≤3 params; ctor exempt) |
|---|---|---|---|
| `CalculatorPage.razor` (component) | Thin presentation: render `Calculator` state, forward `InputKey` presses | `@inject Calculator` | `Press(InputKey)`, `Restore(HistoryEntry)`, `ErrorText(CalcError?)` |
| `CalculatorPage.razor.css` (scoped CSS) | Keypad grid, display typography, badges, history panel, error banner | — | — |
| `MauiProgram` (edit) | Register Domain aggregate in DI | `builder.Services.AddSingleton<Calculator>()` | — |

No new Domain classes. Error→subtitle mapping is a presentation concern: a static map inside `CalculatorPage` (e.g. `"Cannot divide by zero"`, `"Square root of negative"`, `"Log of non-positive"`, `"Invalid factorial"`, `"asin/acos input out of range"`, `"Overflow"`, `"Malformed expression"`).

## 4. Integration Points / Project Structure

Unchanged file layout; edits only:

```
src/SciCalc/Components/CalculatorPage.razor   (implement)
src/SciCalc/Components/CalculatorPage.razor.css (new)
src/SciCalc/MauiProgram.cs                     (add DI registration)
src/SciCalc/SciCalc.csproj                     (drop plain net10.0 from TargetFrameworks)
SciCalc.sln                                    (remove MAUI project, or keep + document scoped test command — decision below)
README.md                                      (document verification commands & workaround)
```

**Solution-hygiene decision:** remove `SciCalc.csproj` from `SciCalc.sln` so root-level `dotnet test` (build of all sln projects) passes on net10.0 without MAUI workloads; the project file remains in `src/SciCalc/` for building on a machine with workloads. README documents both the removal and the exact commands.

## 5. Implementation Sequence

1. **Solution hygiene**: `dotnet sln SciCalc.sln remove src/SciCalc/SciCalc.csproj`; verify `dotnet test` at root → Domain+Tests green (205 tests).
2. **UI (TDD-lite; manual visual check where possible)**:
   a. Layout in `CalculatorPage`: error banner slot → two-line display (top `Buffer.Text()`, bottom `Preview`/`LastAnswer`) + badges (DEG/RAD from `Mode`; M1/M2/M3 non-empty indicators) → keypad grid → scrollable history panel.
   b. Keypad (all keys emit `Calculator.Press(InputKey)`): main pad `[AC DEL % ÷] [7 8 9 ×] [4 5 6 −] [1 2 3 +] [0 . ANS =]`; scientific pad (sin cos tan, asin acos atan, sinh cosh tanh, log ln, e^x 10^x, x² x³, √ ∛, ^, mod, |x|, 1/x, n!, π, e, `(`, `)`); memory row (M1–M3 store/recall/clear); DEG/RAD toggle.
   c. Error state renders "Error" + mapped subtitle; history items `@onclick → Calculator.RestoreHistory(entry)`.
   d. DI registration + scoped CSS (grid tiles, sticky display, badges, history list).
3. **MAUI csproj**: drop plain `net10.0` from `TargetFrameworks`; keep android/ios/maccatalyst (+ conditional windows).
4. **Verification gate**: root `dotnet test` passes; `dotnet build src/SciCalc/SciCalc.csproj` attempted, failure tolerated here (documented), must pass where `maui` workloads exist (Windows primary).
5. **README update**: run/verify instructions + ANS-before-answer→0 decision + solution-hygiene note.

## 6. Assessment

**Simple enough to implement from this high-level plan only.** Remaining surface = one Razor component + scoped CSS + DI + solution/build hygiene + README. No domain redesign; spec-vs-code re-verification found no gaps beyond UI. A full `/plan-and-design` workflow is **not** required.

## 7. Assumptions, Decisions, Trade-offs

- **No Application layer project**: the aggregate IS the session glue; `MauiProgram` registers it, Razor binds to it. Rich Domain Model preserved — Razor adds zero logic (only `CalcError→subtitle` text map lives in the component).
- **Postfix-style keys** (`x² x³ √ ∛ |x| 1/x n!`): `Calculator.AppendFunction` already emits prefix/wrap-around token sequences as tested; UI merely sends the key — no UI-side wrapping.
- **History tap** uses `Calculator.RestoreHistory(entry)` (buffer replaced with stored token snapshot), ignored while locked — already tested.
- **ANS before first answer → 0**: documented decision, already implemented+tested; README restates it.
- **Solution hygiene**: removing the MAUI project from the sln is chosen vs. keeping-and-scoping, because acceptance says "`dotnet test` must pass on net10.0" at repo scope; slimming TargetFrameworks additionally removes the accidental Tizen fallback.
- **CSS**: Blazor scoped CSS (`CalculatorPage.razor.css`) over global stylesheets; theme = clean default only.
- **No persistence** of history/memory across launches (unchanged scope).

## 8. In Scope / Out of Scope

- **In**: UI implementation, DI, solution/build hygiene, README, verification gate, (optional) any missed §10-style tests discovered during UI walk-through.
- **Out**: persistence, theming, device testing for iOS/Android/macOS, packaging.

## 9. Acceptance Criteria

- `dotnet test` at repo root passes on net10.0 (205 tests; solution builds only Domain+Tests).
- `CalculatorPage` renders: two-line display, DEG/RAD badge always visible, memory badges on non-empty slots, history (≤10) tap-to-re-insert, error shows "Error" + reason, post-error only AC accepted, live preview while typing.
- `SciCalc.csproj` targets only platform TFMs (android/ios/maccatalyst + conditional windows); builds where workloads available (Windows primary).
- README documents verification commands, workloads caveat, and ANS-before-answer decision.

## 10. Test Cases to Implement

Existing 205 tests cover the original §10 checklist (re-verified: precedence/associativity, percent semantics, all functions×both angle modes, full error taxonomy, buffer/DEL/AC/lockout, history cap+restore, ANS, memory independence, angle round-trip, preview). Remaining test actions:

- Re-run full suite after each UI/build-hygiene change (no new Domain behavior expected).
- Any new behavior introduced during UI review (e.g., previously-uncaught edge like `mod` chained or nested function call) → add tests first (TDD discipline preserved).

---
# High-Level Plan: SciCalc (.NET MAUI Blazor Hybrid) — Resume Pass

> This updates the original SciCalc plan below for the committed scaffold at `732575b`.

## 1. Architecture / Approach Overview

Three projects in one solution (already scaffolded):

- **`SciCalc.Domain`** (net10.0 class library, no external deps): the whole calculator engine — hand-written parser to an AST, evaluator, session behavior (input buffer, history, memory, ANS, angle mode, error lockout). Rich Domain Model: behavior lives on entities/value objects.
- **`SciCalc`** (single MAUI Blazor Hybrid project, references `SciCalc.Domain`): BlazorWebView + `CalculatorPage.razor`; pure presentation — buttons send `InputKey` presses, render `Calculator` state.
- **`SciCalc.Tests`** (xUnit, references `SciCalc.Domain` only): the test target.

**Environment constraint:** .NET 10 SDK present on Linux, but **MAUI workloads are NOT installed**. The MAUI csproj may fail `dotnet build` with missing-workload errors — acceptable; it must not block verification. All logic verification runs via `dotnet test` on Domain + Tests (net10.0).

## 2. Existing State vs. Gap Analysis (from `732575b`)

| Exists | Gap to close |
|---|---|
| `Token`, `TokenKind`, `OperatorKind`, `FunctionKind`, `ConstantKind`, `AngleMode`, `MemorySlotId`, `InputKey` (full key surface incl. all functions/memory keys) | Nothing — enums are complete |
| Parser: precedence for +,-,*,/,mod; unary minus; right-assoc `^`; paren match; `BinaryNode`(+,-,*,/, %, pow with zero-division guard), `UnaryMinusNode`, `NumberNode` | **Functions**: `FunctionKind` not yet routed. Add `FunctionNode` + parser branch for `TokenKind.Function` (function consumes following `(...)` arg). **Postfix level missing**: `ParsePower` calls `ParsePrimary` directly; insert `ParsePostfix` between them to handle `!` (factorial) and `%` as postfix operators per the grammar in §7 |
| `Token.Percent()` factory exists | **Percent semantics**: parse-time handling — `p/100` standalone; vs preceding operand for +/− (200 + 10% → 220) |
| `TokenKind.Constant` handled as `NumberNode` today | Fine as-is (π/e are plain numbers at eval time); tests must confirm |
| `CalcError` enum | Verify codes: `AsinAcosOutOfRange`, `InvalidFactorial`, `NegativeSqrt`, `NonPositiveLog` — `FunctionNode` maps each function's domain violation |
| `EvaluationContext(AngleMode, double? Ans)` | Trig angle conversion (`ToRadians`/`ToDegrees`) used by `FunctionNode` |
| Test scaffolds `ParserTests`, `EvaluatorTests`, `TestTokens` (key→token mapping helpers) | Extend `TestTokens` with function/percent tokens; fill in all §10 tests |
| MAUI app shell (`MauiProgram`, `App`, `MainPage`, empty `CalculatorPage`) | UI implementation |
| Nothing for session | **New**: `Calculator` aggregate, `InputBuffer`, `MemoryBank`, `HistoryEntry` |

## 3. New/Extended Classes Planned

| Class | Responsibilities | State/Fields | Methods (≤3 params; ctor exempt) |
|---|---|---|---|
| `Calculator` (aggregate root) | Session state machine: buffer, history, memory, ANS, angle mode, preview, error lockout | `InputBuffer Buffer`, `MemoryBank Memory`, `List<HistoryEntry> History`, `AngleMode Mode`, `CalculationResult LastAnswer`, `CalcError? ActiveError`, `bool Locked`, `CalculationResult Preview` | `Press(InputKey)` — single entry point; `ToggleAngleMode()` |
| `InputBuffer` (entity) | Holds token sequence being typed; DEL semantics | `List<Token> Tokens` | `Add(Token)`, `RemoveLastToken()`, `Clear()`, `Text()` |
| `MathExpression` (existing) | Extend parser: `TokenKind.Function` branch creating `FunctionNode`; `%` postfix handling; existing precedence/parenthesis logic preserved | token list | `Evaluate(EvaluationContext)` |
| `FunctionNode` (new Node) | One-arg function evaluation with per-function domain checks + angle conversion | `FunctionKind Kind`, `Node Arg` | `EvaluateNode(EvaluationContext)` |
| `PercentNode` (optional; else in-parser rewrite) | Contextual percent (baseline scale or literal /100) | `Node Base` | `EvaluateNode(EvaluationContext)` — simpler: rewrite to `BinaryNode(Mul, base, NumberNode(0.01))` except +/− context |
| `EvaluationContext` (existing VO) | Mode + ANS | add `ToRadians`/`ToDegrees` helpers | optional `ToRadians`/`ToDegrees` helpers |
| `MemoryBank` (entity) | 3 slots as `double?` fields | `M1/M2/M3` | `Store(value, slot)` — ≤3 params ok, `Recall(slot)`, `Clear(slot)`, `IsNonEmpty(slot)` |
| `HistoryEntry` (VO) | Expression + result | `ExpressionText`, `Value` | — |

## 4. Data Flow / Control Flow

- **Preview**: every `Press` → recompute `Preview = MathExpression(Buffer).Evaluate(ctx)`; malformed in-progress → blank preview, no lockout.
- **Postfix-style function keys** (x², x³, √, ∛, 1/x, n!): `Calculator.Press` translates these into prefix function-call tokens — e.g., pressing `Square` after `5` emits `Function(Square), OpenParen, <current buffer tokens>, CloseParen` so the parser always sees `func(expr)` form. The exact wrapping strategy is decided during `Calculator` implementation; the parser grammar remains purely prefix for functions.
- **Equals**: success → push `HistoryEntry` (cap 10, FIFO), set `LastAnswer`, clear buffer; error → set `ActiveError` + `Locked=true`.
- **Lockout**: `Press` early-returns for any key ≠ `AllClear` while `Locked`. UI renders "Error" title + reason subtitle from `ActiveError`.
- **Angle**: `DegRadToggle` flips `Mode`; `FunctionNode` converts DEG↔RAD for trig & inverse-trig.
- **Memory**: Store uses last evaluated answer (or current preview); Recall inserts `Token.Number`; per-slot isolation.
- **History tap**: re-lex the stored `ExpressionText` into tokens and replace buffer (decided during Calculator implementation; re-lexing keeps `HistoryEntry` as pure data).

## 5. Implementation Sequence

1. **Parser/evaluator completion (TDD)**: add `FunctionNode`, route `TokenKind.Function`, percent semantics, all error codes; extend via `tests/SciCalc.Tests` (ParserTests/EvaluatorTests/TestTokens).
2. **Calculator session (TDD)**: `InputBuffer`, `MemoryBank`, `HistoryEntry`, `Calculator.Press` with lockout/preview/history/ANS.
3. **UI**: implement `CalculatorPage.razor` — keypad grid, two-line display, DEG/RAD badge, memory non-empty badges, history list; register `Calculator` singleton in `MauiProgram`.
4. **Verification**: `dotnet test` as the quality gate; attempt MAUI csproj build, tolerate workload-missing failure; document in README if workaround needed.

## 6. Assessment

**Simple enough to implement from this high-level plan only.** The scaffold exists, the new surface is bounded (one aggregate + a handful of small entities + one UI component), and the test checklist here is enumerated to the level of a design. A full `/plan-and-design` workflow is **not** required.

## 7. Assumptions, Decisions, Trade-offs

- **Parser grammar** (existing + extension): `expr := term((+|-) term)*`; `term := unary((*|/|mod) unary)*`; `unary := '-'* power`; `power := postfix('^' unary)?` (right-assoc via recursive unary); `postfix := primary('!'/'%')*`; `primary := number | const | function '(' expr ')' | '(' expr ')'`.
- **Percent**: parse-time handling; for the preceding `+`/`-` case apply baseline-scale, otherwise literal `p/100` (50% standalone → 0.5; 200+10% → 220). Decide exact rewrite in tests.
- **Overflow**: Infinity/NaN normalization already in `MathExpression.Normalize`; factorial capped at 170 (boundary-tested).
- **ANS with no history**: plan: insert `0` literal (deterministic); document in README.
- **No persistence** across launches for history/memory.

## 8. In Scope / Out of Scope

- **In**: 14 MVP features, Domain-only TDD verification, UI component implemented (compiles where workloads permit).
- **Out**: persistence, device testing for mobile/mac, packaging/Store deployment, theming.

## 9. Acceptance Criteria

- `dotnet test` green on Domain + Tests covering §10.
- MAUI csproj `dotnet build` attempted; workload-missing failures tolerated and not blocking.
- UI: DEG/RAD badge always visible; memory non-empty badges; history (≤10) tap to re-insert; error title + reason subtitle; post-error only AC accepted.

## 10. Test Cases to Implement

### Parser/Evaluator — precedence/associativity (partially exists; verify & extend)
- `2+3*4` → 14; `(2+3)*4` → 20; nested parens `(((1+2)))` → 3; `2^3^2` → 512 (right-assoc); `-2+3` → 1; `2*-3` → -6; `-(-3)` → 3; `-(2+3)` → -5.
- Percent: `50%` → 0.5; `200+10%` → 220; `200-10%` → 180; `200*10%` → 20; `200/10%` → 2000.
- Functions: sin/cos/tan + asin/acos/atan in both RAD and DEG contexts; hyperbolic sanity values; log10/ln; e^x, 10^x; x²/x³; √/∛ (∛ of negative valid); x^y; n!; |x|; 1/x; mod (incl. negative mod).
- Constants π/e literal values.
- Errors mapped to codes: `/0` and `mod 0` → DivisionByZero; √ of negative → NegativeSqrt; ln/log10 of ≤0 → NonPositiveLog; asin/acos out of [-1,1] → AsinAcosOutOfRange; non-integer/negative factorial → InvalidFactorial; 171!, e^1000, 10^1000, 2^10000 → Overflow; unmatched parens, `1+`, `)(`, `++1`, empty-on-eq → Malformed.

### Calculator session
- Buffer append/delete/render; DEL last-token only; DEL on empty → no-op; AC resets buffer+lock+preview.
- Lockout: after Eq error, only AC accepted; preview unaffected.
- Eq on empty buffer → Malformed → lockout.
- History: 12 evaluations → count 10, oldest evicted; tap-restore replaces buffer.
- ANS: inserts last result literal; no previous result → 0 (documented decision).
- Memory: store/recall/clear per-slot independence; empty-recall no-op; IsNonEmpty flips; ClearM1 untouched M2.
- Angle toggle round-trip; sin(90)=1 in DEG, sin(π/2)=1 in RAD.
- Live preview updates per keypress; malformed in-progress → blank preview, not lockout.

---

# High-Level Plan: SciCalc (.NET MAUI Blazor Hybrid)

## 1. Architecture / Approach Overview

Three projects in one solution; the app itself is a **single MAUI Blazor Hybrid project**:

- **`SciCalc.Domain`** (net10.0 class library, no external deps): the whole calculator engine — tokenization of key presses, hand-written recursive-descent parser to an AST, evaluator, session behavior (input buffer, history, memory, ANS, angle mode, error lockout). Rich Domain Model: behavior lives on entities/value objects, not in pump-off services.
- **`SciCalc`** (single MAUI Blazor Hybrid project, references `SciCalc.Domain`): BlazorWebView + Razor component `CalculatorPage.razor`; registers the `Calculator` aggregate as a singleton in `MauiProgram.cs`; pure presentation layer — buttons send `InputKey` presses, render state.
- **`SciCalc.Tests`** (xUnit, references `SciCalc.Domain` only): the test target. Domain is split from the MAUI project specifically so the test project need not multi-target MAUI TFMs.

Interaction: key press in Razor → `Calculator.Press(InputKey)` → mutate `InputBuffer` tokens → (live) `MathExpression.Evaluate(context)` → preview shown → Equals → history/ANS updated. Errors flip `Calculator` into locked error state; only AC clears it.

## 2. New Classes Planned

| Class | Responsibilities | New State/Fields | Associations | Methods (≤3 params; ctor exempt) |
|---|---|---|---|---|
| `Calculator` (aggregate root entity) | Session state machine: buffers key presses, routes evaluation, maintains history/memory/ANS/angle mode, error lockout | `InputBuffer Buffer`, `MemoryBank Memory`, `List<HistoryEntry> History`, `AngleMode Mode`, `CalculationResult LastAnswer`, `CalcError? ActiveError`, `bool Locked`, `CalculationResult Preview` | owns `InputBuffer`, `MemoryBank`, `HistoryEntry` list | `Press(InputKey key)` (single entry point); `ToggleAngleMode()`; private `Evaluate(Expression)` invoked on Eq/press |
| `InputBuffer` (entity) | Holds typed token sequence being built; enforces DEL semantics | `List<Token> Tokens` | owns `Token` value objects | `Add(Token)`, `RemoveLastToken()`, `Clear()`, `Text()` -> display string |
| `Token` (value object) | One keypad token: kind + optional numeric value/function name | `TokenKind Kind`, `double? NumericValue`, `FunctionKind? Function` | owned by `InputBuffer` / `MathExpression` | factories: `Token.Number(double)`, `Token.Operator(OperatorKind)`, `Token.Function(FunctionKind)`, `Token.OpenParen()`, `Token.CloseParen()`, `Token.Percent()`, `Token.Constant(ConstantKind, double)`; equality by kind+value |
| `MathExpression` (value object) | Owns immutable token list + hand-written recursive-descent parse and evaluation; no identity — equality by token sequence | `IReadOnlyList<Token> Tokens` | produces `Node` AST internally | `Evaluate(EvaluationContext ctx)` -> `CalculationResult`; internal parse guards: unmatched parens, malformed → `CalcError.Malformed` |
| `Node` (abstract AST hierarchy + common base) | Composite evaluating AST; each node type knows how to evaluate itself in context | varies per subclass | composite tree | `EvaluateNode(EvaluationContext)` per subclass: `NumberNode`, `UnaryMinusNode(inner)`, `BinaryNode(op, left, right)`, `FunctionNode(kind, arg)`, `PercentNode(previousOperand?)` |
| `EvaluationContext` (value object) | Mode + last-answer substitution (read-only data carrier; conversion logic lives on `FunctionNode`, not here) | `AngleMode Mode`, `double? Ans` | passed into all Evaluate calls | `ToRadians(double degrees)`, `ToDegrees(double radians)` helper methods (pure convenience, no state mutation) |
| `CalculationResult` (value object) | Success value or error reason | `double? Value`, `CalcError? Error` | returned from evaluate, stored in history | `Ok(double)`, `Fail(CalcError)` factories |
| `CalcError` (enum) | Enumerates domain error codes | — | reason mapped to UI subtitle | members: `DivisionByZero, NegativeSqrt, NonPositiveLog, InvalidFactorial, AsinAcosOutOfRange, Overflow, Malformed` |
| `AngleMode` (enum) | Deg/Rad | — | stored on `Calculator`, feeds `EvaluationContext` | — |
| `MemoryBank` (entity) | Coordinates three memory slots (three `double?` fields — no separate `MemorySlot` class needed; YAGNI) | `double? M1`, `double? M2`, `double? M3` | standalone | `Store(MemorySlotId id, double v)`, `Recall(MemorySlotId id)` -> double?, `Clear(MemorySlotId id)`, `IsNonEmpty(MemorySlotId id)` |
| `HistoryEntry` (value object) | Immutable record of expression + result | `string ExpressionText`, `double Value`, `DateTime At` | owned by `Calculator.History` (capped 10) | — |

Enum kinds: `InputKey` (Digit0..Digit9, Dot, Add/Sub/Mul/Div/Pow/Mod, OpenParen/CloseParen, each function kind, Percent, Pi/E, Ans, AC, DEL, Eq, StoreM1/StoreM2/StoreM3, RecallM1/RecallM2/RecallM3, ClearM1/ClearM2/ClearM3, DegRadToggle), `TokenKind`, `OperatorKind` (+,-,*,/,^,mod), `FunctionKind`, `ConstantKind`, `MemorySlotId` (M1..M3).

Behavior placement honored: parsing/evaluation live **on `MathExpression` and `Node` subclasses** (composite pattern), session semantics on **`Calculator`**, buffer editing on **`InputBuffer`**, memory on **`MemoryBank`** (three `double?` fields, no separate `MemorySlot` class) — no anemic DTOs or static helper blobs; Razor is a thin presentation shell. Percent semantics (`PercentNode`) resolved contextually at parse time using the preceding operand when adjacent to +/− and as `p/100` otherwise ("200 + 10%" → 220; "50%" alone → 0.5). Angle conversions performed by `FunctionNode`, not by `EvaluationContext`.

## 3. Data Flow / Control Flow

- **Key press → preview**: Razor button `@onclick` → `Calculator.Press(InputKey.Sin)` → `InputBuffer.Add(Token.Function(...))` → Calculator recomputes `Preview = MathExpression(Buffer.Tokens).Evaluate(new EvaluationContext(Mode, Ans))` (on failure preview is blank, no lockout) → Blazor re-renders top line `Buffer.Text()` and bottom line via `Preview`.
- **Equals**: `Press(Eq)` → evaluate buffer → on success: `LastAnswer = result`, push `HistoryEntry` (cap 10, FIFO eviction), display result, clear buffer → on error: set `ActiveError`, lock input (`Locked = true`) until AC.
- **Error lockout**: `Press(key)` early-returns for any key ≠ AC while `Locked`.
- **Angle mode**: toggling DegRadToggle flips `Mode`; `FunctionNode.EvaluateNode` converts DEG→RAD before calling trig functions and RAD→DEG after calling inverse-trig functions, reading `AngleMode` from `EvaluationContext`.
- **Memory**: `InputKey.StoreM1/StoreM2/StoreM3` → `Calculator.Press` routes to `MemoryBank.Store(slotId, LastAnswer/Preview)`; `RecallM1/M2/M3` → `MemoryBank.Recall(slotId)` → inserts `Token.Number(value)` into buffer; `ClearM1/M2/M3` → `MemoryBank.Clear(slotId)`; indicator rendered from `MemoryBank.IsNonEmpty`.
- **ANS**: inserts `Token.Number(LastAnswer.Value)`.
- **History tap**: Razor item click → buffer replaced with expression tokens (tokens re-lexed from stored `ExpressionText` — or Buffer snapshot stored in HistoryEntry) → user edits/equals.
- **DEL**: `InputBuffer.RemoveLastToken()`; **AC**: clears buffer + error lock + preview.

## 4. Integration Points / Project Structure (Greenfield)

Repo contains only `README.md`, `Docs/_Current/prompt.md`, `appsettings.json`, `.gitignore`; create:

```
SciCalc.sln
src/SciCalc.Domain/
  AngleMode.cs, CalcError.cs, EvaluationContext.cs, CalculationResult.cs,
  Calculator.cs, InputBuffer.cs, Token.cs, TokenKind.cs, InputKey.cs,
  MemoryBank.cs, MemorySlotId.cs, HistoryEntry.cs,
  MathExpression.cs, Nodes/Node.cs, Nodes/NumberNode.cs, Nodes/UnaryMinusNode.cs,
  Nodes/BinaryNode.cs, Nodes/FunctionNode.cs, Nodes/PercentNode.cs,
  Operators/OperatorKind.cs, Functions/FunctionKind.cs, ConstantKind.cs
src/SciCalc/                    (MAUI Blazor Hybrid, ProjectReference → Domain)
  MauiProgram.cs, App.cs? (per MAUI template), MainPage.razor with BlazorWebView,
  Components/CalculatorPage.razor (+ .css), wwwroot/index.html
tests/SciCalc.Tests/            (xUnit, ProjectReference → Domain)
  ParserTests.cs, EvaluatorTests.cs, CalculatorTests.cs, MemoryTests.cs, HistoryTests.cs
```

`.gitignore` verified sufficient for `bin/obj` (no extra artifacts; SQLite-free app).

## 5. Implementation Sequence

1. Scaffold sln + 3 projects; Domain value objects + enums (`Token`, `CalculationResult`, `EvaluationContext`) TDD'd.
2. `MathExpression` parser/evaluator TDD'd across all functions/precedence/error-paths.
3. `Calculator` session (buffer/history/memory/ANS/lockout/preview) TDD'd.
4. MAUI Blazor UI: keypad grid Razor component, two-line display, DEG indicator, history scroll list, memory indicators; DI singleton wiring.
5. Build on all 4 TFMs (Windows primary; Android/iOS/MacCatalyst compile-only gate).

## 6. Assessment

**Simple enough to implement from this high-level plan** — one bounded context, no external services, all logic in well-known classes. A full `/plan-and-design` workflow is **not** required; the detailed test surface is enumerated in §10 to de-risk implementation.

## 7. Assumptions, Decisions, Trade-offs

- **Hand-written parser**: recursive descent, grammar `expr := term ((+|-) term)*`; `term := unary ((*|/|mod) unary)*`; `unary := '-'* power`; `power := postfix ('^' power)?` (right-assoc); `postfix := primary (n! | %)*`; `primary := number | const | func '(' expr ')' | '(' expr ')'`.
- **Percent semantics**: `PercentNode` resolves versus preceding operand for +/− (200 + 10% → 220) and literal p/100 otherwise (50% → 0.5).
- **Overflow**: |result| ≥ `double` max, Infinity/NaN → `Overflow` error; factorial limited to non-negative integers ≤ 170 (beyond → `Overflow`).
- **Trig conversions** performed by `FunctionNode` using `EvaluationContext.Mode` (DEG↔RAD); `EvaluationContext` provides convenience `ToRadians`/`ToDegrees` helpers but holds no mutable state.
- **History re-insertion**: `HistoryEntry` stores both `ExpressionText` and (optionally) token snapshot; tap replaces buffer.
- **MAUI ProjectReference**: domain split is required for `xUnit` against net10.0; MAUI stays single-project at presentation level.
- **DI**: `Calculator` singleton in `MauiProgram.cs`; component subscribes to `Calculator` changed event only if needed (Blazor re-renders on UI events automatically; history panel refresh uses the same).
- **No app-state persistence** (history/memory are session-scoped; restartable).

## 8. In Scope / Out of Scope

- **In**: the 14 MVP features, MAUI Blazor Hybrid single app, xUnit tests on Domain, compile gate for all TFMs.
- **Out**: persistence of history/memory across launches, graphing/programming, localization, theming beyond clean default, packaging/Store deployment, mobile device testing.

## 9. Acceptance Criteria

- `dotnet build` succeeds for Windows; and other TFMs compile (`-t:android/-t:ios/-t:maccatalyst` subject to SDK availability).
- All MVP behaviors e2e via UI on Windows; `dotnet test` green over §10.
- UI always shows DEG/RAD badge; memory badges reflect non-empty slots; history (up to 10) tap-to-re-insert; error shows "Error" + reason subtitle; post-error non-AC keys ignored.

## 10. Test Cases to Implement

### Parser/Evaluator — precedence/associativity
- `2+3*4` → 14; `(2+3)*4` → 20; nested parens `(((1+2)))` → 3; `2^3^2` → 512 (right-assoc); `-2+3` → 1; `2*-3` → -6; `-(-3)` → 3; unary minus at start `-(2+3)` → -5.

### Percent
- `50%` → 0.5; `200+10%` → 220; `200-10%` → 180; `200*10%` → 20; `200/10%` → 2000.

### Functions (RAD default + DEG when set)
- sin(π/2) = 1; sin(90° in DEG) = 1; asin(1)=π/2 (RAD)/90 (DEG); cos(0)=1; cos(π)=-1; tan(0)=0; acos(1)=0; atan(1)=π/4.
- sinh(0)=0; cosh(0)=1; tanh(0)=0; sinh(1)≈1.1752; cosh(1)≈1.5431; tanh(1)≈0.7616.
- log10(100)=2; log10(1)=0; ln(e)=1; ln(1)=0.
- e^0=1; e^1≈2.71828; e^2≈7.389056; 10^0=1; 10^2=100.
- x²: 3²=9; (-3)²=9; 0²=0. x³: 2³=8; (-2)³=-8.
- √(4)=2; √(0)=0; √(1)=1. ∛(27)=3; ∛(-8)=-2 (valid); ∛(0)=0.
- 2^10=1024; 0^0=1 (IEEE convention).
- 0!=1; 1!=1; 5!=120; 170!=valid (max); |x|: abs(-3)=3; abs(0)=0; abs(3)=3.
- 1/x: 1/4=0.25; 1/(-2)=-0.5. mod: 10 mod 3=1; -10 mod 3=-1 (C# `%` semantics).
- Constants: π≈3.14159265; e≈2.71828182.

### Errors (each → CalcError code)
- `1/0` → DivisionByZero; `1/x` where x=0 → DivisionByZero; `10 mod 0` → DivisionByZero.
- `√(-1)` → NegativeSqrt; `√(-0.001)` → NegativeSqrt.
- `ln(0)` → NonPositiveLog; `ln(-2)` → NonPositiveLog; `log10(0)` → NonPositiveLog; `log10(-1)` → NonPositiveLog.
- `asin(2)` → AsinAcosOutOfRange; `acos(-2)` → AsinAcosOutOfRange; `asin(-1.001)` → AsinAcosOutOfRange; `acos(1.001)` → AsinAcosOutOfRange. Boundary: `asin(1)` and `asin(-1)` succeed.
- `1.5!` → InvalidFactorial; `(-2)!` → InvalidFactorial; `(-1)!` → InvalidFactorial.
- `171!` → Overflow; `200!` → Overflow; `2^10000` → Overflow; `e^1000` → Overflow; `10^1000` → Overflow.
- `(1+2` → Malformed; `1+` → Malformed; `)(` → Malformed; empty expr on Eq → Malformed; `)` alone → Malformed; `++1` → Malformed.

### Calculator session
- Buffer append/delete/render; DEL removes only last token; DEL on empty buffer → no-op (no crash); AC resets buffer+lock+preview.
- Lockout: after Eq error, only AC accepted; all other keys ignored while locked; preview unaffected by lock.
- Equals on empty buffer → Malformed error → lockout.
- History cap: 12 Eq evaluations → History.Count = 10; oldest evicted; tap-restore into buffer replaces current buffer.
- ANS inserts last result as literal; ANS when no previous result → inserts 0 (or no-op — decision to be documented).
- Memory: store/recall/clear per slot independent; StoreM1 and StoreM2 are independent; RecallM1 after StoreM1 returns stored value; ClearM1 does not affect M2; IsNonEmpty flips correctly per slot; Recall from empty slot → no-op / inserts nothing.
- Angle toggle flips mode, affects trig consistency; toggle DEG→RAD→DEG round-trip; verify sin(90) gives 1 in DEG and sin(π/2) gives 1 in RAD.
- Live preview updates on every keypress; preview failure (malformed in-progress expression) shows blank, not error lockout.

---
# User Prompt

Create a high level plan for this task (high level plan is enough, do not run a full plan and design workflow): "Build a URL shortener REST API with base62 encoding, rate limiting, and SQLite storage using EF Core. Include a simple but polished static HTML page (no framework, no build step) for creating short links and following redirects. The page should be slick and minimal — one input, one button, a clean list of created short links, and click-to-redirect."

# High-Level Plan: URL Shortener (ASP.NET Core + EF Core/SQLite + Static HTML)

## 1. Architecture / Approach Overview

A **single ASP.NET Core web project** (minimal APIs) that serves both the REST API and the static HTML page. Physical components:

- **Minimal API host** (`Program.cs`): registers routing, static files, rate limiting middleware, EF Core.
- **Domain model**: a `ShortLink` entity that owns its behavior (URL validation, short-code assignment, click tracking) — no service blobs. `ShortCode` value object encapsulates base62 encoding/decoding and code validation (replaces a static utility class).
- **Persistence**: EF Core `ShortenerDbContext` + SQLite (`Microsoft.EntityFrameworkCore.Sqlite`), one table, file `shortener.db`. Endpoints inject `ShortenerDbContext` directly (no repository abstraction needed at this scope).
- **Rate limiting**: built-in ASP.NET Core `System.Threading.RateLimiting` middleware, fixed-window per client IP, applied to the create endpoint (429 on excess).
- **Frontend**: `wwwroot/index.html` — hand-written HTML/CSS/JS, no framework, no build step. Served via `UseStaticFiles` + `UseDefaultFiles`; API under `/api`, redirect route at root `/{code}`.

Interaction: Browser loads `index.html` → JS calls `POST /api/links` / `GET /api/links` → endpoints use domain entity + `ShortenerDbContext` → SQLite. Clicking a short link hits `GET /{code}` → 302 redirect to original URL.

## 2. New Classes Planned

| Class | Responsibilities | New State/Fields | Associations | Methods |
|---|---|---|---|---|
| `ShortLink` (domain entity) | Owns a shortened URL: validation, code derivation, click counting | `long Id`, `string OriginalUrl`, `ShortCode Code`, `DateTime CreatedAt`, `int ClickCount` | owns `ShortCode` value object | `ShortLink(string originalUrl)` ctor — validates absolute http/https URL, throws on invalid; `AssignCode(long id)` — sets `Code = ShortCode.FromId(id)` (called by endpoint after save assigns the auto-increment Id); `RegisterClick()` — increments `ClickCount` |
| `ShortCode` (value object) | Encapsulates a base62 short code: encoding, decoding, validation, equality | `string Value` (the base62 string), alphabet const `[0-9a-zA-Z]` | owned by `ShortLink` | `ShortCode.FromId(long id)` — factory; encodes id to base62, throws on negative; `ShortCode.Parse(string code)` — factory; validates base62 chars, throws on null/empty/invalid; `ToString()` → `Value`. EF Core maps via value conversion (`ShortCode` ↔ `string` column). |
| `ShortenerDbContext` | EF Core session/Unit of Work | `DbSet<ShortLink> Links` | maps `ShortLink` | `OnModelCreating` — configure keys/index on `Code` (unique) |
| Endpoints (`LinkEndpoints` static class) | HTTP plumbing only — parse, delegate, map status codes | none | injects `ShortenerDbContext` directly (no repository abstraction — YAGNI for a single-project app with one persistence implementation) | `MapLinkEndpoints(WebApplication)`: `POST /api/links`, `GET /api/links`, `GET /{code}` |
| DTOs (`CreateLinkRequest`, `LinkResponse`) | API contract decoupled from entity | `Url`, `Code`, `OriginalUrl`, `ShortUrl`, `ClickCount` | mirrors entity outward | none |
| Rate limit config (in `Program.cs`) | Throttle link creation | policy: e.g. 20 requests / 60 s per IP, fixed window | applied to POST | `AddRateLimiter(...)` registration + `RequireRateLimiting` on POST |
| `index.html` (static) | UI: one input, one button, list of links | — | calls REST API | fetch create/list, render list, anchor click → redirect |

Behavior placement honored: code derivation and validation live **on `ShortCode` value object** (owned by `ShortLink`), not in a static utility or service; the endpoint is a thin coordinator. No `IShortLinkRepository` interface — endpoints use `ShortenerDbContext` directly, keeping the codebase simple.

## 3. Data Flow / Control Flow

- **Create**: `POST /api/links {url}` → rate-limit middleware → DTO → `new ShortLink(url)` (validates; 400 on failure) → `db.Links.Add` + `SaveChanges` (gets auto-increment `Id`) → `link.AssignCode(link.Id)` → `SaveChanges` → `201 {code, shortUrl}`. (Two saves required because code derives from auto-increment Id; this is intentional and acceptable for simplicity.)
- **Redirect**: `GET /{code:regex(^[0-9a-zA-Z]+$)}` → `db.Links.FirstOrDefault(l => l.Code == code)` → 404 if missing → `link.RegisterClick()` → save → `302 Location: link.OriginalUrl`. Route constraint ensures only base62-valid codes reach this endpoint; other paths fall through to static files or 404.
- **List**: `GET /api/links` → `db.Links` query (latest N, ordered by `CreatedAt` descending) → 200 DTO array (empty array `[]` when no links exist) → page renders.
- **UI**: on load `GET /api/links`; button `POST`s; list items are anchors to the short URL (click → 302).

## 4. Integration Points / Project Structure (Greenfield)

Repo is effectively empty (README.md and appsettings.json are Digital Worker tooling files, not application code); create:

```
UrlShortener/                (web project)
  Program.cs
  Domain/ShortLink.cs, ShortCode.cs
  Data/ShortenerDbContext.cs
  Api/LinkEndpoints.cs, Dtos.cs
  wwwroot/index.html
UrlShortener.Tests/          (xUnit)
UrlShortener.sln
```

Existing files (`README.md`, `appsettings.json`) are untouched; `.gitignore` already suits .NET build output but **must be updated** to exclude `*.db` (SQLite database files).

## 5. Implementation Sequence

1. Solution + projects scaffold; update `.gitignore` to add `*.db`; `ShortCode` value object + `ShortLink` entity with unit tests (TDD).
2. EF Core: `ShortenerDbContext`, SQLite wiring, `EnsureCreated` on startup.
3. Endpoints (create/list/redirect with route constraint) + integration tests.
4. Rate-limiting policy + 429 test.
5. Static `index.html` + manual verification.

## 6. Assessment

**Simple enough to implement from this high-level plan** — one entity, one value object, one DbContext, three endpoints, one static page; no complex associations or cross-aggregate logic. A full `/plan-and-design` workflow is **not** required.

## 7. Assumptions, Decisions, Trade-offs

- **No architecture doc**: `Docs/architecture.md` does not exist in this repo. This plan is self-contained.
- **Code generation**: derive code from SQLite autoincrement `Id` via `ShortCode.FromId(id)` (base62) → zero collisions by construction, no retry loop. Trade-off: sequential/predictable codes (acceptable, no auth demo).
- **Two-save create flow**: First save to get auto-increment `Id`, then `AssignCode(id)` + second save. Acceptable for simplicity; the alternative (pre-generating codes) adds complexity.
- **Code length**: 1–7 chars, grows naturally from Id; no fixed padding.
- **Alphabet**: `[0-9a-zA-Z]`; `ShortCode.Parse` validates membership.
- **Route constraint**: `GET /{code:regex(^[0-9a-zA-Z]+$)}` to prevent the catch-all from swallowing favicon, robots.txt, or other non-API paths.
- **No repository abstraction**: endpoints use `ShortenerDbContext` directly. For this scope (3 endpoints, one persistence target, integration-tested with WebApplicationFactory) an `IShortLinkRepository` is YAGNI.
- **Rate limiting**: built-in in-memory middleware (single instance). Distributed store (Redis) out of scope.
- **Storage init**: `Database.EnsureCreated()` for simplicity vs. migrations (optional upgrade path noted).
- **Duplicates**: same long URL may be submitted multiple times → new short link each time (simplest semantics).
- **Custom slugs**: out of scope (generated codes only).
- **No auth, no expiry, no deletion.**

## 8. In Scope / Out of Scope

- **In**: create/list/redirect API, base62 codes, per-IP rate limiting (429), SQLite persistence, click counting, polished single-page UI (input, button, link list, click-to-redirect), automated tests.
- **Out**: authentication/users, custom aliases, link expiry/deletion, analytics UI, distributed rate limiting, Docker/deployment, multiple frameworks or build steps for frontend.

## 9. Acceptance Criteria

- `POST /api/links` with valid absolute http(s) URL → 201 with base62 `code` and `shortUrl`; invalid URL → 400.
- `GET /{code}` → 302 to original URL; unknown code → 404; click count increments.
- `GET /api/links` → JSON list of created links (code, url, clicks).
- Exceeding POST rate limit from one IP → 429.
- `index.html` loads at `/`, creates and lists links without a build step, links are clickable and redirect.
- All tests pass: `dotnet test`.

## 10. Test Cases to Implement

### Unit — ShortCode (value object)
- `ShortCode.FromId(0)` → `Value` is `"0"` (zero edge)
- `ShortCode.FromId(1)` → `"1"`, `FromId(9)` → `"9"`, `FromId(10)` → `"a"` (digit-to-letter transition)
- `ShortCode.FromId(61)` → last single-char code `"Z"` (upper boundary of 1-char codes)
- `ShortCode.FromId(62)` → first two-char code `"10"` (carry boundary)
- `ShortCode.FromId(63)` → `"11"` (second two-char code)
- `ShortCode.FromId(3843)` → last two-char code (upper boundary of 2-char codes)
- `ShortCode.FromId(3844)` → first three-char code (carry boundary)
- `ShortCode.FromId(long.MaxValue)` → valid string without overflow
- `ShortCode.FromId(negative)` → throws `ArgumentOutOfRangeException`
- `ShortCode.Parse("0")` → `Value` is `"0"`, `Parse("Z")` → `"Z"` (single-char boundaries)
- `ShortCode.Parse("10")` → valid (two-char boundary)
- `ShortCode.Parse("")` → throws (empty string)
- `ShortCode.Parse(null)` → throws
- `ShortCode.Parse("abc!@#")` → throws (invalid characters)
- `ShortCode.Parse("abc def")` → throws (whitespace in code)
- Round-trip: `ShortCode.Parse(ShortCode.FromId(n).Value)` equals `ShortCode.FromId(n)` for values 0, 1, 61, 62, 63, 3843, 3844, 100000, `long.MaxValue`
- Equality: two `ShortCode` instances with same value are equal (`Equals`, `==`, `GetHashCode`)

### Unit — ShortLink
- Ctor with valid `http://` URL → succeeds, sets `OriginalUrl`, `CreatedAt` set, `ClickCount` = 0, `Code` is null
- Ctor with valid `https://` URL → succeeds
- Ctor with `null` → throws
- Ctor with `""` (empty string) → throws
- Ctor with `"   "` (whitespace-only) → throws
- Ctor with `"example.com"` (no scheme) → throws
- Ctor with `"ftp://example.com"` (non-http scheme) → throws
- Ctor with relative URL `"/path/page"` → throws
- `AssignCode(id)` with known id → sets `Code` to `ShortCode` matching `ShortCode.FromId(id)`
- `AssignCode` with `id = 0` → sets `Code` to `ShortCode` with value `"0"` (edge case: auto-increment typically starts at 1 but entity must handle it)
- `RegisterClick()` once → `ClickCount` = 1
- `RegisterClick()` three times → `ClickCount` = 3

### Integration (WebApplicationFactory, in-memory/temp SQLite)
- `POST /api/links` with valid https URL → 201 with `code` and `shortUrl` in response body
- `POST /api/links` with valid http URL → 201 (both schemes accepted)
- `POST /api/links` with empty body → 400
- `POST /api/links` with malformed JSON → 400
- `POST /api/links` with `url: ""` → 400
- `POST /api/links` with `url: "not-a-url"` → 400
- `POST /api/links` with `url: "ftp://x.com"` → 400
- Two sequential creates → each gets unique code
- `GET /{code}` for existing link → 302 with correct `Location` header
- `GET /{code}` for unknown code → 404
- `GET /abc!@#` (invalid base62 chars in path) → does not match redirect route (falls through to 404 or static files)
- `GET /` → serves `index.html` (default file), not redirect endpoint
- `GET /api/links` when no links exist → 200 with empty JSON array `[]`
- `GET /api/links` after creating N links → returns all N in response, ordered by most recent first
- Click count increments: create link → redirect via `GET /{code}` → `GET /api/links` → verify `clickCount` = 1
- Multiple redirects increment count accurately: redirect 3 times → verify `clickCount` = 3

### Rate-limit integration
- Burst POSTs up to policy limit → all succeed (200/201)
- Burst POSTs at limit+1 → the excess request returns 429
- Requests from different IPs (simulated via headers or test config) are rate-limited independently
- After the fixed window expires, requests succeed again (use a test-friendly small window)

### Concurrency
- Two simultaneous `POST /api/links` requests → both succeed with distinct codes (no duplicate code collision)
