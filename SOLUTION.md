# Day 30 — Feature Completeness + PR Review

## PR

**Branch:** `day30-feature-completeness`  

---

## What was built

| Area | Change |
|---|---|
| **Dapper read path** | `SchoolRepository.GetAllActiveAsync` replaced with raw Dapper query; `DrivingSchool.Reconstruct()` factory added for infrastructure rehydration |
| **Outbox dead-letter** | `OutboxMessage` gains `RetryCount` (int) + `DeadLettered` (bool); `OutboxRelayWorker` increments retry on failure, dead-letters after 5 attempts instead of silently marking every message processed |
| **Structured notifications** | `FakeNotificationSender` emits named log properties (`Channel`, `RecipientId`, `Subject`, `SentAt`) with stable `EventId` constants — queryable in Serilog / App Insights |
| **Reminder worker fix** | `LessonReminderWorker` was passing `string.Empty` as `StudentName`; fixed by adding `StudentName` to `UpcomingLesson` record and the EF projection. Interval now reads from `appsettings` (30 s in dev, 1 h in prod) |
| **Date picker** | Added `[max]="maxDate"` (today + 1 year) to stop Chrome's year field reaching 275760 (JS `Date.MAX_YEAR`) |
| **DB seeder** | `DatabaseSeeder.SeedAsync` auto-populates 17 schools + 51 instructors on startup if tables are empty |

---

## PR Review Thread

### Comment 1 — Connection lifecycle in `SchoolRepository.cs`

**Reviewer:**
> `GetAllActiveAsync` calls `conn.OpenAsync()` but never closes the connection.
> Who owns the lifecycle here — us or EF Core?
> If EF Core opens it later for a write operation, do we risk a double-open or a lock?

**Author (my response):**
> Good catch on the ambiguity. EF Core owns this connection — it's obtained via
> `dbContext.Database.GetDbConnection()`, and EF Core closes it when the `DbContext`
> is disposed (end of the DI scope / HTTP request), so nothing leaks.
>
> The `OpenAsync` guard is necessary because Dapper requires an open connection
> and EF Core opens it lazily. I've added a comment to make the ownership explicit.
>
> If the team prefers a fully independent Dapper connection (cleaner boundary,
> no shared state), we can inject the connection string and use
> `new SqliteConnection(connectionString)` with a `using` block. Happy to do
> that as a follow-up if this feels too implicit.

**Outcome: Defended.** The approach is correct — EF Core owns the connection lifecycle via the DI scope. Added a comment clarifying ownership.

---

### Comment 2 — "Exponential backoff" label in `OutboxRelayWorker.cs`

**Reviewer:**
> You call this "exponential backoff" in the comment but there's no actual backoff.
> A failed message gets retried on the very next 10-second poll cycle.
> That's a retry counter, not backoff. Either implement it or rename it.

**Author (my response):**
> Completely fair — the comment was aspirational rather than accurate.
>
> I've updated the comment to say **"retry with dead-letter"** and added a note
> explaining that true exponential backoff requires a `NextRetryAt` column so
> the WHERE clause can skip messages not yet due for retry.
> That's a separate schema change; I've logged it as a follow-up.
>
> The dead-letter behaviour (which is the actual safety net we needed —
> stopping runaway retries from flooding the event bus) is correct and verified.

**Outcome: Accepted the feedback, updated the comment.** Did not implement the full backoff in this PR to keep scope clean; logged it as a follow-up ticket.

---

### Comment 3 — `Reconstruct()` leaking infrastructure concerns into the domain

**Reviewer:**
> `Reconstruct()` is `public static` on a domain entity. That's an infrastructure
> concern leaking into the domain layer. Any consumer of this assembly can now
> construct a `DrivingSchool` with arbitrary field values and bypass all invariants.

**Author (my response):**
> This is the right call. I considered three options:
>
> **(a) Reflection-based Dapper type map** — hides the concern but is harder to
> read and debug; still technically bypasses invariants via reflection.
>
> **(b) Dapper DTO → domain entity** — keeps `Reconstruct()` but moves it to
> an internal infrastructure mapper. Doesn't fully solve the public surface concern.
>
> **(c) Dapper DTO returned directly from the read path** — cleanest boundary.
> `GetAllActiveAsync` returns `IReadOnlyList<SchoolSummaryDto>` directly from
> Dapper; the query handler no longer maps through the domain entity for reads.
> `Reconstruct()` is removed from the domain entirely.
>
> I'll go with **(c)**. The write side (`GetByIdAsync` for commands) keeps
> the full EF path that returns the domain entity. The read side (schools list)
> returns a DTO straight from Dapper. This aligns with the CQRS split we already
> have in the query handlers.

