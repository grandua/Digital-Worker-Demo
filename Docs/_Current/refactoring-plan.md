# SciCalc — prefix-after-equals CloseParen fix — Code Smells Audit

**Workflow:** `[****]-and-plan-refactoring`  
**Session:** `e1440e1e59dc4c5b82a0ac3936ad5698`  
**Mode:** AUDIT ONLY — findings + `//TODO` markers only; no behavior/architecture/implementation fixes.  
**Date:** 2026-09-17  

## Scope (user-specified)

Unstaged / staged only (`git status --short` / `git diff`):

| Path | Status | Kind |
|------|--------|------|
| `Calculator/Domain/SciCalc.Domain/Calculator.cs` | modified | +1 line: `Buffer.Add(Token.CloseParen())` in `SeedAnswer` prefix branch |
| `Calculator/Domain/SciCalc.Domain.UnitTests/ContinuationTests.cs` | modified | Updated sin fact; new 10-row theory; 2 new facts |
| `Calculator/Presentation/SciCalc.Maui/README.md` | modified | Doc-only: `sin(5` → `sin(5)` wording |
| `Docs/_Current/prompt.md` | modified | Workflow artifact (not product source) |
| `Docs/_Current/issues.md` | untracked | Workflow artifact (not product source) |

**Out of product review:** Docs/_Current artifacts (prompt/issues/[****]).

## Smell inventory (this scope)

| Severity | Count | Notes |
|----------|------:|-------|
| CRITICAL | 0 | No layer/circular/external-domain deps |
| HIGH | 0 | No LPL >3, non-const statics introduced, Feature Envy, methods >20, duplicates >2 |
| MEDIUM | 0 | No message chains 3+, naming issues, or methods requiring action |
| LOW | 0 | No comment-clarity issues in delta |
| **Total new open** | **0** | |

## Explicit non-findings

### Production (`Calculator.cs` — one added statement)

```csharp
// SeedAnswer prefix branch
AppendFunction(function);
Buffer.Add(Token.Number(LastAnswer!.Value));
Buffer.Add(Token.CloseParen()); // ← sole production delta
```

- **Architecture / CRITICAL:** Domain-only; no I/O, no interface injection, no presentation leakage. Mirrors `WrapBufferInFunction` close-paren intent without new types.
- **Long method (`;` count):** `SeedAnswer` ~6–7 statement lines (<10 per step 1.d). Total physical lines ~13; not HIGH (>20).
- **LPL / data clumps / speculative generality:** `SeedAnswer(InputKey key)` — 1 param; no unused members.
- **Statics:** No static members on `Calculator` (only `const MaxHistoryEntries`).
- **Feature Envy / TDA:** Own `Buffer`, `functionKeys`, `LastAnswer`, `AppendFunction`/`AppendKey`.
- **Message chains:** None (no 3+ property dots).
- **Duplicates:** Number seed twice in method (2× only). CloseParen parallel to wrap path (2 sites).
- **CC / conditionals:** Single `if` with `TryGetValue && IsPrefixCall` — `IsPrefixCall` already extracted.
- **Inappropriate intimacy / Func polymorphism:** None.
- **Class state over parameters:** Per-press `InputKey` is call state, not stable DI; fields already hold session state.

### Tests (`ContinuationTests.cs`)

- Methods: updated `PrefixFunctionAfterEqualsWrapsAnswer`; new `PrefixFunctionAfterEqualsProducesCompleteCalculatedExpression` (theory 10 rows); `PrefixFunctionAfterEqualsThenEqualsCalculatesResult`; `PrefixFunctionSeededFromAnswerCanBeExtended`.
- **LPL:** Theory 3 params (not >3). Facts 0.
- **`;` length:** Bodies ≤6 statements.
- **Statics:** Pre-existing `private static AssertPreview` — stateless test utility, not introduced by delta.
- **Duplicates:** Assert block in sin fact + theory (2× only).
- **Pre-existing pattern note (not opened):** `AssertPreview` also in `CalculatorTests` / `MemoryTests` with different bodies — optional future shared helper; out of this defect-fix necessity.
- **Magic InlineData:** Test oracles for Math.* of answer `1` — acceptable.
- **Naming:** Long but intent-clear.

### README

- Documentation string only; no code smells.

## TODO markers

_None added._ No smell locations warrant `//TODO:`.

## Named refactoring steps (this scope)

_None._ No behavior-preserving structural steps required.

## Related non-smell note

`Docs/_Current/issues.md` already records LOW [****]/test-name drift (artifact only, not product code). Not a code smell under this workflow.

## XP simplicity (this delta)

- Runs tests (263 Domain unit tests pass per user verification).
- Intent clear: complete prefix call after `=` so preview/eval work.
- No new product duplicate code; one-line close paren.
- Fewest classes/methods: no new types.

## Verdict

**No smells found in review scope.** One-line Domain fix + focused continuation tests + doc line.

## Loop progress notes (complete)

| # | Iteration theme | Result |
|---|-----------------|--------|
| 1 | Member signatures (LPL, data clumps, speculative generality) | None |
| 2 | Static members (+ [****] sub) | None |
| 3 | Feature Envy / TDA / chains / layers (1.c) | None |
| 4 | Implementation: &&/||, Long Method by `;`, comments | None |
| 5 | Inappropriate Intimacy / polymorphism | None |
| 6 | Feature Envy / TDA / chains per method | None |
| 7 | Data Class / Lazy Class | None — no new classes |
| 8 | Architecture / layer boundaries | None — Domain-only fix |
| 9 | Duplicate code vs pre-existing | None HIGH; optional AssertPreview scatter noted |
| 10 | Complexity | None — CC≤2 |
| 11 | Framework extensibility / named steps | No [~] findings; no steps |

*End prefix-after-equals smells audit — session `e1440e1e59dc4c5b82a0ac3936ad5698`.*
