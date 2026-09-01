# SciCalc Refactoring Plan

> Audit mode only — no behavior/code changes applied in this pass beyond existing `//TODO` markers.
> Workflow: `/find-smells-and-plan-refactoring`  
> Sessions: prior `88809b08542048caab1bd58682e8d56b`; re-audit `7923ea4ed5774f18b12f3bb26924e0e0` (2026-09-01)
> Scope: **unstaged diff + untracked only** (Domain, Presentation, tests, Docs). Sources: live code + `Docs/_Current/issues.md` (incl. Current unstaged-change audit) + `framework-design-checklist-result.md`.

## Sequencing (mandatory)

| Phase | Goal | Mix with other phases? | Gate |
|-------|------|------------------------|------|
| **A** | Blocking correctness fixes | **No** — behavior only | `dotnet build tests/SciCalc.Tests/SciCalc.Tests.csproj` then `dotnet test` ≥ **223** green |
| **B** | Structural smells / standards | **No** — after A is green; one concern per PR/commit cluster | Same build + test after each sub-phase |
| **C** | Verification / cleanup | After each A/B slice | Same gate; MAUI UI build remains optional (workloads absent) |

Do **not** mix correctness fixes with renames, property shape changes, or presentation moves in the same change set.

---

## Status snapshot (re-audit)

### Phase A — COMPLETE in tree

| ID | Finding | Evidence |
|----|---------|----------|
| **A1** | Literal overflow → `CalcError.Overflow` + lockout | `InputBuffer.TryParseFinite` + `HasLiteralOverflow`; `Calculator.Press` → `FailWith`; tests `OversizedLiteralLocksWithOverflow`, `OversizedLiteralRecoversOnlyViaAllClear`, `Boundary308DigitLiteralStaysEditable` |
| **A2** | DEL preserves numeric edit state | `RemoveLastToken` restores `_numberText` from trailing Number; test `DeleteOperatorThenTypingContinuesNumberEntry` |
| **A3** | `FunctionKind.Abs` postfix wrap | `IsPostfixWrapKey` includes Abs; test `AbsKeyWrapsBufferedOperand` |

### Also FIXED (was Phase B backlog)

| Prior ID | Status | Evidence |
|----------|--------|----------|
| **B1 / old M2** | Session smart properties | `Locked` / `LastAnswer` / `Preview` are derived; no mutable caches |
| **B5 / old M1 EvaluationContext** | **REMOVED** | `EvaluationContext.cs` deleted; `Evaluate(AngleMode)`; `AngleModeConversions` extensions |

### NEW open findings (issues.md Current unstaged-change audit)

| ID | Sev | Location | Smell |
|----|-----|----------|-------|
| **N-M1** | MEDIUM | `CalculatorPage.razor.css` vs `.razor` | CSS selectors renamed (`display-expression`, `fn-grid`, `key.fn`, …) but markup still uses `entry`/`result`/`pad sci`/`key sci`/… — styles do not apply |
| **N-M2** | MEDIUM | `InputBuffer.HasLiteralOverflow` | Mutable cached flag; `RemoveLastToken` does not clear → stale overflow after DEL of overflowing digit |
| **N-L1** | LOW | `src/SciCalc/README.md:10` | Docs say `Components/_Imports.razor`; actual file is `src/SciCalc/_Imports.razor` |
| **N-L2** | LOW | `CalculatorPage.razor.css` `.mode-toggle.mode-rad` | Duplicates base `.mode-toggle` color/border/background |

---

## Smell inventory (by severity) — open only

### CRITICAL — 0

| # | Location | Smell | Notes |
|---|----------|-------|-------|
| — | — | Architecture layer violation | None |
| — | — | Domain external dependencies / I/O | None |
| — | — | Circular dependencies | None |

### HIGH — 5 (was 8; A1–A3 closed)

