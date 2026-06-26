# Day 32 — Ship + Demo + Postmortem

## Live URL

**https://green-meadow-07312bf00.7.azurestaticapps.net**

---

## One-Page Postmortem

### What I'd Do Differently

**Start with auth architecture, not features.**
Instructor login was bolted on late — a localStorage-based scheme that stored credentials keyed by email with a base64 password hash. It worked, but it created a structural lie: the frontend thought it was talking to a real auth system while the backend had no idea the instructor had "logged in." This forced the two-phase seeding hack (fake UUIDs first, real IDs later) just to make demo login reliable. Had I designed instructor auth as a proper backend session from Day 1 — even a simple username/password endpoint returning a JWT — none of that complexity would exist.

**Design the notification pipeline end-to-end before writing a single handler.**
Notifications were wired up as an afterthought across multiple days. The outbox processor, integration events, and the frontend polling all touched different modules with no shared contract. When the student books a lesson and no notification appears for the instructor, it's impossible to tell which layer dropped it without tracing through four modules. A thin vertical slice — one event, one handler, one test, one UI row — on Day 1 would have given a working skeleton to build on instead of a mystery to debug later.

---

### What the Hardest Bug Taught Me

The hardest bug was **instructor demo login silently failing** with "No account found for this email."

The symptom was clear. The cause was invisible: `localStorage['instructor_profiles']` was empty in a fresh browser, so every email lookup returned nothing. The fix seemed obvious — seed the profiles on login. That broke too: seeding needed real instructor IDs from the backend, the backend only returned instructors that were already registered, and registration failed on duplicate license numbers. Each fix exposed the next dependency.

What it taught me: **a system that only works when the database is in a specific state is fragile by definition.** The real fix wasn't a smarter seeding strategy — it was separating "can the user log in right now" from "do we have the correct IDs." Phase 1 (fake UUIDs, instant, always works) handles the first. Phase 2 (real IDs, background, best-effort) handles the second. The user never waits, and the system degrades gracefully if the backend is unavailable.

The lesson generalises: when debugging a chain of failures, stop patching each link. Find the assumption that makes all of them true and break it.

---

### The One Thing I'm Proudest Of

The **premium SaaS visual redesign done entirely through a CSS token system** — no layout changes, no component rewrites, just a handful of new custom properties (`--card-bg`, `--shadow-card-hover`, `--border-blue-hover`, `--shadow-nav`) propagated across every component via SCSS.

It would have been easy to hardcode per-component colors and shadows. Instead, the design tokens act as a single source of truth: change `--card-bg` in `styles.scss` and every card in the app updates. The layered body background (dot texture + two radial gradients), the glassmorphism navbar, the `translateY(-4px)` card hover with a blue border highlight — all of it comes from four lines in `:root`.

What I'm proudest of isn't the visual result (though it looks noticeably better). It's that the approach is **maintainable**: a future redesign is a token swap, not a grep-and-replace across twenty SCSS files.
