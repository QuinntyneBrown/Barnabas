# Barnabas detailed designs

This tree covers all 120 detailed requirements in [L2](../specs/L2.md), tracing to the 18 parent requirements in [L1](../specs/L1.md). Barnabas is a private congregation board for lending, giving, selling, and offering help. It brokers introductions; members arrange handoffs in person. It handles no payments, deposits, shipping, delivery, or return management.

The existing feature paths remain stable. Congregations, access, members, listings, board, requests, messaging, notifications, moderation, and platform group the vertical capabilities. Platform pages describe behaviours shared by those capabilities. The requirements use level-only identifiers such as `L2-001`; their labelled L1/L2 sections satisfy the requirements gate without rewriting identifiers.

Each feature has Overview, Description, Requirements, and Diagrams sections. Requirement tables reproduce source wording with whitespace normalised and preserve every identifier and parent. Acceptance-verification links identify every applicable API/E2E criterion in its original Given–When–Then form. The index records design coverage, not completed implementation or test passes. Existing source anchors identify the inspected types; planned extensions are labelled separately.

The [open decisions register](open-decisions.md) records missing policy and conflicting criteria with `<TO SUPPLY>`. These are explicit design inputs awaiting resolution, not implemented defaults or omitted scope. [Verification](verification.md) records the checks performed on the completed tree.

**Architecture.** The API uses .NET, MediatR pinned to `12.5.0`, dependency injection, Options, and Configuration. Controllers bind, dispatch, and return. Commands, queries, handlers, and validators live in Application. Domain references no other project. Infrastructure implements persistence and external-service contracts. Each backend type occupies its own named file. Runtime diagrams show calls through infrastructure contracts; arrows do not authorise Domain references to Infrastructure. The existing Application persistence contract exposes EF abstractions; this design records that boundary rather than inventing a replacement repository layer.

SQL Server is the sole persistence provider in development, acceptance, and production, following [ADR-0001](../adr/backend/0001-use-sql-server-for-persistence.md). Conditional updates, filtered unique indexes, and `byte[]` rowversion enforce concurrent single-use and decision rules. Each planned feature uses migrations and SQL Server acceptance fixtures. Authentication is JWT plus server-side session validation; congregation and member identifiers come from verified records, never caller-selected tenancy.

Angular libraries depend in one direction: `components` imports Angular only; `domain` may import `components` and `api`; `barnabas` composes all three. Route pages and navigation chrome belong in `barnabas`. Congregation-aware content regions belong in `domain`. Plain inputs/outputs, dialogs, icons, and controls belong in `components`. Class, template, and styles occupy separate files. Signals hold state; services implement behaviour. Consumers inject interface tokens declared beside their interfaces in `*.contract.ts`; only the host binds the real or test implementation. Contracts and implementations use singular `Service` names. Existing stores that are consumed concretely are identified as implementation boundaries rather than precedent for new service seams.

**Acceptance approach.** Backend acceptance drives the API against SQL Server in `backend/tests/Barnabas.IntegrationTests`. Frontend acceptance uses Playwright and one page object per screen in `frontend/tests/e2e`. Tests state intent and contain no selectors. New behaviour starts with a failing explicit criterion and keeps the trace comment required by L2. Documentation changes do not claim that unimplemented criteria pass. Existing slice implementation history remains in [feature-slice.md](../feature-slice.md); it does not limit this design tree's requirements coverage.

**Feature catalogue.** Implementation boundaries reflect inspected source as of 2026-09-06. “Planned” identifies proposed components; “Existing” identifies code present, without certifying all criteria in that feature.


