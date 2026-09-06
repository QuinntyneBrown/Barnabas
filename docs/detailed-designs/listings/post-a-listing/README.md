# Post a listing

## Overview

Posting is how anything reaches the board. A member says what they can spare — a thing or an
afternoon — and it becomes visible to their congregation.

**kind** — which of Lend, Give, Sell, or Help a listing is, chosen before any detail is
entered and fixed thereafter

The four kinds are not interchangeable, and the design turns on that. Lend keeps ownership
and needs a date the item comes back. Give transfers ownership and has no price. Sell states
an asking figure that Barnabas never handles. Help offers time rather than an object, so it
has no photo and needs the windows when the member is free.

The kind is therefore chosen first, on a screen of its own, and the form that follows asks
only what that kind needs. An earlier iteration of this product sent Lend, Give, and Sell to
one shared form carrying a price field, which made a gift look like a sale and lost the
return date a loan depends on.

A listing is created Active and belongs to the member who posted it and to their
congregation. Neither is supplied by the client.

## Description

Four commands, four handlers, four validators, one entity.

- **`ChooseKindComponent`** — Angular screen offering the four kinds and no detail fields.
- **`PostLendComponent`**, **`PostGiveComponent`**, **`PostSellComponent`**,
  **`PostHelpComponent`** — one form per kind, each rendering only its own fields.
- **`IListingsApi`** and **`ListingsApi`** — the interface the forms depend on and its typed
  client.
- **`ListingsController`** — exposes `POST /listings/lend`, `/give`, `/sell`, and `/help`.
- **`PostListingCommand`** and its four subtypes — the shared fields are title, description,
  category, and neighbourhood; each subtype adds its own terms.
- **The four handlers** — each creates a `Listing` of its kind and commits. `SaveChanges`
  stamps the congregation and the owner from the session.
- **The four validators** — declare the bounds each kind accepts, such as a Help command
  needing at least one window.
- **`IForbidFields` on three of the commands** — Lend and Give forbid `price`; Help forbids
  `price` and `photo`. This is what makes the per-kind prohibitions in `L2-027`, `L2-028`,
  and `L2-030` enforceable at all: those requirements demand a *rejection* of a submitted
  price, and a command with no `Price` property has nothing for a validator to check.
  Declaring the field as forbidden lets it be caught on the raw payload before binding
  discards it, as `platform/validate-and-bound-input` describes.
- **`Listing`** — domain entity holding kind, status, and the terms belonging to its kind as
  owned value types: `LoanTerms`, `SaleTerms`, `AvailabilityWindow`.
- **`ListingPhotoService`** — re-encodes and stores one image for a goods listing. Help
  listings accept none.

The four handlers stay separate rather than collapsing behind one command with a kind
discriminator. A single handler would branch on kind at every step — which fields to read,
which terms to build, which to reject — and that branching is precisely the shape that let
the kinds blur together before.

Validation bounds live in the validators; the mechanism running them is described once in
`platform/validate-and-bound-input`.

## Requirements

The feature realizes the following level-2 (L2) requirements. Each L2
requirement refines a level-1 (L1) requirement, cited by identifier. The
**Slice** column marks the requirements implemented by feature slice 1; the
remainder are designed here and implemented in a later slice.

| L2 ID | Refines (L1) | Slice | Requirement |
|-------|--------------|-------|-------------|
| `L2-026` | `L1-005` | &mdash; | Posting shall begin by choosing Lend, Give, Sell, or Help, and the chosen kind shall determine which detail form is presented. The kind shall not be silently defaulted. |
| `L2-027` | `L1-005` | 1 | A Lend listing shall carry a title, description, category, neighbourhood, and the period within which the owner expects the item back. It shall not carry a price. |
| `L2-028` | `L1-005` | &mdash; | A Give listing shall carry a title, description, category, and neighbourhood, and shall be marked as free with pickup arranged by the members. It shall not carry a price. |
| `L2-029` | `L1-005` | &mdash; | A Sell listing shall carry a title, description, category, neighbourhood, condition, and a price in Canadian dollars. The price is a stated asking figure only; see L2-034. |
| `L2-030` | `L1-005` | &mdash; | A Help listing shall carry a title, description, category, neighbourhood, and one or more availability windows. It shall not carry a price or a photo, because it offers time rather than an object. |
| `L2-031` | `L1-005` | 1 | Listing fields shall be bounded and validated, and errors shall name the field at fault. |
| `L2-032` | `L1-005` | &mdash; | A Lend, Give, or Sell listing shall accept at most one image, of a permitted type and bounded size. See L2-102 for upload safety. |
| `L2-033` | `L1-005` | 1 | After posting, the member shall be shown that the listing is live and offered the ways onward: view it, post another, or return to the board. |
| `L2-034` | `L1-005` | &mdash; | A price on a Sell listing is a stated asking figure. The system shall not collect, hold, or transmit payment at any point. |

## Diagrams

### Components

The controller dispatches to the handler matching the kind. All four build the same entity,
differing only in the terms they attach.

![C4 component view for posting a listing](diagrams/c4-component.png)

### Class structure

The four commands share title, description, category, and neighbourhood, and diverge in
their terms. `Listing` composes whichever value type its kind carries. Three of the commands
also declare the fields their kind forbids, which is how a price on a Lend listing is
rejected rather than silently dropped.

![Class diagram for posting a listing](diagrams/class-structure.png)

### Behaviour — posting a Lend listing

The kind is chosen before any field is shown, satisfying `L2-026`, and the form that follows
carries a return date and no price, which is what `L2-027` distinguishes.

![Sequence diagram for posting a Lend listing](diagrams/sequence-post-lend.png)

### Behaviour — a listing that fails its bounds

The validator names each failing field and the form marks them without losing what was
typed. The running of validators belongs to the platform; this feature only declares the
bounds.

![Sequence diagram for a validation failure](diagrams/sequence-invalid.png)