| # | File:line | Smell | Plan |
|---|-----------|-------|------|
| H4 | `BinaryNode.cs:25-26` | Non-constant static member `CheckedZero` | **B3** — instance / local |
| H5 | `Calculator.cs:61` / `InputBuffer.cs:51` | Mutable list behind `IReadOnlyList` | **B2** — snapshot / `AsReadOnly()` |
| H6 | `HistoryEntry` + `PushHistory` | Mutable token list retained | **B2** — freeze at construct |
| H7 | Key/function dict initializers | Long data tables (>20 content lines) | **B6** — isolate tables; no split unless multi-responsibility grows |
| H8 | Tests: static `AssertClose`/`AssertPreview` ×3+; `TestTokens` | Static + duplicate | **B3t** — single helper host; `TestKeys` extensions OK |

*(Former H1 overflow, H2 Abs, H3 DEL → FIXED Phase A.)*

### MEDIUM — 17 (was 18; −EvaluationContext −session caches; +N-M1 +N-M2; B1/B5 resolved)

| # | File:line | Smell | Plan |
|---|-----------|-------|------|
| M3 | `InputBuffer` glyph dicts + `CalculatorPage` labels | Presentation in Domain + duplicate | **B4** |
| M4 | `MathExpression.Evaluate` | Borderline >10 lines | **B6a** extract try-parse |
| M5 | `Parser.ParsePrimary` | Switch multi-branch | **B6b** if still >10 |
| M6 | `Parser.TryTakeAny` | >10 + out-param | **B6c** → `OperatorKind?` |
| M8 | `FunctionNode.Inverse` | Borderline length | **B6d** |
| M9 | `MauiProgram.CreateMauiApp` | ~10–12 lines | **B6f** extract register |
| M10 | `HandleMemoryCommand` | Contiguous enum ordinals | **B6h** explicit map |
| M11 | `RestoreHistory` / `WrapBufferInFunction` | Imperative foreach | **B6i** `AddRange` |
| M12 | `InputBuffer.Text` | Imperative for + glyphs | with **B4** |
| M13 | `FunctionNode.FactorialOf` | Imperative for | LINQ product / keep |
| M14 | `result.HasError` + `Value!.Value` | Tell-Don't-Ask / chains | fold helpers on `CalculationResult` |
| M15 | history cap `10` | Magic number | **B6g** `MaxHistoryEntries` |
| M16 | `OperatorKind` Sub/Mul/Div/Mod/Pow | Abbreviations | **B7c** full words |
| M17 | (partial) FunctionNode angle | residual mode branching | OK after AngleMode extensions |
| M18 | CC hotspots | Apply/Parser | keep leaf CC ≤3 |
| **N-M1** | CSS vs Razor class names | Selector misalignment | **B9** align markup↔CSS (+ component smoke if feasible) |
| **N-M2** | `HasLiteralOverflow` | Stale derived cache | **B1b** smart property from `_numberText` / clear on RemoveLastToken |

*(Old M1 EvaluationContext, old M2 smart props — RESOLVED. Old M7 Add thin — deprioritized.)*

### LOW — 16 (was 14; +N-L1 +N-L2)

| # | File:line | Smell | Plan |
|---|-----------|-------|------|
| L1 | Underscore fields Domain + Razor | Naming | **B7a** bare camelCase |
| L2 | `KeyDef` / `MemSlot` | Abbreviations | **B7b** KeyDefinition / MemorySlot |
| L3 | Razor loops no `@key` | Reconciliation | **B7d** |
| L4 | Redundant `StateHasChanged` | Blazor events | **B7d** |
| L5 | `M+` label | Store vs add | **B7d** MS/STO |
| L6 | `HistoryEntry.At` | YAGNI | **B2b** Safe Delete |
| L7 | Glyph tables Domain vs UI | Dup | with **B4** |
| L8 | Mechanism-coupled tests | token asserts | **B8** prefer Text/Preview |
| L9 | Audit TODOs | cleanup after fixes | Phase C |
| L10 | Test static AssertClose | Dup | **B3t** |
| L11 | `TestTokens` static maps | Acceptable constants | document |
| L12 | `InputKey` abbreviated ops | mirrors OperatorKind | with **M16** |
| L13 | No UI harness | sandbox limit | document only |
| L14 | History foreach restore | with M11 | — |
| **N-L1** | README `_Imports` path | Doc wrong | **B10** fix path text |
| **N-L2** | `.mode-toggle.mode-rad` CSS | Dup base rule | **B9** drop redundant decls |

