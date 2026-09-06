# Feature slice 2 — post and request every kind

## Purpose

Barnabas rests on a claim: Lend, Give, Sell and Help are not interchangeable. Each has its own
fields, its own request, and its own close-out verb. Until this slice only Lend could be posted,
so the claim was asserted in the domain language and nowhere else — three of the four factories
existed with nothing able to call them.

This slice makes the claim true end to end. All four kinds can be posted, each on a form asking
only for what its kind needs; all four can be asked for, each collecting the terms that kind
requires; and each closes out in its own words.

## L1 requirements touched

| L1 | Title | Before | After | Status |
|----|-------|--------|-------|--------|
| `L1-005` | Creating listings | 3 of 9 | 8 of 9 | Partial — `L2-032`, photos, is slice 9 |
| `L1-006` | Listing lifecycle | 3 of 7 (one partial) | 4 of 7 | Partial |
| `L1-007` | Browsing the board | 2 of 6 | 3 of 6 | Partial |
| `L1-009` | Requests | 7 of 11 | **11 of 11** | **Complete** |

`L1-009` is the first L1 requirement in the product to be finished.

## L2 requirements implemented

| L2 | Requirement | Criteria |
|----|-------------|----------|
| `L2-026` | Choose a listing kind before entering details | 4 |
| `L2-028` | Create a Give listing | 3 |
| `L2-029` | Create a Sell listing | 4 |
| `L2-030` | Create a Help listing | 4 |
| `L2-034` | Do not collect payment when posting | 2 |
| `L2-037` | Close out a listing in the vocabulary of its kind | 4 remaining |
| `L2-043` | Filter the board by kind | 3 |
| `L2-055` | Compose a request for a Give listing | 2 |
| `L2-056` | Compose a request to buy a Sell listing | 3 |
| `L2-057` | Compose a request for a Help listing | 3 |
| `L2-063` | Do not handle payment, delivery, or deposits in a request | 2 |

**34 acceptance criteria, all covered.** `L2-037` is complete rather than partial for the first
time; `L2-026 AC2` is covered by the existing Lend-form test, whose trace comment now says so.

## Decisions taken

- **Each kind gets its own route, command, handler and validator.** The endpoint is what ties a
  request to the kind of listing it is against, and that is the check stopping a Sell listing
  acquiring a request carrying loan terms. It is also what gives `L2-026` its meaning: the kind
  is the address, so it cannot be silently defaulted.

- **The kinds needed less than the design outline suggested.** Sell gains a `Condition`; Help
  gains availability windows. **Give gains nothing at all** — "free, with pickup arranged by the
  members" follows from the kind, and a column repeating it could only ever disagree with it.
  `SaleTerms` was not added either: `Listing.Price` already existed, and wrapping one decimal in
  a value type buys nothing.

- **`AvailabilityWindow` is an entity, and a table.** A Help request names the window it wants,
  so the windows need identity; two windows holding the same day and hours are still two
  different offers, and a value type would make them indistinguishable. The times are local to
  the congregation and carry no zone — members of one parish arrange a lift in the hours they
  both recognise, and an offset would imply a precision the offer does not have. Open decision
  `D-04` is resolved this way and recorded here.

- **Window identifiers are minted by the server.** A submitted window carries none, so a request
  can only ever name a window the listing actually declared.

- **Refusing a field is not the same as ignoring one.** A command with no `price` property has
  nothing to bind to, so an unrecognised price would be discarded and the listing created — and
  `L2-028 AC2`, which requires the listing to be *rejected*, would be unsatisfiable. The refused
  names now live in one place, `PaymentDeliveryAndDepositFields`, covering payment instruments,
  delivery addresses and deposits across every listing and request endpoint — including the Lend
  ones that predate this slice and were not refusing them.

- **The board speaks in its own words.** A placard reads "Giving away", not "Give". The bare enum
  name does not satisfy `L2-028 AC3`'s "identified as being given away rather than sold" — it
  leaves the reader to infer it from the absence of a price. The same vocabulary
  (`listing-words.ts` in `domain`) labels the filter chips, the call to action, and the close-out
  button, so the four cannot drift apart.

- **A filtered board with nothing in it says so in its own terms.** Telling a member who filtered
  to Help that "the board is empty" would be false; the congregation has posted plenty.

## What this cost that was not planned

- **`NgModel` needs its `name` bound, not its attribute.** The Help form's repeating window rows
  used `[attr.name]`, which sets the DOM attribute but not the input `NgModel` reads. Inside a
  `<form>` that breaks the control silently: the second window row rendered with blank `<option>`
  text and an unlabelled input. Bound as `[name]` it works.

- **A trailing `.When()` in FluentValidation applies to the whole chain.** The Sell price rules
  were one chain ending in `.When(price is not null)`, which switched off its own `NotNull()`
  exactly when the price was missing — the case it existed to catch. A missing price answered 500
  rather than 400. Split into two rules.

## Known duplication, deliberately left

The four post forms share four fields — title, category, description, neighbourhood — as
duplicated template markup. Extracting a shared region was considered and deferred: slice 3 adds
listing editing, which needs the same fields a fifth time, and that is the point at which the
shared shape is proven rather than guessed.

## Verification

```
backend   dotnet build   0 warnings
backend   dotnet test    129 passed, 0 failed, 0 skipped   (was 101)
frontend  playwright     60 passed, 0 failed              (was 47)
```

Walkable end to end against the running system:

```
sign in → post a Give, a Sell and a Help listing → see each on the board in its own words
       → filter the board to one kind, and clear it
       → a second member asks for each: "Request this", "Request to buy", "Request this help"
       → the owner closes each out: given away, sold, completed
```
