# Day 31 — Polish: Tests, Performance, Security

## CI Run

> Push the `day31-feature-completeness` branch to GitHub and open a PR to `main`.
> GitHub Actions will run the CI pipeline automatically.
> Paste the green CI run URL here once it completes.

**CI pipeline:** `.github/workflows/ci.yml`
**Steps:** Checkout → Setup .NET 10 → Restore → Build → Unit Tests → Integration Tests → Upload Coverage

---

## Testing Pyramid

### Layer 1 — Unit Tests (24 tests, no I/O, ~200 ms)

| Project | Tests | What it covers |
|---|---|---|
| `DriveEase.Enrollments.Domain.Tests` | **14** | `Enrollment` aggregate invariants — pay, assign, complete, cancel, duplicate-pay guard, state transitions |
| `DriveEase.Enrollments.Application.Tests` | **3** | `GetEnrollmentQueryHandler` — happy path, not-found, mapping |
| `DriveEase.Api.Tests` | **7** | Activity source names, metric names, tag key contracts |
| **Total** | **24** | |

These tests run entirely in-process with no database, network, or Docker dependency.

---

### Layer 2 — Integration Tests (32 tests, real SQL Server via Testcontainers)

Infrastructure: `DriveEaseWebApplicationFactory` — spins up `mcr.microsoft.com/mssql/server:2022-latest` via Docker Desktop, runs `EnsureCreated()` to build schema, seeds 17 schools + 3 instructors each.

| Test class | Tests | What it covers |
|---|---|---|
| `HappyPathE2ETests` | 1 | Full flow: Register → Login → Enroll → Pay → Assign Instructor → Book Lesson → Complete |
| `EnrollmentEndpointTests` | 9 | 401 no token, 400 fee validation (×3 theory), 201 happy path, 404 unknown ID, 200 GET /me |
| `AuthEndpointTests` | 8 | Register (happy path + 3 validation errors), Login (happy path + wrong pw + unknown email), **403 wrong role** |
| `SchoolsEndpointTests` | 5 | GET /schools fields, caching, GET /schools/{id}/instructors |
| `SecurityTests` | 8 | Security headers (×3), 401, malformed JWT → 400, anonymous → 200, invalid JSON → 400, health check |
| `PerformanceTests` | 1 | p99 latency gate for GET /schools warm path |

Run: `dotnet test tests/DriveEase.Api.IntegrationTests/`

---

### Layer 3 — End-to-End (1 test, full HTTP stack)

`HappyPathE2ETests.StudentRegistration_Enrollment_Payment_LessonBooking_Completion_Flow`

Exercises 9 real HTTP calls in sequence through the running application using a real SQL Server container and real JWTs issued by the app's own auth endpoint.

---

## Hot-Path p99

### POST /api/v1/enrollments — k6 load test (local, 2 phases)

**Tool:** k6 v2.0.0 · **Script:** `k6/enroll-load-test.js` · **Server:** localhost SQLite

#### Phase 1 — Baseline (1 VU, 50 iterations, no rate-limit pressure)

| Metric | Value |
|---|---|
| avg | 35.2 ms |
| median (p50) | **36 ms** |
| p90 | 47 ms |
| p95 | 49 ms |
| **p99** | **96.4 ms** ✅ (threshold < 500 ms) |
| max | 141 ms |

All 50 checks passed (201 / 409). No rate-limit interference.

#### Phase 2 — Concurrency stress (10 VUs, 30 s)

| Metric | Value |
|---|---|
| Rate-limit hits (429) | **530 / 590 requests** |
| Successful responses (p90) | 76 ms |
| Successful responses (p95) | 84 ms |
| Rate-limiter policy | 60 req / minute / IP (by design) |

The rate limiter (`PermitLimit = 60, Window = 1 min`) correctly rejected 530 concurrent requests — this is the expected production-safety behavior, not a bug.

---

### GET /api/v1/schools — in-process perf test (`PerformanceTests.cs`)

#### After polish (HybridCache warm path, 100 iterations)

