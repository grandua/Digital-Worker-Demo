# User Prompt

Create a high level plan for this task (high level plan is enough, do not run a full plan and design workflow): "Build a URL shortener REST API with base62 encoding, rate limiting, and SQLite storage using EF Core. Include a simple but polished static HTML page (no framework, no build step) for creating short links and following redirects. The page should be slick and minimal — one input, one button, a clean list of created short links, and click-to-redirect."

# High-Level Plan: URL Shortener (ASP.NET Core + EF Core/SQLite + Static HTML)

## 1. Architecture / Approach Overview

A **single ASP.NET Core web project** (minimal APIs) that serves both the REST API and the static HTML page. Physical components:

- **Minimal API host** (`Program.cs`): registers routing, static files, rate limiting middleware, EF Core.
- **Domain model**: a rich `ShortLink` entity that owns its behavior (URL validation, short-code assignment, click tracking) — no service blobs.
- **Persistence**: EF Core `ShortenerDbContext` + SQLite (`Microsoft.EntityFrameworkCore.Sqlite`), one table, file `shortener.db`. Thin repository (`IShortLinkRepository`) used by endpoints.
- **Rate limiting**: built-in ASP.NET Core `System.Threading.RateLimiting` middleware, fixed-window per client IP, applied to the create endpoint (429 on excess).
- **Frontend**: `wwwroot/index.html` — hand-written HTML/CSS/JS, no framework, no build step. Served via `UseStaticFiles` + `UseDefaultFiles`; API under `/api`, redirect route at root `/{code}`.

Interaction: Browser loads `index.html` → JS calls `POST /api/links` / `GET /api/links` → endpoints use domain entity + repository → SQLite. Clicking a short link hits `GET /{code}` → 302 redirect to original URL.

## 2. New Classes Planned

| Class | Responsibilities | New State/Fields | Associations | Methods |
|---|---|---|---|---|
| `ShortLink` (domain entity, rich) | Owns a shortened URL: validation, code derivation, click counting | `long Id`, `string OriginalUrl`, `string Code`, `DateTime CreatedAt`, `int ClickCount` | none (root entity) | `ShortLink(string originalUrl)` ctor — validates absolute http/https URL, throws on invalid; `AssignShortCode()` — derives `Code` from `Id` via Base62 (called once `Id` exists); `RegisterClick()` — increments `ClickCount` |
| `Base62Codec` (static domain utility) | Number↔base62 string conversion | alphabet const `[0-9a-zA-Z]` | used by `ShortLink` | `Encode(long value)`, `Decode(string code)` — throw on negative/invalid input |
| `ShortenerDbContext` | EF Core session/Unit of Work | `DbSet<ShortLink> Links` | maps `ShortLink` | `OnModelCreating` — configure keys/index on `Code` (unique) |
| `IShortLinkRepository` + `ShortLinkRepository` | Persistence boundary used by endpoints | holds `ShortenerDbContext` | operates on `ShortLink` | `Add(ShortLink)`, `GetByCode(string)`, `List(int count)`, `SaveChanges` |
| Endpoints (in `Program.cs` or `LinkEndpoints` static class) | HTTP plumbing only — parse, delegate, map status codes | none | uses `IShortLinkRepository` + entity behavior | `MapLinkEndpoints(WebApplication)`: `POST /api/links`, `GET /api/links`, `GET /{code}` |
| DTOs (`CreateLinkRequest`, `LinkResponse`) | API contract decoupled from entity | `Url`, `Code`, `OriginalUrl`, `ShortUrl`, `ClickCount` | mirrors entity outward | none |
| Rate limit config (in `Program.cs`) | Throttle link creation | policy: e.g. 20 requests / 60 s per IP, fixed window | applied to POST | `AddRateLimiter(...)` registration + `RequireRateLimiting` on POST |
| `index.html` (static) | UI: one input, one button, list of links | — | calls REST API | fetch create/list, render list, anchor click → redirect |

Behavior placement honored: code derivation and validation live **on `ShortLink`**, not in a service; the endpoint is a thin coordinator.

## 3. Data Flow / Control Flow

- **Create**: `POST /api/links {url}` → rate-limit middleware → DTO → `new ShortLink(url)` (validates; 400 on failure) → repository `Add` + save (gets `Id`) → `link.AssignShortCode()` → save → `201 {code, shortUrl}`.
- **Redirect**: `GET /{code}` → repository `GetByCode` → 404 if missing → `link.RegisterClick()` → save → `302 Location: link.OriginalUrl`. (Static-files middleware only handles existing files, so `/{code}` reaches routing.)
- **List**: `GET /api/links` → repository `List` (latest N) → 200 DTO array → page renders.
- **UI**: on load `GET /api/links`; button `POST`s; list items are anchors to the short URL (click → 302).

## 4. Integration Points / Project Structure (Greenfield)

Repo is effectively empty; create:

```
UrlShortener/                (web project)
  Program.cs
  Domain/ShortLink.cs, Base62Codec.cs
  Infrastructure/ShortenerDbContext.cs, ShortLinkRepository.cs
  Api/LinkEndpoints.cs, Dtos.cs
  wwwroot/index.html
UrlShortener.Tests/          (xUnit)
UrlShortener.sln
```

Existing files (`README.md`, `appsettings.json`) are untouched; `.gitignore` already suits .NET build output.

## 5. Implementation Sequence

1. Solution + projects scaffold; `Base62Codec` + `ShortLink` with unit tests (TDD).
2. EF Core: `DbContext`, repository, SQLite wiring, `EnsureCreated` on startup.
3. Endpoints (create/list/redirect) + integration tests.
4. Rate-limiting policy + 429 test.
5. Static `index.html` + manual verification.

## 6. Assessment

**Simple enough to implement from this high-level plan** — one entity, one utility, one repository, three endpoints, one static page; no complex associations or cross-aggregate logic. A full `/plan-and-design` workflow is **not** required.

## 7. Assumptions, Decisions, Trade-offs

- **Code generation**: derive code from SQLite autoincrement `Id` via Base62 → zero collisions by construction, no retry loop. Trade-off: sequential/predictable codes (acceptable, no auth demo).
- **Code length**: 1–7 chars, grows naturally from Id; no fixed padding.
- **Alphabet**: `[0-9a-zA-Z]`; `Decode` validates membership.
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

- **Unit — Base62Codec**: `Encode(0)`, known vectors (e.g. 1→"1", 62→"10", sample large values), `Encode(negative)` throws, `Decode` invalid char throws, round-trip `Decode(Encode(n)) == n` over a range.
- **Unit — ShortLink**: ctor rejects null/relative/non-http URL; `AssignShortCode` sets expected code from Id; `RegisterClick` increments.
- **Integration (WebApplicationFactory, temp SQLite)**: create→201 with code; create invalid→400; redirect→302 with Location; redirect unknown→404; list returns created items; click count increments after redirect.
- **Rate-limit integration**: burst POSTs beyond policy → some 429s (use isolated test policy with small limits).
