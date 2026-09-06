# Changelog

All notable changes to Barnabas are recorded here.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project
adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

Each entry names the requirements it delivered. The full text of those requirements is in
[`docs/specs/L2.md`](docs/specs/L2.md), and each feature slice has a write-up in
[`docs/slices/`](docs/slices) recording what it decided, what it found, and what it deliberately
left undone.

## [Unreleased]

Nothing yet. The specification is complete and implemented; the first release has not been tagged.

---

## Initial implementation

Built in ten feature slices, each ending green and committed. All **18** high-level requirements
and all **120** detailed requirements are implemented, and every one is named by an acceptance test
or by the performance harness.

**306** backend acceptance tests and **112** browser acceptance tests, none skipped, with the build
clean under `TreatWarningsAsErrors`.

### Slice 1 — Borrow a ladder

The vertical slice everything else reuses: the MediatR pipeline, EF Core with a congregation-scoped
global query filter, JWT issue and validation, the `ProblemDetails` contract, the Angular workspace
with its `api` / `components` / `domain` libraries, and both acceptance suites with the trace-comment
convention. One listing kind — Lend — end to end.

### Slice 2 — The other three kinds

`L2-026`, `L2-028`, `L2-029`, `L2-030`, `L2-034`, `L2-037`, `L2-043`, `L2-055`–`L2-057`, `L2-063`.

Give, Sell and Help, across posting and requesting. The four kinds stop being interchangeable: each
collects its own fields and closes out in its own words. `IForbidFields` refuses a submitted price
on a gift by name rather than dropping it.

### Slice 3 — Listing lifecycle, board states, pagination

`L2-036`, `L2-038`–`L2-040`, `L2-045`–`L2-047`, `L2-064`, `L2-105`.

Edit, shelve, restore, delete, and a real 404 screen. The board's keyset cursor generalised to
every other collection. **Decided:** restore refuses when the listing was closed out — a returned
loan and a shelved one are both `Archived`, and only `ClosedOutAt` can tell them apart.

### Slice 4 — Provisioning, invitation and joining

`L2-001`–`L2-011`, `L2-013`, `L2-017`, `L2-090`, `L2-095`, `L2-099`.

`SeedData` stops being how a congregation comes into being. **Decided:** administrators belong to a
seeded *platform* congregation, so provisioning works through the ordinary sign-in and the ordinary
role gate with no second authentication scheme
([ADR-0002](docs/adr/backend/0002-settle-the-joining-policy-values.md)). Invite redemption is a
conditional update whose affected-row count is the answer.

**Fixed:** `AuthorisationBehaviour` read the caller's role before checking that one was resolved, so
an anonymous caller on a role-gated command got 500 rather than 403; the sign-in lookup required an
*approved* member, making the awaiting-approval screen unreachable; three screens hard-coded one
parish's name for every congregation.

### Slice 5 — Profiles and the directory

`L2-020`–`L2-025`, `L2-059`, `L2-068`, `L2-069`, `L2-076`–`L2-079`.

**Fixed:** `L2-059 AC2` had never been delivered — a requester's name did not open their profile —
and slice 1's count was corrected from 31-in-full to 30-in-full and 3-in-part.

### Slice 6 — Search

`L2-048`–`L2-053`, `L2-088`.

`EF.Functions.Like` over title and description, reusing the board's cursor and the congregation
filter. **Decided:** the searched columns declare `SQL_Latin1_General_CP1_CI_AS` explicitly, so
case-insensitivity is a property of Barnabas rather than of whichever instance a developer
installed.

### Slice 7 — Notifications

`L2-070`–`L2-075`.

In-app only. **Decided:** notifications stage into the calling handler's own unit of work and never
save, so a request refused by the unique index and an acceptance refused by the row version both
leave nothing behind. A disabled kind is never *created*, not merely hidden.

**Fixed:** the development reset and the integration-test reset were two lists that had drifted, so
five tables were emptied between integration tests and left standing between browser tests.

### Slice 8 — Moderation

`L2-080`–`L2-087`.

Report a listing; review the queue; let members in or turn them away. **Decided:** `Flagged` is a
mark rather than a status, because only `Active` appears on the board and an approved listing must
*stay* there; and `Removed` is distinct from `Archived`, or restore would be an undo button for a
moderator's decision. An unfiltered unique index on `(ListingId, ReporterId)` decides "already
reported".

### Slice 9 — Photographs and hardening

`L2-032`, `L2-091`, `L2-092`, `L2-097`, `L2-098`, `L2-100`–`L2-102`, `L2-106`.

One photograph per goods listing, decoded and re-encoded from pixels — so nothing embedded in the
original survives — and served at a size chosen for where it appears. Security headers, HTTPS
redirection with HSTS, a declared JSON encoder, and a member's right to export and to be forgotten.

**Decided:** SkiaSharp rather than ImageSharp, which moves to a split licence at 3.0. The request
body limit became a property of the endpoint, because one number could not both refuse 1 MB of JSON
and accept a 2 MB photograph. Erasure anonymises in place rather than deleting, so the surviving
party's thread stays legible.

**Fixed:** the acceptance clock was frozen, so every row in a test shared an instant and two suites'
worth of "newest first" assertions were being decided by a random identifier tiebreak.

### Slice 10 — Performance, accessibility, observability

`L2-103`, `L2-104`, `L2-107`, `L2-110`, `L2-113`–`L2-119`.

Health per dependency, a correlation identifier on every log entry, metrics by route, and telemetry
carrying nothing a member wrote. Touch targets, contrast, reduced motion and semantics, each
asserted rather than assumed.

**Decided:** the wall-clock budgets do not gate the ordinary run, and both harnesses report the
numbers they measured rather than only pass or fail. The page budgets run against a production build
behind a server that compresses, because a development server serves four times the bytes a
deployment does.

**Fixed:** the board's cumulative layout shift was 0.18 against a budget of 0.1 — it rendered, then
the placards arrived and shoved the page. Now 0.0128. The four post forms hard-coded one parish's
neighbourhoods. One accessibility assertion had been passing by measuring a screen before its rows
had rendered.

[Unreleased]: https://github.com/QuinntyneBrown/Barnabas/commits/main
