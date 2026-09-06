# Feature slice 4 — provisioning, invitation and joining

## Purpose

Congregations were seeded and nobody could get into one. There was no way to bring a parish into
being, name its neighbourhoods, make anybody a moderator, issue a code, or join with one — and no
administrator existed to do the first of those.

This slice is how Barnabas comes to have members at all.

## L1 requirements touched

| L1 | Title | Before | After | Status |
|----|-------|--------|-------|--------|
| `L1-001` | Congregation provisioning and configuration | 0 of 4 | **4 of 4** | **Complete** |
| `L1-002` | Invitation and joining | 0 of 7 | **7 of 7** | **Complete** |
| `L1-003` | Authentication and session | 6 of 8 | **8 of 8** | **Complete** |
| `L1-014` | Tenant isolation | 2 of 5 | 3 of 5 | Partial |
| `L1-015` | Security and data protection | 3 of 10 | 5 of 10 | Partial |

Three more L1 requirements finished, bringing the total to six of eighteen.

## L2 requirements implemented

`L2-001`, `L2-002`, `L2-003`, `L2-004`, `L2-005`, `L2-006`, `L2-007`, `L2-008`, `L2-009`,
`L2-010`, `L2-011`, `L2-013`, `L2-017`, `L2-090`, `L2-095`, `L2-099` — **43 acceptance criteria**.

## Decisions taken

ADR-0002 settles five `<TO SUPPLY>` entries from `open-decisions.md` and gives the reasoning. In
short:

- **An administrator belongs to a platform congregation of its own.** A `Member` is
  `ITenantOwned` and belongs to exactly one congregation, so an administrator must too. Giving
  them their own costs no second authentication scheme, no anonymous provisioning route and no
  change to the token — and `Congregations` was already the one ungated set, so provisioning
  needed no new escape hatch.
- **Codes are eight characters from a 32-symbol alphabet** with no `I`, `O`, `0` or `1`. A code is
  read aloud and typed by hand; the excluded four are the ones people confuse on paper.
- **Thirty days for a code, fifteen minutes for a joining session.** The second matches the
  sign-in link because both are single-use credentials held by somebody the system does not yet
  know. The first is not inherited from it — they are different acts.
- **Joining collects an email address and an optional reason.** Neither is in `L2-010`. `Member`
  requires an address and sign-in looks a member up by one, so a member created without one could
  never sign in; and `L2-085` requires a moderator to read a reason `L2-010` never collects.

## Two specification gaps, flagged rather than absorbed

- **`L2-001` cannot produce a usable congregation.** Only a moderator may issue an invite code, so
  a parish provisioned with no members could never gain any. `ProvisionCongregationCommand`
  therefore also takes a founding moderator's email — beyond what the requirement asks.
- **`L2-010` collects no email address**, while `Member` requires one. Collected at the profile
  step, which keeps the code anonymous until somebody commits to joining.

## What the shape of the code says

- **Joining is two steps and cannot be collapsed.** `L2-006` says redeeming returns the
  congregation, `L2-009` that the code is spent exactly once including under concurrency, and
  `L2-010` that a bad profile creates no member. So the code is burnt at step one and a hashed
  bearer token carries the right to step two. Handing back the congregation's identifier instead
  would let the next call name any congregation it liked. The cost: abandoning the profile step
  burns the code.
- **Two conditional updates decide what no check can.** Both the code and the joining session are
  spent by an `UPDATE` whose affected-row count is the answer, exactly as the sign-in token is.
  Two people typing the same code at the same moment both find it redeemable before either
  commits.
- **Expired, spent and revoked are one reply.** Which of the three it was would say something
  about a code the caller is not entitled to know about. The title is identical and only the
  trace identifier differs — asserted.
- **A third narrow store.** Redemption is anonymous, so it cannot go through the gated
  `InviteCodes` set, which would raise `CongregationContextMissingException` and answer 500 where
  `L2-007` asks for 404. `IInvitationStore` joins `IAuthenticationStore` and `IProvisioningStore`
  as a deliberate, documented hole in the tenant boundary.
- **The founding moderator is inserted directly.** They belong to the congregation just created,
  not to the administrator's, and the save-time stamp would have claimed them. Going round the
  tracker for that one row keeps the guard intact for everyone else rather than weakening it.
- **Role and status come from the record on every request.** `L2-003` asks that a moderator grant
  work for that member's *subsequent* requests and `L2-086` that an approval let them onto the
  board on their next visit; a claim minted an hour ago can promise neither. `SessionValidator`
  already read the session per request, so this costs one join rather than one round trip.

## Three shipped defects this found

- **`AuthorisationBehaviour` read `Role` before checking whether a session existed**, so an
  anonymous caller on a role-gated route would have got 500 where `L2-095` asks for 403.
- **The sign-in lookup required an approved member**, which made `L2-011`'s awaiting-approval
  screen unreachable: somebody could join and then have no way to find out where they stood.
- **The board heading, the landing page and the sign-in screen all hard-coded "St. Aidan's"**,
  which `L2-004 AC2` forbids — every member of every parish saw the same parish. The board now
  reads its congregation from the API; the two public screens name none, because signed out there
  is no congregation to name.

## Structural choices worth review

- **The sign-in throttle is application state, not the framework's rate limiter.** `L2-017 AC2`
  requires a further request to succeed once fifteen minutes have elapsed, and the middleware's
  window is replenished by a timer no test can advance — the criterion could only be tested by
  waiting a real quarter of an hour. Counted against the injected clock it is deterministic.
- **It counts before the address is looked up**, so an unregistered address is throttled exactly
  as a registered one is. Throttling only the addresses that exist would make the limiter itself
  the disclosure `L2-013` forbids.
- **`MembershipBehaviour` refuses in one place** rather than in every handler, and answers 403
  rather than 401 — 401 reads as "sign in again" and would send a pending member round a loop
  they cannot leave. Requests opt out with `IAllowUnapproved`, so the default is closed.

## What this cost that was not planned

- **The E2E suite met the throttle.** It signs the same member in dozens of times, and five per
  fifteen minutes is the rule working exactly as intended. The development reset endpoint the
  suite already calls now clears the throttles alongside the database — they count in memory, so
  emptying tables alone left one spec's sign-ins as the next spec's head start.
- **`messages.spec.ts` joins `borrow-a-ladder` as a declared slow test.** Two members, a request,
  an acceptance and a message before it asserts anything; it fits the default timeout alone and
  exceeded it under full-suite load.

## Not covered here

- **`L2-002 AC3`** — the neighbourhood control on the *profile* screen. The joining form's control
  is asserted, but editing a profile is `L1-004` and arrives with slice 5.
- **`L2-003 AC3`** is satisfied by a moderator's invite page rather than a moderation queue, which
  is slice 8. The entry is real and leads somewhere that works.

## Verification

```
backend   dotnet build   0 warnings
backend   dotnet test    196 passed, 0 failed, 0 skipped   (was 151)
frontend  playwright     77 passed, 0 failed              (was 69)
```

Walkable end to end against the running system:

```
an administrator provisions a congregation with its neighbourhoods and a founding moderator
  → that moderator signs in and issues a code
  → somebody redeems it, supplies a profile, and is told a moderator has to let them in
  → they sign in later and are still told, and still cannot reach the board
```
