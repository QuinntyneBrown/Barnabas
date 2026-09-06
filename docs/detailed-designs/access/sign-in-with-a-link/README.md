# Sign in with a link

## Overview

Barnabas connects members of one church congregation through a private board. A **sign-in token** — single-use secret exchanged for a member session — proves possession of the registered mailbox. The member requests a link, follows it, and receives access to the congregation identified by the member record.

The link expires after 15 minutes. Reuse and expiry produce the same recovery route. The address submission response does not identify registered members. No password is collected or stored.

## Description

**Implementation boundary: Existing core; planned delivery, timing, limiting, and atomic completion.**

`SignInComponent`, `CheckYourEmailComponent`, `SignInLandingComponent`, and `LinkExpiredComponent` are routed screens in `barnabas`. `SessionStore` holds the access token in a signal and consumes `ISessionService` through `SESSION_SERVICE`. `SessionService` implements that contract in `api`; `app.config.ts` binds it at the host.

`SessionsController` binds `POST /sessions/link` and `POST /sessions`. `RequestSignInLinkCommandHandler` calls `IAuthenticationStore.FindApprovedMemberByEmailAsync`, creates a random secret through `ISecretService`, persists its hash, and calls `IEmailSender`. The current adapter is `InMemoryEmailSender`. A production delivery adapter and provider remain `<TO SUPPLY>`.

`ExchangeSignInTokenCommandHandler` calls `TryConsumeSignInTokenAsync`. `AuthenticationStore` performs a parameterised conditional update of an unconsumed row. Exactly one concurrent exchange wins; missing, expired, or reused secrets return 410. Expiry is checked after consumption. The handler creates `Session` and `RefreshToken`, then `JwtIssuer` issues member, congregation, role, and `sid` claims from server records.

The existing flow commits consumption, session creation, and refresh creation separately. The target persistence operation encloses them in one SQL Server transaction; failure rolls back session creation and consumption together. No successful token response precedes commit. A lost response can require a fresh link, which the expired-link screen offers.

`L2-013` timing equivalence and `L2-017` limiting remain planned. The target performs bounded asynchronous address processing and responds 202 before delivery, including the unknown-address case. The [abuse design](../../platform/limit-abuse/README.md) applies five requests per address per 15 minutes and a source limit. Delivery failure remains retryable without changing the public response. Pending-member sign-in extends the approved-only lookup as described in [joining](../join-a-congregation/README.md). Stored hashes reduce exposure of token secrets; they do not remove the sensitivity of member records.

The planned target pairs durable address work with a bounded delivery worker and a retry/dead-letter state. A provider outage does not alter the member-enumeration response; operations exposes the queue failure without logging the address or link. Delivery retry reuses the queued token rather than issuing a new sign-in identity. Provider integration and retry bounds remain the explicit D-08 inputs.

**Source anchors.** [ExchangeSignInTokenCommandHandler.cs](../../../../backend/src/Barnabas.Application/Access/ExchangeSignInToken/ExchangeSignInTokenCommandHandler.cs), [AuthenticationStore.cs](../../../../backend/src/Barnabas.Infrastructure/Persistence/AuthenticationStore.cs), [session.service.contract.ts](../../../../frontend/projects/api/src/lib/sessions/session.service.contract.ts).

**Acceptance verification.** [L2-012](../../../specs/L2.md#l2-012-request-a-sign-in-link): API AC 1, 2, 4; E2E AC 3. [L2-013](../../../specs/L2.md#l2-013-do-not-disclose-whether-an-email-address-is-registered): API AC 1; E2E AC 2. [L2-014](../../../specs/L2.md#l2-014-exchange-a-sign-in-link-for-a-session): API AC 1, 2, 4, 5; E2E AC 3. [L2-015](../../../specs/L2.md#l2-015-expire-a-sign-in-link): API AC 1, 2; E2E AC 3. [L2-016](../../../specs/L2.md#l2-016-make-a-sign-in-link-single-use): API AC 1, 2. [L2-017](../../../specs/L2.md#l2-017-rate-limit-sign-in-link-requests): API AC 1, 2. The linked criteria retain their Given–When–Then wording. API acceptance uses SQL Server; E2E acceptance uses one page object per screen. New behaviour begins with its failing criterion. Design coverage does not assert an acceptance test pass.

## Requirements

The requirement text and identifiers are reproduced from [L2](../../../specs/L2.md). Each row names its [L1 parent](../../../specs/L1.md).

| L2 ID | Refines (L1) | Requirement |
|-------|--------------|-------------|
| `L2-012` | `L1-003` | A member shall be able to request a single-use sign-in link sent to their registered email address. The system shall never store a password. |
| `L2-013` | `L1-003` | The sign-in response shall be identical whether or not the address belongs to a member, so the endpoint cannot be used to enumerate members. |
| `L2-014` | `L1-003` | Following a valid sign-in link shall issue a JWT bound to the member and their congregation. |
| `L2-015` | `L1-003` | A sign-in token shall expire no more than 15 minutes after issue. |
| `L2-016` | `L1-003` | A sign-in token shall be consumed on first exchange. |
| `L2-017` | `L1-003` | Requests for sign-in links shall be limited per address and per source to prevent mailbox flooding. |

## Diagrams

The context identifies the actor and the Barnabas capability. Congregation boundaries also apply to linked resources.

![C4 context view for sign in with a link](diagrams/c4-context.png)

The container view places the Angular client, .NET API, and SQL Server persistence around this capability.

![C4 container view for sign in with a link](diagrams/c4-container.png)

The component view separates screen composition, API dispatch, application behaviour, domain rules, and persistence.

![C4 component view for sign in with a link](diagrams/c4-component.png)

The class view shows the feature types, their data, and typed dependencies. Planned additions are identified in the description.

![Class structure for sign in with a link](diagrams/class-structure.png)

The target link request returns the same 202 before address processing and delivery. Rate limiting rejects the sixth address request within 15 minutes.

![Sequence for request link](diagrams/sequence-request-link.png)

The target exchange consumes the link and persists the session in one transaction. The SQL affected-row count settles concurrent use.

![Sequence for exchange](diagrams/sequence-exchange.png)
