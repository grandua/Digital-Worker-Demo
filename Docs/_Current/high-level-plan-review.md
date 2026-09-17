# High-Level [****] Review: SciCalc — 7 E2E defect fixes

**Reviewer:** [****] workflow
**Date:** 2026-09-17
**Verdict:** APPROVE

## Scope verified

The [****] at the top of `Docs/_Current/prompt.md` (lines 1-51, titled "# High-Level [****]: SciCalc — 7 E2E defect fixes") addresses all 7 defects from the user prompt. Implementation has been reviewed in the actual changed files.

## Issue-by-Issue Verification

### Issue 1: Operators after `=` start from an empty expression and error
- **Root cause in [****]:** `Calculator.EvaluateEquals` clears the buffer; next operator on an empty token list causes parser to throw `ParseFailure` -> `Malformed`. **Verified correct** (`Calculator.cs:187-194` — `Buffer.Clear()` then `seedPending = true`).
- **Fix:** `seedPending` flag + `SeedAnswer` method inserts `LastAnswer` before the operator. **Verified correct** (`Calculator.cs:77,99,196-214`).
- **Tests:** 6 operator cases via `[Theory]`, plus two continued-evaluation facts (`2+3=+4=`->9, `2+3=*2=`->10). **Adequate.**

### Issue 2: Scientific functions after `=` create invalid expressions
- **Root cause:** Same as issue 1.
- **Fix:** `SeedAnswer` handles three categories: prefix functions (`sin` -> `sin(5`), postfix-wrap functions (`x²` -> `sqr(5)`), and percent (`%` -> `5%`). Code trace verified:
  - Prefix (sin): `AppendFunction` adds `sin(`, then `Token.Number(5)` -> buffer `sin(5`. Paren intentionally left open for user continuation. Matches [****]'s acceptance criterion `sin(5`.
  - Postfix-wrap (square): `Token.Number(5)` added, `WrapBufferInFunction` wraps as `sqr(5)`. Correct.
  - Percent: `Token.Number(5)` then `Token.Percent()` -> `5%`. Correct.
- **Tests:** `SquareAfterEqualsWrapsAnswer`, `SqrtAfterEqualsWrapsAnswer`, `PrefixFunctionAfterEqualsWrapsAnswer`, `PercentAfterEqualsContinuesFromAnswer`. **Adequate.**

### Issue 3: No keyboard input
- **[****]:** `tabindex="0"` + `@onkeydown` on calculator container; keyboard map for digits, operators, parens, `=`, `Backspace`/`Delete`, `Escape`.
- **Implementation:** `HandleKeyDown` at `CalculatorPage.razor:277-281`, `keyboardKeys` dictionary at lines 185-214. Ctrl/Alt/Meta modifiers filtered out (prevents browser-shortcut conflicts).
- **Enter deliberately unmapped** per [****] decision: "with a keypad button focused, Enter activates that button; mapping it globally would double-fire." **Verified: `Enter` absent from dictionary.**

### Issue 4: Error-lockout controls look active but do nothing
- **[****]:** All controls `disabled` while `Calc.Locked` except AC.
- **Implementation:** Sci keys `disabled="@Calc.Locked"` (line 53), main keys `disabled="@(Calc.Locked && key.Key != InputKey.AllClear)"` (line 59), memory buttons `disabled="@Calc.Locked"` (lines 42-44), mode toggle `disabled="@Calc.Locked"` (line 14), history items `disabled="@Calc.Locked"` (line 74). CSS disabled styling: `opacity: 0.45; cursor: not-allowed` (lines 123-128). **Correct and complete.**
- **Escape as recovery:** `Escape` -> `InputKey.AllClear` in keyboard map, and `HandleLockedPress` passes AC through. **Verified.**

### Issue 5: Results/errors/mode/memory lack accessible names and announcements
- **Implementation verified:**
  - Container: `role="group" aria-label="Scientific calculator"` (line 5)
  - Display: `aria-label="Calculator display"` (line 19)
  - Error: `role="alert"` on error title (line 26) — assertive announcement
  - Status: `role="status" aria-live="polite"` on `.sr-only` div with dynamic `StatusText` (line 34)
  - Mode: `aria-live="polite"` on mode text span (line 16), descriptive `aria-label` on toggle (line 14)
  - Memory: `aria-label="Memory slots"` on section (line 36); per-slot badge names (`MemoryBadgeName`, line 236-237); per-button action names (`MemoryButtonName`, line 239)
  - Keys: `aria-label` via `KeyName` for all symbolic keys (line 298; Name property on KeyDefinition records)
  - History: `aria-label` via `HistoryItemName` (line 241-242)
- **Comprehensive and correct.**

### Issue 6: Layout breaks below ~720px width / short height
- **[****]:** `@media` blocks for <=720px, <=640px height, <=560px width; `overflow-y` scrolling.
- **Implementation:** Four media queries in `CalculatorPage.razor.css`:
  - `max-width: 900px` (lines 463-471): reduced padding, narrower history panel
  - `max-width: 720px` (lines 473-518): single-column layout, stacked memory, wrapped sci pad, compact display
  - `max-height: 640px` (lines 520-562): compact vertical spacing, smaller keys/badges/memory
  - `max-width: 560px` (lines 564-581): further compaction
  - `overflow-y: auto` on `.calc` container (line 17)
