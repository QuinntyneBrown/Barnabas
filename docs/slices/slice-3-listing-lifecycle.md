# Feature slice 3 — listing lifecycle, board states, pagination

## Purpose

A listing could be posted and closed out, and nothing in between. An owner could not correct a
typo, take something down for a fortnight, put it back, or remove a listing they regretted. This
slice gives a listing a life after it is posted, and makes the board's three ordinary states —
empty, arriving, and failed — states rather than accidents.

## L1 requirements touched

| L1 | Title | Before | After | Status |
|----|-------|--------|-------|--------|
| `L1-006` | Listing lifecycle | 4 of 7 | **7 of 7** | **Complete** |
| `L1-007` | Browsing the board | 3 of 6 | **6 of 6** | **Complete** |
| `L1-010` | Coordination messaging | 3 of 6 | 4 of 6 | Partial — `L2-068`, `L2-069` need profiles |
| `L1-016` | Performance and scale | 0 of 5 | 1 of 5 | Partial |

Two more L1 requirements finished, bringing the total to three.

## L2 requirements implemented

| L2 | Requirement | Criteria |
|----|-------------|----------|
| `L2-036` | Edit a listing | 3 |
| `L2-038` | Archive a listing | 2 |
| `L2-039` | Restore or repost an archived listing | 2 |
| `L2-040` | Permanently delete a listing | 3 |
| `L2-045` | Show an empty board | 2 |
| `L2-046` | Show the board loading | 2 |
| `L2-047` | Show the board failing to load | 2 |
| `L2-064` | Create a message thread when a request is accepted | 3 |
| `L2-105` | Paginate long collections | 3 |

**22 acceptance criteria, all covered.**

## Decisions taken

- **`ClosedOutAt` is what tells archive from close-out.** Both leave a Lend listing `Archived`,
  and only one may be undone: restoring a returned ladder would reopen a finished loan.
  `Listing.CanBeRestored` is where `L2-037` and `L2-039` stop disagreeing, and the API answers it
  per listing so the screen shows a restore action only where there is one to offer.

- **Deleting is only from the archive.** Requiring a listing to be taken off the board first puts
  an undoable step between stopping offering something and destroying it, along with any
  conversation it opened.

- **The delete cascade is written out rather than left to the schema.** Only messages and read
  marks cascade from a thread; requests and threads hang off the listing by identifier alone. A
  thread whose listing had gone would lead nowhere, which `L2-064` does not allow.

- **A page size beyond the maximum is clamped, not refused.** `L2-105 AC3` wants the maximum
  applied where `L2-096 AC1` wants an over-long field rejected naming it. The two only conflict
  if a page size is treated as member content: it is a transport hint, so it is clamped in the
  controller and never reaches a validation rule. `GetBoardQueryValidator` says so in as many
  words, because the next reader will wonder where the rule went.

- **`L2-064` is written as the messaging-side assertions on the behaviour `L2-060` produces** —
  that a thread carries its listing and both members, and that nothing else brings one into
  being. Neither requirement is implemented twice. `docs/feature-slice.md` warned the pair would
  eventually disagree; they now say different things about one rule.

- **`AC3` is stated as behaviour, not by reading the route table.** The ways a caller might try to
  start a conversation are attempted and none of them opens one, plus the invariant that there are
  never more threads than accepted requests.

- **A `Listing` row version was not added.** The design proposes one to serialise competing edits,
  but no acceptance criterion in this slice concerns concurrent editing, and `AGENTS.md` asks for
  the least code that satisfies the criteria. Noted rather than built.

- **The 404 screen replaces the catch-all redirect.** `docs/mocks/404.html` existed with no
  requirement behind it and the router sent unknown addresses to the landing page, which reads as
  being signed out. No L2 mandates it; leaving a mock nobody owned was the worse option.

## A defect this slice found in slice 2

**The close-out verbs in `listing-words.ts` were wrong for two kinds.** `L2-037` is precise: a
Give listing is marked *taken* and becomes `Given away`; a Help offer is marked *booked* and
becomes `Completed`. Slice 2's table used the resulting statuses as the labels. It survived
because slice 2's E2E asserted only the Sell case, which was right. The table now carries the
verbs, and `MyListingsComponent`'s own duplicate switch — which disagreed with it — is gone.

## What this cost that was not planned

- **A test that navigated while a save was in flight.** The edit spec clicked save and went
  straight to the board, aborting the PUT; the board then honestly showed the old title. The spec
  now waits for the listing screen to show the new one. This is the second time in three slices
  that an in-flight write has been aborted by a navigation.

- **A dialog heading bound to the row just clicked never updated.** `showModal()` puts a native
  `<dialog>` into the top layer in the same tick as the signal write, before the binding flushes,
  so the heading showed the previous listing's wording. The per-kind heading was an invention —
  `L2-037 AC5` asks for the *action* to carry the verb, and the button already does — so it was
  dropped rather than worked around.

- **The longest journey is now declared slow.** `borrow-a-ladder` walks two members through seven
  screens and intermittently exceeded the 30-second default in a full run while passing alone.
  `test.slow()` says that plainly instead of leaving it to chance.

## Verification

```
backend   dotnet build   0 warnings
backend   dotnet test    151 passed, 0 failed, 0 skipped   (was 130)
frontend  playwright     69 passed, 0 failed              (was 60)
```