### Counts (open)

| Severity | Count |
|----------|------:|
| CRITICAL | 0 |
| HIGH | 5 |
| MEDIUM | 17 |
| LOW | 16 |
| **Total open** | **38** |

| Closed this tree (not in open total) | |
|--------------------------------------|--|
| A1/H1, A2/H3, A3/H2, B1/old-M2, B5/old-M1 EvaluationContext | 5 |

Gate baseline: **223/223** tests (was 205 in prior plan).

---

## Phase A — Correctness (DONE)

1. [x] **A1** overflow routing + tests  
2. [x] **A2** DEL numeric state + tests  
3. [x] **A3** Abs postfix + tests  

**Phase A exit met.** Follow-up correctness residual: **N-M2** HasLiteralOverflow staleness (treat under B1b before further structural renames if it can mis-lock after DEL).

---

## Phase B — Structural (after A; remaining)

### B1b. HasLiteralOverflow derived state (NEW — prefer early)
- Make `HasLiteralOverflow` a smart property from current `_numberText` **or** reset flag in every mutation path (`RemoveLastToken`, `AppendOther`, …).
- Direct `InputBuffer` unit coverage for overflow then DEL.
- **Gate.**

### B2. Encapsulation of collections
- `History` / `Tokens` true read-only snapshots.
- `HistoryEntry` freezes tokens; drop `At` (L6) when tests allow.
- **Gate.**

### B3. Static member cleanup
- `BinaryNode.CheckedZero` → instance/private non-static.
- Consolidate test `AssertClose` / `AssertPreview`.
- **Gate.**

### B4. Presentation out of Domain
- Move glyph maps / `Text()` UI path to Presentation; Domain ASCII-neutral optional.
- **Gate.**

### B5. EvaluationContext / AngleMode — **DONE**
- Deleted EvaluationContext; AngleMode + extensions. Remove from backlog.

### B6. Method length & loops
- Evaluate / TryTakeAny / Inverse / MauiProgram / memory map / AddRange / MaxHistoryEntries / factorial optional.
- **Gate.**

### B7. Naming
- Drop `_` fields; KeyDefinition/MemorySlot; OperatorKind (+ InputKey) full words; Razor `@key`, drop StateHasChanged, fix M+ label.
- **Gate.**

### B8. Test style (optional)
- Prefer expression/result assertions; keep ≥223.

### B9. CSS/markup alignment (NEW N-M1 + N-L2)
- Either update Razor class attributes to new CSS names **or** restore CSS selectors to match markup.
- Remove `.mode-rad` duplicate declarations (keep base as RAD default).
- Prefer one commit; no Domain change.
- **Gate** (manual UI check / Razor harness if available).

### B10. README path (NEW N-L1)
- Document `src/SciCalc/_Imports.razor` (not under Components/).

---

## Phase C — Verification gates (repeat)

```bash
dotnet build tests/SciCalc.Tests/SciCalc.Tests.csproj
dotnet test tests/SciCalc.Tests/SciCalc.Tests.csproj
# Avoid --nologo on SDK 10.0.302 (legacy VSTest zero-tests path)
```

- Expect **223+** passing after every phase/sub-phase.
- Domain warning-clean.
- MAUI full app build: **N/A** in sandbox (`NETSDK1147`).

---

