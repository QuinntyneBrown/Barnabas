# Feature slice 7 — notifications

## Purpose

Everything that happened to a member happened silently. A request on your listing waited in an
inbox you had no reason to open; an acceptance waited in another. Notifications are how Barnabas
tells somebody that something has happened to them.

## L1 requirements touched

| L1 | Title | Before | After | Status |
|----|-------|--------|-------|--------|
| `L1-011` | Notifications | 0 of 6 | **6 of 6** | **Complete** |

**Eleven of eighteen L1 requirements complete.**

## L2 requirements implemented

`L2-070`, `L2-071`, `L2-072`, `L2-073`, `L2-074`, `L2-075` — **15 acceptance criteria**.

## Decisions taken

- **Notifications stage into the calling handler's own unit of work.** `Notifier` adds a row and
  never saves; the row joins whatever `SaveChangesAsync` the handler was about to run. That is not
  tidiness — `MakeLoanRequestCommandHandler` catches a `DbUpdateException` from the filtered unique
  index and `AcceptRequestCommandHandler` catches a `DbUpdateConcurrencyException` from the row
  version, and in both cases the losing caller must leave **no** notification behind.
- **Not a pipeline behaviour, and not a post-commit publish.** A behaviour sees the command and the
  result but not the domain facts — it does not know the listing's owner or the thread's other
  party, so it would have to re-read them after the transaction committed. A post-commit publish
  leaves a window in which an accepted request has no notification. Both were considered and both
  are worse.
- **A disabled kind is never created.** `L2-075 AC1` asks for exactly that, and the preference is
  read at the moment of creation rather than at the read. Filtering at the read would leave the row
  in the database and the unread count wrong.
- **Absence means enabled.** A preference row exists only where somebody has expressed one, so a
  new kind arrives switched on for everybody without a backfill.
- **A moderator's removal cannot be silenced.** `L2-084` requires the owner to be told, and a
  decision made about somebody that they could opt out of hearing would be made behind their back.
  `ListingRemoved` is simply not among the configurable kinds; submitting it is accepted and
  ignored rather than refused, because a member has not done anything wrong by asking.
- **Every kind has its own factory, each requiring the identifiers its destination needs.** That is
  how `L2-074` is kept: a notification without a destination cannot be constructed, so there is no
  path that produces one leading nowhere. The test asserts the property rather than the rows.
- **The destination is not a stored URL.** Where a thing lives is the client's business, and a path
  in a row would be wrong the first time a route changed. `destinationOf` routes from the kind.
- **The name and title are copied onto the row** rather than joined at read time. A notification is
  a record of what was true when it happened; a listing since renamed should still say what the
  member was told.
- **The unread count is held once, in the shell.** `L2-073` puts it on every screen at every width,
  and several copies asking independently would disagree the moment one went stale. The bell is in
  the header at every band, so that is the one place it needs to be — and the count is read out as
  part of the link's own name, not only drawn.

## A shipped defect this found

**The development reset and the integration-test reset were two lists that had drifted.** Five
tables added since slice 2 — `JoiningSessions`, `MemberHelpTags`, `AvailabilityWindows`,
`Notifications`, `NotificationPreferences` — were emptied between integration tests and left
standing between Playwright specs. It surfaced as a notification spec failing only when run with
its neighbours. The lists are now one list, shared, so they cannot drift again.

## What this cost that was not planned

- **A test read a list with `count()` before it had loaded.** `expect(locator)` retries; `count()`
  does not. The page object now has a `waitForRows`, exactly as the board's has `waitForPlacards`
  for the same reason.
- **An in-flight write aborted by a sign-in, for the third time in six slices.** The helper that
  makes a request now waits for the product's own confirmation before handing the browser to
  somebody else.

## Verification

```
backend   dotnet build   0 warnings
backend   dotnet test    240 passed, 0 failed, 0 skipped   (was 230)
frontend  playwright     90 passed, 0 failed              (was 87)
```