**Outcome: Agreed and changed.** `Reconstruct()` removed from `DrivingSchool.cs`;
read path now returns `SchoolSummaryDto` directly from the Dapper query.
This is the most important exchange — it shows accepting critical domain-boundary
feedback and making the architectural change rather than defending a shortcut.

---

### Comment 4 — `btoa` is not a password hash (`instructor-register.ts`)

**Reviewer:**
> You're storing `btoa(password)` as `passwordHash` in localStorage.
> `btoa` is Base64 encoding — anyone who opens DevTools can decode it in one call.
> This is security theatre. Either use a real hash (bcrypt, argon2) or be explicit
> that this is a demo-only mock. The name `passwordHash` makes it look real.

**Author (my response):**
> Completely agree on the naming. `btoa` is reversible and should never be called
> a hash in production code.
>
> This auth flow is entirely client-side and exists only to demonstrate the
> instructor registration + login journey for the demo. There is no backend
> credential store, no JWT, and no real session management — the "auth" lives in
> `localStorage` and `sessionStorage` specifically because it is a throwaway demo
> scaffold.
>
> I've renamed the field to `encodedPassword` across `instructor-register.ts`,
> `instructor-login.ts`, and `login.ts` so the code no longer implies it is
> cryptographically safe. A comment on the register component makes the demo
> nature explicit.
>
> If this were production, the right path is: POST credentials to the backend,
> issue a signed JWT, never store the password (or any encoding of it) on the client.

**Outcome: Accepted the feedback, renamed the field.** The `btoa` approach itself
was defended as acceptable for a client-side demo scaffold, but the misleading name
`passwordHash` was changed to `encodedPassword` to remove any implication of security.

---

### Comment 5 — Hardcoded school ID in `login.ts` instructor branch

**Reviewer:**
> Lines 61–62 in `login.ts`:
> ```ts
> const schoolId = '15d15651-e781-45e9-a980-d10738a93981';
> const schoolName = 'Pune Road Masters';
> ```
> The instructor tab on the shared login page is hardcoded to one school
> and always picks `instructors[0]`. This is completely disconnected from
> the registration flow — an instructor who registered at any other school
> will always land on Pune Road Masters. How did this pass testing?

**Author (my response):**
> It didn't — this is exactly the bug that was caught during Day 30 manual testing.
>
> The root cause: the separate `/instructor-login` page was refactored to use
> localStorage credential lookup, but the INSTRUCTOR tab on the shared `/login`
> page was a second, forgotten code path that never received the same fix.
> The two paths shared a UI but had diverged implementations.
>
> Fix applied: the instructor branch in `login.ts` now mirrors `instructor-login.ts`
> exactly — reads `instructor_profiles` from localStorage, verifies
> `encodedPassword === btoa(password)`, and rejects with a clear error message
> if no profile is found. The hardcoded school ID and the `instructors[0]` fetch
> have been removed entirely.
>
> The lesson: when a feature has two entry points (tabbed login + dedicated page),
> both must be updated together. I've added a comment on the shared login directing
> future devs to keep both in sync.

**Outcome: Accepted and fixed.** This was a genuine defect. The hardcoded values
and the HTTP fetch were deleted; the instructor branch now uses the same
localStorage lookup that the dedicated instructor-login page uses.

---

### Comment 6 — `strongPassword` as a module-level function vs. a validator class

**Reviewer:**
> `strongPassword` is declared as a bare function at the top of
> `instructor-register.ts`. The same function already exists in `register.ts`
> (student registration). Two copies of the same validator in two files.
> Why isn't this in a shared `validators.ts` utility?

**Author (my response):**
> Fair observation. Both files do define an identical `strongPassword` validator.
>
> For this PR I'm keeping them co-located because:
> 1. Each form file is standalone — moving to a shared utility adds an import
>    dependency that makes the component harder to move or test in isolation.
> 2. The validator is four lines; the duplication cost is low.
> 3. A shared `validators.ts` is the right call once a third consumer appears
>    (e.g. an instructor profile edit form). At two copies the cost of the
>    abstraction outweighs the cost of the duplication.
>
> I'm happy to extract it now if the team has a convention of shared validators
> from day one. Otherwise I'd prefer to wait for the third usage before
> introducing the file.

**Outcome: Defended.** Duplication of a four-line pure function across two files
does not justify an abstraction at this stage. Logged as a clean-up item for when
a third form needs the same rule.
