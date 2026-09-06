# Feature slice 1 — borrow a ladder

## Purpose

The first slice takes one listing kind, **Lend**, all the way through the product's
purpose: a member posts a ladder, another member asks to borrow it, the owner accepts,
the two arrange the handoff in a message thread, and the owner marks it taken.

The slice is chosen for what it forces into existence rather than for how many screens it
delivers. It is the narrowest path that involves **two different members**, so ownership
and tenant scoping are exercised from the first commit rather than retrofitted across
every handler later. It carries a real state machine — `Pending → Accepted | Declined`
and `Active → Archived` — so the domain layer holds behaviour instead of passing data
through.

On completion the slice shall satisfy **33 of the 120 L2 requirements**, spread across
**9 of the 18 L1 requirements**.

## What completion establishes

Four .NET projects with the MediatR pipeline and dependency injection · EF Core with
migrations and a congregation-scoped query filter · JWT issue and validation · the
`ProblemDetails` error contract · the Angular workspace with `api`, `components`, and
`domain` libraries, signals, and interface-based service consumption · both acceptance
suites with the trace-comment convention, `Barnabas.IntegrationTests` and Playwright page
objects.

Every later feature reuses these. Give, Sell, and Help each become one command, one
handler, one validator, and one form.

## L1 requirements touched

No L1 is completed by this slice. Each is advanced by the L2 requirements listed in the
next section, and remains open until its other L2 requirements are implemented.

| L1 | Title | L2 in slice | L2 total | Status on completion |
|----|-------|------------|----------|----------------------|
| `L1-003` | Authentication and session | 6 | 8 | Partial |
| `L1-005` | Creating listings | 3 | 9 | Partial — Lend kind only |
| `L1-006` | Listing lifecycle | 3 | 7 | Partial |
| `L1-007` | Browsing the board | 2 | 6 | Partial |
| `L1-009` | Requests | 7 | 11 | Partial — Lend requests only |
| `L1-010` | Coordination messaging | 3 | 6 | Partial |
| `L1-014` | Tenant isolation | 2 | 5 | Partial |
| `L1-015` | Security and data protection | 3 | 10 | Partial |
| `L1-017` | Responsive layout and accessibility | 4 | 8 | Partial — for the screens built |

The nine L1 requirements not touched at all: `L1-001`, `L1-002`, `L1-004`, `L1-008`,
`L1-011`, `L1-012`, `L1-013`, `L1-016`, `L1-018`.

## L2 requirements implemented

### L1-003 — Authentication and session

| L2 | Requirement |
|----|-------------|
| `L2-012` | Request a sign-in link |
| `L2-014` | Exchange a sign-in link for a session |
| `L2-015` | Expire a sign-in link |
| `L2-016` | Make a sign-in link single use |
| `L2-018` | Expire and renew a session |
| `L2-019` | Sign out |

### L1-005 — Creating listings

| L2 | Requirement |
|----|-------------|
| `L2-027` | Create a Lend listing |
| `L2-031` | Validate listing input |
| `L2-033` | Confirm that a listing is published |

### L1-006 — Listing lifecycle

| L2 | Requirement |
|----|-------------|
| `L2-035` | List own active listings |
| `L2-037` | Close out a listing in the vocabulary of its kind |
| `L2-041` | Restrict listing modification to its owner |

### L1-007 — Browsing the board

| L2 | Requirement |
|----|-------------|
| `L2-042` | Display the board |
| `L2-044` | Convey a listing's kind without relying on colour |

### L1-009 — Requests

| L2 | Requirement |
|----|-------------|
| `L2-054` | Compose a request to borrow a Lend listing |
| `L2-058` | Send a request and confirm it |
| `L2-059` | Review incoming requests |
| `L2-060` | Accept a request |
| `L2-061` | Decline a request |
| `L2-062` | Reject ineligible requests |
| `L2-120` | Review my own requests |

### L1-010 — Coordination messaging

| L2 | Requirement |
|----|-------------|
| `L2-065` | List a member's message threads |
| `L2-066` | Read a thread |
| `L2-067` | Send a message in a thread |

### L1-014 — Tenant isolation

| L2 | Requirement |
|----|-------------|
| `L2-088` | Scope every read to the actor's congregation |
| `L2-089` | Return not found for cross-congregation access |

### L1-015 — Security and data protection

| L2 | Requirement |
|----|-------------|
| `L2-093` | Authenticate every non-public endpoint |
| `L2-094` | Authorise by ownership |
| `L2-096` | Validate and bound all input |

### L1-017 — Responsive layout and accessibility

| L2 | Requirement |
|----|-------------|
| `L2-108` | Lay out correctly across every viewport band |
| `L2-109` | Keep primary navigation reachable at every band |
| `L2-111` | Operate every screen by keyboard |
| `L2-112` | Show focus visibly on every surface |

## Detailed designs to implement

Every requirement above has a detailed design under `docs/detailed-designs/`. All **seventeen**
designs are touched by this slice: six are implemented in full, and eleven are implemented in
part, with the remainder of each falling to a later slice.

A design is listed once. Where only some of its requirements are in the slice, the deferred
ones are named so that a reader building from the design knows which sections of it to leave
alone for now.

### platform — implement first

These four fix contracts every other design depends on. A feature built before them invents
its own authorisation and scoping story, which then has to be reconciled.

