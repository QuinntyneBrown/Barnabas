# Feature slice 9 — photographs, and hardening

## Purpose

Two things at once, and they belong together. A board of lent and sold goods with no pictures is
a board people scroll past, so this slice puts one photograph on a listing — and an upload
endpoint is the largest new attack surface the product has, so the same slice is where transport,
headers, injection, and a member's right to be forgotten are settled.

## L1 requirements touched

| L1 | Title | Before | After | Status |
|----|-------|--------|-------|--------|
| `L1-005` | Posting a listing | 11 of 12 | **12 of 12** | **Complete** |
| `L1-014` | Tenant isolation | 2 of 4 | **4 of 4** | **Complete** |
| `L1-015` | Security and privacy | 3 of 7 | **7 of 7** | **Complete** |
| `L1-016` | Performance | 1 of 4 | 2 of 4 | Slice 10 |

**Fifteen of eighteen L1 requirements complete.**

## L2 requirements implemented

`L2-032`, `L2-091`, `L2-092`, `L2-097`, `L2-098`, `L2-100`, `L2-101`, `L2-102`, `L2-106` —
**23 acceptance criteria**.

## Decisions taken

### Photographs

- **SkiaSharp, not ImageSharp.** MIT bindings over BSD-3-Clause Skia, with no revenue tier.
  `SixLabors.ImageSharp` is Apache-2.0 only to 2.1.x and moves to the Six Labors Split Licence at
  3.0 — the same trap the MediatR pin exists to avoid.
- **The uploaded bytes are never stored.** Every rendition is decoded and re-encoded from pixels,
  which is what `L2-102 AC2` asks for and is also what removes the embedded metadata: an encode
  writes a new file from a bitmap, and a bitmap holds no EXIF block, no GPS tag and no colour
  profile to carry across. The metadata is not stripped so much as never picked up.
- **Three refusals, in an order that matters.** The bytes are sniffed against their declared type
  first, so a PHP script named `.jpg` never reaches a decoder. The dimensions are read from the
  header second, because a 400 KB file may declare a 30000 × 30000 canvas that decodes to 3.6 GB
  and the byte-length gate would never have seen it coming. Only then is it decoded. All three
  answer 415 with the same message, because saying which would tell somebody probing the endpoint
  how close they got.
- **The body limit became a property of the endpoint.** `L2-096 AC2` requires an oversized JSON
  body refused at 1 MB; `L2-032 AC1` requires a 2 MB JPEG accepted. One number cannot do both.
  `MaxRequestBodyAttribute` raises it on the upload route alone, read as endpoint metadata rather
  than matched against a path — which is why `UseRouting` is now called explicitly, ahead of the
  limit middleware that reads it. A test asserts the ordinary 1 MB cap is unchanged.
- **Photos are served anonymously, by capability.** This is the one deliberate exception in the
  product and it is worth stating plainly. A browser fetching an `<img>` cannot attach a bearer
  token; fetching every picture through `HttpClient` into a blob would defeat the lazy loading
  `L2-106 AC2` requires, and authenticating off the refresh cookie would widen what that cookie is
  for. What stands in for the session is the identifier: a version-4 GUID nobody can enumerate,
  issued to the uploader and disclosed only inside a congregation's own responses. The response
  says `Cache-Control: private`, carries `X-Content-Type-Options: nosniff` and its own
  `Content-Type`, and is served by an endpoint rather than from a folder — nothing under the photo
  root is ever mapped as static content, which is `L2-102 AC3`.
- **Two sizes, not a resize service.** The board shows a mosaic of small fields and the listing
  screen shows one large one, and those are the only two places a photo appears. A service taking
  arbitrary dimensions would be infrastructure nothing asked for.
- **Bytes go to a folder behind `IPhotoStore`, not into a row.** Image bytes in a table make every
  query that touches it slower and every backup larger, and nothing about a photo needs a
  transaction with the listing: a listing that saved without its picture is a listing without a
  picture, which is a state the product already has a name for.
