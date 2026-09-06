# Make a request

## Overview

Barnabas is a lending, giving, selling, and helping board scoped to a single church congregation. A member posts a **listing** — offer of an item or of time, published to one congregation's board — and other members ask for it.

**request** — a member's ask against one listing, carrying a message to the owner and the terms that kind of listing needs

This feature covers composing and sending that ask. It is the entry point to the coordination flow: a request is the only thing that opens a message thread between two members, so nothing else in the product connects a requester to an owner.

A listing carries one of four kinds, and the four are not interchangeable. **Lend**, **Give**, and **Sell** offer goods; **Help** offers time. Each kind collects different terms, and each presents a different call to action — *Request to borrow*, *Request this*, *Request to buy*, *Request this help*. A Lend request needs to say when the item comes back. A Help request needs to name which of the offered windows the requester wants. Treating the four as one form was the defect this design exists to prevent.

Barnabas brokers the introduction and nothing beyond it. The request flow settles no payment, arranges no delivery, and records no deposit; the two members meet in person and sort out the rest between themselves.

## Description

The slice runs from the Angular request screen to the database.

- **`RequestLendComponent`**, **`RequestGiveComponent`**, **`RequestSellComponent`**, **`RequestHelpComponent`** — Angular page components in the Barnabas web application, one per listing kind. Each renders only the fields its kind collects. Template, styles, and class occupy separate files, and component state is held in signals.
- **`IRequestsApi`** — the interface the components depend on. Components consume the abstraction; dependency injection supplies the implementation.
- **`RequestsApi`** — typed HTTP client implementing `IRequestsApi`. It builds the request for the endpoint matching the kind and returns a typed result.
- **`RequestStore`** — signal-backed store holding the in-progress request and the result of sending it.
- **`RequestsController`** — ASP.NET Core controller in the Barnabas API. It exposes four endpoints under `/listings/{listingId}/requests`, authenticates the caller, applies the endpoint policy, and dispatches the matching command. It holds no logic of its own.
- **`MakeLoanRequestCommand`**, **`MakeGiftRequestCommand`**, **`MakePurchaseRequestCommand`**, **`MakeHelpRequestCommand`** — request objects, one per kind. Each carries `ListingId` and `Message`, plus the terms its kind requires.
- **`MakeLoanRequestCommandHandler`** and its three siblings — MediatR handlers holding the application logic. Each loads the listing within the caller's congregation, applies the eligibility policy, creates the request, and commits in one unit of work.
- **`MakeLoanRequestCommandValidator`** and its three siblings — FluentValidation validators enforcing the per-kind field rules before a handler runs.
- **`RequestEligibilityPolicy`** — domain service holding the four conditions a request has
  to satisfy: the listing is not the caller's own, the listing is active, its kind matches
  the endpoint the request arrived on, and the caller holds no open request against it
  already.

  The last two were absent from an earlier draft, and their absence left real behaviour
  undefined. A listing that has been sold or archived is still reachable by its identifier,
  so a request could be made against something already gone. And nothing tied the endpoint
  to the listing's kind, so a Sell listing could acquire a request carrying loan terms and
  no price.
- **`ListingRequest`** — domain entity owning request state. Its `Accept()` and `Decline()` methods enforce the transitions used by the sibling features.
- **`RequestStatus`** — enumeration of the states a request holds: `Pending`, `Accepted`, `Declined`.
- **`LoanTerms`**, **`PickupTerms`**, **`AvailabilityWindow`** — value types carrying the per-kind terms.

The four handlers stay separate rather than collapsing behind one generic command. A single
handler would need a conditional over the kind at every step, and that shape is what allowed
the four kinds to drift into one form.

**The duplicate rule lives in two places, and needs to.** `RequestEligibilityPolicy` checks
for an open request so that the common case returns a clear 409 without touching the
database twice. A unique index on `(ListingId, RequesterId)` where the status is `Pending`
then enforces it, because two simultaneous requests both pass the check before either
commits. A policy alone cannot make a rule true under concurrency; only the constraint can,
and `L2-062` asks for exactly one request to survive.