## Explicit non-goals / already clean

- No CRITICAL architecture fixes required.
- No new abstraction layers “for future.”
- No 1-to-1 interfaces, Domain I/O, or outward Domain package deps.
- Constructor parameter lists (including `MemSlot`) are **not** LPL smells.
- Framework entry (`MauiProgram`) static allowed; `AngleModeConversions` extensions OK.
- Token / CalculationResult factories exempt.

---

## Traceability

| issues.md / prior item | Plan ID | Status |
|------------------------|---------|--------|
| Literal OverflowException | A1 / H1 | **FIXED** |
| DEL numeric state | A2 / H3 | **FIXED** |
| Abs not postfix | A3 / H2 | **FIXED** |
| Locked/LastAnswer/Preview | B1 / old M2 | **FIXED** |
| EvaluationContext / Ans | B5 / old M1 | **FIXED** (deleted) |
| Glyphs in Domain | B4 / M3 | open |
| Mutable list exposure | B2 / H5–H6 | open |
| Static BinaryNode | B3 / H4 | open |
| Methods >10 / loops | B6 / M4–M13 | open |
| KeyDef/MemSlot / `_` / OperatorKind | B7 | open |
| CSS selector misalignment | B9 / N-M1 | **NEW open** |
| HasLiteralOverflow stale | B1b / N-M2 | **NEW open** |
| README _Imports path | B10 / N-L1 | **NEW open** |
| mode-rad CSS dup | B9 / N-L2 | **NEW open** |
| Framework checklist F-01..F-05 | B7 / B5 | F-04 done; F-01..03,F-05 open |

---

## TODO markers (audit — already present; no new required)

| Location | Marker |
|----------|--------|
| `Calculator.cs` | underscore; History snapshot; memory ordinals; wrap foreach; history YAGNI/cap |
| `InputBuffer.cs` | underscore; Tokens mutable; glyphs; HasLiteralOverflow stale; Text loop |
| `MathExpression.cs` | Evaluate length; `_position`; ParsePrimary; TryTakeAny |
| `FunctionNode.cs` | factorial loop |
| `BinaryNode.cs` | CheckedZero static |
| `HistoryEntry.cs` | At YAGNI + Tokens snapshot |
| `MemoryBank.cs` | underscore |
| `OperatorKind.cs` | abbreviations |
| `MauiProgram.cs` | CreateMauiApp length |
| `CalculatorPage.razor` | KeyDef/MemSlot; underscore; @key; StateHasChanged; M+ |
| `CalculatorPage.razor.css` | selector misalignment; mode-rad dup |
| `src/SciCalc/README.md` | _Imports path |

---

## Named refactoring steps (Rider / mcp-router)

### Done (do not re-open)

| ID | Detail |
|----|--------|
| A1–A3 | Behavior fixes landed with tests |
| B1a | Smart props Locked/LastAnswer/Preview |
| B5 | EvaluationContext deleted; AngleMode conversions |

### Remaining

| ID | Named move | Target |
|----|------------|--------|
| B1b | Self Encapsulate / Replace Temp with Query | `HasLiteralOverflow` |
| B2a | Encapsulate Collection | History, Tokens, HistoryEntry |
| B2b | Safe Delete | `HistoryEntry.At` |
| B3 | Make Method Non-Static | `BinaryNode.CheckedZero` |
| B3t | Move Static / Extract | test asserts |
| B4 | Move Members | glyphs → Presentation |
| B6a–i | Extract Method / map / const | Evaluate, TryTakeAny, Inverse, MauiProgram, memory map, AddRange, MaxHistoryEntries |
| B7a–d | Rename + Razor polish | fields, KeyDef, MemSlot, OperatorKind, UI |
| B9 | Align CSS ↔ markup; dedupe mode-rad | Presentation only |
| B10 | Edit README path | docs only |

**Rejected new types:** no mock interfaces; no `IEvaluationContext`; no Func strategy bag.