| Design | In slice | Deferred to a later slice |
|--------|----------|---------------------------|
| `platform/scope-queries-to-a-congregation` | `L2-088`, `L2-089` | `L2-091`, `L2-092` |
| `platform/authorise-a-request` | `L2-093`, `L2-094` | `L2-095` |
| `platform/validate-and-bound-input` | `L2-096` | *(complete)* |
| `platform/responsive-and-accessible-shell` | `L2-108`, `L2-109`, `L2-111`, `L2-112` | `L2-110`, `L2-113`, `L2-114`, `L2-115` |

### access — implement second

Two real identities are needed before ownership means anything.

| Design | In slice | Deferred to a later slice |
|--------|----------|---------------------------|
| `access/sign-in-with-a-link` | `L2-012`, `L2-014`, `L2-015`, `L2-016` | `L2-013`, `L2-017` |
| `access/end-a-session` | `L2-018`, `L2-019` | *(complete)* |

### listings and board — the supply side

| Design | In slice | Deferred to a later slice |
|--------|----------|---------------------------|
| `listings/post-a-listing` | `L2-027`, `L2-031`, `L2-033` | `L2-026`, `L2-028`, `L2-029`, `L2-030`, `L2-032`, `L2-034` |
| `listings/manage-my-listings` | `L2-035`, `L2-041` | `L2-036` |
| `listings/close-out-and-archive` | `L2-037` | `L2-038`, `L2-039`, `L2-040` |
| `board/browse-the-board` | `L2-042`, `L2-044` | `L2-043`, `L2-045`, `L2-046`, `L2-047` |

Only the Lend branch of `post-a-listing` is built. The design describes all four kinds; the
other three are new commands, handlers, validators, and forms against an architecture this
slice has already settled.

### requests — the demand side

| Design | In slice | Deferred to a later slice |
|--------|----------|---------------------------|
| `requests/make-a-request` | `L2-054`, `L2-058`, `L2-062` | `L2-055`, `L2-056`, `L2-057`, `L2-063` |
| `requests/review-incoming-requests` | `L2-059` | *(complete)* |
| `requests/accept-a-request` | `L2-060` | *(complete)* |
| `requests/decline-a-request` | `L2-061` | *(complete)* |
| `requests/track-my-requests` | `L2-120` | *(complete)* |

### messaging — where the handoff is arranged

| Design | In slice | Deferred to a later slice |
|--------|----------|---------------------------|
| `messaging/find-a-thread` | `L2-065` | `L2-064` |
| `messaging/read-and-reply` | `L2-066`, `L2-067` | `L2-068`, `L2-069` |

### One requirement to watch

`L2-064` — a thread is created when a request is accepted — is marked deferred above, but
`L2-060`, which is in the slice, already requires that accepting a request open a thread. The
behaviour is therefore delivered by this slice; the two requirements state it from the request
side and the messaging side. Nothing is missing, and `messaging/find-a-thread` will be closer
to complete than its row suggests. The pair is worth revisiting when the messaging slice comes
up, because two requirements describing one behaviour will eventually disagree.

## Out of scope

Deferred to later slices, each of which reuses the architecture this slice establishes:

- **The other three listing kinds** — Give, Sell, and Help (`L2-028`, `L2-029`, `L2-030`,
  `L2-055`, `L2-056`, `L2-057`, and the per-kind close-out variants).
- **Congregation provisioning and the invite flow** — `L1-001`, `L1-002` entirely.
- **Member profiles and the directory** — `L1-004`, `L1-012`.
- **Search** — `L1-008`. **Notifications** — `L1-011`. **Moderation** — `L1-013`.
- **Performance and observability targets** — `L1-016`, `L1-018`.
- **Listing edit, archive, restore, and permanent delete** — `L2-036`, `L2-038`, `L2-039`,
  `L2-040`.

## Amended after design review

`docs/detailed-designs-critical-gaps.md` found that three acceptance criteria already in this
slice — in `L2-058`, `L2-061`, and `L2-071` — refer to a screen showing a member their own
requests, and that no requirement mandated it. `L2-120` was added to close that gap, and the
slice grew from 32 requirements to 33 and from 16 designs to 17. The same review broadened
`L2-062` from self-requests and duplicates to the four conditions a request has to satisfy.

## Decisions taken

- **The congregation, its invite codes, and two approved members shall be seeded** rather
  than provisioned through the product. Two real identities are needed to make
  authorisation meaningful; the administrative surface that creates them is not. This
  keeps `L1-001` and `L1-002` out of the slice without weakening what it proves.
- **Sign-in shall be built for real**, passwordless by single-use emailed link. Faking
  identity would make every authorisation and tenant-scoping test meaningless, and those
  are the slice's main reason for existing.
- **Only the Lend kind shall be implemented.** The four kinds differ in their fields, not
  their architecture, so building all four first would teach one pattern four times while
  exercising no authorisation and no state transition.

## Definition of done

The slice is complete when every L2 requirement listed above has at least one acceptance
test that fails before its implementation and passes after, each test carrying a trace
comment naming the requirements it covers, and when a member can complete the following
path against the running system without manual intervention:

```
sign in → post a Lend listing → see it on the board
       → a second member requests to borrow it
       → the owner accepts → a thread opens → both members exchange a message
       → the owner marks it taken → it leaves the board
```
