# Review incoming requests

## Overview

Barnabas keeps requests within the congregation that owns their listing.

An **incoming request** — ask against a listing owned by the reader — presents the requester, listing, message, and terms. The owner uses this screen to understand an ask before accepting or declining it.

## Description

**Implementation boundary: Existing Lend projection; planned other-kind terms and bounded collection.**

`IncomingRequestsComponent` composes the incoming inbox screen in `barnabas`. `RequestStore.loadIncoming` calls `IRequestService.incoming` through `REQUEST_SERVICE`. `GetIncomingRequestsQuery` has no payload; `GetIncomingRequestsQueryHandler` reads the owner from `ICongregationContext`. It joins requests to owned listings and requester members within the congregation, orders by `MadeAt`, and projects `IncomingRequestDto`. The DTO includes requester and listing identifiers for links, message, kind, status, and formatted terms. `LoanTermsText` formats existing Lend terms; other kinds extend that projection when their commands arrive.

The target extracts the request row into a `domain` component with destinations passed from the page. Requester names open `/members/:memberId`; listing titles open `/listings/:listingId`. Decision behaviour remains in `RequestStore` and the accept/decline slices. Loading, empty, and retry states do not substitute for a successful collection. Pagination extends this currently unbounded collection through the collection design.

**Source anchors.** [GetIncomingRequestsQueryHandler.cs](../../../../backend/src/Barnabas.Application/Requests/GetIncomingRequests/GetIncomingRequestsQueryHandler.cs), [request.store.ts](../../../../frontend/projects/domain/src/lib/requests/request.store.ts).

**Acceptance verification.** [L2-059](../../../specs/L2.md#l2-059-review-incoming-requests): API AC 1, 3; E2E AC 2. The linked criteria retain their Given–When–Then wording. API acceptance uses SQL Server; E2E acceptance uses one page object per screen. New behaviour begins with its failing criterion. Design coverage does not assert an acceptance test pass.

## Requirements

The requirement text and identifiers are reproduced from [L2](../../../specs/L2.md). Each row names its [L1 parent](../../../specs/L1.md).

| L2 ID | Refines (L1) | Requirement |
|-------|--------------|-------------|
| `L2-059` | `L1-009` | A listing owner shall be able to see the requests made on their listings, each showing the requester, the listing, the message, and the details that kind of request carries. |

## Diagrams

The context identifies the actor and the Barnabas capability. Congregation boundaries also apply to linked resources.

![C4 context view for review incoming requests](diagrams/c4-context.png)

The container view places the Angular client, .NET API, and SQL Server persistence around this capability.

![C4 container view for review incoming requests](diagrams/c4-container.png)

The component view separates screen composition, API dispatch, application behaviour, domain rules, and persistence.

![C4 component view for review incoming requests](diagrams/c4-component.png)

The class view shows the feature types, their data, and typed dependencies. Planned additions are identified in the description.

![Class structure for review incoming requests](diagrams/class-structure.png)

The handler constrains the collection to the reader and projects destinations with the request summary.

![Sequence for review](diagrams/sequence-review.png)