- **A replaced photo's bytes are deleted after the save, never before.** Deleting first would mean
  a save that failed had already destroyed the picture that was there.

### Hardening

- **The JSON encoder is declared rather than inherited.** `L2-097 AC1` requires member-supplied
  text returned encoded, and what the framework's default encoder escapes has changed between
  releases — the assertion caught it doing nothing. `JavaScriptEncoder.Create(UnicodeRanges.All)`
  escapes the HTML-sensitive characters whatever else it is given, and `UnicodeRanges.All` is what
  stops it also escaping every accented letter in a member's name.
- **Security headers are middleware, not a filter.** An error page, a health check and a photo
  carry them as surely as a controller action does — the responses most likely to be forgotten are
  exactly the ones no controller wrote. They are registered on `OnStarting`, so the response an
  exception handler produced carries them too.
- **`Strict-Transport-Security` is emitted by hand rather than by `UseHsts`.** The built-in
  middleware skips any request that did not arrive over HTTPS; behind a terminating proxy that is
  every request, so it would quietly emit nothing in exactly the deployment shape this is meant
  for.
- **HTTPS redirection is configurable and off in the two suites**, which drive the API over plain
  HTTP. The one test that asserts the redirect turns it back on for itself — and had to supply
  `https_port`, because a test server binds no HTTPS address and the middleware silently passes
  requests through when it cannot work out where to send them. Without that, the requirement would
  have looked met and would not have been.
- **Erasure anonymises in place and never deletes.** `L2-101 AC2` says "removed or irreversibly
  anonymised", and this is the second on purpose: `L2-066 AC1` requires the surviving party's
  thread to stay legible, and a conversation with half its turns missing reads as a fault rather
  than as somebody having exercised a right. The member's row becomes a tombstone, their listings
  keep their identity and lose their content, and their messages say they were erased.
- **The erased address is derived, not hashed.** A hash of a known address is a lookup table away
  from being the address again. It becomes `erased-{id}@erased.invalid` — `.invalid` is reserved by
  RFC 2606 and can never be delivered to.
- **Erasure ends every session, not the calling one.** It has to reach the phone in a pocket, or a
  member who has been forgotten is still signed in somewhere as a person with no name.
- **The photographs are the one thing genuinely deleted.** Bytes in a folder are pointed at by
  nothing and have no reason to survive their owner.
- **An export holds only what this member wrote.** A thread has two sides. Answering one member's
  rights by handing over another's messages would be a breach dressed as compliance.
- **Export and erasure allow an unapproved member.** Somebody still waiting on a moderator, and
  somebody who has left, are the people most likely to want to know what is held about them.

## A latent flake this found and fixed

**Two suites' worth of ordering assertions were passing on a random draw.** The acceptance suite's
`FakeTimeProvider` was frozen, so every row a test created carried the same instant; every list
that orders by time then fell back to comparing identifiers, which are random — so "newest first"
and "in the order they were sent" were decided afresh on each run. Two tests failed in a full run
having passed in isolation, and one of them (`Only_the_other_party_is_told_about_a_message`) had
been in the suite since slice 7.

The fix is one line: `AutoAdvanceAmount = TimeSpan.FromTicks(1)`. A hundred nanoseconds per reading
is enough to break the tie and far too little to disturb anything reasoning about a fifteen-minute
expiry. Three consecutive full runs are green.

## A defect found and not fixed

**The four post forms hard-code St. Aidan's neighbourhoods.** `post-lend`, `post-give`, `post-sell`
and `post-help` each carry the same eight-string literal, where `L2-022` requires only the
congregation's own. `CongregationStore` already holds the right list and the joining form already
reads it. This is a slice-2 defect surviving slice 4's correction of the same class of problem
elsewhere; it is outside this slice's requirements, so it is recorded here rather than fixed
quietly. It belongs in slice 10.

## Verification

```
backend   dotnet build   0 warnings
backend   dotnet test    294 passed, 0 failed, 0 skipped   (was 260)
frontend  ng build       0 errors
frontend  playwright     103 passed, 0 failed, 0 skipped   (was 99)
```
