# Find a thread

## Overview

A message thread in Barnabas is always about something. It comes into existence when a
listing's owner accepts a request, and it carries that listing for as long as it lasts.

**thread** — a conversation between two members about one listing, opened by an accepted
request

There is no way to start a conversation any other way. A member cannot message another
member out of the blue, and that is a deliberate product stance rather than a missing
feature: it keeps the board the centre of gravity, stops the parish turning into a chat
application, and means every thread has a subject a reader can see six weeks later.

It also makes the thread list legible. Each row can name what the conversation is about,
because there is no such thing as a conversation about nothing.

A member sees the threads they are party to and no others.

## Description

A thread is created by another feature and read by this one.

- **`MessageThread`** — domain entity carrying the congregation, **the request that opened
  it**, the listing, the owner, and the requester, with the time it opened. `IsParty`
  answers whether a given member may see it.

  `RequestId` carries a uniqueness constraint, and that constraint is what makes "one thread
  per accepted request" a fact rather than an intention. Without it, two callers accepting
  the same request at once would each create a thread and both would be valid; a declined
  request that somehow acquired a thread would be indistinguishable from an accepted one at
  the data layer.
- **`AcceptRequestCommandHandler`** — belongs to `requests/accept-a-request`, and is where
  a thread is created. It commits the acceptance and the thread in one unit of work.
- **`ThreadsController`** — exposes `GET /threads`.
- **`GetMyThreadsQuery`** and its handler — read the threads where the caller is owner or
  requester, with each thread's latest message, and project them.
- **`ThreadSummaryDto`** — read model carrying the other member's display name, the listing
  title, the latest message, and whether the thread is unread for this reader.
- **`MessagesComponent`** — the Angular screen, one of three chips in the inbox alongside
  requests received and requests made.

Unread is a property of the reader rather than the thread. Two members looking at the same
thread can disagree about whether it is unread, so it is computed per caller from when each
last opened it.

The query filters on the caller being a party. The congregation filter applies underneath,
from `platform/scope-queries-to-a-congregation`, so a thread in another congregation is
already invisible before this predicate is considered.

## Requirements

The feature realizes the following level-2 (L2) requirements. Each L2
requirement refines a level-1 (L1) requirement, cited by identifier. The
**Slice** column marks the requirements implemented by feature slice 1; the
remainder are designed here and implemented in a later slice.

| L2 ID | Refines (L1) | Slice | Requirement |
|-------|--------------|-------|-------------|
| `L2-064` | `L1-010` | &mdash; | A thread shall exist only as the consequence of an accepted request, and shall always carry the listing it concerns. |
| `L2-065` | `L1-010` | 1 | A member shall be able to see all their threads, each showing the other member, the listing, the latest message, and whether it is unread. |

## Diagrams

### Components

Creation and reading sit in different features. `AcceptRequestCommandHandler` is shown here
because it is the only thing that brings a thread into existence.

![C4 component view for finding a thread](diagrams/c4-component.png)

### Class structure

A thread references the listing it concerns and both members. `IsParty` is what the read
path uses to decide visibility.

![Class diagram for finding a thread](diagrams/class-structure.png)

### Behaviour — a thread coming into existence

The thread and the acceptance commit together. `L2-064` makes the thread a consequence of
accepting rather than a second act the owner has to remember.

![Sequence diagram for a thread opening](diagrams/sequence-thread-opened.png)

### Behaviour — listing the threads a member is party to

The handler filters on party membership; the congregation filter has already applied
underneath. Each row can name its listing because every thread has one.

![Sequence diagram for listing threads](diagrams/sequence-list-threads.png)
