import { BoardPage } from '../page-objects/board.page';
import { ListingDetailPage } from '../page-objects/listing-detail.page';
import { MyListingsPage } from '../page-objects/my-listings.page';
import { RequestLendPage, RequestSentPage } from '../page-objects/request.page';
import { Members, SeededListings } from '../support/members';
import { expect, test } from '../support/barnabas';

// Acceptance Test
// Traces to: L2-035, L2-037 (partial - the Lend branch), L2-041, L2-062
// Description: An owner sees what is waiting on their listings and what they can do with them; a
// visitor sees neither.
//
// L2-037 AC5 names a Sell listing's close-out wording. Posting a Sell listing is out of feature
// slice 1, so this covers the Lend wording instead and the requirement stands as partial.

// L2-035 AC2: Given a member with a listing carrying open requests, when they open their
// listings, then the count is shown and leads to those requests.
test('the open request count is shown and leads to the requests', async ({ page, signInAs }) => {
  const board = new BoardPage(page);
  const detail = new ListingDetailPage(page);
  const requestLend = new RequestLendPage(page);
  const sent = new RequestSentPage(page);
  const myListings = new MyListingsPage(page);

  await signInAs(Members.priya.emailAddress);

  await board.goto();
  await board.open(SeededListings.ladder);
  await detail.ask();
  await requestLend.fillAndSend({
    message: 'Painting the back bedroom.',
    pickupOn: '2026-09-13',
    returnBy: '2026-09-20',
  });

  await expect(sent.heading).toContainText(Members.marion.displayName);

  await signInAs(Members.marion.emailAddress);

  // Asserted first so a failure says which half broke: the request arriving, or the count of it.
  await page.goto('/inbox/requests');
  await expect(page.getByText(Members.priya.displayName)).toBeVisible();

  await myListings.goto();

  await expect(myListings.requestCount(SeededListings.ladder)).toHaveText('1 request');

  await myListings.requestCount(SeededListings.ladder).click();

  await expect(page).toHaveURL(/\/inbox\/requests$/);
  await expect(page.getByText(Members.priya.displayName)).toBeVisible();
});

// L2-037: the close-out action reads in the vocabulary of the listing's kind, not a generic one.
test('a lend listing closes out as taken, and a sell listing as sold', async ({ signInAs, page }) => {
  const myListings = new MyListingsPage(page);

  await signInAs(Members.marion.emailAddress);
  await myListings.goto();

  await expect(myListings.closeOut(SeededListings.ladder)).toHaveText('Mark as taken');

  // Seeded rather than posted: the Sell form is a later slice, but the wording is decided by the
  // kind and is worth asserting for more than one of them.
  await expect(myListings.closeOut(SeededListings.drill)).toHaveText('Mark as sold');
});

// L2-041 AC3: Given a member viewing another member's listing, when the screen loads, then no
// edit, archive, or close-out action is present.
// L2-062 AC4: Given a member viewing their own listing, when the screen loads, then no request
// action is present.
test('a listing offers its owner and a visitor different things', async ({ page, signInAs }) => {
  const detail = new ListingDetailPage(page);

  const board = new BoardPage(page);

  await signInAs(Members.priya.emailAddress);
  await board.goto();
  await board.open(SeededListings.ladder);

  // A visitor may ask for it, and may not close it out.
  await expect(detail.requestToBorrow).toBeVisible();
  await expect(detail.markAsTaken).toHaveCount(0);

  const listingUrl = page.url();

  await signInAs(Members.marion.emailAddress);
  await page.goto(listingUrl);

  // The owner may close it out, and is not offered the chance to request their own listing.
  await expect(detail.markAsTaken).toBeVisible();
  await expect(detail.requestToBorrow).toHaveCount(0);
});

// A member sees only their own listings on this screen.
test('a member sees only their own listings', async ({ page, signInAs }) => {
  const myListings = new MyListingsPage(page);

  await signInAs(Members.priya.emailAddress);
  await myListings.goto();

  await expect(myListings.row(SeededListings.ladder)).toHaveCount(0);
});
