# Support

Barnabas is maintained by volunteers. Please read this before opening an issue — it will usually
get you an answer faster.

## Where to ask

| I want to… | Go to |
|---|---|
| Ask a question, or work out whether something is a defect | [GitHub Discussions](https://github.com/QuinntyneBrown/Barnabas/discussions) |
| Report a defect | [Open a bug report](https://github.com/QuinntyneBrown/Barnabas/issues/new?template=bug_report.yml) |
| Suggest a change | [Open a feature request](https://github.com/QuinntyneBrown/Barnabas/issues/new?template=feature_request.yml) |
| Report a security vulnerability | **[Privately](https://github.com/QuinntyneBrown/Barnabas/security/advisories/new)** — never a public issue. See [SECURITY.md](SECURITY.md). |
| Contribute code | [CONTRIBUTING.md](CONTRIBUTING.md) |

Issues are for work that can be done. If you are not sure yet, a discussion is the right place, and
nobody will mind.

## Answer it yourself, faster

Most questions about *why* Barnabas does something have already been written down.

| Question | Where the answer is |
|---|---|
| What is this supposed to do? | [`docs/specs/L2.md`](docs/specs/L2.md) — 120 requirements with acceptance criteria. Search for the endpoint or the screen. |
| Why is it built this way? | [`docs/adr/`](docs/adr) for the load-bearing decisions, and [`docs/detailed-designs/`](docs/detailed-designs) for the feature designs. |
| Why was *this particular* choice made, and what was rejected? | The comments. The codebase explains decisions rather than restating the line beneath, and the counter-intuitive ones — a 404 where a 403 would be natural, a deletion ordered after a save — say why where you will find them. |
| What did each stage of the build deliver, and what did it not? | [`docs/slices/`](docs/slices). Each one records what was decided, what was found, and what was deliberately left undone. |
| What are the conventions? | [`AGENTS.md`](AGENTS.md). |
| What is it meant to look like? | [`docs/mocks/`](docs/mocks) — static HTML for every screen. |

## Common setup problems

**The client starts but nothing loads.** The API is not running, or it is not on
`http://localhost:5003`. The client proxies `/api` there; see `frontend/proxy.conf.json`.

**Port 4300 is already taken.** Barnabas assumes 4300 for the client, and the API's sign-in link
template names it too. If you move one, move both — `frontend/angular.json` and
`Database`/`SignInLink` in `backend/src/Barnabas.Api/appsettings.Development.json`.

**The API cannot reach the database.** Development defaults to `.\SQLEXPRESS`. Point
`Database:ConnectionString` at whichever instance you have. Barnabas persists to SQL Server and to
no other provider — see [ADR-0001](docs/adr/backend/0001-use-sql-server-for-persistence.md) for
why.

**I never receive a sign-in link.** You are not meant to. In Development the link is written to an
outbox rather than sent, and the client reads it back for you. No mail server is needed.

**Playwright fails on things that work by hand.** Kill any stale dev server first. One left
mid-recompile serves an error overlay that intercepts clicks, and the failure reads like a product
defect.

**A performance budget failed.** They are wall-clock measurements and are excluded from the
ordinary run for exactly that reason. Run them deliberately, on an otherwise idle machine, and read
the number they print rather than only whether they passed — see the Testing section of the
[README](README.md#testing).

## Response times

This is a volunteer project. There is no service level, and no guarantee of a reply. Issues that
name the acceptance criterion they violate, or that come with a failing test, are the ones that get
looked at first — because they are the ones somebody can act on immediately.

Security reports are the exception: see [SECURITY.md](SECURITY.md) for the timelines we hold
ourselves to.
