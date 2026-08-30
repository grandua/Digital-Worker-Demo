# Current Task Prompt

> Build a URL shortener REST API with base62 encoding, rate limiting, and SQLite storage using EF Core. Include a simple but polished static HTML page (no framework, no build step) for creating short links and following redirects. The page should be slick and minimal - one input, one button, a clean list of created short links, and click-to-redirect.

---

# High-Level Plan - URL Shortener (.NET 10, ASP.NET Core, EF Core + SQLite)

## 0. Review Verdict

This is a small, well-bounded greenfield feature. The plan is sufficient for implementation without a separate detailed design workflow. Keep the design layered, but avoid abstractions and features that are not needed by the three use cases.

No `Docs/architecture.md` exists. Follow standard layered ASP.NET Core practices and Rich Domain Model (PEAA) conventions: domain behavior owns invariants, application handlers coordinate use cases, infrastructure implements persistence ports, and the API is the composition and transport boundary.

## 1. Architecture and Approach

Create one `UrlShortener.sln` targeting `net10.0`, with four small source projects and focused test projects:

```
src/UrlShortener.Domain          entities, value objects, base62 encoding; no dependencies
src/UrlShortener.Application     use-case handlers and persistence port; depends on Domain
src/UrlShortener.Infrastructure  EF Core context, mapping, repository, migration; depends on Application and Domain
src/UrlShortener.Api             host, endpoints, DTOs, rate limiting, static files; composes all layers
tests/UrlShortener.Domain.Tests
tests/UrlShortener.Application.Tests
tests/UrlShortener.IntegrationTests
```

Dependency direction is `Api -> Application -> Domain`; `Infrastructure -> Application + Domain`, with Infrastructure registered only by the API composition root. Do not add mediator, generic repository, generic result, mapping framework, separate unit-of-work abstraction, controllers, or domain-event machinery.

Four source projects are acceptable here because they directly enforce the requested layered architecture. Keep each layer minimal; do not create one class per trivial operation beyond the classes listed below.

## 2. Components and Request Flow

```
Browser
  GET /                 static index.html, app.css, app.js
  POST /api/links       create a link; rate limited per client IP
  GET /api/links        return the 20 most recently created links
  GET /{code}           redirect to the target URL
       |
UrlShortener.Api        transport validation, DTO mapping, ProblemDetails, rate limiting
       |
Application handlers   coordinate create, resolve, and list use cases
       |
Domain                  ShortenedUrl, LongUrl, ShortCode, Base62Encoder
       |
Repository port <------- EF Core implementation and SQLite database
```

Use built-in exception handling/ProblemDetails and built-in ASP.NET Core rate limiting. Serve existing static files before endpoint execution. Apply the named create policy only to `POST /api/links`; do not rate-limit static assets, listing, or redirects.

## 3. Domain and Class Design

### Domain

| Class | Kind | State and behavior |
|---|---|---|
| `ShortenedUrl` | Aggregate root | DB-generated positive `Id`, `LongUrl Target`, optional `ShortCode Code` only during initial persistence, and UTC `DateTimeOffset CreatedAtUtc`. `Create(LongUrl, DateTimeOffset)` guards required target and UTC timestamp. `AssignCode(ShortCode)` permits exactly one assignment. No public setters. |
| `LongUrl` | Value object | `Create(string)` accepts a non-empty absolute HTTP or HTTPS URL of at most 2,048 characters and preserves the validated value. Equality is by value. Do not silently trim or rewrite user input. |
| `ShortCode` | Value object | `Create(string)` accepts exactly 1 to 11 ASCII characters from the canonical alphabet `0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz`. Equality is ordinal and case-sensitive. |
| `Base62Encoder` | Pure domain service | `Encode(long)` uses the canonical alphabet above, returns `"0"` for zero, and rejects negative values. Decoding is out of scope because no use case needs it. |
| `DomainValidationException` | Domain exception | Represents invalid domain input that the API maps to a 400 ProblemDetails response. Use `InvalidOperationException` for the programmer error of assigning a code twice rather than creating an exception hierarchy. |

Eleven characters are sufficient for every non-negative `long` encoded in base62. The production database starts IDs at 1, but zero remains a defined encoder boundary and is unit tested.

### Application

