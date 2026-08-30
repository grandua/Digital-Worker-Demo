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
- **Middleware ordering**: `UseDefaultFiles`/`UseStaticFiles` run before endpoint mapping; the redirect route never conflicts because a single-segment base62 `{code}` path never matches a real file, and `/api/*` routes are matched by endpoint routing first. Validation confirmed.
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

## 11. Plan Validation (Refinement Pass)

Re-validated in a second planning run (plan already existed from a prior run; refined and verified rather than rewritten):

- **Repo state re-verified**: still greenfield — `README.md` and `appsettings.json` are Digital Worker tooling files (no application code); `Docs/architecture.md` still absent; `.gitignore` still lacks `*.db` (update remains queued in Implementation Sequence step 1). No integration points changed.
- **Domain-first design / Anti-Procedural Checklist re-run**: passes. Behavior lives on `ShortLink` (validation, code assignment, click tracking) and `ShortCode` (encoding/parsing/equality); no anemic entities, no static utility classes, no service blobs, no repository interface (YAGNI at this scope); DTOs carry nothing but data; endpoints stay thin coordinators.
- **Architectural correctness validated**: single physical component (one ASP.NET Core host) serving both API and static page — interaction shown in §1/§3; route constraint keeps the catch-all redirect from swallowing `/api/*`, `index.html`, and static assets; value-object EF mapping via value conversion is the correct pattern; two-save create flow and id-derived codes confirmed sound.
- **Test cases reviewed**: boundary values verified arithmetically (e.g. alphabet `[0-9a-zA-Z]`: id 61 → `"Z"`, id 62 → `"10"`, id 3843 → `"ZZ"`, id 3844 → 3 chars).
- **Assessment confirmed**: implementable from this high-level plan alone — **no `/plan-and-design` required**. No open questions; no structural changes to the plan were needed, only the clarifications above.
