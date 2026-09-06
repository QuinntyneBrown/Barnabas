# Accept a request

## Overview

A request against a listing sits in the `Pending` state until the listing's owner decides it. This feature covers the affirmative decision: the owner agrees, and the two members are put in touch.

Acceptance is the hinge of the whole product. Barnabas holds no payment, arranges no delivery, and moves no goods; its single job is to connect the member who has something with the member who needs it. Accepting is the moment that connection is made.

**message thread** — a conversation between two members about one listing, opened by an accepted request

Acceptance therefore does two things at once. It moves the request to `Accepted`, and it opens the message thread on which the handoff is arranged. The thread is not a separate act the owner performs afterward: a thread exists only as a consequence of an accepted request, which is what guarantees every conversation in Barnabas has a subject. Free-form messaging between members does not exist.

A request is decided once. A request already accepted or already declined accepts no second decision, and the attempt is reported as a conflict rather than silently ignored.

## Description

The slice runs from the inbox screen to the database and writes in one transaction.

- **`IncomingRequestsComponent`** — Angular page component offering the accept action on each pending row, described further in the *Review incoming requests* design.
- **`IRequestsApi`** and **`RequestsApi`** — the interface the component depends on and its typed HTTP client implementation.
- **`RequestsController`** — ASP.NET Core controller exposing `POST /requests/{requestId}/accept`. It authenticates the caller, applies the endpoint policy, and dispatches the command.
- **`AcceptRequestCommand`** — request object carrying the target `RequestId`. The deciding member is taken from the session.
- **`AcceptRequestCommandHandler`** — MediatR handler holding the application logic. It loads the request and its listing within the caller's congregation, applies the acceptance, opens the thread, and commits in one unit of work.
- **`AcceptRequestResult`** — result carrying the request identifier and the identifier of the thread just opened, so the screen can offer the thread directly.
- **`ListingRequest`** — domain entity owning the request state machine. Its `Accept()` method enforces that only a `Pending` request transitions.
- **`RequestStatus`** — enumeration of the states a request holds: `Pending`, `Accepted`, `Declined`.
- **`MessageThread`** — domain entity carrying the listing, the owner, and the requester.
- **`RequestAlreadyDecidedException`** — raised by `ListingRequest` when a decision is
  applied to a request that already holds one. The controller maps it to `409`.
- **`ListingRequest.RowVersion`** — a concurrency token checked on save. Two owners, or one
  owner in two tabs, can load the same pending request and one accept while the other
  declines; without the token both would commit and the last write would silently win.

The transition lives on the entity rather than in the handler. Both this feature and *Decline a request* change the same state, and putting the rule in one place keeps the two decisions from drifting apart.

The thread is created in the same unit of work as the state change. A committed acceptance
without a thread would leave the requester notified of a decision they cannot act on.

**Two constraints make "one decision, one thread" true rather than intended.** The row
version on `ListingRequest` decides an accept racing a decline, so exactly one transition
survives and the other receives `409`. A unique constraint on `MessageThread.RequestId`
decides the thread, so a request cannot acquire two even if two callers reach the creation
step together. Checking the status in the handler is a courtesy that produces a clear error
in the common case; it is not what makes the rule hold.

## Requirements

The feature realizes the following level-2 (L2) requirements. Each L2
requirement refines a level-1 (L1) requirement, cited by identifier. The
**Slice** column marks the requirements implemented by feature slice 1; the
remainder are designed here and implemented in a later slice.

| L2 ID | Refines (L1) | Slice | Requirement |
|-------|--------------|-------|-------------|
| `L2-060` | `L1-009` | 1 | Accepting shall set the request to `Accepted`, open a message thread between the two members, and confirm to the owner what to do next. |

## Diagrams

### System context

The owner accepts a request made by another member of the same congregation. Barnabas notifies the requester of the decision through an external email provider.

![C4 system context for accepting a request](diagrams/c4-context.png)

### Containers

The decision travels from the Barnabas web application to the Barnabas API, which writes the request and the thread to the Barnabas database and queues the requester's notification.

![C4 container view for accepting a request](diagrams/c4-container.png)

### Components

`RequestsController` dispatches one command. `AcceptRequestCommandHandler` mutates `ListingRequest`, creates `MessageThread`, and persists both together.

![C4 component view for accepting a request](diagrams/c4-component.png)

### Class structure

`ListingRequest` owns the transition and raises `RequestAlreadyDecidedException` on a second decision. `MessageThread` holds the listing and both members, and `AcceptRequestResult` returns its identifier to the screen.

![Class diagram for accepting a request](diagrams/class-structure.png)

### Behaviour — accept a request

The handler loads the request within the caller's congregation, applies the `L2-060`
transition on the entity, opens the thread, and commits in one unit of work. A request that
already holds a decision returns `409` rather than transitioning a second time — and so does
one decided by another caller between the load and the save, which the row version catches
and the status check cannot.

![Sequence diagram for accepting a request](diagrams/sequence-accept.png)
