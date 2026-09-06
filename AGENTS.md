# Barnabas

## Purpose

A private lending, giving, selling, and helping board scoped to a single church congregation. Members post something to lend, give away, or sell, or offer their time — a ride, tutoring, help moving. Barnabas brokers the introduction; the handoff happens in person. It does not handle payments, shipping, or delivery.

## Speed Is Not the Goal

Quality and completeness beat finishing fast. Finish the whole task — edge cases,
error paths, no stubs or `TODO`s. If it is bigger than it looked, complete it and
say what it cost rather than quietly narrowing scope.

## Technology

- Use .NET for the API.
- Use MediatR, pinned to **12.5.0**. Do not upgrade. 12.5.0 is the last release
  under plain Apache-2.0; from 13.0.0 MediatR is commercially licensed, free only
  under a registered Community tier that lapses above $5M USD annual revenue.
- Use Microsoft.Extensions libraries and patterns, including:
  - Dependency injection (DI)
  - Options
  - Configuration
- Persist to SQL Server, and to one provider only. Development and both acceptance suites use
  the same engine as production, so a filtered index, a `rowversion` and a conditional update
  behave in a test exactly as they will in a deployment. A second provider bought portability
  once and cost four acceptance criteria that could not run under it. See
  `docs/adr/backend/0001-use-sql-server-for-persistence.md`.
- Use Angular for the web client.
- Authenticate with JWT.

## Architecture and Design

- Implement requirements radically simply: the least code that satisfies the
  acceptance criteria, and nothing more. Simple in design, never reduced in scope.
- Apply SOLID principles throughout the codebase.
- Organize features and behaviors into vertical slices.
- Keep back end code in the `\backend` folder and front end code in the `\frontend` folder.
- Within each, keep source in `src` and tests in `tests`.

## Backend

- Use Clean Architecture. Dependencies point inward. `Domain` references nothing.
- Keep controllers thin: bind, dispatch through MediatR, return. No logic in a controller.
- Commands, queries, handlers, and validators live in `Application`.
- One file per type. Every class, interface, record, and enum gets its own file,
  named for the type it holds.

## Frontend

- Prefer signals over RxJS. Reach for RxJS only for genuine streams and events.
- No single-file components. Template, styles, and class each live in their own file.
- Consume services through an interface, never a concrete class. A contract is a
  TypeScript `interface` and an `InjectionToken` declared together in a
  `*.contract.ts` file; a consumer calls `inject(THE_TOKEN)` and has no
  compile-time knowledge of any implementation.
- Name a service for the one thing it serves, singular, with a `Service` suffix.
  Never an `Api` suffix: the interface says what the caller may ask for, not that
  an HTTP call happens to be how the answer arrives.
- Prefix an interface with `I` only where it is a behavioural contract with
  swappable implementations — `IListingService`, against `ListingService` and
  `ListingServiceMock`. The implementation drops the prefix and never takes an
  `Impl` suffix. A data structure that nothing polymorphs over carries no prefix.
- Bind a token to an implementation at the host and nowhere else: the application
  binds the real one, a test host binds the mock. Nothing beneath the host names
  both, which is what makes the seam a real one.
- Organize the workspace into `api`, `components`, and `domain` libraries, consumed
  by the `barnabas` application.
- State lives in signals, behaviour lives in services. A component class wires the
  two to a template and does nothing else.

### Component placement — settle this before creating the folder

Where a component lives is not a matter of taste. Ask these in order and stop at
the first `yes`.

1. **Is it reached by a route, is it page chrome, or does it compose a whole
   screen?** → `barnabas`
2. **Does it inject a token from `@barnabas/api`, or name a type from it?** → `domain`
3. **Neither.** → `components`

Dependencies run one way and never back:

```text
components  ->  (nothing)
domain      ->  components, api
barnabas    ->  domain, components, api
```

An import pointing the other way is a defect, not a shortcut.

Page chrome is the reason the first question asks about it as well as about
routes. A navigation bar, a chip row, a back link — none is a screen, and none
touches the API, so the last two questions would send it to `components`, which
may not name the router at all.

