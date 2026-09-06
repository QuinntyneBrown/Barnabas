# Feature slice 10 — performance, accessibility, observability

## Purpose

The last slice, and the one that makes the product operable rather than merely correct. Until now
Barnabas could not say whether it was well, nothing it logged could be followed across a request,
nobody could see what it was serving, and four accessibility criteria were unasserted. It also had
never been measured.

## L1 requirements touched

| L1 | Title | Before | After | Status |
|----|-------|--------|-------|--------|
| `L1-016` | Performance | 2 of 4 | **4 of 4** | **Complete** |
| `L1-017` | Accessibility | 3 of 7 | **7 of 7** | **Complete** |
| `L1-018` | Observability | 0 of 4 | **4 of 4** | **Complete** |

**All eighteen L1 requirements complete.**

## L2 requirements implemented

`L2-103`, `L2-104`, `L2-107`, `L2-110`, `L2-113`, `L2-114`, `L2-115`, `L2-116`, `L2-117`, `L2-118`,
`L2-119` — **29 acceptance criteria**.

## Decisions taken

### Observability

- **The correlation identifier is a log scope, not a value passed around.** Every entry a request
  produces is inside it — including EF Core's and the framework's — which is what `L2-117 AC1`
  means by "every log entry arising from it". A caller's own identifier is reused, because a
  request that crossed a proxy and this API should be one line of enquiry rather than two.
- **A supplied identifier is validated before it is written down.** Bounded to 128 characters and
  restricted to characters that cannot break a log line: a newline in a correlation identifier is
  how one forged entry becomes two convincing ones. An acceptance test sends exactly that.
- **`System.Diagnostics.Metrics`, not a monitoring library.** Barnabas records into the platform's
  own instruments; a deployment that wants OpenTelemetry adds the exporter and points it at the
  same meter. `/metrics` exists so `L2-118`'s "when metrics are scraped" is answerable without the
  product having chosen a vendor, and renders Prometheus text because that is what every collector
  reads.
- **Everything is tagged by route pattern, never by path.** `/listings/{listingId}` is one series
  rather than one per listing — a metric with an identifier in its tags grows without bound and is
  thrown away by whatever is storing it.
- **A 429 is counted as a rejection and not as an error.** A rate limit doing its job is the
  product working, and folding it into the error rate would make that look like a fault.
- **The health writer says which dependency and whether it is up, and nothing else.** The obvious
  alternative returns each check's exception and description, which for a database check is the
  connection string and the server version. `L2-116 AC3` refuses both, and the endpoint is
  anonymous because a monitor cannot sign in.
- **A refusal is logged with the exception's type name, never its message.** A refusal's message is
  written for the member and often carries what they typed — `L2-119` keeps that out of the log,
  and a test drives a description, a message body and an email address through the API and greps
  everything that was written.

### Accessibility

- **The spacing exception is implemented, not assumed.** `L2-110` says 44 by 44 pixels *or
  equivalent separating space*, so the test measures both: a control under 44 fails only if another
  target's centre is within 44 pixels of it. Without that, every inline link in a list row would
  have had to be padded to thumb size.
- **Three controls genuinely needed raising** and none could claim the exception: the filter chips
  sit in a row touching one another, and the bell and the brand sit beside each other. The file
  input needed it too — it is the only control in the product the design system never drew.
- **Contrast is computed from what the browser painted**, walking up the tree for the background,
  because a transparent element is drawn on whatever its ancestors painted. Reading
  `backgroundColor` off the element itself would have measured most of the product against
  `rgba(0,0,0,0)` and passed everything.

### Performance

- **The budgets do not gate the ordinary run**, and both suites say why in their own words. They
  are wall-clock assertions that can fail because a build was running in another window, and a
  suite that goes red for that teaches people to ignore it.
- **The page budgets run against a production build behind a server that gzips.** The development
  server serves 483 KB unminified with no compression; a deployment serves 118 KB. That difference
  is most of the paint budget, so measuring the dev server would have been measuring the build
  tool. `tests/e2e/support/serve-production.mjs` is fifty lines of static file server with an API
  proxy, used by nothing else.
- **The API budgets are a console harness**, out of `dotnet test` entirely, with its own
  congregations so that five hundred listings are never left where the acceptance suites can find
  them. It warms up before measuring, reports percentiles rather than an average, and reads its
  budgets from configuration so a deployment on slower hardware can state its own honestly.

## Defects this found

**The board's layout shift was 0.18, against a budget of 0.1.** The board rendered its masthead and
chips, then the placards arrived and shoved everything below them down the page. The masthead had
been saying "Loading listings…" and its own comment described skeletons that were never built; they
are built now, and the shift is **0.0128**.

They render only when there is nothing to show. Making them render on every load replaced the
placards a member was looking at with grey blocks each time they changed a filter, which the
acceptance suite caught immediately.

**The four post forms and the edit form hard-coded St. Aidan's neighbourhoods** — recorded in slice
9 as found-and-not-fixed, and fixed here. `L2-022` asks a member to choose from their own parish's
list, and the API would have refused a Parkdale member's own neighbourhood as one their
congregation does not offer. `CongregationStore` gained an `ensureLoaded` so five screens asking at
once make one call.

**One accessibility assertion was passing by having found nothing.** The touch-target test measured
the directory before its rows had rendered. It waits for the network to settle now, and promptly
found the row titles it had been missing — which is how the spacing exception came to be
implemented properly.

**The acceptance suite followed an absolute sign-in link.** The API builds it from a configured
template naming port 4300, so the budget suite — which serves the production build on a port of its
own — was sent to a server that was not there. The suite now follows the path and lets the base URL
supply the host.

## Measured numbers

The API harness, against SQL Express on a developer machine:

```
met  L2-103 AC1  the board at 500 listings                       p50   7.3ms  p95  13.7ms  budget 300ms
met  L2-103 AC2  a search at 500 listings                        p50   6.2ms  p95   7.9ms  budget 500ms
met  L2-103 AC3  posting a listing                               p50   2.5ms  p95   4.2ms  budget 500ms
met  L2-107 AC1  the board with 50 congregations active          p50   4.3ms  p95  25.2ms  budget 300ms
met  L2-107 AC2  another congregation's board beside a noisy one p50   4.9ms  p95   7.2ms  budget 300ms
```

The page budgets, against the production build behind a gzipping server:

```
largest contentful paint    472 ms     budget 2500 ms
cumulative layout shift     0.0128     budget 0.1
compressed javascript       118 KB     budget 300 KB
```

## Verification

```
backend   dotnet build   0 warnings
backend   dotnet test    306 passed, 0 failed, 0 skipped   (was 294)
frontend  ng build       0 errors
frontend  playwright     111 passed, 0 failed, 0 skipped   (was 103)

backend   dotnet run --project tools/Barnabas.Budgets            5 of 5 budgets met
frontend  npx playwright test --config playwright.budgets.config.ts   3 passed
```
