# Contributing to Barnabas

Thank you for taking the time. This document covers how to get set up, how work is expected to be
done here, and what a reviewer will look for.

Two documents matter more than this one once you are writing code:

- **[AGENTS.md](AGENTS.md)** — the conventions the codebase is written to. Architecture, naming,
  component placement, and the domain's vocabulary. **Read it before your first pull request.**
- **[docs/specs/L2.md](docs/specs/L2.md)** — the 120 detailed requirements and their acceptance
  criteria. Every change traces to one.

By participating you agree to abide by our [Code of Conduct](CODE_OF_CONDUCT.md).

---

## Ways to contribute

- **Report a defect.** [Open an issue](https://github.com/QuinntyneBrown/Barnabas/issues/new/choose)
  with the acceptance criterion it violates, if you can identify one. Never open a public issue for
  a security vulnerability — see [SECURITY.md](SECURITY.md).
- **Improve the documentation.** The specifications, the ADRs, and the slice write-ups are as much
  the product as the code is.
- **Take a requirement further.** The specification is complete and implemented, but it is not the
  last word. If you think a criterion is wrong, say so in an issue before writing code against it.
- **Fix something you found.** Small, well-tested pull requests are the easiest to accept.

## Development setup

| | Version |
|---|---|
| [.NET SDK](https://dotnet.microsoft.com/download) | **10.0.101** or later (pinned in `global.json`) |
| [Node.js](https://nodejs.org/) | **22 LTS** or later, npm 10.9+ |
| SQL Server | Express, Developer, or LocalDB |

```bash
git clone https://github.com/QuinntyneBrown/Barnabas.git
cd Barnabas

cd backend && dotnet restore && dotnet build
cd ../frontend && npm install && npx playwright install --with-deps
```

Run the API on `http://localhost:5003` and the client on `http://localhost:4300`:

```bash
cd backend/src/Barnabas.Api && dotnet run    # terminal one
cd frontend && npm start                      # terminal two
```

The API applies its migrations and seeds a congregation on first run. In Development a sign-in
link is written to an outbox rather than sent, and the acceptance suite reads it back — no mail
server is needed.

> **Port 4300 is Barnabas.** Both the client and the sign-in link template assume it. If something
> else is already listening there, change both together.

## How work is done here

### Acceptance tests come first

Barnabas is built with acceptance test-driven development, and it is not a formality:

1. **Write the failing acceptance test**, naming the criterion it covers.
2. **Implement the least code** that makes it pass.
3. **Keep the criterion, the test, and the implementation aligned.** If the requirement is wrong,
   change the requirement — in the same pull request, and say so in the description.

Every test carries a trace comment, and a test narrower than the requirement it traces to says so
plainly:

```csharp
// Acceptance Test
// Traces to: L2-080, L2-081, L2-082
// Description: A member reports a listing and it is marked for a moderator without leaving the
// board. A moderator sees the queue with the reason and the poster; nobody else sees it at all.
```

```csharp
// L2-080 AC2: Given a listing a member has already reported, when they report it again, then the
// response is 409 Conflict and no second report is recorded.
[Fact]
public async Task The_same_member_cannot_report_the_same_listing_twice()
```

Backend tests are **integration tests against the running API** — not unit tests against a handler.
Most of what this product has to prove lives between the two: authentication, the congregation
filter, the ownership behaviour, and the error contract are all pipeline, and a test that called a
handler directly would assert none of them.

Frontend tests are **Playwright, using the Page Object Model**. One page object per screen; it owns
the selectors and the interactions. **Never put a selector in a test** — tests state intent, page
objects know the DOM.

### Run the gate before you push

```bash
cd backend  && dotnet build          # must be 0 warnings; TreatWarningsAsErrors is on
cd backend  && dotnet test           # must be 0 failed, 0 skipped
cd frontend && npx playwright test   # must be 0 failed
```

`skipped: 0` is the number to watch. A skipped test is a requirement quietly not being met, and a
pull request that adds one will be asked to explain it.

Kill a stale dev server before a Playwright run. A server left mid-recompile serves an error
overlay that intercepts clicks, and the failure that follows looks like a product defect.

### The performance budgets are separate

They do not gate the ordinary run, because they are wall-clock assertions that can fail on a
developer machine for reasons unrelated to correctness. Run them deliberately if you touched
anything that could affect them:

```bash
cd backend  && dotnet run --project tools/Barnabas.Budgets
cd frontend && npm run build && npx playwright test --config playwright.budgets.config.ts
```

## What a reviewer will look for

### Correctness lives in the right place

A rule that two callers could race past is not a rule a handler can enforce with a read followed by
a write. Barnabas puts those in the database — a filtered unique index for one open request per
member per listing, an unfiltered one for one report, a `rowversion` for who wins two simultaneous
acceptances — and reads the refusal. If you are adding a rule of that shape, expect to be asked
which constraint decides it.

### The congregation boundary is not optional

Every read goes through the global query filter, and the `DbSet` accessors fail closed when no
congregation is in scope. If you find yourself calling `IgnoreQueryFilters()` outside a seeder, a
test, or one of the three sanctioned stores, stop and say why in the pull request. Reaching across
the boundary answers `404`, never `403`.

### Simplicity, without reducing scope

> Implement requirements radically simply: the least code that satisfies the acceptance criteria,
> and nothing more. Simple in design, never reduced in scope.

A helper nothing else uses, an abstraction with one implementation, or a configuration knob nobody
asked for will be questioned. So will a stub, a `TODO`, or an unhandled edge case.

### Comments say *why*

The codebase's comments explain decisions and the alternatives that were rejected, not what the
line beneath does. Match the density and the register of the code around you. If a choice is
counter-intuitive — a 404 where a 403 would be natural, a deletion ordered after a save — say why
where somebody will find it.

### Vocabulary is exact

**listing** (a post; "post" is the verb) · **board** (never "feed") · **member** (never "user") ·
**congregation** or **parish** · **request** (an ask against a listing, which opens a **message
thread**) · **neighbourhood** (Commonwealth spelling, throughout). The four kinds — Lend, Give,
Sell, Help — are not interchangeable and each has its own close-out verb.

## Pull requests

- **Branch from `main`**, named `feature/<what-it-does>` or `fix/<what-it-fixes>`.
- **One concern per pull request.** A refactor bundled with a behaviour change is two reviews
  wearing one hat.
- **Say what you decided and why**, not only what you changed. Say what you left undone, and say
  what you found and did not fix. The [slice documents](docs/slices) are the model: they record
  what was delivered *and* what was not.
- **Include the verification output** — the three commands above and their counts.
- **Update the documentation you invalidated.** An ADR that is no longer true is worse than no ADR.

Commits are squash-merged, so the pull request title and description become the permanent record.
Write them for somebody reading the history in a year.

### A worked example

The pull requests on `main` are the house style. [#12](https://github.com/QuinntyneBrown/Barnabas/pull/12)
is representative: what it closed, the two decisions it rests on and why the alternatives were
worse, a latent defect it found in shipped code, one thing it deliberately did **not** fix and
where that is recorded, and the measured verification.

## Reporting a defect

A good report names the criterion. If you can say *"`L2-084 AC2` says a listing in another
congregation answers 404, and this answers 403"*, the fix usually follows immediately. If you
cannot find the criterion, describe what you did, what happened, and what you expected instead —
that is enough.

## Questions

See [SUPPORT.md](SUPPORT.md).