| Class | Responsibility |
|---|---|
| `IShortenedUrlRepository` | `CreateAsync(ShortenedUrl, CancellationToken)` atomically persists the entity, uses the domain `Base62Encoder` and `ShortCode` factory to derive its code from the generated ID, assigns it through the aggregate, persists the code, and returns with both ID and code populated; `FindByCodeAsync`; `ListRecentAsync(int, CancellationToken)`. |
| `CreateShortLinkHandler` | Creates `LongUrl` and `ShortenedUrl`, supplies time through injected `TimeProvider`, and delegates atomic persistence. |
| `ResolveShortCodeHandler` | Creates a case-sensitive `ShortCode` and resolves it, returning null when absent. |
| `GetRecentLinksHandler` | Requests the fixed page size of 20; no public pagination/query parameter is introduced. |

The repository's `CreateAsync` is intentionally use-case-specific rather than generic. Its EF implementation wraps the initial insert and code update in one explicit database transaction: save to obtain the positive ID, encode that ID, call `AssignCode`, save again, then commit. Any failure rolls back both writes. This keeps EF and generated-ID mechanics out of the domain and application handlers while preserving the aggregate's assign-once invariant.

### Infrastructure

| Class | Responsibility |
|---|---|
| `UrlShortenerDbContext` | Owns `DbSet<ShortenedUrl>` and SQLite configuration. |
| `ShortenedUrlConfiguration` | Maps value objects with conversions, stores the URL at length 2,048 and code at length 11, requires target/time, and creates a unique case-sensitive code index/collation. The code column is nullable only to permit the first insert inside the create transaction. |
| `ShortenedUrlRepository` | Implements the application port, explicit create transaction, no-tracking reads, and deterministic recent ordering by `CreatedAtUtc DESC, Id DESC`. |
| `InitialCreate` migration | Creates the single schema and is applied at startup for this single-instance demo. |

Persistent schema: `ShortenedUrls(Id INTEGER PRIMARY KEY AUTOINCREMENT, Code TEXT NULL UNIQUE COLLATE BINARY, LongUrl TEXT NOT NULL, CreatedAtUtc TEXT NOT NULL)`. SQLite cannot defer a `NOT NULL` constraint while obtaining the generated ID, so atomicity comes from the explicit transaction: a code-less row is never committed or observable outside that transaction, and `CreateAsync` never returns one.

### Anti-Procedural Checklist

- URL validity and preservation rules live in `LongUrl`, not an endpoint, handler, or repository.
- Base62 alphabet and encoding rules live in the domain `Base62Encoder`; Infrastructure only supplies the generated ID at the persistence boundary.
- The aggregate owns the assign-exactly-once invariant through `AssignCode`; the repository cannot bypass private state.
- Application handlers coordinate domain creation, time, cancellation, and ports without containing validation or persistence rules.
- EF mapping, generated-ID mechanics, explicit transaction handling, ordering translation, and migrations remain in Infrastructure.
- Endpoints perform transport/DTO mapping only, and domain objects are not serialized directly.
- No public-setter entity, procedural manager, generic repository, mediator, domain-event framework, or speculative extension point is introduced.

### API and static page

| Component | Responsibility |
|---|---|
| `Program.cs` | DI composition, ProblemDetails/exception handling, static files, rate limiter, endpoints, and startup migration. Expose `partial Program` for integration tests. |
| `LinkEndpoints` | Thin mappings for `POST /api/links`, `GET /api/links`, and constrained `GET /{code}`. |
| `CreateLinkRequest`, `LinkResponse` | Boundary DTOs only. `LinkResponse` contains code, absolute short URL, long URL, and creation time. Domain objects are never serialized directly. |
| `wwwroot/index.html` | Accessible labeled URL input, one submit button, status/error region, and recent-links list. |
| `wwwroot/app.css` | Responsive, polished system-font styling, visible focus states, reduced-motion support, and light/dark color schemes. |
| `wwwroot/app.js` | Load recent links, submit JSON safely, prepend successful results, render with DOM APIs/text content, and use same-tab anchors to `/{code}` for click-to-redirect. |

Do not add a copy button, framework, bundler, external CDN asset, or `target="_blank"`; these conflict with the requested one-button, click-to-redirect page or are unnecessary. Disable the submit button while a request is active and show server errors in the status region.

Constrain `/{code}` to the canonical base62 alphabet and length 1 to 11 so malformed paths and static asset names are not treated as short codes. API routes have precedence; static files are served first when a matching file exists.

## 4. Endpoint Behavior