| Subsystem | Feature | L2 coverage | Implementation boundary |
|---|---|---|---|
| access | [Sign in with a link](access/sign-in-with-a-link/README.md) | `L2-012`, `L2-013`, `L2-014`, `L2-015`, `L2-016`, `L2-017` | Existing core; planned delivery, timing, limiting, and atomic completion |
| access | [End a session](access/end-a-session/README.md) | `L2-018`, `L2-019` | Existing renewal and revocation; planned atomic rotation and failure presentation |
| board | [Browse the board](board/browse-the-board/README.md) | `L2-042`, `L2-043`, `L2-044`, `L2-045`, `L2-046`, `L2-047` | Existing board; planned continuation and remaining kind presentation |
| listings | [Post a listing](listings/post-a-listing/README.md) | `L2-026`, `L2-027`, `L2-028`, `L2-029`, `L2-030`, `L2-031`, `L2-032`, `L2-033`, `L2-034` | Existing Lend slice; planned Give, Sell, Help, and photos |
| listings | [Manage my listings](listings/manage-my-listings/README.md) | `L2-035`, `L2-036`, `L2-041` | Existing own-list query; planned editing |
| listings | [Close out and archive](listings/close-out-and-archive/README.md) | `L2-037`, `L2-038`, `L2-039`, `L2-040` | Existing close-out; planned archive, restore, and deletion |
| requests | [Make a request](requests/make-a-request/README.md) | `L2-054`, `L2-055`, `L2-056`, `L2-057`, `L2-058`, `L2-062`, `L2-063` | Existing Lend request; planned remaining kinds and active-state race guard |
| requests | [Review incoming requests](requests/review-incoming-requests/README.md) | `L2-059` | Existing Lend projection; planned other-kind terms and bounded collection |
| requests | [Track my requests](requests/track-my-requests/README.md) | `L2-120` | Existing Lend projection; planned other-kind terms and bounded collection |
| requests | [Accept a request](requests/accept-a-request/README.md) | `L2-060` | Existing decision; planned notification integration |
| requests | [Decline a request](requests/decline-a-request/README.md) | `L2-061` | Existing decision; planned notification integration |
| messaging | [Find a thread](messaging/find-a-thread/README.md) | `L2-064`, `L2-065` | Existing list and thread creation; planned bounded projections |
| messaging | [Read and reply](messaging/read-and-reply/README.md) | `L2-066`, `L2-067`, `L2-068`, `L2-069` | Existing read and send; planned race-safe read positions and bounded messages |
| platform | [Scope queries to a congregation](platform/scope-queries-to-a-congregation/README.md) | `L2-088`, `L2-089`, `L2-091`, `L2-092` | Existing filtered accessors; planned write guards and new-feature scoping |
| platform | [Authorise a request](platform/authorise-a-request/README.md) | `L2-093`, `L2-094`, `L2-095` | Existing JWT and ownership; planned current-status policy and administrative surfaces |
| platform | [Validate and bound input](platform/validate-and-bound-input/README.md) | `L2-096` | Existing validation pipeline; planned streaming and full binding-error hardening |
| platform | [Use the responsive and accessible shell](platform/responsive-and-accessible-shell/README.md) | `L2-108`, `L2-109`, `L2-110`, `L2-111`, `L2-112`, `L2-113`, `L2-114`, `L2-115` | Existing shell and primitives; planned full-screen conformance and notification count |
| congregations | [Configure a congregation](congregations/configure-a-congregation/README.md) | `L2-001`, `L2-002`, `L2-004` | Existing congregation entity and seeding; planned configuration slice |
| congregations | [Designate moderators](congregations/designate-moderators/README.md) | `L2-003` | Existing role model; planned grant and current-role presentation |
| access | [Issue an invite](access/issue-an-invite/README.md) | `L2-005`, `L2-090` | Existing seeded entity; planned issue and revoke |
| access | [Join a congregation](access/join-a-congregation/README.md) | `L2-006`, `L2-007`, `L2-008`, `L2-009`, `L2-010`, `L2-011` | Planned joining; existing invite and member entities |
| members | [Edit own profile](members/edit-own-profile/README.md) | `L2-020`, `L2-021`, `L2-022`, `L2-023` | Planned profile slice; existing name and neighbourhood fields |
| members | [View a member profile](members/view-a-member-profile/README.md) | `L2-024` | Planned; current member route is a placeholder |
| members | [Leave a congregation](members/leave-a-congregation/README.md) | `L2-025` | Planned |
| members | [Export and erase personal data](members/export-and-erase-personal-data/README.md) | `L2-101` | Planned |
| members | [Browse the directory](members/browse-the-directory/README.md) | `L2-076`, `L2-077`, `L2-078`, `L2-079` | Planned |
| board | [Search listings](board/search-listings/README.md) | `L2-048`, `L2-049`, `L2-050`, `L2-051`, `L2-052`, `L2-053` | Planned; current search route is a placeholder |
| notifications | [Receive notifications](notifications/receive-notifications/README.md) | `L2-070`, `L2-071`, `L2-072` | Planned |
| notifications | [Review notifications](notifications/review-notifications/README.md) | `L2-073`, `L2-074` | Planned; current notification route is a placeholder |
| notifications | [Control notifications](notifications/control-notifications/README.md) | `L2-075` | Planned |
| moderation | [Report a listing](moderation/report-a-listing/README.md) | `L2-080`, `L2-081` | Planned |
| moderation | [Review reported listings](moderation/review-reported-listings/README.md) | `L2-082`, `L2-083`, `L2-084` | Planned |
| moderation | [Review pending members](moderation/review-pending-members/README.md) | `L2-085`, `L2-086`, `L2-087` | Planned; existing member status enumeration |
| listings | [Attach a listing photo](listings/attach-a-listing-photo/README.md) | `L2-032`, `L2-102`, `L2-106` | Planned |
| platform | [Display member text safely](platform/display-member-text-safely/README.md) | `L2-097`, `L2-098` | Existing encoding and parameters; planned hosted CSP and full-surface verification |
| platform | [Limit abuse](platform/limit-abuse/README.md) | `L2-099` | Planned |
| platform | [Secure transport and tokens](platform/secure-transport-and-tokens/README.md) | `L2-100` | Existing JWT/cookies; planned production HTTPS and Options validation |
| platform | [Serve collections under load](platform/serve-collections-under-load/README.md) | `L2-103`, `L2-104`, `L2-105`, `L2-107` | Existing board cursor; planned full paging, budgets, and measured isolation |
| platform | [Observe service health](platform/observe-service-health/README.md) | `L2-116`, `L2-117`, `L2-118`, `L2-119` | Existing shallow health and handled-error logging; planned operational checks |

