# Feature slice 5 — profiles and the congregation directory

## Purpose

Members had a name and a neighbourhood and nothing else. Nobody could say what they could help
with, read anybody else's profile, find a person rather than a listing, or leave.

The board answers *what is on offer*. This slice makes Barnabas able to answer *who is here*.

## L1 requirements touched

| L1 | Title | Before | After | Status |
|----|-------|--------|-------|--------|
| `L1-004` | Member profiles | 0 of 6 | **6 of 6** | **Complete** |
| `L1-012` | Congregation directory | 0 of 4 | **4 of 4** | **Complete** |
| `L1-010` | Coordination messaging | 4 of 6 | **6 of 6** | **Complete** |
| `L1-001` | Congregation provisioning | complete | complete | `L2-002 AC3` now covered on the profile screen |
| `L1-009` | Requests | complete | complete | `L2-059 AC2` closed — see below |

Three more L1 requirements finished: **nine of eighteen**, half the specification.

## L2 requirements implemented

`L2-020`, `L2-021`, `L2-022`, `L2-023`, `L2-024`, `L2-025`, `L2-068`, `L2-069`, `L2-076`,
`L2-077`, `L2-078`, `L2-079` — **28 acceptance criteria**, plus the two partials below.

## A partial requirement closed, and one recorded correction

**`L2-059 AC2` was never met.** It requires the requester's name on the incoming-requests screen
to open their profile. The name was a bare `<span>` while only the listing title was a link, and
nothing in the workspace routed to `/members/`. Slice 1 recorded 31 requirements in full and 2 in
part; the honest count was **30 and 3**. It is now met, and `L1-009` is genuinely complete.

**`L2-002 AC3`** — "only their own congregation's neighbourhoods are offered" on the profile
screen — was deferred out of slice 4 because there was no profile screen. Covered here.

## Decisions taken

- **Help tags are stored two different ways, on purpose.** The congregation's set is a delimited
  column, read whole and ordered, exactly as its neighbourhoods are. A member's own tags are a
  child table, because `L2-077` searches the directory *by tag* — a `LIKE` over a delimited column
  would find "Rides" inside "Joyrides", and cannot be indexed.
- **A congregation gets a default set of help tags.** ADR-0002 left the catalogue `<TO SUPPLY>`; a
  congregation with none configured would leave `L2-023` with nothing to declare and the directory
  with nothing to search. An administrator replaces them; the six defaults are somewhere to start.
- **Ten tags per member.** A member who has ticked everything has told the congregation nothing,
  and the tag search would return everybody.
- **The public profile has no email field at all**, rather than a field somebody remembers to
  leave out. `L2-024 AC2` becomes a property of the type, and the test reads the whole response
  body looking for an at sign rather than trusting the shape.
- **`MemberStatus` gains `Left`,** distinct from `Declined` — one is the member's decision and the
  other a moderator's. A departed member cannot sign in, because the signable lookup allows only
  approved and awaiting members. The record stays, so their old messages remain legible to the
  people they spoke to.
- **Leaving archives rather than closes out.** Nothing was lent, sold or given away; the member
  left, and the wording should not claim otherwise.
- **Leaving does three things in one unit of work** — the member is recorded gone, their active
  listings come off the board, and their session ends. A member recorded as gone whose listings
  were still up would leave the congregation asking somebody who is no longer there.
- **Saving a profile confirms without leaving the screen.** A member correcting a description has
  not finished with the screen, and being thrown back to the board would make small corrections
  expensive.

## What the shape of the code says

- **The neighbourhood and help-tag checks live in the handler, not the validator.** Both are
  relationships between two aggregates, and a validator holds only the command. They are still
  reported as named fields, which is what `L2-022` asks for.
- **Nothing anywhere offers to message a member.** The profile says so in as many words, and
  `ProfilesAndDirectoryTests` attempts the two routes somebody might expect to exist and finds
  neither. A request against a listing, once accepted, remains the only thing that opens a thread.
- **The directory's congregation predicate comes from the global query filter**, which is what
  makes `L2-079 AC2` true without a comparison a handler could forget.

## Verification

```
backend   dotnet build   0 warnings
backend   dotnet test    214 passed, 0 failed, 0 skipped   (was 196)
frontend  playwright     83 passed, 0 failed              (was 77)
```