1. `POST /api/links` accepts `{ "url": "..." }`. A valid URL produces `201 Created`, a `Location` header for `/{code}`, and `LinkResponse`. Missing/null/invalid values produce RFC 7807 `400`. A rejected request produces `429` and a `Retry-After` header derived from rate-limiter metadata.
2. `GET /api/links` returns `200` and at most 20 links ordered newest first, with ID as the tie-breaker. It takes no query parameters.
3. `GET /{code}` performs an ordinal, case-sensitive lookup. A match returns `302 Found` with the original URL in `Location`; an unknown valid code or malformed code path returns `404`.
4. Unexpected failures return generic `500` ProblemDetails without exception details outside Development.

The absolute short URL in responses is derived from the current request scheme and host. No CORS configuration is needed because the page and API are same-origin.

## 5. Rate Limiting

Use a configurable fixed-window policy partitioned by `HttpContext.Connection.RemoteIpAddress`, with documented defaults of 20 permitted create requests per one-minute window and queue limit zero. Use a stable fallback partition key when the remote address is unavailable. Do not enable forwarded-header trust unless trusted proxy configuration is explicitly added later.

Configuration belongs under the API project's appsettings, not the unrelated repository-root dispatcher settings. Validate rate-limit options at startup so non-positive permit/window values fail fast.

## 6. Existing Repository Integration

The repository is greenfield for this application: only the Digital Worker `README.md`, root dispatcher `appsettings.json`, and planning docs exist. There is no application code or architecture document to preserve.

- Add the solution, `src/`, and `tests/` trees; do not modify root `appsettings.json`.
- Add `src/UrlShortener.Api/appsettings.json` with the SQLite connection string and rate-limit options, plus Development overrides only if needed.
- Verify and extend `.gitignore` if necessary for `bin/`, `obj/`, test artifacts, the SQLite database, WAL/SHM files, and local Development settings. Do not assume it already contains these entries.
- Add concise run and test instructions to the existing README without replacing its current content.
- Confirm the .NET 10 SDK before scaffolding rather than treating a machine-specific version as a permanent repository assumption.

## 7. Decisions and Scope

1. ID-based base62 codes are collision-free and minimal but predictable. Predictability is accepted for this local/demo scope.
2. `302 Found` avoids permanent caching and permits future target changes even though editing is currently out of scope.
3. Startup migrations are acceptable for a single local instance, not a multi-instance production deployment.
4. Identical target URLs create distinct links; no deduplication is required.
5. The recent list is fixed at 20 to avoid premature pagination and validation surface.

In scope: create, recent list, redirect, base62 encoding, per-IP create throttling, EF Core SQLite persistence, migration, static page, automated C# tests, and browser smoke coverage.

Out of scope: authentication, users, analytics, custom aliases, expiration, editing/deletion, deduplication, QR codes, copy controls, distributed limiting, proxy deployment, Docker, CI, and production hardening.

## 8. Acceptance Criteria

1. A valid absolute HTTP/HTTPS URL can be shortened and returns `201`, a canonical 1-to-11-character base62 code, absolute short URL, and `Location` header.
2. Missing or invalid URL input returns consistent `400` ProblemDetails and creates no row.
3. An existing code returns `302` to the exact original target; unknown or malformed codes return `404`.
4. The configured number of create requests per IP is accepted, the next is rejected with `429` and `Retry-After`, and another IP has an independent quota.
5. Committed links remain resolvable after recreating the host against the same SQLite file, and failed creates leave no partial row.
6. `GET /api/links` returns no more than 20 links in deterministic newest-first order.
7. `GET /` serves a responsive, keyboard-usable page; create, error, list, and same-tab redirect flows work without a frontend build or external assets.
8. Build and tests pass, and mutation testing of domain/application code reaches 100% mutation score or every surviving/equivalent mutant is explicitly reviewed and documented. Do not weaken exclusions or assertions merely to obtain the number.

## 9. Required Automated Test Matrix

These are behavior partitions, not merely examples. Use explicit assertions that distinguish mutant behavior; add tests for any survivor reported by mutation tooling.

### Domain unit tests

- `Base62Encoder`: exact known results for 0, 1, 9, 10, 35, 36, 61, 62, 63, 3,843, 3,844, and `long.MaxValue`; output uses only the canonical alphabet; negative values including -1 and `long.MinValue` throw. These boundaries kill alphabet-order, division, remainder, loop, and comparison mutants without adding unused decode behavior.
- `LongUrl`: accepts exact HTTP and HTTPS absolute URLs, mixed-case schemes, query/fragment, and lengths 1 below and exactly 2,048 when otherwise valid; rejects null, empty, whitespace, leading/trailing whitespace, embedded control characters, relative URLs, missing host, FTP/file/javascript schemes, and length 2,049. Assert the exact original value is retained and value equality/inequality works.
- `ShortCode`: accepts length 1 and 11 and alphabet boundary characters `0`, `9`, `A`, `Z`, `a`, `z`; rejects null/empty/whitespace, length 12, separators, punctuation, non-ASCII lookalikes, and any character outside the alphabet. Assert ordinal case-sensitive equality.
- `ShortenedUrl`: creation retains target/time; rejects missing target and non-UTC time; begins without a code only for persistence; first assignment succeeds and second assignment throws without changing the original code.

