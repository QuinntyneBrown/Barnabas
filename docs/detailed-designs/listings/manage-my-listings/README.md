# Manage my listings

## Overview

A member who has posted something needs to see it, change it, and find out who has asked for
it. This feature is that view: the listings a member owns, and the edit that changes one.

**open request count** — how many members are currently waiting on a decision about a
listing

The count is the useful part of the screen. A listing with three people waiting is a listing
that needs attention, and the count leads to the requests themselves rather than merely
reporting a number. An earlier iteration showed it as plain text, so a member could see that
three people had asked and had no route to what they had said.

Editing changes a listing's description of itself, not what it is. The kind is fixed at
posting: a Lend listing carries a return date and a Sell listing carries a price, and those
terms are not interchangeable, so changing the kind would leave a listing with terms it
cannot hold.

Only the member who posted a listing may change it. That rule is enforced before the handler
runs, so it holds for every route into the endpoint.

## Description

One query, one command.

- **`MyListingsComponent`** — Angular screen listing the caller's own listings, with chips
  moving between active and wrapped-up.
- **`EditListingComponent`** — the edit form. It is distinct from the create form in its
  heading and its action, which read as editing and saving rather than posting.
- **`GetMyListingsQuery`** and its handler — read the caller's listings and count the open
  requests against each in one query rather than one per row.
- **`MyListingDto`** — read model carrying title, kind, status, price where the kind has
  one, and the open request count.
- **`EditListingCommand`** — carries the listing identifier and the editable fields. It
  implements `IRequireOwnership`, which is what brings it under the ownership check.
- **`EditListingCommandHandler`** — applies the change and commits. It performs no ownership
  check of its own, because by the time it runs the check has passed.
- **`Listing.Edit`** — applies the editable fields on the entity. Kind and owner are not
  among its parameters, so neither can be changed by this route.

Ownership is declared by the command implementing `IRequireOwnership` rather than checked
inside the handler. The mechanism is described in `platform/authorise-a-request`; what this
feature contributes is the declaration that an edit demands ownership.

The query counts requests in the same round trip as the listings. Counting per row would
turn a four-listing screen into five queries, and a member with a busy board into many more.

## Requirements

The feature realizes the following level-2 (L2) requirements. Each L2
requirement refines a level-1 (L1) requirement, cited by identifier. The
**Slice** column marks the requirements implemented by feature slice 1; the
remainder are designed here and implemented in a later slice.

| L2 ID | Refines (L1) | Slice | Requirement |
|-------|--------------|-------|-------------|
| `L2-035` | `L1-006` | 1 | A member shall be able to see their own active listings, each showing its kind, status, and the number of open requests against it. |
| `L2-036` | `L1-006` | &mdash; | A member shall be able to change the details of their own listing. The edit screen shall be distinct from the create screen in its heading and its action. |
| `L2-041` | `L1-006` | 1 | A listing shall be modifiable only by the member who created it, or by a moderator acting under L2-084. |

## Diagrams

### Components

A read and a write over the same entity. The write passes through the authorisation
behaviour; the read does not need to, because the query is already constrained to the
caller.

![C4 component view for managing listings](diagrams/c4-component.png)

### Class structure

`EditListingCommand` implements `IRequireOwnership`, which is the whole of its contribution
to the ownership rule. `Listing.Edit` accepts no kind and no owner.

![Class diagram for managing listings](diagrams/class-structure.png)

### Behaviour — editing, and an edit by someone who does not own it

The count on each row leads to the requests, satisfying `L2-035`. The ownership branch shows
`L2-041` enforced before the handler runs, so the listing is untouched on failure.

![Sequence diagram for editing a listing](diagrams/sequence-edit.png)
