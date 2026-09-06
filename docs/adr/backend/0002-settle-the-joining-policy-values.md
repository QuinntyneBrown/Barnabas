# ADR-0002: Settle the joining policy values

**Date:** 2026-09-06
**Category:** backend
**Status:** Accepted
**Deciders:** Quinntyne Brown

## Context

`docs/detailed-designs/open-decisions.md` records fourteen `<TO SUPPLY>` entries — places where the
requirements do not say enough to implement from. Five of them block congregation provisioning,
invitation and joining, which is the next body of work:

- **`D-01`** — joining collects a display name and a neighbourhood, but `Member` requires an email
  address and sign-in looks a member up by one. `L2-085` also requires a moderator to see a
  member's stated reason for joining, which `L2-010` never collects.
- **`D-02`** — the invite alphabet, its length beyond the stated minimum of six, the issue
  lifetime, the joining capability's lifetime, and the moderator contact shown on a dead code.
- **`D-03`** — how the first administrator comes to exist, and the slug's normalisation and bounds.

None can be deferred. A congregation cannot be provisioned without a slug rule, no invite can be
issued without an alphabet, and nobody can join without an email address to sign in with
afterwards. Leaving them open would mean either inventing values silently or stopping.

The requirements are not wrong; they are quiet. Each value below is a choice this implementation
makes so the work can proceed, recorded so it can be overruled deliberately rather than
discovered later in a validator.

## Decision

Fix the following values. Each is stated in code as a named constant or an Options record, never
as a literal buried in a rule.

### Identity and joining — `D-01`

| Question | Choice |
|---|---|
| Does joining collect an email address? | **Yes**, at the profile step, required. |
| Does joining collect a reason? | **Yes**, optional, at most 500 characters. |
| Is the address verified before approval? | **No.** Possession of the mailbox is proven at first sign-in. |

An address is unavoidable: `Member.EmailAddress` is required, it is globally unique, and the
passwordless sign-in link is sent to it. Collecting it at the profile step rather than at
redemption keeps the invite code anonymous until somebody commits to joining.

Verification is not added, because the product already verifies the mailbox the only way that
matters — the first sign-in link goes there and nothing works until it is followed. A separate
confirmation step before approval would prove the same fact twice.

The reason is optional because `L2-010` does not ask for it, and required to exist because
`L2-085` requires a moderator to read it. Optional-and-present is the only reading that satisfies
both.

### Invitation — `D-02`

| Question | Choice |
|---|---|
| Alphabet | `ABCDEFGHJKLMNPQRSTUVWXYZ23456789` — 32 symbols, no `I`, `O`, `0` or `1` |
| Length | **8 characters**, giving 40 bits |
| Issue lifetime | **30 days**, in `InviteCodeOptions` |
| Joining capability lifetime | **15 minutes**, matching the sign-in token |
| Moderator contact on a dead code | *"Ask whoever invited you for a fresh code."* |

`L2-005` requires at least six characters from an unambiguous alphabet. Eight is chosen over six
because a code is read aloud and typed by hand: 40 bits makes guessing pointless without making
the code unwieldy, and the excluded four symbols are the ones people confuse when reading a code
off a piece of paper.

Thirty days is long enough for a code printed in a bulletin and short enough that a photographed
noticeboard stops working. It is **not** inherited from the sign-in link's fifteen minutes; the two
are different acts and the design says so.

The joining capability *does* match the sign-in token at fifteen minutes, because both are
single-use credentials held by somebody not yet known to the system.

No contact address is named. The congregation has not configured one and inventing an address
would be worse than saying who to ask.

### Provisioning — `D-03`

| Question | Choice |
|---|---|
| Slug shape | `^[a-z0-9]+(-[a-z0-9]+)*$`, 3–64 characters, lower-cased on the way in |
| Slug mutability | **Immutable.** No mutator exists, and a submitted `slug` is refused by name. |
| Congregation name | At most 200 characters |
| Neighbourhoods | 1–50 entries, each 1–100 characters, no case-insensitive duplicates, order preserved |
| First administrator | A seeded **platform congregation**, slug `barnabas`, whose members hold `MemberRole.Administrator` |

