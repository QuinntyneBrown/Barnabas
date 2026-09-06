# Feature slice 8 — moderation

## Purpose

Until now nobody could do anything about a listing that should not be on the board, and nobody
could let a member in. Somebody who joined waited on a moderator who had no screen to decide from,
and `SeedData` was the only thing that had ever approved anybody. This slice gives a parish's two
or three moderators the two queues they actually work: listings people have objected to, and people
waiting to be let in.

## L1 requirements touched

| L1 | Title | Before | After | Status |
|----|-------|--------|-------|--------|
| `L1-013` | Moderation | 0 of 8 | **8 of 8** | **Complete** |

**Twelve of eighteen L1 requirements complete.**

## L2 requirements implemented

`L2-080`, `L2-081`, `L2-082`, `L2-083`, `L2-084`, `L2-085`, `L2-086`, `L2-087` —
**21 acceptance criteria**.

## Decisions taken

- **`Flagged` is a mark on the listing, not a `ListingStatus` value.** Only `Active` appears on the
  board, and `L2-083` requires an approved listing to *stay* there. Had the flag been a status, a
  single complaint would have taken a listing off the board and approving it would have had to
  guess which status to put back. `Listing.FlaggedAt` is nullable; `Flag` sets it, `ClearFlag`
  clears it, and the board query never reads it.
- **`ListingStatus.Removed` is distinct from `Archived`.** The plan's addendum called this out and
  it is real: `Archived` is the owner shelving their own listing, and `L2-039` lets them put it
  back. A moderator's removal is a decision made *about* them, and reusing `Archived` would have
  handed the owner an undo button. `CanBeRestored` requires `Archived`, so a removed listing simply
  cannot be restored — and an acceptance test asserts it.
- **A unique index on `(ListingId, ReporterId)`, unfiltered.** The request index alongside it is
  filtered to open rows, because a declined request may be made again; there is no equivalent
  escape hatch here. A member has either objected to a listing or has not, and objecting a second
  time is the same objection. `L2-080 AC2` is decided by the database, not by a read-then-write.
- **Moderator removal is a separate command declaring `IRequireRole` and *not* `IRequireOwnership`.**
  That is how `L2-041`'s "or by a moderator acting under L2-084" is honoured without weakening the
  ownership check that protects every other listing action. Role and ownership were already separate
  markers in the pipeline; this is the first request to use the one without the other.
- **A moderator's role widens what they may *do*, never what they can *see*.** Every read here goes
  through the ordinary congregation filter, so a listing or a member in another parish is absent
  before the question of moderating it arises, and the answer is 404 rather than 403 — `L2-084 AC2`
  and `L2-089`, held by the same mechanism as everything else.
- **The reporter's identity lives in exactly one projection.** `ReportedListingDto` is the only type
  in either half of the workspace that carries a `ReporterId`. That makes `L2-081` a property of the
  model rather than a rule somebody has to remember: no member-facing type has a field to put one
  in, so there is no path by which it leaks. The E2E test asserts the stronger thing — the owner's
  view of a reported listing contains no occurrence of the words "report" or "flag" at all.
- **Reports are resolved, not deleted.** Clearing the flag and leaving the complaints open would
  mean a later report re-flagged the listing and dragged the settled ones back into the queue with
  it. `ListingReport.Resolve` records the outcome, the moderator, and when.
- **Four reasons, matching the four the dialogue offers.** `docs/mocks/listing-lend.html` is the
  visual specification and offers exactly four: *does not belong*, *already gone*, *not accurately
  described*, *something else*. A fifth reason nobody can choose would be a value the queue has to
  render and never would. `ReportReason` is those four and no more.
- **One screen, two queues.** They are one job, done in one sitting, by the same two or three
  people. Splitting them would mean a moderator checking two places to find out whether anything
  needed them. The store reloads both after any decision, because a decision can change the other
  list too.
- **The two destructive actions are confirmed and the two safe ones are not.** Approving a listing
  leaves it where it was and approving a member lets them in; removing and declining are the ones a
  moderator should have to mean. `L2-084 AC3` and `L2-087 AC2` ask for exactly those two.
- **The queue's rows are landmarks, not divs.** Two lists of `.list-row` on one page need telling
  apart by somebody moving through with a screen reader, and by the page object. `<section
  aria-labelledby>` gives both.
- **The moderator queue has no route guard beyond `approvedGuard`.** Every call the screen makes is
  refused to an ordinary member by the API, so somebody who typed the address sees an empty page
  rather than somebody else's complaints. Hiding the link on the You hub is presentation; the 403
  is the rule — the same division `L2-003` already draws for invitations.
- **A report's free-text note refuses the payment vocabulary.** It is exactly where somebody would
  paste the card number they were complaining about, and `PaymentDeliveryAndDepositFields` refuses
  it by name rather than dropping it silently.

## What was already there

- `Member.Approve` and `Member.Decline`, and `MemberStatus.AwaitingApproval` / `Declined`, all built
  in slice 4. `L2-086` and `L2-087` needed two handlers and no domain code.
- `NotificationKind.ListingRemoved`, `Notification.ListingRemoved` and `INotifier.ListingRemovedAsync`,
  all written unwired in slice 7 against this slice. `L2-084 AC1`'s "its owner is notified" is one
  call.
- `IRequireRole` and the role gate in `AuthorisationBehaviour`, wired in slice 1 and first used in
  slice 4. Six of this slice's seven endpoints are a one-line declaration on a command.

## A defect this found in shipped code

**`ListingDetailPage` declared `title` twice.** The Playwright page object had two getters of the
same name — one reading the level-one heading, one reading `.detail__title` — and the second
silently won. It is not type-checked by the Playwright run, so it had gone unnoticed since slice 2.
The duplicate is removed; the surviving getter reads the same element the first one was aiming at.

## Where ATDD was not followed to the letter

The backend tests here were written **after** the implementation rather than before it. They fail
without it — none of the seven endpoints, the entity, or the migration existed, so the suite did not
compile — but that is the degenerate form of red-green, not the real thing, and the earlier slices
did better. The Playwright specs were written against screens that did not exist yet and did go red
first. Recorded rather than papered over.

## Verification

```
backend   dotnet build   0 warnings
backend   dotnet test    260 passed, 0 failed, 0 skipped   (was 240)
frontend  ng build       0 errors
frontend  playwright     99 passed, 0 failed, 0 skipped    (was 90)
```

The two load-bearing constraints were verified in the database rather than only in the model, as the
plan requires after this slice:

```
IX_ListingReports_OneReportPerMember   is_unique = 1   filter_definition = NULL
IX_Listings_Flagged                    is_unique = 0   filter_definition = ([FlaggedAt] IS NOT NULL)
```