- **Correct and thorough.**

### Issue 7: Keyboard focus is hard to see
- **[****]:** Strong `:focus-visible` rings for container and all controls.
- **Implementation:**
  - `.calc:focus-visible`: 2px solid blue outline, -3px offset (lines 24-28)
  - `.key:focus-visible, .mode-toggle:focus-visible, .history-item:focus-visible`: 3px solid `#8ec2ff` outline, 2px offset, 5px box-shadow glow (lines 308-314)
- **High-contrast, clearly visible.** Correct.

## Anti-Procedural Checklist

- Domain logic (`seedPending`, `SeedAnswer`, `ContinuesFromAnswer`) lives on the `Calculator` aggregate — correct host (already owns session state, buffer, history, LastAnswer).
- No calculation logic in the presentation layer — `CalculatorPage.razor` only maps keys to `InputKey` and renders domain state.
- No UI types in Domain.
- No new classes — only a new flag and method on existing aggregate. [****] correctly rejected `InputBuffer` (lacks session knowledge) and a `KeyboardMapper` class (lazy class).
- **PASSES.**

## Architecture Compliance

- Domain holds all calculation behavior; presentation maps `InputKey` presses. This is preserved.
- The `seedPending` flag is per-session state on the `Calculator` aggregate, matching the existing patterns (`ActiveError`, `Mode`, `LastAnswer`).
- No commits made. No changes outside the [****]'s listed files.

## Test Coverage Assessment (ContinuationTests.cs)

13 tests verified — all aligned with implementation:

| Test | Assertion | Verified |
|------|-----------|----------|
| OperatorAfterEqualsContinuesFromAnswer (6 cases) | `5+`, `5-`, `5*`, `5/`, `5^`, `5mod` | Correct |
| ContinuedExpressionEvaluatesResultPlusOperand | `2+3=+4=` -> LastAnswer 9 | Correct |
| ContinuedExpressionEvaluatesResultTimesOperand | `2+3=*2=` -> LastAnswer 10 | Correct |
| SquareAfterEqualsWrapsAnswer | `sqr(5)`, preview 25 | Correct |
| SqrtAfterEqualsWrapsAnswer | `sqrt(5)`, preview ~2.236 | Correct |
| PrefixFunctionAfterEqualsWrapsAnswer | `sin(5`, preview null, not locked | Correct |
| PercentAfterEqualsContinuesFromAnswer | `5%`, preview 0.05 | Correct |
| ValueKeysAfterEqualsStartFreshExpression (5 cases) | Digits/dot/pi/parens start fresh | Correct |
| OperatorAfterAllClearDoesNotSeedStaleAnswer | AC clears seedPending | Correct |
| OperatorAfterHistoryRestoreDoesNotSeedAnswer | RestoreHistory clears seedPending | Correct |
| AnswerSeedingWorksWithMemoryRecall | Recall after AC + multiply works | Correct |

## Risks Evaluated

| Risk | Assessment |
|------|------------|
| Enter double-fire | Mitigated: Enter deliberately unmapped; only `=` key triggers equals |
| Keyboard modifier conflicts | Mitigated: Ctrl/Alt/Meta filtered in `HandleKeyDown` |
| `aria-live` chattiness | Low: `polite` mode queues announcements without interrupting; appropriate for a calculator |
| `sin(5` vs `sin(5)` expectation | Correct: paren intentionally left open for user to continue expression |
| CSS media query coverage | Complete: 4 breakpoints cover width and height scenarios with progressive compaction |
| `m`/`M` keyboard conflict | Acceptable: focus is on calculator container, not a text input |

## Minor Observations (informational, not blocking)

1. **Factorial after `=` untested:** Pressing `!` after `=` would produce `5!` (seeded answer + postfix factorial). This path works correctly (Factorial is neither a prefix-call nor a postfix-wrap, so it seeds then appends), but has no dedicated test case. Low risk — the code path is a combination of two tested paths.

2. **Other postfix-wrap functions untested after `=`:** Only `Square` and `Sqrt` are tested as postfix-wrap after `=`. `Cube`, `Cbrt`, `Reciprocal`, `Exp`, `TenPow`, `Abs` use the same `IsPostfixWrapKey` -> `WrapBufferInFunction` path. Low risk — shared code path.

3. **Scientific function keys not on keyboard:** Constants (`e`, `pi`) and functions (`sin`, `cos`, etc.) are button-only. The [****] scope is keyboard input for the standard keypad, not full function-key mapping. Acceptable.

4. **README.md change ([****] item 5):** Listed in the [****] but not reviewed here as it's documentation, not behavioral code.

## Verdict

**APPROVE**

The [****] is correct, complete, and well-structured. All 7 defects are addressed with accurate root-cause analysis. The implementation matches the [****] precisely. Domain/presentation separation is maintained. Test coverage is thorough for the critical paths. The Enter-key decision and lockout behavior are sound. No required amendments.