Lower-casing in the factory is what lets a plain unique index enforce case-insensitive uniqueness
without depending on the server's collation — the same argument ADR-0001 makes about constraints
behaving in a test as they will in a deployment.

The platform congregation is the least invasive answer to the bootstrap problem. A `Member` is
`ITenantOwned` and belongs to exactly one congregation, so an administrator must belong to one
too. Giving them a congregation of their own costs no new authentication scheme, no anonymous
provisioning route, and no change to the token: an administrator signs in through the same
passwordless link as everybody else, and `IRequireRole` does the rest. `Congregations` is already
the one ungated `DbSet`, so provisioning needs no new escape hatch.

**A congregation provisioned with no members has nobody who can issue an invite**, and `L2-005`
requires a moderator to issue one — so a congregation with no moderator is unreachable forever.
`ProvisionCongregationCommand` therefore also takes a founding moderator's email address. This
goes beyond what `L2-001` asks for, and is flagged rather than hidden.

## Options Considered

### Option 1: Fix the values here, in one ADR
- **Pros:** Work proceeds; every value has one home and one reason; overruling any of them is an
  edit to a constant rather than an excavation.
- **Cons:** Choices are made without the congregation that will live with them.

### Option 2: Put all fourteen questions to the product owner first
- **Pros:** Nothing is invented.
- **Cons:** Stops all provisioning and joining work behind a conversation, for questions where the
  defensible answer is obvious in most cases — nobody is going to argue for an alphabet
  containing both `O` and `0`.

### Option 3: Implement around them — no slug rule, no alphabet, no bootstrap
- **Pros:** Nothing is decided prematurely.
- **Cons:** Not implementable. Every one of these blocks a specific acceptance criterion, and the
  result would be stubs, which `AGENTS.md` forbids.

## Consequences

### Positive
- Provisioning, invitation and joining can be built to their acceptance criteria in full.
- The values sit in `InviteCodeOptions`, `Congregation`, and `Member` as named constants, so
  changing one is a one-line change with a compiler to find the callers.
- Two genuine specification gaps — the missing email field and the missing founding moderator —
  are now written down instead of being absorbed silently.

### Negative
- Five decisions belong to the product and were made by its implementer.
- The platform congregation is a real concept in the data that no requirement mentions. A reader
  of `L1-001` will not expect it.

### Risks
- **The founding-moderator field may be rejected.** If provisioning is meant to be followed by a
  separate designation step, `ProvisionCongregationCommand` loses a field and `L2-001` gains a
  note about the resulting unreachable state.
- **Thirty days may be too long** for a congregation that treats a code as a one-evening thing.
  It is an Options value precisely so this is configuration rather than a change.
- **Not verifying the address** means a mistyped one produces a pending member who can never sign
  in. A moderator sees the address in the approval queue, which is the cheapest place to catch it.

## Implementation Notes

- `InviteCodeOptions` is bound through `Microsoft.Extensions.Options` and `Configuration`, so the
  lifetime is deployment configuration rather than a recompile.
- The alphabet and length live on `InviteCode` as constants, because they are a property of what a
  code *is* rather than of how a deployment is set up.
- `CongregationSeeder` creates the platform congregation and one administrator from
  `Provisioning:AdministratorEmail`, idempotently, in the shape it already uses for St. Aidan's.
- `open-decisions.md` keeps its entries; the affected rows now point here.

## References

- `docs/detailed-designs/open-decisions.md` — `D-01`, `D-02`, `D-03`
- `docs/detailed-designs/congregations/configure-a-congregation/README.md`
- `docs/detailed-designs/access/issue-an-invite/README.md`
- `docs/detailed-designs/access/join-a-congregation/README.md`
- `docs/adr/backend/0001-use-sql-server-for-persistence.md` — the collation argument reused here
