import { BoardPage } from '../page-objects/board.page';
import { IncomingRequestsPage, OutgoingRequestsPage } from '../page-objects/inbox.page';
import { ListingDetailPage } from '../page-objects/listing-detail.page';
import { RequestAcceptedPage, RequestLendPage, RequestSentPage } from '../page-objects/request.page';
import { Members, SeededListings } from '../support/members';
import { expect, test } from '../support/barnabas';

// Acceptance Test
// Traces to: L2-054, L2-058, L2-059, L2-060, L2-061, L2-120
// Description: A member asks to borrow something and is told it was sent; the owner sees what was
// asked and decides it; the requester can see what became of it either way.

const ask = {
  message: 'Painting the back bedroom the weekend after next.',
  pickupOn: '2026-09-13',
  returnBy: '2026-09-20',
};

async function askToBorrowTheLadder(page: import('@playwright/test').Page): Promise<void> {
  const board = new BoardPage(page);
  const detail = new ListingDetailPage(page);
  const requestLend = new RequestLendPage(page);

  await board.goto();
  await board.open(SeededListings.ladder);
  await detail.ask();
  await requestLend.fillAndSend(ask);

  await expect(new RequestSentPage(page).heading).toBeVisible();
}

// L2-054 AC3: Given a Lend listing, when a member chooses to request it, then the action reads
// "Request to borrow" and the form offers a return date.
test('the action reads request to borrow, and the form asks when it comes back', async ({
  page,
  signInAs,
  board,
}) => {
  const detail = new ListingDetailPage(page);
  const requestLend = new RequestLendPage(page);

  await signInAs(Members.priya.emailAddress);

  await board.goto();
  await board.open(SeededListings.ladder);

  await expect(detail.requestToBorrow).toHaveText('Request to borrow');

  await detail.ask();

  await expect(requestLend.returnBy).toBeVisible();
  await expect(requestLend.pickupOn).toBeVisible();
  await expect(requestLend.acknowledgement).toBeVisible();
});

// L2-058 AC1, AC2, AC3: the confirmation names the owner, offers the two ways onward, and says
// that Barnabas handles no payment or delivery.
test('sending a request confirms it and says what Barnabas does not do', async ({ page, signInAs }) => {
  const sent = new RequestSentPage(page);

  await signInAs(Members.priya.emailAddress);
  await askToBorrowTheLadder(page);

  await expect(sent.heading).toContainText(Members.marion.displayName);
  await expect(sent.seeMyRequests).toBeVisible();
  await expect(sent.backToTheBoard).toBeVisible();
  await expect(sent.noPaymentOrDelivery).toBeVisible();
});

// L2-120 AC4: Given a member who has just sent a request, when they choose to see their own
// requests, then the new request is shown as pending.
test('a member can follow the confirmation to their own requests', async ({ page, signInAs }) => {
  const sent = new RequestSentPage(page);
  const outgoing = new OutgoingRequestsPage(page);

  await signInAs(Members.priya.emailAddress);
  await askToBorrowTheLadder(page);

  await sent.seeMyRequests.click();

  await expect(outgoing.statusOf(SeededListings.ladder)).toHaveText('Pending');
  await expect(outgoing.threadFor(SeededListings.ladder)).toHaveCount(0);
});

// L2-059 AC2: Given an owner viewing incoming requests, when a request is shown, then the listing
// title opens the listing.
test('an owner sees the message and the terms, and can open the listing', async ({ page, signInAs }) => {
  const incoming = new IncomingRequestsPage(page);
  const detail = new ListingDetailPage(page);

  await signInAs(Members.priya.emailAddress);
  await askToBorrowTheLadder(page);

  await signInAs(Members.marion.emailAddress);
  await incoming.goto();

  const row = incoming.from(Members.priya.displayName);

  await expect(row).toContainText(ask.message);
  await expect(row).toContainText('back by 2026-09-20');

  await row.getByRole('link', { name: SeededListings.ladder }).click();

  await expect(detail.title).toHaveText(SeededListings.ladder);
});

// L2-060 AC3: Given an owner accepting a request, when it succeeds, then a confirmation names the
// requester and offers to open the message thread.
test('accepting names the requester and offers the thread', async ({ page, signInAs }) => {
  const incoming = new IncomingRequestsPage(page);
  const accepted = new RequestAcceptedPage(page);

  await signInAs(Members.priya.emailAddress);
  await askToBorrowTheLadder(page);

  await signInAs(Members.marion.emailAddress);
  await incoming.goto();
  await incoming.accept(Members.priya.displayName).click();

  await expect(accepted.heading).toContainText(Members.priya.displayName);
  await expect(accepted.openTheThread).toBeVisible();
});

// L2-061 AC2: Given an owner declining a request, when they choose to decline, then a
// confirmation is required before it proceeds.
// L2-061 AC3: Given a declined request, when the requester views their own requests, then its
// status is shown as declined.
// L2-120 AC5: and no thread is offered.
test('declining is confirmed first, and the requester is told', async ({ page, signInAs }) => {
  const incoming = new IncomingRequestsPage(page);
  const outgoing = new OutgoingRequestsPage(page);

  await signInAs(Members.priya.emailAddress);
  await askToBorrowTheLadder(page);

  await signInAs(Members.marion.emailAddress);
  await incoming.goto();
  await incoming.decline(Members.priya.displayName).click();

  const dialog = page.getByRole('dialog', { name: 'Decline this request?' });

  // The dialog gates the call rather than the response: cancelling sends nothing at all.
  await expect(dialog).toBeVisible();
  await dialog.getByRole('button', { name: 'Cancel' }).click();

  await expect(incoming.from(Members.priya.displayName)).toContainText('Pending');

  await incoming.decline(Members.priya.displayName).click();
  await dialog.getByRole('button', { name: 'Decline' }).click();

  await signInAs(Members.priya.emailAddress);
  await outgoing.goto();

  await expect(outgoing.statusOf(SeededListings.ladder)).toHaveText('Declined');
  await expect(outgoing.threadFor(SeededListings.ladder)).toHaveCount(0);
});
