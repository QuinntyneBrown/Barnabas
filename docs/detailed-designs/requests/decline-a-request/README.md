# Decline a request

## Overview

Barnabas is a private board where church congregation members offer goods or time through listings.

A **pending request** — ask awaiting the listing owner's decision — can become Accepted or Declined once. Declining retains the request with a Declined status and creates no message thread.

The decision belongs to the listing owner within the congregation. The owner confirms before the command is sent. Cancellation leaves the request pending.

## Description

**Implementation boundary: Existing decision; planned notification integration.**

`IncomingRequestsComponent` calls `RequestStore.decline`, which consumes `IRequestService` through `REQUEST_SERVICE`. `RequestsController` dispatches `DeclineRequestCommand(RequestId)` from `POST /requests/{requestId}/decline`. The command declares `IRequireOwnership<RequestOwnership>`; `RequestOwnershipLookup` resolves ownership through the listing. Same-congregation non-owners receive 403; missing and foreign identifiers receive 404.

`DeclineRequestCommandHandler` loads `ListingRequest` and calls `Decline(asOf)`. `ListingRequest.RowVersion` is a SQL Server `byte[]` row version. The domain rejects a previously decided request through `RequestAlreadyDecidedException`, and concurrency failures return 409 through `ProblemDetailsExceptionHandler`. Error mapping belongs to that exception handler, not the controller.

The handler changes only the request state. `ConfirmDialogComponent` is the shared confirmation primitive; there is no ConfirmDeclineDialog type. `DeclineRequestResult` returns `RequestId` and Status. The requester sees Declined in the outgoing inbox. The command has no decline-reason field because the requirements specify none. A declined request cannot later be accepted, and a new eligible request is a separate record.

Decision notifications are a planned addition in [receive notifications](../../notifications/receive-notifications/README.md). The target commits an enabled notification with the decision and, on acceptance, the thread. A failed save produces no notification. The current acceptance handler maps all `DbUpdateException` instances to a decision conflict; the target narrows that mapping to the expected uniqueness violation so storage outages remain server failures. Stale decisions refresh the affected inbox row and explain the conflict instead of claiming success.

**Source anchors.** [DeclineRequestCommandHandler.cs](../../../../backend/src/Barnabas.Application/Requests/DeclineRequest/DeclineRequestCommandHandler.cs), [ListingRequestConfiguration.cs](../../../../backend/src/Barnabas.Infrastructure/Persistence/Configurations/ListingRequestConfiguration.cs), [MessageThreadConfiguration.cs](../../../../backend/src/Barnabas.Infrastructure/Persistence/Configurations/MessageThreadConfiguration.cs).

**Acceptance verification.** [L2-061](../../../specs/L2.md#l2-061-decline-a-request): API AC 1, 4; E2E AC 2, 3. The linked criteria retain their Given–When–Then wording. API acceptance uses SQL Server; E2E acceptance uses one page object per screen. New behaviour begins with its failing criterion. Design coverage does not assert an acceptance test pass.

## Requirements

The requirement text and identifiers are reproduced from [L2](../../../specs/L2.md). Each row names its [L1 parent](../../../specs/L1.md).

| L2 ID | Refines (L1) | Requirement |
|-------|--------------|-------------|
| `L2-061` | `L1-009` | Declining shall set the request to `Declined`, require confirmation, and shall not create a message thread. |

## Diagrams

The context identifies the actor and the Barnabas capability. Congregation boundaries also apply to linked resources.

![C4 context view for decline a request](diagrams/c4-context.png)

The container view places the Angular client, .NET API, and SQL Server persistence around this capability.

![C4 container view for decline a request](diagrams/c4-container.png)

The component view separates screen composition, API dispatch, application behaviour, domain rules, and persistence.

![C4 component view for decline a request](diagrams/c4-component.png)

The class view shows the feature types, their data, and typed dependencies. Planned additions are identified in the description.

![Class structure for decline a request](diagrams/class-structure.png)

The decision and its dependent writes commit together. The request row version settles a simultaneous accept and decline.

![Sequence for decline](diagrams/sequence-decline.png)
