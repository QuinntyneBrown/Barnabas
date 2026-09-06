import { BoardPage } from '../page-objects/board.page';
import { IncomingRequestsPage } from '../page-objects/inbox.page';
import { ListingDetailPage } from '../page-objects/listing-detail.page';
import { ThreadPage, ThreadsPage } from '../page-objects/messages.page';
import { RequestAcceptedPage, RequestLendPage, RequestSentPage } from '../page-objects/request.page';
import { Members, SeededListings } from '../support/members';
import { expect, test } from '../support/barnabas';

// Acceptance Test
// Traces to: L2-065, L2-067, L2-068
// Description: A member finds the thread an accepted request opened, sees it marked unread, says
// something in it that appears attributed to them, and can reach from it both the listing it is
// about and the member they are talking to.

/** Gets Marion and Priya as far as an open thread, which is the only way one comes into being. */
async function openTheThread(page: import('@playwright/test').Page, signInAs: (email: string) => Promise<void>) {
  const board = new BoardPage(page);
  const detail = new ListingDetailPage(page);
  const requestLend = new RequestLendPage(page);
  const incoming = new IncomingRequestsPage(page);

  await signInAs(Members.priya.emailAddress);
  await board.goto();
  await board.open(SeededListings.ladder);
  await detail.ask();
  await requestLend.fillAndSend({
    message: 'Painting the back bedroom.',
    pickupOn: '2026-09-13',
    returnBy: '2026-09-20',
  });
  await expect(new RequestSentPage(page).heading).toBeVisible();

  await signInAs(Members.marion.emailAddress);
  await incoming.goto();
  await incoming.accept(Members.priya.displayName).click();
  await expect(new RequestAcceptedPage(page).openTheThread).toBeVisible();
}

// L2-065 AC1: each thread shows the other member, the listing, and the latest message.
// L2-065 AC3: Given a member with an unread thread, when they open their messages, then that
// thread is marked unread.
test('a thread is listed with the other member, the listing, and whether it is unread', async ({
  page,
  signInAs,
}) => {
  // Two members, a request, an acceptance and a message before it can assert anything. It fits
  // the default timeout alone and has exceeded it in a full run, which is a slow test rather
  // than a failing one.
  test.slow();

  const accepted = new RequestAcceptedPage(page);
  const thread = new ThreadPage(page);
  const threads = new ThreadsPage(page);

  await openTheThread(page, signInAs);

  await accepted.openTheThread.click();
  await thread.say('It will be by the side door whenever suits.');

  await signInAs(Members.priya.emailAddress);
  await threads.goto();

  const row = threads.with(Members.marion.displayName);

  await expect(row).toContainText(SeededListings.ladder);
  await expect(row).toContainText('It will be by the side door whenever suits.');

  // Unread as a reader is told it, not as it is drawn: the dot is hidden from assistive
  // technology and the word is in the row's own text.
  await expect(threads.unreadMarker(Members.marion.displayName)).toBeAttached();
});

// Opening it marks it read, for the reader alone.
test('opening a thread marks it read for the member who opened it', async ({ page, signInAs }) => {
  const accepted = new RequestAcceptedPage(page);
  const thread = new ThreadPage(page);
  const threads = new ThreadsPage(page);

  await openTheThread(page, signInAs);

  await accepted.openTheThread.click();
  await thread.say('It will be by the side door.');

  await signInAs(Members.priya.emailAddress);
  await threads.goto();
  await expect(threads.unreadMarker(Members.marion.displayName)).toBeAttached();

  await threads.open(Members.marion.displayName);
  await expect(thread.message('It will be by the side door.')).toBeVisible();

  await threads.goto();
  await expect(threads.unreadMarker(Members.marion.displayName)).toHaveCount(0);
});

// L2-067 AC4: Given a member in a thread, when they send a message, then it appears in the thread
// attributed to them.
test('a message appears in the thread attributed to whoever sent it', async ({ page, signInAs }) => {
  const accepted = new RequestAcceptedPage(page);
  const thread = new ThreadPage(page);
  const threads = new ThreadsPage(page);

  await openTheThread(page, signInAs);

  await accepted.openTheThread.click();
  await thread.say('Thursday after choir suits me.');

  // Confirmation of a send is the message appearing. There is no toast, because nothing
  // happened beyond the thing the member can see.
  await expect(thread.message('Thursday after choir suits me.')).toBeVisible();
  await expect(thread.attributionOf('Thursday after choir suits me.')).toContainText('You');
  await expect(thread.composer).toHaveValue('');

  await signInAs(Members.priya.emailAddress);
  await threads.goto();
  await threads.open(Members.marion.displayName);

  // The same message, read from the other side, is attributed to Marion rather than to "You".
  await expect(thread.attributionOf('Thursday after choir suits me.')).toContainText(
    Members.marion.displayName,
  );

  await thread.say('Thursday it is. Thank you.');
  await expect(thread.attributionOf('Thursday it is.')).toContainText('You');
});

// An empty message is refused, and nothing is appended.
test('an empty message is not sent', async ({ page, signInAs }) => {
  const accepted = new RequestAcceptedPage(page);
  const thread = new ThreadPage(page);

  await openTheThread(page, signInAs);
  await accepted.openTheThread.click();

  await thread.send.click();

  await expect(thread.messages).toHaveCount(0);
});

// A member who is not a party is told the thread is not there, rather than that they may not
// look at it.
test('a member who is not a party cannot open the thread', async ({ page, signInAs }) => {
  const accepted = new RequestAcceptedPage(page);

  await openTheThread(page, signInAs);
  await accepted.openTheThread.click();

  const threadUrl = page.url();

  await signInAs(Members.grace.emailAddress);
  await page.goto(threadUrl);

  await expect(page.getByRole('heading', { name: 'That conversation is not here' })).toBeVisible();
});

// L2-068 AC1: Given a thread, when it is displayed, then the listing it concerns is shown and
// opens that listing.
// L2-068 AC2: Given a thread, when it is displayed, then the other member's name opens their
// profile.
test('a thread leads back to the listing and to the other member', async ({ page, signInAs }) => {
  // Two members, a request and an acceptance before it can assert anything.
  test.slow();

  await openTheThread(page, signInAs);

  const threads = new ThreadsPage(page);
  const thread = new ThreadPage(page);

  // Read from the requester's side, so the other member is the owner - whose profile is the one
  // AC2 is about. The helper leaves the browser signed in as the owner, who would see themselves.
  await signInAs(Members.priya.emailAddress);

  await threads.goto();
  await threads.open(Members.marion.displayName);

  // The listing it is about, named and reachable. A thread that did not lead back to it leaves a
  // member scrolling their own words to remember what they asked for.
  await expect(thread.listing).toContainText(SeededListings.ladder);

  await thread.listing.click();

  await expect(new ListingDetailPage(page).title).toHaveText(SeededListings.ladder);

  // And the other party, whose name opens their profile rather than only labelling the thread.
  await page.goBack();

  await thread.otherMember(Members.marion.displayName).click();

  await expect(page.getByRole('heading', { name: Members.marion.displayName })).toBeVisible();
});
