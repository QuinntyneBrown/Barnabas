# ADR-0001: Use SQL Server for Persistence

**Date:** 2026-09-06
**Category:** backend
**Status:** Accepted
**Deciders:** Quinntyne Brown

## Context

The detailed designs specify SQL Server. Twenty C4 diagram nodes across the seventeen feature
designs declare `ComponentDb(db, "Barnabas database", "SQL Server", ...)`, and the class diagrams
in `requests/accept-a-request` and `requests/make-a-request` declare `+byte[] RowVersion` — the
SQL Server `rowversion` idiom.

The implementation did not. The Stage 0 scaffold pinned `Npgsql.EntityFrameworkCore.PostgreSQL`
and made `ListingRequest.RowVersion` a `uint`, which is the PostgreSQL `xmin` idiom, and every
later stage was built on that. The only written record of the choice was an XML comment in
`Directory.Packages.props` asserting that "Postgres remains the production provider" — a claim
that contradicted twenty diagrams and that nobody had reconciled.

A second provider then accumulated on top. SQLite was added so the acceptance suites could run on
a machine without a container runtime, since PostgreSQL was reached through Testcontainers. That
bought portability at a price: `SqliteTimestamps` existed because SQLite cannot order a
`DateTimeOffset`, the filtered index carried a provider-neutral predicate, and the concurrency
token was configured one way for PostgreSQL and another for SQLite.

The price was not only complexity. **Four acceptance criteria never ran.** `L2-016 AC2`,
`L2-018 AC4`, `L2-060 AC4` and `L2-062 AC5` each require a real row version and a genuine second
writer. SQLite can express neither, so they were marked to require PostgreSQL and reported as
skipped — and PostgreSQL needed Docker, whose daemon was unavailable throughout development. The
filtered unique index, the row version and the conditional updates were all written and none was
ever exercised.

## Decision

Persist to **SQL Server**, as the designs always said, with **no second provider**.

Development, both acceptance suites and production use one engine. `ListingRequest.RowVersion`
becomes `byte[]`, mapped with `IsRowVersion()` to a real `rowversion` column that the server
maintains.

The default instance is **SQL Express**, reached at `Server=.\SQLEXPRESS`.
`Database:ConnectionString` remains the seam for a container or a deployed instance.

## Options Considered

### Option 1: SQL Server via LocalDB
- **Pros:** Starts on demand, needs no service running, creates its files under the user profile — the lightest option for a suite that drops and recreates a database on every run.
- **Cons:** **Does not work on this machine.** LocalDB is loaded into the calling process through `SQLUserInstance.dll`, which ships x64 only. An ARM64 .NET host fails with `%1 is not a valid Win32 application`. This was chosen first and abandoned when the suite could not open a connection.

### Option 2: SQL Server via SQL Express — chosen
- **Pros:** A service reached over a pipe, so the architecture of the calling process never arises. Already installed and running. Same engine and same feature set as a deployed SQL Server, so a filtered index and a `rowversion` behave in the suite exactly as they will in production.
- **Cons:** The service must be running for anything to work. Databases outlive a run unless dropped, so the suite names its own uniquely and drops it on the way out.

### Option 3: Keep PostgreSQL, keep SQLite
- **Pros:** No change. The suites stay runnable on a machine with neither SQL Server nor Docker.
- **Cons:** Leaves the implementation contradicting the designs. Keeps every provider-conditional branch. Keeps four acceptance criteria unverified, which is the part that matters: a rule nothing exercises is a rule nobody knows holds.

### Option 4: PostgreSQL only, via Testcontainers
- **Pros:** One provider, one set of semantics, portable to any machine with a container runtime.
- **Cons:** Still contradicts the designs, and still could not run here — Docker was unavailable for the whole of development, which is precisely how four criteria went unverified for so long.

## Consequences

### Positive
- All 101 backend acceptance tests run. **Skipped went from four to zero**, and the concurrency criteria pass against a real engine rather than being asserted in prose.
- The persistence layer loses `SqliteTimestamps`, `DatabaseProviders`, `DatabaseOptions.Provider`, three test harness classes, a skip attribute, and every `IsSqlite()` / `IsNpgsql()` branch.
- The row version is maintained by the server. No application code has to remember to advance it, so nobody can delete the line that made the rule true.
- `DateTimeOffset` maps to `datetimeoffset` natively, so the conversion that existed to make SQLite sortable is gone.
- Migrations are applied on every start, including every acceptance run, so the migration cannot drift from the model unnoticed.
- `L2-048` — case-insensitive search, currently deferred — becomes free under the default collation rather than needing `ILIKE` or `lower()`.

### Negative
- SQL Server must be present to run the backend suite. There is no longer an in-process fallback, so a machine without it cannot run the tests at all.
- The suite is slower: roughly ninety seconds against SQL Express, where SQLite took two. It is doing considerably more.
- CI needs a SQL Server service. That is ordinary on hosted runners but is a step that did not exist before.

### Risks
- **Accepted:** binding the development story to a locally installed service. Mitigated by `Database:ConnectionString`, which points anywhere without a code change.
- **Accepted:** SQL Server's default `READ COMMITTED` is lock-based where PostgreSQL is MVCC, so the two behave differently under a concurrent accept and decline. Nothing in the design depends on read isolation — the row version settles it — and the criteria that turn on it now pass, which is the evidence that matters.
- Filtered indexes require `QUOTED_IDENTIFIER ON` at creation *and* for later DML on the table. `Microsoft.Data.SqlClient` sets it on by default, so EF is unaffected; a tool that does not, such as `sqlcmd` without `-I`, will fail against these tables in a way whose error message does not mention the index.

## Implementation Notes

- `RowVersion` reads back as type `timestamp` in `sys.columns` — that is SQL Server's internal name for `rowversion`, not the deprecated `timestamp` datatype.
- The integration suite creates `BarnabasTests_{guid}` per run and drops it on dispose, taking it single-user first so a pooled connection cannot block the drop. A shared database would let a crashed run poison the next.
- The Playwright suite uses `Barnabas_E2E` with `Database:ResetOnStart`, so it starts from a known schema every time.
- The migration was regenerated rather than translated. There was no data to preserve.

## References

- The twenty `ComponentDb(..., "SQL Server", ...)` nodes under `docs/detailed-designs/*/*/diagrams/`
- `docs/detailed-designs/requests/accept-a-request/diagrams/class-structure.puml` — `+byte[] RowVersion`
- `docs/detailed-designs/requests/make-a-request/README.md` — the unique index on `(ListingId, RequesterId)` where the status is `Pending`
- `docs/detailed-designs/access/sign-in-with-a-link/README.md` — consumption as one conditional update whose row count decides
- `docs/specs/L2.md` — `L2-016`, `L2-018`, `L2-060`, `L2-062`, the four criteria this change made runnable