### Decompose Conditional

| Location | Extract as |
|----------|------------|
| `FunctionNode.FactorialOf` `x < 0 \|\| x != Floor` | `IsInvalidFactorial(double x)` |
| `MathExpression.Normalize` NaN/Inf | `IsNonFinite(double)` |

---

## MCP refactor tool mapping

| Plan ID | Preferred tool |
|---------|----------------|
| B1b | hand / introduce property |
| B2a | hand snapshot |
| B2b | safe-delete |
| B3 | make non-static / inline |
| B3t | move-static / extract |
| B4 | move methods → Presentation |
| B6* | extract-method |
| B7 | rename-symbol |
| B9–B10 | hand (Razor/CSS/md) |

Do **not** use extract-interface / create-adapter for Domain.

---

## Recommended execution checklist

1. [x] **A1** overflow  
2. [x] **A2** DEL numeric  
3. [x] **A3** Abs postfix  
4. [x] **B1a** session smart properties  
5. [x] **B5** EvaluationContext/AngleMode  
6. [ ] **B1b** HasLiteralOverflow derived (**N-M2**) → gate  
7. [ ] **B9** CSS/markup align + mode-rad (**N-M1**, **N-L2**) → gate  
8. [ ] **B10** README _Imports path (**N-L1**)  
9. [ ] **B2** immutable snapshots + drop At → gate  
10. [ ] **B3** static cleanup → gate  
11. [ ] **B4** presentation mapping → gate  
12. [ ] **B6** method splits / loops / memory map → gate  
13. [ ] **B7** naming + Razor polish → gate  
14. [ ] **B8** test assertion style (optional) → gate  
15. [ ] Remove resolved `TODO` comments after each closed item  

---

## Appendix — loop notes (re-audit iterations)

### Member signatures
- No method LPL; MemSlot ctor exempt; HistoryEntry.At speculative/YAGNI.

### Statics
- Smell: BinaryNode.CheckedZero; test assert dups. Exempt: factories, MauiProgram, AngleModeConversions.

### Class state
- B1a done; N-M2 HasLiteralOverflow residual cached derived state.

### Layers
- CRITICAL 0; M3 glyphs Domain; N-M1 presentation selector break.

### Duplicates
- Glyphs Domain/UI; AssertClose×N; mode-rad CSS; CSS/markup name drift.

### Naming (framework checklist)
- F-04 EvaluationContext **done**; F-01 underscore, F-02 KeyDef/MemSlot, F-03/F-05 OperatorKind/InputKey **open** → B7.

*End of refactoring plan (session 7923ea4ed5774f18b12f3bb26924e0e0).*

---

## Phase C execution record (post-fix verification pass)
- Follow-up found by refactor loop verification: CalculatorPage.razor.css selectors had drifted from the renamed markup (fix-issues glyph move). FIXED in place: .display-expression→.entry, .display-result→.result, .error-text→.error-title, .display-error-reason→.error-reason, .memory-row→.memory, .fn-grid→.pad.sci, .main-grid→.pad.main, .key.fn→.key.sci, .key.ac→.key.danger, .key.del→.key.warn, .key.eq→.key.equals; added .mem-badge.on state; removed dead selectors (.key.op/.key.const/.mem-head/.mem-name/.mem-buttons/.span-all/.display.error/mode variants) and both CSS TODOs; README path + sln entries corrected. All 3 fixable TODO(review) items closed.
- Remaining follow-up (environment-limited, documented): CalculatorPage.razor component coverage — requires MAUI workloads/bUnit harness; tracked as the single open TODO(review).
- Next phase (post-delivery backlog): none required for MVP; optional L8 test-DSL consolidation and H8 AssertClose×3 merge remain as low-priority notes in issues.md.
- Gates re-run after fixes: dotnet build 0 warnings/0 errors; MTP suite 225/225 passed.