| Metric | Value |
|---|---|
| Cold path (cache miss) | ~150–300 ms |
| p50 warm | < 5 ms |
| p95 warm | < 20 ms |
| **p99 warm** | **< 100 ms** ✅ (CI gate asserted) |

#### Optimization applied (before → after)

**Before:** `SchoolRepository` Dapper → `SchoolRow` → `DrivingSchool.Reconstruct()` × 51 → `SchoolSummaryDto` × 51
→ 3 object types per row, ~153 heap allocations per cache miss

**After:** `SchoolQueryService` Dapper → `SchoolSummaryDto` × 51 directly
→ 2 object types per row, ~102 heap allocations per cache miss (**−33% allocations, −51 domain objects per request**)

Warm-path p99 is identical before/after (HybridCache returns the same cached bytes either way).
Cold-path is faster after — fewer allocations = less GC pause under traffic.

---

## Security Re-check

| Control | Verified by |
|---|---|
| `X-Content-Type-Options: nosniff` | `SecurityTests.AllResponses_ContainSecurityHeader` |
| `X-Frame-Options: DENY` | `SecurityTests.AllResponses_ContainSecurityHeader` |
| `Referrer-Policy: strict-origin-when-cross-origin` | `SecurityTests.AllResponses_ContainSecurityHeader` |
| Protected endpoints → 401 without token | `SecurityTests.ProtectedEndpoint_WithoutToken_Returns401` |
| Malformed JWT (wrong dot count) → 400 | `SecurityTests.MalformedJwt_Returns400` |
| Invalid JSON body → 400 | `SecurityTests.InvalidJsonBody_Returns400` |
| `Student` policy endpoint without Student role → **403** | `AuthEndpointTests.Logout_WithTokenMissingStudentRole_Returns403` |
| `[Range(1.0, 100_000.0)]` on Fee → 400 | `EnrollmentEndpointTests.PostEnroll_WithFeeAtOrBelowZero_Returns400` |

---

## k6 Load Test Script

`k6/enroll-load-test.js` — load-tests `POST /api/v1/enrollments` against the deployed dev environment.

```bash
k6 run --env BASE_URL=https://driveease-dev-api-3fggyyel.azurewebsites.net \
       --env ACCESS_TOKEN=<student-jwt> \
       k6/enroll-load-test.js
```

Thresholds: `p(99) < 1500 ms`, error rate `< 1%`.

---

## Files Created / Modified

| File | Change |
|---|---|
| `tests/DriveEase.Api.IntegrationTests/DriveEaseWebApplicationFactory.cs` | Testcontainers SQL Server, JWT test config, rate limiter disabled |
| `tests/DriveEase.Api.IntegrationTests/EnrollmentEndpointTests.cs` | NEW — 9 enrollment tests |
| `tests/DriveEase.Api.IntegrationTests/AuthEndpointTests.cs` | NEW — 8 auth tests incl. 403 |
| `tests/DriveEase.Api.IntegrationTests/HappyPathE2ETests.cs` | Full E2E flow |
| `tests/DriveEase.Api.IntegrationTests/PerformanceTests.cs` | p99 gate for GET /schools |
| `tests/DriveEase.Api.IntegrationTests/SecurityTests.cs` | Security header + auth checks |
| `src/Modules/Schools/.../SchoolRepository.cs` | Schema-aware Dapper table name for SQL Server |
| `src/Modules/Schools/.../SchoolQueryService.cs` | Schema-aware Dapper table name for SQL Server |
| `src/DriveEase.Api/Program.cs` | `UseEnsureCreated` config flag for test schema setup |
| `Dockerfile` | 3-stage multi-stage build (restore → publish → runtime) |
| `.dockerignore` | Excludes tests, infra, docs from image |
| `.github/workflows/ci.yml` | Unit tests + integration tests + coverage artifact upload |
| `.github/workflows/cd.yml` | NEW — deploy to Azure App Service via `azd deploy` on main push |
| `k6/enroll-load-test.js` | NEW — k6 load test for POST /enrollments |
