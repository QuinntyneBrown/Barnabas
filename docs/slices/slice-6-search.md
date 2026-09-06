# Feature slice 6 — search

## Purpose

The board holds everything a congregation is offering, newest first. That works for a parish with
a dozen listings and stops working before it reaches a hundred. Search is how a member finds the
thing they came for rather than the thing posted most recently.

## L1 requirements touched

| L1 | Title | Before | After | Status |
|----|-------|--------|-------|--------|
| `L1-008` | Search and filtering | 0 of 6 | **6 of 6** | **Complete** |
| `L1-014` | Tenant isolation | 3 of 5 | 4 of 5 | `L2-088` now **complete** |
| `L1-015` | Security and data protection | 5 of 10 | 6 of 10 | `L2-098 AC1` covered |

**Ten of eighteen L1 requirements complete.**

## L2 requirements implemented

`L2-048`, `L2-049`, `L2-050`, `L2-051`, `L2-052`, `L2-053` — **14 criteria** — plus
**`L2-088 AC2`**, the last outstanding criterion of tenant isolation's read rule, and
`L2-098 AC1`.

`L2-088` was the second of slice 1's two partial requirements. Both are now whole.

## Decisions taken

- **The collation is declared, not inherited.** `L2-048 AC3` requires `LADDER` and `ladder` to
  give the same answer. SQL Server's default collation happens to be case-insensitive, and
  `TestDatabase` creates each run's database at that default — so a test asserting AC3 would have
  been proving something about the machine rather than about Barnabas. `Collations.CaseInsensitive`
  is now on the searched columns, which is the argument ADR-0001 makes about constraints applied to
  a comparison.
- **`LIKE`, not full-text.** Full-Text Search is an optional SQL Server Setup feature absent from a
  default SQL Express install, and `MigrateAsync` runs on every acceptance run — a
  `CREATE FULLTEXT CATALOG` would fail on a clean machine, which contradicts ADR-0001 directly. At
  a congregation's scale it would buy nothing measurable.
- **No `ToLower()` on either side.** Lowering the column defeats the index that serves the board;
  lowering only the term leaves the answer depending on the server's collation. The column's own
  collation settles it and keeps the index usable.
- **The term is escaped, not merely parameterised.** Parameterisation stops member text being read
  as SQL. It does not stop it being read as a *pattern*: without escaping, a member searching for
  `100%` matches everything and one searching for `_` matches every single character. That is a
  wrong answer rather than an injection, and just as much a defect. `LikePattern` escapes `%`, `_`,
  `[` and the escape character itself, and names the escape character to SQL Server explicitly —
  `LIKE` has no default, so one that is not named does not exist.
- **A price filter excludes everything without a price.** `L2-051 AC2` in one line:
  `listing.Price != null`. Only a Sell listing carries one, so the null test excludes Lend, Give
  and Help without naming a kind. Somebody filtering by price is shopping, and a free ladder is not
  an answer to that.
- **The cursor is the board's.** `BoardCursor` is reused rather than reinvented, so a filtered
  search pages exactly as the board does.

## What the shape of the code says

**There is no congregation predicate in the search handler at all.** The global query filter
supplies it, which is the whole of `L2-088 AC2` — there was no line to forget, because there is no
line. The test states it from both sides: Priya cannot find St. Brigid's canoe, and Hank can.

## Verification

```
backend   dotnet build   0 warnings
backend   dotnet test    230 passed, 0 failed, 0 skipped   (was 214)
frontend  playwright     87 passed, 0 failed              (was 83)
```