### Application unit tests with a strict fake or mock repository

- Create handler passes the validated target and deterministic `TimeProvider` UTC value to persistence, forwards cancellation, and returns the populated aggregate.
- Invalid URL fails before repository invocation.
- Resolve handler passes a valid case-sensitive code and cancellation token, and returns both found and null outcomes; malformed code fails before repository invocation.
- Recent handler always requests exactly 20, forwards cancellation, and returns repository order unchanged, including an empty result.

### SQLite infrastructure and API integration tests

- Use temporary-file SQLite with the production provider and migrations. Do not substitute EF InMemory. Isolate database and limiter state per test.
- Migration creates the expected constraints; create persists target/time/code, IDs produce known sequential codes, the unique binary index treats case variants as distinct, and reads are ordinal case-sensitive.
- Repository recent query returns empty, fewer than 20, exactly 20, and truncates 21; verifies timestamp-descending and ID-descending tie ordering.
- Repository creation is atomic: successful create has no observable code-less intermediate row, and a forced failure before the second save rolls back the insert.
- POST tests cover valid HTTP/HTTPS, missing body/property, null, empty, malformed, unsupported scheme, 2,048 and 2,049 boundaries; verify status, content type, ProblemDetails shape, response fields, `Location`, database row count, and no exception leakage.
- Redirect tests cover existing code with redirects disabled in the client, exact `302` and `Location`, unknown valid code, too-long/invalid-character paths, and codes differing only by case. `/` remains the static page rather than being treated as an empty code.
- List tests cover empty and populated responses, fixed maximum 20, complete DTO mapping, and deterministic order.
- Rate-limit tests configure a small positive limit: requests 1 through N succeed, N+1 returns `429` with `Retry-After`, invalid requests still consume permits because limiting precedes endpoint validation, a distinct IP remains allowed, and quota resets after the configured window using controllable time where supported. Invalid zero/negative options fail startup validation.
- Persistence restart test creates with one host, disposes it, starts a second host against the same temporary SQLite file, then resolves and lists the link.
- Static asset tests verify `/`, `/app.css`, and `/app.js` content types; missing assets and malformed short-code paths return 404 rather than invoking resolution.

### Browser and quality verification

- One lightweight browser test or equivalent automated smoke verifies page load, labeled input/one submit button, successful create and prepend, visible error handling, safe rendering of URL text, and same-tab navigation through the short link. Keep visual desktop/mobile polish as a manual check because pixel-level aesthetics are not mutation-test concerns.
- Run mutation testing on Domain and Application. Review survivors, then strengthen assertions or document truly equivalent/unreachable mutants. Integration tests cover EF, middleware, routing, and serialization behavior that unit mutation tests cannot represent reliably.

## 10. Implementation Sequence

1. Verify SDK and repository ignore rules; scaffold solution/projects and add only required packages.
2. Implement Domain test-first: value objects, aggregate invariants, and canonical base62 encoder.
3. Implement Application handlers and repository port test-first with deterministic time/cancellation tests.
4. Implement EF Core mapping, migration, transactional repository, and temporary-file SQLite integration tests.
5. Implement API DTOs/endpoints, ProblemDetails, rate limiter, startup migration, and API integration tests.
6. Implement the three static assets and browser smoke; verify keyboard, desktop, and mobile behavior.
7. Add README run/test instructions; run formatting, build, all tests, mutation testing, and final manual smoke.

## 11. Risks and Assumptions

- Single local instance and local SQLite file are assumed. SQLite's single-writer behavior and startup migration strategy are acceptable only at this scale.
- Sequential codes reveal creation volume and are enumerable. Switching to random codes would require collision/retry behavior and is deliberately not designed now.
- Client-IP limiting uses the direct connection address. A future reverse-proxy deployment must explicitly configure trusted forwarded headers before relying on per-client quotas.
- Mutation score applies to meaningful authored domain/application C# code. Generated migrations, host bootstrap code, DTO property declarations, and static assets should be assessed with integration/browser tests rather than broad mutation exclusions that hide business logic.
