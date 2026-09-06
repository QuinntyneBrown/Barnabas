# End a session

## Overview

A session begun by following a sign-in link does not last indefinitely. It expires, it can
be renewed without another trip to the member's inbox, and it can be ended deliberately.

**refresh token** — a long-lived secret held by the client that exchanges for a fresh access
token, and which is replaced each time it is used

**session** — one member's sign-in on one device, holding the access and refresh tokens
issued together and revocable independently of the member's other sign-ins

Two lifetimes are in play. The access token is short, so a leaked one is useful only
briefly. The refresh token is long, so a member is not asked for their email address every
hour, and rotation on use makes the pair safe: a refresh token is replaced every time it is
exchanged, so presenting an old one is evidence of a copy and is refused.

Both bind to a session, and signing out revokes the session rather than a token. That
matters twice over. `L2-019` requires a signed-out member's tokens to stop being accepted,
with no window — a signature alone cannot express revocation, so the session is read on
every authenticated request, as `platform/authorise-a-request` describes. And a member
signed in on a phone and a laptop holds two sessions, so signing out on one leaves the other
working rather than leaving the outcome to whichever token happened to be found first.

Signing out is deliberate and confirmed, because a member on a shared machine choosing it
means it.

## Description

Two commands over two entities.

- **`Session`** — domain entity holding the member, the congregation, when it opened, and
  when it was revoked. `IsLive` answers whether tokens carrying its identifier are still
  accepted. It is what both token kinds bind to.
- **`RefreshToken`** — holds its hash, expiry, revocation, the session it belongs to, and
  the identity of the token that replaced it. `IsActive`, `Rotate`, and `Revoke` hold the
  rules.
- **`RefreshSessionCommand`** and its handler — find the token, confirm it and its session
  are live, rotate it, and issue a fresh access token for the same session. Rotation is a
  conditional update on the row still being unrotated, so two callers racing produce one
  winner and one 401 rather than two valid tokens.
- **`SignOutCommand`** and its handler — revoke the *session* the caller's token names, and
  with it the refresh token bound to that session. The command carries no payload; the
  session is taken from the `sid` claim.
- **`ConfirmSignOutDialog`** — Angular dialog requiring confirmation. It is a native
  `dialog` element, so it stays closed and harmless when scripting is unavailable.
- **`JwtIssuer`** — shared with `sign-in-with-a-link`, which is where its claims are
  described. Every access token it issues carries the session identifier.

Only hashes are stored, as with sign-in tokens. The access token is not stored at all — but
it is not trusted on signature alone either: it names a session, and that session is read on
every authenticated request. That read is what makes `L2-019` true rather than
approximately true, and it is the reason the access token can be revoked at all.

Revoking the session rather than the token is also what makes concurrent sign-ins behave.
Each device holds its own session, so signing out on one revokes exactly one.

## Requirements

The feature realizes the following level-2 (L2) requirements. Each L2
requirement refines a level-1 (L1) requirement, cited by identifier. The
**Slice** column marks the requirements implemented by feature slice 1; the
remainder are designed here and implemented in a later slice.

| L2 ID | Refines (L1) | Slice | Requirement |
|-------|--------------|-------|-------------|
| `L2-018` | `L1-003` | 1 | A session shall expire after a bounded lifetime and shall be renewable without a further email round trip while it remains valid. |
| `L2-019` | `L1-003` | 1 | A member shall be able to end their session, after which their tokens shall no longer be accepted. |

## Diagrams

### Components

Both commands act on the session and the token bound to it. The controller holds no logic;
rotation and revocation are the entities' own.

![C4 component view for ending a session](diagrams/c4-component.png)

### Class structure

A session owns the refresh token, so revoking one revokes both. The token records what
replaced it, which makes a superseded token distinguishable from one that never existed.

![Class diagram for ending a session](diagrams/class-structure.png)

### Behaviour — signing out, and both tokens afterwards

Revocation is what makes sign-out real. The diagram carries three consequences `L2-019` and
`L2-018` require: the access token is refused despite being unexpired and correctly signed,
the refresh token is refused, and a second session on another device keeps working.

![Sequence diagram for signing out](diagrams/sequence-sign-out.png)
