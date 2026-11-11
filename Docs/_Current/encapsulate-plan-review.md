# Plan Review: `encapsulate` smell detector (revised plan)

**Date:** 2026-11-07 (second pass)
**Scope:** only the `encapsulate` plan at the top of `Docs/_Current/prompt.md` (now lines 1-114). The historical Calculator content below it was not reviewed.
**Verdict:** APPROVE WITH CHANGES. Amendments R1-R17 are applied in `prompt.md` §5, §5a, Glossary, §6, §7 and §7a. Human checkpoints C1-C4 are **still open**. Implementation must not start without them.

## Alignment with the clarified requirements
| Requirement | Plan | Status |
|---|---|---|
| `.cs` only, case-insensitive | §3 Languages | OK |
| 0/1 positional path, CWD default, recursive, no flags/config/exclusions | §3 Path/Discovery, R9 | OK (hidden-entry default fixed in pass 1). §5 still said "pruned enumeration"; fixed |
| Repeated members count (both rules) | §3 Counting | OK |
| Same receiver expression text is enough, no aliasing | §3 Receiver key, R1, R2 | OK |
| FE: >= 2 external accesses per method/compound expression, excluding this/base | §3 FE, R8, R4, R15 | OK. The reported position was undefined; now defined (R15) |
| PO: >= 2 separate field/property args of the same receiver | §3 PO, R5, R6 | OK. §6 still used the dropped root-member rule for `link.Code!.Value`; fixed |
| .NET 10 / C# 14 | §2, §4 | OK. `Microsoft.CodeAnalysis.CSharp` 5.9.0 is the latest on nuget.org (re-checked) |

## CRC cards (user requirement)
- **They were absent.** The plan and `navigation-map.md` had only a class/state/behaviour table and no Responsibilities/Collaborators cards.
- **Added** as compact CRC cards for all 10 new classes in `prompt.md` §5a. No separate `crc-cards.md` was created (to keep the change minimal).

## Defects found in this pass
1. **Layer violation:** `SourceFile` was in Data, but Domain `Codebase` consumes it, so Domain depended on Data. Moved to Domain (R14).
2. **Wrong corpus expectation:** "Calculator.cs reports FE on `Buffer`" is false. Every unit has exactly one `Buffer` field/property access (lines 93, 102, 166, 198, 225, 237). It is now a negative regression (R16).
3. **Reported column undefined** for FE/PO. That makes R4 dedupe and the ordering tie-break untestable. Defined in R15.
4. **Stale PO test row** (`link.Code!.Value` counts `Code`) contradicted R5. Fixed in §6.
5. **LOC under-estimate:** about 300 LOC is not realistic for R1-R17 plus about 80 test cases. Re-estimated to about 600 (about 300 production, about 300 tests). The gate outcome is unchanged (> 200, so a navigation map is required).
6. **Navigation map drift:** "pruned", "root receiver key", member symbol (YAGNI), `CodeUnit.Examine(IOperation)` taking a parameter that is already its state, and `CallSite.Examine` returning a finding while the plan uses a computed `PassesObjectParts`. Map updated to match the plan.
7. **Missing tests** (mutation/boundary): FE+PO at the same position with the smell-name tie-break, named-argument column, `M(this.X, base.Y)` with no PO, tuple elements counted as fields (R15, R17).

## Architecture / KISS / statics / glossary
- `Docs/architecture.md` and `Docs/cross-class-concepts-glossary.md` are absent. The plan now states it explicitly: the sibling flow is none (greenfield, no CLI/scan/analyzer flow in the repo).
- A `## Cross-Class Concepts Glossary` section was added to the plan (Receiver Key, Smell Scan). Creating the file is deferred until the canonical classes exist. Nothing was written to the glossary in this review.
- No DI, no singletons. The only static member is `Program.Main` (R12). `SmellFinding` kind names are `const`.
- Anti-procedural check passes: classes are named after domain concepts and hold their own state and behaviour; there are no service/manager/helper classes; value objects are `SourceFile` and `SmellFinding`.

## Human approval needed (not given)
- **C1** Go/no-go on the amended plan.
- **C2** PO direct-receiver rule (R5).
- **C3** Indexers excluded (R7).
- **C4** PO includes `this`/implicit-this receivers (highest false-positive impact).