Chrome means a component whose purpose is to move you around. Content that
happens to be clickable is not chrome: a placard is a listing that opens, and a
listing is content. Ask what the component is for, not whether it contains a
link.

#### `components` — dumb, congregation-agnostic, publishable

The plain vocabulary of an interface: dialogs, icons, the skip link, form fields,
empty states.

- It imports Angular and nothing else of ours. No `@barnabas/api`, no
  `@barnabas/domain`, no router, no `HttpClient`, no notion of a listing or a
  congregation.
- Treat it as a package that ships to npm and drops into an unrelated product.
  That constraint is the whole point of the library; honour it while the package
  is still private.
- Data in through `input()`, events out through `output()`. It never fetches,
  never persists, never navigates, and injects no service of ours.
- A component belongs here only if it can be rendered from a literal object. If it
  needs Barnabas to make sense, it is in the wrong library.

#### `domain` — congregation-aware regions, and the state behind them

The stores, the guards, and the components that speak the product's own
vocabulary. Not publishable.

- A component here may `inject()` a token from `@barnabas/api`, hold signal state,
  and map what comes back into what a template needs.
- It composes `components` for presentation and passes plain values down. It does
  not restyle its children.
- It is a self-contained region of a screen — a placard, a request row — never a
  screen and never chrome. It defines no routes and navigates nothing
  programmatically, though it may render a link to a destination it was handed.
- Stores and guards stay flat under `domain/src/lib/<area>/`; each component gets
  its own folder, class and template and styles in separate files as everywhere.

#### `barnabas` — pages, shell, routing, composition

Page components, the two shells, routes, providers, and the state a route owns.

- A page arranges `domain` regions and `components` primitives. A widget built
  inline in a page is one that was placed wrong.
- Binding every service token to its implementation happens here, and only here.

## Domain Language

Use these terms exactly, in code and in copy:

- **listing** — a post. "Post" is the verb, never the noun.
- **board** — the main feed. Never "feed".
- **member** — a user. The group is a **congregation** or **parish**.
- **request** — an ask against a listing. It opens a **message thread**.
- **neighbourhood** — Commonwealth spelling, throughout.
- The four listing types are **Lend**, **Give**, **Sell**, and **Help**. Lend, Give,
  and Sell are goods; Help is time. They are not interchangeable — each has its own
  request fields and its own close-out verb.

## Testing Approach

Use acceptance test-driven development (ATDD):

- Begin with a failing acceptance test.
- Link each test to explicit acceptance criteria written using the **Given–When–Then** format.
- Implement the behavior required to make the test pass.
- Keep acceptance criteria, tests, and implementation aligned.

Back end: integration tests against the API.

Front end: Playwright, using the Page Object Model.

- One page object per screen. It owns the selectors and the interactions.
- Tests state intent; page objects know the DOM. Never put a selector in a test.

## Folder Structure

```text
Barnabas/
├── backend/
│   ├── src/
│   │   ├── Barnabas.Api/             controllers
│   │   ├── Barnabas.Application/     commands, queries, handlers, validators
│   │   ├── Barnabas.Domain/          entities and rules, no dependencies
│   │   └── Barnabas.Infrastructure/  persistence, email, external services
│   └── tests/
│       └── Barnabas.IntegrationTests/
├── frontend/                         Angular multi-project workspace
│   ├── projects/
│   │   ├── api/                      contracts, DTOs, typed clients, interceptors
│   │   ├── components/               dumb, publishable primitives
│   │   ├── domain/                   stores, guards, screen regions
│   │   └── barnabas/                 pages, shell, routes, providers
│   └── tests/
│       └── e2e/
│           ├── page-objects/
│           ├── specs/
│           └── support/
└── docs/
    ├── mocks/               static HTML mocks of every screen
    ├── detailed-designs/    one folder per feature, with C4 and sequence diagrams
    └── specs/
        ├── L1.md high level requirements
        └── L2.md detailed requirments linked to a L1.md also acceptance criteria that shall be linked to acceptance tests
```
