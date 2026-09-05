# Framework Design Guidelines — Naming/Types Checklist Result

> **Workflow:** `framework-design-guidelines` (reference checklist, 0 executable steps)  
> **Session ID:** `5d39b010ef3f4823aa5edae5c949d38d`  
> **Final status:** `complete`  
> **Profile:** naming-types  
> **Mode:** AUDIT — no code changes applied  
> **Source:** [Microsoft Framework Design Guidelines — Naming](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/naming-guidelines), [Names of Type Members](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/names-of-type-members), [Type Design Guidelines](https://learn.microsoft.com/en-us/dotnet/standard/design-guidelines/type)

## Scope

Types audited (new/changed in this feature branch):

| # | Type | Kind | File |
|---|------|------|------|
| 1 | `FunctionNode` | sealed class | `src/SciCalc.Domain/Nodes/FunctionNode.cs:6` |
| 2 | `Calculator` | sealed class | `src/SciCalc.Domain/Calculator.cs:3` |
| 3 | `InputBuffer` | sealed class | `src/SciCalc.Domain/InputBuffer.cs:5` |
| 4 | `MemoryBank` | sealed class | `src/SciCalc.Domain/MemoryBank.cs:3` |
| 5 | `HistoryEntry` | sealed record | `src/SciCalc.Domain/HistoryEntry.cs:4` |
| 6 | `KeyDef` | sealed record (nested) | `src/SciCalc/Components/CalculatorPage.razor:169` |
| 7 | `MemSlot` | sealed record (nested) | `src/SciCalc/Components/CalculatorPage.razor:171` |
| 8 | `OperatorKind` | enum | `src/SciCalc.Domain/OperatorKind.cs:4` |
| 9 | `EvaluationContext` | readonly record struct | `src/SciCalc.Domain/EvaluationContext.cs:4` |

Adjacent enums/structs checked for member naming: `FunctionKind`, `InputKey`, `CalcError`, `MemorySlotId`, `CalculationResult`.

---

## Checklist Results

### 1. General Naming Conventions — Word Choice

| Rule | Verdict | Detail |
|------|---------|--------|
| ❌ DO NOT prefix field names with `_` | **FAIL** | Multiple types use `_` prefix. See [F-01](#f-01). |

### 2. General Naming Conventions — Abbreviations and Acronyms

| Rule | Verdict | Detail |
|------|---------|--------|
| ❌ DO NOT use abbreviations or contractions in identifiers | **FAIL** | `KeyDef`, `MemSlot`, `OperatorKind.Sub/Mul/Div/Mod/Pow`. See [F-02](#f-02), [F-03](#f-03). |
| ❌ DO NOT use acronyms that are not widely accepted | **PASS** | No unaccepted acronyms found. `Calc`, `Ln`, `Sqrt` etc. are standard math abbreviations (widely accepted domain terms). |

### 3. Capitalization Conventions

| Rule | Verdict | Detail |
|------|---------|--------|
| ✔️ DO use PascalCase for type names, property names, enum members | **PASS** | All type names and public members use PascalCase. |
| ✔️ DO use camelCase for parameter names | **PASS** | All method parameters use camelCase. |

### 4. Names of Classes, Structs, and Interfaces

| Rule | Verdict | Detail |
|------|---------|--------|
| ✔️ DO name classes and structs with nouns or noun phrases | **PASS** | `FunctionNode`, `Calculator`, `InputBuffer`, `MemoryBank`, `HistoryEntry`, `CalculationResult` — all noun phrases. |
| ❌ DO NOT use "Context" suffix for stateful domain data | **FAIL** | `EvaluationContext` carries state and behavior. See [F-04](#f-04). |

### 5. Names of Enumerations

| Rule | Verdict | Detail |
|------|---------|--------|
| ✔️ DO use a singular type name for non-flags enums | **PASS** | `OperatorKind`, `FunctionKind`, `InputKey`, `CalcError`, `MemorySlotId` — all singular. |
| ❌ DO NOT use abbreviations in enum member names | **FAIL** | `OperatorKind`: `Sub`, `Mul`, `Div`, `Mod`, `Pow`. See [F-03](#f-03). |
| ✔️ DO use full words for enum members | **PARTIAL** | `InputKey` mirrors `OperatorKind` abbreviations (`Sub`, `Mul`, `Div`, `Pow`, `Mod`). See [F-05](#f-05). |

### 6. Names of Type Members

| Rule | Verdict | Detail |
|------|---------|--------|
| ✔️ DO name methods with verbs or verb phrases | **PASS** | `EvaluateNode`, `Apply`, `Press`, `Store`, `Recall`, `Clear`, `Add`, `RemoveLastToken`, etc. |
| ✔️ DO name properties with nouns, noun phrases, or adjectives | **PASS** | `Buffer`, `Memory`, `History`, `Mode`, `Locked`, `Preview`, `Value`, `Tokens`, etc. |

### 7. Type Design Guidelines

| Rule | Verdict | Detail |
|------|---------|--------|
| ✔️ DO prefer classes over structs for domain objects | **PASS** | `Calculator`, `InputBuffer`, `MemoryBank` are classes. `CalculationResult` is appropriately a struct (small value type). |
| ✔️ DO make records immutable when they represent value objects | **PASS** | `HistoryEntry` is a sealed record (immutable by design). |
| ✔️ DO seal classes when not designed for inheritance | **PASS** | All concrete classes are `sealed`. |

### 8. Record / Struct Shape

| Rule | Verdict | Detail |
|------|---------|--------|
| ✔️ CONSIDER using record types for immutable DTOs | **PASS** | `HistoryEntry`, `KeyDef`, `MemSlot` are records. |
| ❌ AVOID mutable state exposed as read-only facade | **FAIL** | See cross-ref issues.md line 19 (H5/H6). Not a naming finding — not repeated here. |

---

## Findings

### F-01 — Underscore-prefixed private fields {#f-01}

**Severity:** Low  
**Guideline:** ❌ DO NOT prefix field names with `_`  
**Cross-ref:** issues.md line 22 (verify-class-names), refactoring-plan.md B7/L1

| Type | Fields |
|------|--------|
| `Calculator` | `_history` (`:6`), `_keyTokens` (`:7`), `_functionKeys` (`:32`) |
| `InputBuffer` | `_tokens` (`:8`), `_operatorNames` (`:9`), `_functionNames` (`:18`), `_constantNames` (`:41`), `_numberText` (`:46`) |
| `MemoryBank` | `_m1` (`:6`), `_m2` (`:7`), `_m3` (`:8`) |
| `CalculatorPage` | `_functionKeys` (`:73`), `_mainKeys` (`:99`), `_memorySlots` (`:127`) |

**Recommendation:** Rename to bare camelCase (`history`, `keyTokens`, `tokens`, `m1`, etc.).  
**Planned fix:** refactoring-plan B7a.

---

### F-02 — Abbreviated type names: `KeyDef`, `MemSlot` {#f-02}

**Severity:** Low  
**Guideline:** ❌ DO NOT use abbreviations or contractions as part of identifier names  
**Cross-ref:** issues.md line 21, refactoring-plan.md B7/L2

| Current | Proposed | File:Line |
|---------|----------|-----------|
| `KeyDef` | `KeyDefinition` | `CalculatorPage.razor:169` |
| `MemSlot` | `MemorySlot` | `CalculatorPage.razor:171` |

**Recommendation:** Rename to `KeyDefinition` and `MemorySlot`.  
**Planned fix:** refactoring-plan B7b.

---

### F-03 — Abbreviated enum members in `OperatorKind` {#f-03}

**Severity:** Medium  
**Guideline:** ❌ DO NOT use abbreviations in enum member names; ✔️ DO use full words  
**Cross-ref:** issues.md line 103 (verify-class-names OperatorKind finding), refactoring-plan.md B7/M16

| Current | Proposed | File:Line |
|---------|----------|-----------|
| `OperatorKind.Sub` | `OperatorKind.Subtract` | `OperatorKind.cs:7` |
| `OperatorKind.Mul` | `OperatorKind.Multiply` | `OperatorKind.cs:8` |
| `OperatorKind.Div` | `OperatorKind.Divide` | `OperatorKind.cs:9` |
| `OperatorKind.Pow` | `OperatorKind.Power` | `OperatorKind.cs:10` |
| `OperatorKind.Mod` | `OperatorKind.Modulo` | `OperatorKind.cs:11` |

Note: `Add` is already a full word and passes.

**Recommendation:** Rename all five abbreviated members to full words. Wide rename across all references (`Calculator.cs`, `InputBuffer.cs`, `BinaryNode.cs`, `Token.cs`, tests).  
**Planned fix:** refactoring-plan B7c.

---

### F-04 — `EvaluationContext` uses prohibited "Context" suffix {#f-04}

**Severity:** Medium  
**Guideline:** ❌ AVOID "Context" suffix for stateful domain data types (per FDG: Context implies ambient/environmental, not domain-owned state)  
**Cross-ref:** issues.md line 20, issues.md line 89 (verify-class-names), refactoring-plan.md B5/M1

`EvaluationContext` (`EvaluationContext.cs:4`) is a `readonly record struct` carrying `AngleMode` and an unused `Ans` field. The `ToRadians`/`ToDegrees` conversion methods are angle-conversion utilities that belong on `AngleMode` itself. After removing `Ans` (YAGNI) and moving conversions, the remaining parameter (just `AngleMode`) can be passed directly — eliminating the type entirely. If a parameter object is still needed, rename to `EvaluationSettings` or `AngleSettings`.

**Recommendation:** Delete `Ans`, move `ToRadians`/`ToDegrees` onto `AngleMode`, pass `AngleMode` directly into `Evaluate`/`EvaluateNode`.  
**Planned fix:** refactoring-plan B5a/B5b.

---

### F-05 — `InputKey` abbreviated operator members (alignment issue) {#f-05}

**Severity:** Low  
**Guideline:** ✔️ DO use full words for enum members  
**Cross-ref:** refactoring-plan.md L12

`InputKey` mirrors `OperatorKind` abbreviations for operator-key members:

| Current | Proposed | File:Line |
|---------|----------|-----------|
| `InputKey.Sub` | `InputKey.Subtract` | `InputKey.cs:18` |
| `InputKey.Mul` | `InputKey.Multiply` | `InputKey.cs:19` |
| `InputKey.Div` | `InputKey.Divide` | `InputKey.cs:20` |
| `InputKey.Pow` | `InputKey.Power` | `InputKey.cs:21` |
| `InputKey.Mod` | `InputKey.Modulo` | `InputKey.cs:22` |

**Recommendation:** Rename in alignment with `OperatorKind` renames (F-03).  
**Planned fix:** refactoring-plan B7c / L12.

---

### F-06 — `FunctionKind` borderline abbreviations (PASS with note) {#f-06}

**Severity:** Informational (no action required)  
**Guideline:** ❌ DO NOT use abbreviations — unless widely accepted

`FunctionKind` members `Sin`, `Cos`, `Tan`, `Asin`, `Acos`, `Atan`, `Sinh`, `Cosh`, `Tanh`, `Ln`, `Sqrt`, `Cbrt`, `Exp`, `Abs` are **standard mathematical function names** universally recognized in scientific computing, `System.Math`, and IEEE 754 references. They qualify as "widely accepted" abbreviations per FDG exception clause.

`Log10`, `TenPow`, `Square`, `Cube`, `Factorial`, `Reciprocal` are full words or near-full words and pass without qualification.

**Verdict:** PASS — no rename needed.

---

## Types That Pass All Checklist Items (Clean)

| # | Type | File | Notes |
|---|------|------|-------|
| 1 | `FunctionNode` | `Nodes/FunctionNode.cs:6` | PascalCase, noun phrase, sealed, no abbreviations in type name, Interpreter pattern name justified. |
| 2 | `Calculator` | `Calculator.cs:3` | PascalCase, real-world noun, sealed, rich aggregate. Underscore fields (F-01) are the only finding. |
| 3 | `InputBuffer` | `InputBuffer.cs:5` | PascalCase, noun phrase, sealed. Underscore fields (F-01) only. |
| 4 | `MemoryBank` | `MemoryBank.cs:3` | PascalCase, noun phrase, sealed. Underscore fields (F-01) only. |
| 5 | `HistoryEntry` | `HistoryEntry.cs:4` | PascalCase, noun phrase, sealed record, immutable. Clean on naming. |
| 6 | `CalculationResult` | `CalculationResult.cs:3` | PascalCase, noun phrase, value type. Clean. |
| 7 | `CalcError` | `CalcError.cs:4` | PascalCase, singular enum, full-word members. Clean. |
| 8 | `MemorySlotId` | `MemorySlotId.cs:3` | PascalCase, singular enum, members M1/M2/M3 (identifiers, not abbreviations). Clean. |

---

## Summary

| Severity | Count | Finding IDs |
|----------|------:|-------------|
| Medium | 2 | F-03, F-04 |
| Low | 3 | F-01, F-02, F-05 |
| Informational | 1 | F-06 |
| **Total actionable** | **5** | |

All 5 actionable findings are already tracked in `Docs/_Current/issues.md` and have planned fixes in `Docs/_Current/refactoring-plan.md` (Phase B7 for naming, Phase B5 for EvaluationContext). No new previously-unknown violations were discovered.

### Checklist Pass/Fail Summary

| Checklist Category | Items Checked | Pass | Fail | Partial |
|--------------------|--------------|------|------|---------|
| Word Choice (underscore fields) | 1 | 0 | 1 | 0 |
| Abbreviations & Acronyms | 2 | 1 | 1 | 0 |
| Capitalization (PascalCase/camelCase) | 2 | 2 | 0 | 0 |
| Class/Struct Naming | 2 | 1 | 1 | 0 |
| Enum Naming | 3 | 1 | 1 | 1 |
| Type Member Naming | 2 | 2 | 0 | 0 |
| Type Design | 3 | 3 | 0 | 0 |
| Record/Struct Shape | 2 | 1 | 1 | 0 |
| **Totals** | **17** | **11** | **5** | **1** |

---

*Generated by plan-reviewer agent. Workflow: `framework-design-guidelines`, Session: `5d39b010ef3f4823aa5edae5c949d38d`, Status: complete.*
