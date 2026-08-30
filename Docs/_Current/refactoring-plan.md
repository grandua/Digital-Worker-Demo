# URL Shortener Refactoring Plan — CURRENT STATE

**Mode:** AUDIT ONLY — findings + `//TODO` markers only; no behavior/architecture/implementation fixes.  
**Scope:** UrlShortener + UrlShortener.Tests (unstaged feature files listed in audit prompt).  
**Verification:** `dotnet build` 0 errors; `dotnet test` 59/59 passed; XPlat Code Coverage = 100% line on all changed product files.  
**Architecture (current):** Domain (`ShortLink`, `ShortCode` — zero external deps) · Data (`ShortenerDbContext`) · Presentation (`Program`, `LinkEndpoints`, `LinkRegistry`, DTOs / `ToLinkResponse` extension).

Smell thresholds: method body ideally ≤10 `;`-statements; params ≤3 (ctors exempt); static non-constants flagged; duplicate >2 = HIGH; feature envy; message chains (3+ dots); magic numbers/strings; naming.

---

## Stale findings (FIXED — do not re-open)

| Prior ID | Was | Status |
|----------|-----|--------|
| C1 | Endpoints own EF + two-save create orchestration | **FIXED** — `LinkRegistry` owns transactional create + click update; endpoints thin HTTP |
| C-POST1 | Domain→Api via `ToLinkResponse` | **REJECTED false positive** — mapping is `ShortLinkResponseExtensions` in Api; Domain has zero Api refs |
| H1 | Static `SemaphoreSlim CreateGate` | **FIXED** — removed; DB transaction + unique code index |
| H2/H3/H4 | Static handler envy / `ToResponse` on endpoints | **FIXED** — `LinkRegistry` + Api extension mapper |
| H5 | Test WAF×3 fixture duplication | **FIXED** — shared `TestServerFixture` |
| M5 | `AssignCode(long id)` | **FIXED** — parameterless `AssignCode()` uses `this.Id` |
| M8/M9/M13/M14 | Fat create/redirect lambdas + temporal coupling in endpoints | **FIXED** — thin handlers; create UoW in registry transaction |
| M11/M12 | Complex conditionals in ctor/Parse | **FIXED** — `IsAbsoluteHttpUrl` / `IsValidCode` |
| M1/M7/M15/R10 | Compressed JS + unsafe DOM | **FIXED** — extracted helpers; `textContent` / `replaceChildren`; named 201/429 |
| M-POST1 / R12 | Duplicate `"create-link"` string | **FIXED** — `LinkEndpoints.CreateLinkPolicy` single source |
| L-POST1 / R13 | Tests re-set connection string | **FIXED** — `RateLimitTests` only overrides rate settings; concurrency uses base |
| R1–R10, R11–R12 | Prior refactor phase | **DONE** (see Completed section) |

Also remediated (correctness, not pure smells): atomic transactional create; `ExecuteUpdateAsync` click increments; forwarded headers; no static semaphore.

---

## CRITICAL (open)

_None._ Domain clean; no circular deps; no Domain external deps; presentation layering matches stated architecture.

---

## HIGH (open)

_None._ No method >20 statements; no param lists >3; no static mutable state; no duplicate code >2 occurrences; no Feature Envy requiring move.

---

## MEDIUM (open — refactor when convenient)

| ID | Location | Smell | Snippet / note | Planned refactor |
|----|----------|-------|----------------|------------------|
| M-CUR1 | `Program.cs:36` | Message chain (3+ dots) | `scope.ServiceProvider.GetRequiredService<ShortenerDbContext>().Database.EnsureCreatedAsync()` | Optional further extract on `IServiceProvider`/`WebApplication` helper that hides the chain (already inside `EnsureDatabaseCreated`) |
| M-CUR2 | `Data/ShortenerDbContext.cs:12` | Magic number | `HasMaxLength(2048)` | `Introduce Constant` e.g. `ShortLink.OriginalUrlMaxLength` or data-layer named const (behavior-preserving only) |
| M-CUR3 | `Api/LinkEndpoints.cs:13` | Magic string (route constraint) | `regex(^[0-9a-zA-Z]+$)` | Named const for alphabet route pattern aligned with `ShortCode` alphabet intent |
| M-CUR4 | `Tests/RateLimitTests.cs:17` | Magic number | `await Task.Delay(3200)` | Named test const `RateLimitWindowBufferMs` (or derive from configured window + skew) |

---

## LOW (open — nice to have)

