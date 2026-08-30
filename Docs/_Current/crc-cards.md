# CRC Cards — URL Shortener

## As-Is CRC Cards

- **ShortCode (Domain)** — Class: value object holding a base62 code.
  - State: `Value` (string), const `Alphabet` [0-9a-zA-Z] (all new)
  - Behavior: `FromId(long)`, `Parse(string)`, `ToString()`, equality members (all new)
  - Collaborators: none (pure BCL)

- **ShortLink (Domain)** — Entity owning link lifecycle.
  - State: `Id`, `OriginalUrl`, `Code`, `CreatedAt`, `ClickCount` (all new)
  - Behavior: ctor URL validation, `AssignCode(long)`, `RegisterClick()` (all new)
  - Collaborators: `ShortCode` (Domain) only — no infrastructure collaborators

- **ShortenerDbContext (Data/Infrastructure)** — EF Core session/mapper.
  - State: `DbSet<ShortLink> Links` (new)
  - Behavior: `OnModelCreating` mapping only (new) — no business logic

- **LinkEndpoints (Presentation)** — Static extension class mapping HTTP endpoints.
  - Behavior: `MapLinkEndpoints`, create/redirect handlers, `ToResponse` (all new)
  - Flagged deviation: currently orchestrates the two-save create and click persistence (CRITICAL layer finding → refactor R1, fixes F1/F2)

- **CreateLinkRequest / LinkResponse (Presentation)** — API boundary records (new).

- **Program (Presentation)** — Composition root: DI, rate limiter, static files, EnsureCreated, endpoint mapping (new).

## To-Be CRC Cards

- **ShortLink (Domain)** (to-change) — gains `ToLinkResponse(Uri baseUri)` (R3), self-bound `AssignCode()` (R6/F4), extracted `IsAbsoluteHttpUrl` guard (R5a). Grows mapping + identity-invariant behavior.

- **ShortCode (Domain)** (to-change) — gains `IsValidCode(string)` guard method (R5b). New members only; class already exists.

- **ShortCode/ShortLink constants** (Domain) (new members) — `Base = 62`, buffer size, `UrlMaxLength` (R7).

- **LinkEndpoints (Presentation)** (to-change) — slims to a thin HTTP map delegating to a concrete collaborator owning `ShortenerDbContext` (R1); static `CreateGate` field removed after atomic create (R2/F1); atomic click increment via DB update (F2).

- **ShortenerDbContext (Data)** (to-change) — supports atomic `ClickCount` increment (F2); mapping otherwise unchanged.

- **TestServerFixture (tests)** (new) — shared WebApplicationFactory + temp SQLite lifecycle replacing ×3 duplicated fixtures (R4); reuses API `LinkResponse`, drops nested `LinkBody` (R9).

- **index.html script** (to-change) — extracted `createLink`/`showError`/`clearForm` functions, textContent-safe rendering, awaited refresh, handled fetch errors (R10/F3).

Layer validation: Domain classes have no Data/infrastructure collaborators and no external dependencies; Data contains no business logic (mapping only); after R1/R3, Presentation holds no domain logic or orchestration — state and logic maximized in Domain.

## Test CRC Cards (As-Is, all (new))

- **ShortCodeTests (Domain unit tests)** — [Theory] table-driven FromId/Parse boundary cases, round-trips, equality/inequality.
- **ShortLinkTests (Domain unit tests)** — ctor validation table, AssignCode/RegisterClick behavior.
- **LinkApiTests (Presentation integration tests)** — WebApplicationFactory, temp SQLite; create/400 cases, list, redirect, click counts, contract fields.
- **RateLimitTests (Presentation integration tests)** — override RateLimiting config (3 permits/2s window); 429, window expiry, redirect-not-limited.
- **ConcurrencyTests (tests)** — parallel creates → distinct codes.
- **TestServerFixture (tests)** (new) — planned shared fixture base (R4).

Testability: Domain cards are pure and unit-testable (no I/O); Data is exercised via integration tests; Presentation via WebApplicationFactory over HTTP; UI verified by serving test + manual checks. All [Theory] table-driven where parameters vary.

## Explicitly rejected types
- ILinkService/ILinkRepository (1-to-1 interface, YAGNI/banned fake reason), parameter objects, decorators, second response DTO, CreateGate replacement wrapper (replaced by DB-level atomicity).

---

Class design for the implemented URL shortener feature (source: Docs/_Current/prompt.md high-level plan; validated by correctness/standards and code-smells reviews).