## Requirements

The feature realizes the following level-2 (L2) requirements. Each L2
requirement refines a level-1 (L1) requirement, cited by identifier. The
**Slice** column marks the requirements implemented by feature slice 1; the
remainder are designed here and implemented in a later slice.

| L2 ID | Refines (L1) | Slice | Requirement |
|-------|--------------|-------|-------------|
| `L2-054` | `L1-009` | 1 | A Lend request shall collect a message, a proposed pickup date, a proposed return date, and an acknowledgement that the item remains the owner's. |
| `L2-055` | `L1-009` | &mdash; | A Give request shall collect a message and a proposed pickup time. It shall not collect a price or a return date. |
| `L2-056` | `L1-009` | &mdash; | A Sell request shall collect a message and a proposed pickup time, and shall restate that payment is settled in person between the members. |
| `L2-057` | `L1-009` | &mdash; | A Help request shall collect a message and require the requester to choose one of the availability windows the offer declared. |
| `L2-058` | `L1-009` | 1 | Sending a request shall confirm to the requester that it was sent, name the owner, and say what happens next. |
| `L2-062` | `L1-009` | 1 | A member shall not request their own listing, and shall not hold more than one open request against the same listing. A request shall name a listing that is active, and shall be made through the endpoint matching that listing's kind. |
| `L2-063` | `L1-009` | &mdash; | The request flow shall not take payment details, arrange delivery, or record a deposit for any kind of listing. |

## Diagrams

### System context

The requester asks for a listing through Barnabas, which notifies the listing owner through an external email provider. Both members belong to the same congregation.

![C4 system context for making a request](diagrams/c4-context.png)

### Containers

The request travels from the Barnabas web application to the Barnabas API, which persists it in the Barnabas database and queues the owner's notification.

![C4 container view for making a request](diagrams/c4-container.png)

### Components

Inside the API, `RequestsController` dispatches to the handler matching the listing kind. All four handlers consult `RequestEligibilityPolicy` before creating a `ListingRequest`.

![C4 component view for making a request](diagrams/c4-component.png)

### Class structure

The four commands share `ListingId` and `Message` and diverge in their terms. `ListingRequest` composes `LoanTerms` for a Lend request and `PickupTerms` for a Give or Sell request, and references the `AvailabilityWindow` chosen for a Help request.

![Class diagram for making a request](diagrams/class-structure.png)

### Behaviour — request to borrow

The Lend path carries the fullest set of terms. `RequestLendComponent` collects the pickup date, return date, and acknowledgement required by `L2-054`; the handler applies the `L2-062` eligibility checks and commits in one unit of work. A missing return date or a withheld acknowledgement returns `ProblemDetails` naming the field, and the screen holds.

![Sequence diagram for requesting to borrow](diagrams/sequence-borrow.png)

### Behaviour — request to buy

The Sell path adds the `L2-056` restatement that payment is settled in person. No payment field is rendered and no payment field is accepted, per `L2-063`. The Give path follows this same shape without the payment restatement, since a Give listing carries no price.

![Sequence diagram for requesting to buy](diagrams/sequence-purchase.png)

### Behaviour — request help

The Help path differs from the goods paths in one way that matters: the screen first reads the windows the listing declared and offers only those, and the handler rejects a window the listing did not declare. Help offers time, so there is no item and no pickup of an object.

![Sequence diagram for requesting help](diagrams/sequence-help.png)

### Behaviour — eligibility guards

`L2-062` is enforced wherever it can be. The screen hides the action from the listing's own
owner; the policy rejects a self-request, a closed listing, and a mismatched kind; and the
unique index decides the duplicate case, which is the only one a policy cannot settle on its
own. Self-request and kind mismatch return `400`; a closed listing and a duplicate return
`409`.

![Sequence diagram for the eligibility guards](diagrams/sequence-guards.png)