**Requirement traceability.** Every L2 requirement has a feature design; shared requirements appear in both affected slices.

| L2 ID | Refines (L1) | Design |
|---|---|---|
| `L2-001` | `L1-001` | [Configure a congregation](congregations/configure-a-congregation/README.md) |
| `L2-002` | `L1-001` | [Configure a congregation](congregations/configure-a-congregation/README.md) |
| `L2-003` | `L1-001` | [Designate moderators](congregations/designate-moderators/README.md) |
| `L2-004` | `L1-001` | [Configure a congregation](congregations/configure-a-congregation/README.md) |
| `L2-005` | `L1-002` | [Issue an invite](access/issue-an-invite/README.md) |
| `L2-006` | `L1-002` | [Join a congregation](access/join-a-congregation/README.md) |
| `L2-007` | `L1-002` | [Join a congregation](access/join-a-congregation/README.md) |
| `L2-008` | `L1-002` | [Join a congregation](access/join-a-congregation/README.md) |
| `L2-009` | `L1-002` | [Join a congregation](access/join-a-congregation/README.md) |
| `L2-010` | `L1-002` | [Join a congregation](access/join-a-congregation/README.md) |
| `L2-011` | `L1-002` | [Join a congregation](access/join-a-congregation/README.md) |
| `L2-012` | `L1-003` | [Sign in with a link](access/sign-in-with-a-link/README.md) |
| `L2-013` | `L1-003` | [Sign in with a link](access/sign-in-with-a-link/README.md) |
| `L2-014` | `L1-003` | [Sign in with a link](access/sign-in-with-a-link/README.md) |
| `L2-015` | `L1-003` | [Sign in with a link](access/sign-in-with-a-link/README.md) |
| `L2-016` | `L1-003` | [Sign in with a link](access/sign-in-with-a-link/README.md) |
| `L2-017` | `L1-003` | [Sign in with a link](access/sign-in-with-a-link/README.md) |
| `L2-018` | `L1-003` | [End a session](access/end-a-session/README.md) |
| `L2-019` | `L1-003` | [End a session](access/end-a-session/README.md) |
| `L2-020` | `L1-004` | [Edit own profile](members/edit-own-profile/README.md) |
| `L2-021` | `L1-004` | [Edit own profile](members/edit-own-profile/README.md) |
| `L2-022` | `L1-004` | [Edit own profile](members/edit-own-profile/README.md) |
| `L2-023` | `L1-004` | [Edit own profile](members/edit-own-profile/README.md) |
| `L2-024` | `L1-004` | [View a member profile](members/view-a-member-profile/README.md) |
| `L2-025` | `L1-004` | [Leave a congregation](members/leave-a-congregation/README.md) |
| `L2-026` | `L1-005` | [Post a listing](listings/post-a-listing/README.md) |
| `L2-027` | `L1-005` | [Post a listing](listings/post-a-listing/README.md) |
| `L2-028` | `L1-005` | [Post a listing](listings/post-a-listing/README.md) |
| `L2-029` | `L1-005` | [Post a listing](listings/post-a-listing/README.md) |
| `L2-030` | `L1-005` | [Post a listing](listings/post-a-listing/README.md) |
| `L2-031` | `L1-005` | [Post a listing](listings/post-a-listing/README.md) |
| `L2-032` | `L1-005` | [Post a listing](listings/post-a-listing/README.md), [Attach a listing photo](listings/attach-a-listing-photo/README.md) |
| `L2-033` | `L1-005` | [Post a listing](listings/post-a-listing/README.md) |
| `L2-034` | `L1-005` | [Post a listing](listings/post-a-listing/README.md) |
| `L2-035` | `L1-006` | [Manage my listings](listings/manage-my-listings/README.md) |
| `L2-036` | `L1-006` | [Manage my listings](listings/manage-my-listings/README.md) |
| `L2-037` | `L1-006` | [Close out and archive](listings/close-out-and-archive/README.md) |
| `L2-038` | `L1-006` | [Close out and archive](listings/close-out-and-archive/README.md) |
| `L2-039` | `L1-006` | [Close out and archive](listings/close-out-and-archive/README.md) |
| `L2-040` | `L1-006` | [Close out and archive](listings/close-out-and-archive/README.md) |
| `L2-041` | `L1-006` | [Manage my listings](listings/manage-my-listings/README.md) |
| `L2-042` | `L1-007` | [Browse the board](board/browse-the-board/README.md) |
| `L2-043` | `L1-007` | [Browse the board](board/browse-the-board/README.md) |
| `L2-044` | `L1-007` | [Browse the board](board/browse-the-board/README.md) |
| `L2-045` | `L1-007` | [Browse the board](board/browse-the-board/README.md) |
| `L2-046` | `L1-007` | [Browse the board](board/browse-the-board/README.md) |
| `L2-047` | `L1-007` | [Browse the board](board/browse-the-board/README.md) |
| `L2-048` | `L1-008` | [Search listings](board/search-listings/README.md) |
| `L2-049` | `L1-008` | [Search listings](board/search-listings/README.md) |
| `L2-050` | `L1-008` | [Search listings](board/search-listings/README.md) |
| `L2-051` | `L1-008` | [Search listings](board/search-listings/README.md) |
| `L2-052` | `L1-008` | [Search listings](board/search-listings/README.md) |
| `L2-053` | `L1-008` | [Search listings](board/search-listings/README.md) |
| `L2-054` | `L1-009` | [Make a request](requests/make-a-request/README.md) |
| `L2-055` | `L1-009` | [Make a request](requests/make-a-request/README.md) |
| `L2-056` | `L1-009` | [Make a request](requests/make-a-request/README.md) |
| `L2-057` | `L1-009` | [Make a request](requests/make-a-request/README.md) |
| `L2-058` | `L1-009` | [Make a request](requests/make-a-request/README.md) |
| `L2-059` | `L1-009` | [Review incoming requests](requests/review-incoming-requests/README.md) |
| `L2-060` | `L1-009` | [Accept a request](requests/accept-a-request/README.md) |
| `L2-061` | `L1-009` | [Decline a request](requests/decline-a-request/README.md) |
| `L2-062` | `L1-009` | [Make a request](requests/make-a-request/README.md) |
| `L2-063` | `L1-009` | [Make a request](requests/make-a-request/README.md) |
| `L2-064` | `L1-010` | [Find a thread](messaging/find-a-thread/README.md) |
| `L2-065` | `L1-010` | [Find a thread](messaging/find-a-thread/README.md) |
| `L2-066` | `L1-010` | [Read and reply](messaging/read-and-reply/README.md) |
| `L2-067` | `L1-010` | [Read and reply](messaging/read-and-reply/README.md) |
| `L2-068` | `L1-010` | [Read and reply](messaging/read-and-reply/README.md) |
| `L2-069` | `L1-010` | [Read and reply](messaging/read-and-reply/README.md) |
| `L2-070` | `L1-011` | [Receive notifications](notifications/receive-notifications/README.md) |
| `L2-071` | `L1-011` | [Receive notifications](notifications/receive-notifications/README.md) |
| `L2-072` | `L1-011` | [Receive notifications](notifications/receive-notifications/README.md) |
| `L2-073` | `L1-011` | [Review notifications](notifications/review-notifications/README.md) |
| `L2-074` | `L1-011` | [Review notifications](notifications/review-notifications/README.md) |
| `L2-075` | `L1-011` | [Control notifications](notifications/control-notifications/README.md) |
| `L2-076` | `L1-012` | [Browse the directory](members/browse-the-directory/README.md) |
| `L2-077` | `L1-012` | [Browse the directory](members/browse-the-directory/README.md) |
| `L2-078` | `L1-012` | [Browse the directory](members/browse-the-directory/README.md) |
| `L2-079` | `L1-012` | [Browse the directory](members/browse-the-directory/README.md) |
| `L2-080` | `L1-013` | [Report a listing](moderation/report-a-listing/README.md) |
| `L2-081` | `L1-013` | [Report a listing](moderation/report-a-listing/README.md) |
| `L2-082` | `L1-013` | [Review reported listings](moderation/review-reported-listings/README.md) |
| `L2-083` | `L1-013` | [Review reported listings](moderation/review-reported-listings/README.md) |
| `L2-084` | `L1-013` | [Review reported listings](moderation/review-reported-listings/README.md) |
| `L2-085` | `L1-013` | [Review pending members](moderation/review-pending-members/README.md) |
| `L2-086` | `L1-013` | [Review pending members](moderation/review-pending-members/README.md) |
| `L2-087` | `L1-013` | [Review pending members](moderation/review-pending-members/README.md) |
| `L2-088` | `L1-014` | [Scope queries to a congregation](platform/scope-queries-to-a-congregation/README.md) |
| `L2-089` | `L1-014` | [Scope queries to a congregation](platform/scope-queries-to-a-congregation/README.md) |
| `L2-090` | `L1-014` | [Issue an invite](access/issue-an-invite/README.md) |
| `L2-091` | `L1-014` | [Scope queries to a congregation](platform/scope-queries-to-a-congregation/README.md) |
| `L2-092` | `L1-014` | [Scope queries to a congregation](platform/scope-queries-to-a-congregation/README.md) |
| `L2-093` | `L1-015` | [Authorise a request](platform/authorise-a-request/README.md) |
| `L2-094` | `L1-015` | [Authorise a request](platform/authorise-a-request/README.md) |
| `L2-095` | `L1-015` | [Authorise a request](platform/authorise-a-request/README.md) |
| `L2-096` | `L1-015` | [Validate and bound input](platform/validate-and-bound-input/README.md) |
| `L2-097` | `L1-015` | [Display member text safely](platform/display-member-text-safely/README.md) |
| `L2-098` | `L1-015` | [Display member text safely](platform/display-member-text-safely/README.md) |
| `L2-099` | `L1-015` | [Limit abuse](platform/limit-abuse/README.md) |
| `L2-100` | `L1-015` | [Secure transport and tokens](platform/secure-transport-and-tokens/README.md) |
| `L2-101` | `L1-015` | [Export and erase personal data](members/export-and-erase-personal-data/README.md) |
| `L2-102` | `L1-015` | [Attach a listing photo](listings/attach-a-listing-photo/README.md) |
| `L2-103` | `L1-016` | [Serve collections under load](platform/serve-collections-under-load/README.md) |
| `L2-104` | `L1-016` | [Serve collections under load](platform/serve-collections-under-load/README.md) |
| `L2-105` | `L1-016` | [Serve collections under load](platform/serve-collections-under-load/README.md) |
| `L2-106` | `L1-016` | [Attach a listing photo](listings/attach-a-listing-photo/README.md) |
| `L2-107` | `L1-016` | [Serve collections under load](platform/serve-collections-under-load/README.md) |
| `L2-108` | `L1-017` | [Use the responsive and accessible shell](platform/responsive-and-accessible-shell/README.md) |
| `L2-109` | `L1-017` | [Use the responsive and accessible shell](platform/responsive-and-accessible-shell/README.md) |
| `L2-110` | `L1-017` | [Use the responsive and accessible shell](platform/responsive-and-accessible-shell/README.md) |
| `L2-111` | `L1-017` | [Use the responsive and accessible shell](platform/responsive-and-accessible-shell/README.md) |
| `L2-112` | `L1-017` | [Use the responsive and accessible shell](platform/responsive-and-accessible-shell/README.md) |
| `L2-113` | `L1-017` | [Use the responsive and accessible shell](platform/responsive-and-accessible-shell/README.md) |
| `L2-114` | `L1-017` | [Use the responsive and accessible shell](platform/responsive-and-accessible-shell/README.md) |
| `L2-115` | `L1-017` | [Use the responsive and accessible shell](platform/responsive-and-accessible-shell/README.md) |
| `L2-116` | `L1-018` | [Observe service health](platform/observe-service-health/README.md) |
| `L2-117` | `L1-018` | [Observe service health](platform/observe-service-health/README.md) |
| `L2-118` | `L1-018` | [Observe service health](platform/observe-service-health/README.md) |
| `L2-119` | `L1-018` | [Observe service health](platform/observe-service-health/README.md) |
| `L2-120` | `L1-009` | [Track my requests](requests/track-my-requests/README.md) |