| ID | Location | Smell | Snippet / note | Planned refactor |
|----|----------|-------|----------------|------------------|
| L-CUR1 | `Api/LinkEndpoints.cs:23` | Mild Tell-Don't-Ask | `Results.Created($"/api/links/{link.Code}", link.ToLinkResponse(...))` asks `Code` while mapping already exposes it | Build Location from mapped `LinkResponse.Code` / `ShortUrl` only |
| L-CUR2 | `Domain/ShortCode.cs:12-14`, `Data/ShortenerDbContext.cs:12-13`, dense test one-liners | Intent / compression | Multi-statement physical lines | Expand only when next touched; not Long Method by `;` count |
| L-CUR3 | `LinkRegistry.CreateAsync` | Temporal coupling (documented) | Add → Save → AssignCode → Save inside transaction | Keep (EF identity pattern); only change if Id strategy changes (would be design/behavior — track in issues if pursued) |
| L-CUR4 | Static inventory keep-list | Document only | VO factories, Map* extension, consts, operators | No change — acceptable statics |

---

## Static member inventory (current)

| Member | Kind | Verdict |
|--------|------|---------|
| `LinkEndpoints` static class | Minimal API host | Acceptable |
| `CreateLinkPolicy` | const | Acceptable |
| `MapLinkEndpoints` / handlers / `GetBaseUri` | static | Acceptable (no mutable state) |
| `ShortLinkResponseExtensions.ToLinkResponse` | extension | Acceptable |
| `ShortCode.FromId` / `Parse` / `IsValidCode` | VO factories | Acceptable |
| `ShortCode` Alphabet/CodeBase/BufferLength | const | Acceptable |
| `==` / `!=` | operators | Acceptable |
| `ShortLink.IsAbsoluteHttpUrl` | private static util | Acceptable |
| `Program.EnsureDatabaseCreated` | local function | Acceptable |
| ~~`CreateGate`~~ | ~~static mutable~~ | **Removed** |

---

## Named refactoring steps (open only — behavior-preserving)

| Step | Standard refactoring | Target | Addresses | Detail |
|------|---------------------|--------|-----------|--------|
| R14 | **Extract Method** / hide chain | `EnsureDatabaseCreated` body | M-CUR1 | Optional one-liner wrapper for GetRequiredService+EnsureCreated |
| R15 | **Introduce Constant** | `2048` max URL length | M-CUR2 | Shared name used by EF config (and domain guard if ever enforced same limit) |
| R16 | **Introduce Constant** | route regex | M-CUR3 | `LinkEndpoints` or shared with ShortCode alphabet character class |
| R17 | **Introduce Constant** | `3200` delay | M-CUR4 | Test const tied to window seconds |
| R18 | **Replace Temp/Inline** Location | `CreateLink` Created URI | L-CUR1 | `var response = link.ToLinkResponse(...); Results.Created($"/api/links/{response.Code}", response)` |

**Rejected / not planned:** new Parameter Objects; `ILinkService` 1-to-1; moving `LinkRegistry` solely for purity (architecture already places it in Presentation); re-introducing static gates; Domain holding Api DTOs.

---

## Suggested order

1. **R18** (LOW, local) → **R15/R16** (MEDIUM constants) → **R17** (tests) → **R14** (optional polish).  
2. Do **not** reopen FIXED table items.  
3. Correctness/behavior gaps stay in `issues.md` only (none filed from this smell pass).

---

## Decisions and trade-offs

1. **AUDIT only** — `//TODO` markers allowed at open smell sites; no logic/tests/architecture edits.  
2. Prior plan body described pre-refactor state; this file is the **current** source of truth.  
3. XP: tests green (59/59); intent mostly clear after R5–R10; no product duplication; class count already minimal.  
4. Residual MEDIUM items are polish, not blockers.  
5. `LinkRegistry` two-save transaction is intentional EF identity allocation — not a CRITICAL smell.

---

## Completed refactor notes (historical)

- R1: `LinkRegistry` + thin `LinkEndpoints`  
- R2: removed `CreateGate`  
- R3: `ToLinkResponse` extension in Api (not Domain)  
- R4: `TestServerFixture`  
- R5a/b: `IsAbsoluteHttpUrl` / `IsValidCode`  
- R6: parameterless `AssignCode`  
- R7: named CodeBase/BufferLength + rate defaults/policy  
- R8: `EnsureDatabaseCreated`  
- R9: naming cleanup  
- R10: JS extract + safe DOM + named statuses  
- R11: rejected false positive  
- R12: single `CreateLinkPolicy`  
- R13: connection-string dup removed from rate tests  

---

## XP simplicity check (current)

- Runs all tests (59/59).  
- Intent clear in Domain; minor compression residual (L-CUR2).  
- No duplicate product code.  
- Fewest classes needed for stated architecture.
