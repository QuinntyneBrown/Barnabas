<div align="center">

# Barnabas

**A private lending, giving, selling, and helping board scoped to a single church congregation.**

Members post something to lend, give away, or sell, or offer their time — a ride, tutoring, help
moving. Barnabas brokers the introduction; the handoff happens in person.

[![Build](https://github.com/QuinntyneBrown/Barnabas/actions/workflows/ci.yml/badge.svg)](https://github.com/QuinntyneBrown/Barnabas/actions/workflows/ci.yml)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet&logoColor=white)](https://dotnet.microsoft.com/)
[![Angular](https://img.shields.io/badge/Angular-21-DD0031?logo=angular&logoColor=white)](https://angular.dev/)
[![Code of Conduct](https://img.shields.io/badge/Code%20of%20Conduct-Contributor%20Covenant-ff69b4.svg)](CODE_OF_CONDUCT.md)

[Watch the demo](docs/demo/barnabas-demo.webm) ·
[Getting started](#getting-started) ·
[Architecture](#architecture) ·
[Testing](#testing) ·
[Documentation](#documentation) ·
[Contributing](CONTRIBUTING.md)

</div>

---

## See it work

**[A five-minute demonstration](docs/demo/barnabas-demo.webm)** — the real application driving
itself, in one continuous take. No compositing, no screen capture, no second attempt: it is a
Playwright script using the same page objects as the acceptance suite, and it asserts as it goes,
so it cannot show something that did not happen.

[![The board, with a listing and its photograph](docs/demo/poster.png)](docs/demo/barnabas-demo.webm)

Signing in without a password · the board and its four kinds · posting with a photograph · asking,
accepting, and the thread it opens · closing out in the words of the kind · reporting and
moderating · issuing an invitation and letting somebody in.

`docs/demo/barnabas-demo.webm` — 5:01, 1280 × 720, 12 MB. GitHub plays it in the browser; so do
Chrome, Edge, Firefox, Safari 16.4+ and VS Code. The
**[chapter index](docs/demo/)** says what happens when, and how to re-record it.

## What Barnabas is

A parish has a ladder in one garage, a crib in another loft, and somebody with a free Saturday and
a van. Barnabas is the noticeboard that lets them find each other — private to one congregation,
and no larger.

- **Four kinds of listing, and they are not interchangeable.** *Lend*, *Give*, and *Sell* are
  goods; *Help* is time. Each collects its own fields, opens its own request form, and closes out
  in its own words: a loan is *returned*, a gift is *taken*, a sale is *sold*, an offer of time is
  *booked*.
- **Invitation only.** A moderator issues a code; somebody redeems it, supplies a profile, and
  waits to be let in.
- **One congregation, absolutely.** Every read and every write is constrained to the congregation
  in the caller's token, underneath the query rather than inside each handler. Reaching across the
  boundary answers `404`, never `403` — a refusal would confirm the thing exists.
- **Passwordless.** Identity is possession of the mailbox. Barnabas holds no password and no
  password hash, so there is nothing there to breach and no reset flow to protect.

## What Barnabas is not

It does not handle **payments**, **shipping**, or **delivery**. A Sell listing carries an asking
figure and says, on the same screen, that money changes hands between the two members in person.
Every endpoint that a member writes to *refuses by name* a card number, a delivery address, or a
deposit — rather than accepting the field and quietly dropping it, which would tell a caller their
card had been taken.

## Status

| | |
|---|---|
| High-level requirements | **18 of 18 complete** |
| Detailed requirements | **120 of 120**, each named by a test or by the budget harness |
| Backend acceptance tests | **306** passing, 0 skipped |
| Browser acceptance tests | **112** passing, 0 skipped |
| Build | 0 warnings, under `TreatWarningsAsErrors` |

Measured on a developer machine, against the budgets the specification sets:

| Measurement | Result | Budget |
|---|---|---|
| Board, 500 listings (p95) | 13.7 ms | 300 ms |
| Search, 500 listings (p95) | 7.9 ms | 500 ms |
| Write endpoint (p95) | 4.2 ms | 500 ms |
| Board, 50 congregations active (p95) | 25.2 ms | 300 ms |
| Largest Contentful Paint, simulated 4G | 472 ms | 2 500 ms |
| Cumulative Layout Shift | 0.0128 | 0.1 |
| Compressed JavaScript | 118 KB | 300 KB |

## Features

| Area | What it does |
|---|---|
| **Provisioning** | An administrator provisions a congregation, its neighbourhoods, and its first moderator. |
| **Invitation & joining** | A moderator issues a single-use code over an alphabet nobody misreads; redeeming it begins a joining session, and the profile that follows creates a member awaiting approval. |
| **Authentication** | Passwordless sign-in by emailed link, short-lived access tokens, rotating refresh tokens, and immediate sign-out. |
| **Profiles & directory** | What the congregation sees of a member, the kinds of help they offer, and who else is here. Never an email address. |
| **The board** | A mosaic of placards, filtered by kind, paginated by keyset cursor. The kind is carried in words as well as in colour. |
| **Listings** | Post, edit, photograph, shelve, restore, close out, delete — each in the vocabulary of its kind. |
| **Requests** | An ask against a listing, one open request per member per listing, enforced by a filtered unique index rather than by a check. |
| **Messaging** | An accepted request opens exactly one thread. Nothing else in the product starts a conversation. |
| **Notifications** | In-app, per-kind preferences, with an unread count on every screen at every width. |
| **Moderation** | Report a listing, review the queue, approve or remove; and let members in or turn them away. |
| **Photographs** | One per goods listing, decoded and re-encoded from pixels — so nothing embedded in the original survives — and served at a size chosen for where it appears. |

## Getting started

### Prerequisites

| | Version | Notes |
|---|---|---|
| [.NET SDK](https://dotnet.microsoft.com/download) | **10.0.101** or later | Pinned in [`global.json`](global.json) |
| [Node.js](https://nodejs.org/) | **22 LTS** or later | npm 10.9+ |
| SQL Server | Express, Developer, or LocalDB | Development defaults to `.\SQLEXPRESS` |

Barnabas persists to **SQL Server and to no other provider**. Development and both acceptance
suites use the same engine as production, so a filtered index, a `rowversion`, and a conditional
update behave in a test exactly as they will in a deployment. See
[ADR-0001](docs/adr/backend/0001-use-sql-server-for-persistence.md).

### Run it

```bash
git clone https://github.com/QuinntyneBrown/Barnabas.git
cd Barnabas

# Terminal one — the API on http://localhost:5003
cd backend/src/Barnabas.Api
dotnet run

# Terminal two — the client on http://localhost:4300
cd frontend
npm install
npm start
```

The API applies its migrations and seeds a congregation on first run. Open
<http://localhost:4300>, ask for a sign-in link with one of the seeded addresses below, and follow
it — in Development the link is written to an outbox the client reads back for you, so no mail
server is needed.

| Member | Address | Role |
|---|---|---|
| Marion T. | `marion@example.com` | Moderator of St. Aidan's. Owns the ladder. |
| Priya K. | `priya@example.com` | Member of St. Aidan's. |
| Grace Papadopoulos | `grace@example.com` | Member of St. Aidan's. |
| Hank W. | `hank@example.com` | Member of St. Brigid's — the other congregation, so every isolation assertion has a real other side. |
| Ada B. | `ada@example.com` | Administrator. Belongs to the platform congregation and to no parish. |

### The whole product, walked

```text
an administrator provisions a congregation and its neighbourhoods
  → a moderator issues an invite code
  → somebody redeems it, supplies a profile, and waits for approval
  → the moderator approves them
  → they post a Lend, a Give, a Sell and a Help listing, one with a photograph
  → another member searches the board, filters it, and requests each kind
  → the owner is notified, accepts, and a thread opens
  → both exchange a message and the owner closes out in the wording of the kind
  → a third member reports a listing and a moderator reviews it
  → a member edits their profile, browses the directory, and leaves
```

## Architecture

### Backend — Clean Architecture, vertical slices

Dependencies point inward. `Domain` references nothing.

```text
Barnabas.Api             controllers, middleware, observability
  └── Barnabas.Infrastructure   persistence, security, email, images
        └── Barnabas.Application  commands, queries, handlers, validators
              └── Barnabas.Domain   entities and rules, no dependencies
```

Controllers bind, dispatch through MediatR, and return. Everything a request does happens in a
handler, and everything a handler is *allowed* to do is declared on the request itself:

```csharp
public sealed record RemoveListingCommand(Guid ListingId)
    : IRequest<ReviewedListingResult>, IRequireRole
{
    public MemberRole RequiredRole => MemberRole.Moderator;
}
```

The pipeline reads those declarations — `ValidationBehaviour` → `MembershipBehaviour` →
`AuthorisationBehaviour` — so a handler that is reached has already been authorised, and no handler
can forget the check.

**The congregation boundary sits underneath every query.** A global filter is applied by reflection
to every entity implementing `ITenantOwned`, and the `DbSet` accessors *fail closed* when no
congregation is in scope rather than treating "unknown" as "unfiltered". A handler writes
`Listings.Where(l => l.Status == Active)` and receives only its own congregation's rows. There is
no `WithCongregation()` call to omit, which is the point: the safe path is the only path.

**Load-bearing rules live in the database, not in a check.** A filtered unique index decides that a
member has one open request per listing; an unfiltered one decides that they report a listing once;
a `rowversion` decides who wins two simultaneous acceptances. A read-then-write would pass both
racers.

### Frontend — Angular 21, zoneless, signals

```text
barnabas     pages, shells, routes, providers      → domain, components, api
domain       stores, guards, screen regions        → components, api
components   dumb, publishable primitives          → (nothing)
api          contracts, DTOs, typed clients        → (nothing of ours)
```

An import pointing the other way is a defect, not a shortcut. Services are consumed through an
interface and an `InjectionToken` declared together in a `*.contract.ts`; the application binds
each token to an implementation in exactly one place, which is what makes the seam a real one.

State lives in signals, behaviour lives in services, and a component class wires the two to a
template and does nothing else.

## Testing

Barnabas is built with **acceptance test-driven development**. Every test names the criterion it
covers, and a test narrower than the requirement it traces to says so.

```csharp
// Acceptance Test
// Traces to: L2-080, L2-081, L2-082, L2-083, L2-084
// Description: A member reports a listing and it is marked for a moderator without leaving the
// board. ...
```

```bash
# Backend — integration tests against the real API and a real database
cd backend
dotnet build           # 0 warnings; TreatWarningsAsErrors is on
dotnet test            # 306 passed, 0 failed, 0 skipped

# Frontend — Playwright against the running product, Page Object Model
cd frontend
npx playwright install --with-deps    # first run only
npx playwright test    # 112 passed, 0 failed, 0 skipped
```

`skipped: 0` is the number to watch. A skipped test is a requirement quietly not being met.

### Performance budgets

Wall-clock budgets do **not** gate the ordinary run — they can fail on a developer machine because
a build was running in another window, and a suite that goes red for that teaches people to ignore
it. They are run deliberately, and report the numbers they measured rather than only pass or fail.

```bash
# API: p95 latency at 500 listings, and across 50 concurrent congregations
cd backend/src/Barnabas.Api && dotnet run          # in another terminal
cd backend && dotnet run --project tools/Barnabas.Budgets

# Client: paint, layout stability, and transfer size, against a production build
cd frontend
npm run build
npx playwright test --config playwright.budgets.config.ts
```

## Project layout

```text
Barnabas/
├── backend/
│   ├── src/
│   │   ├── Barnabas.Api/              controllers, middleware, observability
│   │   ├── Barnabas.Application/      commands, queries, handlers, validators
│   │   ├── Barnabas.Domain/           entities and rules, no dependencies
│   │   └── Barnabas.Infrastructure/   persistence, security, email, images
│   ├── tests/Barnabas.IntegrationTests/
│   └── tools/Barnabas.Budgets/        the API performance harness
├── frontend/
│   ├── projects/{api,components,domain,barnabas}/
│   └── tests/e2e/{page-objects,specs,support}/
└── docs/
    ├── adr/                architecture decision records
    ├── detailed-designs/   one folder per feature, with C4 and sequence diagrams
    ├── mocks/              static HTML mocks of every screen — the visual authority
    ├── slices/             what each feature slice delivered, and what it did not
    └── specs/              L1.md (18 requirements) and L2.md (120, with criteria)
```

## Documentation

| | |
|---|---|
| [`docs/specs/L1.md`](docs/specs/L1.md) | The eighteen high-level requirements. |
| [`docs/specs/L2.md`](docs/specs/L2.md) | The 120 detailed requirements and their acceptance criteria. Every one is named by a test. |
| [`docs/adr/`](docs/adr) | Why the load-bearing decisions were taken, and what was rejected. |
| [`docs/detailed-designs/`](docs/detailed-designs) | Feature designs, with C4 and sequence diagrams. |
| [`docs/slices/`](docs/slices) | One document per feature slice: what it delivered, what it decided, and what it found or failed to finish. |
| [`docs/demo/`](docs/demo) | The [five-minute recording](docs/demo/barnabas-demo.webm) of the product working, its chapter index, and how to re-record it. |
| [`docs/mocks/`](docs/mocks) | Static HTML for every screen. The design system in `frontend/projects/barnabas/src/styles/` is a clean carry of `docs/mocks/styles.css`, so a diff between them stays short and reviewable. |
| [`AGENTS.md`](AGENTS.md) | The conventions this codebase is written to — architecture, naming, component placement, and the domain's vocabulary. Read this before your first pull request. |

## A note on vocabulary

The product's words are used exactly, in code and in copy, because the distinctions are the
product:

**listing** (a post; "post" is the verb) · **board** (never "feed") · **member** (never "user") ·
**congregation** or **parish** · **request** (an ask against a listing, which opens a **message
thread**) · **neighbourhood** (Commonwealth spelling, throughout).

## Contributing

Contributions are welcome. Please read [CONTRIBUTING.md](CONTRIBUTING.md) — it covers the
development setup, the acceptance-test-first workflow, and what a reviewer will look for — and
[AGENTS.md](AGENTS.md) for the conventions the codebase is written to.

By participating you agree to abide by our [Code of Conduct](CODE_OF_CONDUCT.md).

## Security

Please **do not** open a public issue for a security vulnerability. See
[SECURITY.md](SECURITY.md) for how to report one privately.

## Support

Questions, ideas, and help finding your way around: [SUPPORT.md](SUPPORT.md).

## License

Released under the [MIT License](LICENSE).

---

<div align="center">
<sub>Named for Barnabas, called the son of encouragement, who sold a field and brought the money to
the apostles. — Acts 4:36–37</sub>
</div>
