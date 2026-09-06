import { BoardPage } from '../page-objects/board.page';
import { IncomingRequestsPage, OutgoingRequestsPage } from '../page-objects/inbox.page';
import { ListingDetailPage } from '../page-objects/listing-detail.page';
import { ThreadPage, ThreadsPage } from '../page-objects/messages.page';
import { MyListingsPage } from '../page-objects/my-listings.page';
import { ChooseKindPage, ListingPostedPage, PostLendPage } from '../page-objects/post-listing.page';
import { RequestAcceptedPage, RequestLendPage, RequestSentPage } from '../page-objects/request.page';
import { Members } from '../support/members';
import { expect, test } from '../support/barnabas';

// Acceptance Test
// Traces to: L2-012, L2-014, L2-027, L2-033, L2-037, L2-042, L2-054, L2-058, L2-059, L2-060,
//            L2-065, L2-066, L2-067
// Description: The whole of feature slice 1 through the screens - sign in, post a Lend listing,
// see it on the board, ask to borrow it, accept, arrange the handoff, mark it taken.
//
// Every other spec says one thing precisely. This one says that the things join up, which none
// of them can on its own. It is the slice's definition of done, walked.
test('a member borrows a ladder from another member', async ({ page, signInAs }) => {
  // The longest journey in the suite: two members, seven screens, and four sign-ins between
  // them. It fits the default timeout when run alone and has intermittently exceeded it in a
  // full run, which is a slow test rather than a failing one - so it is declared slow instead of
  // being left to chance.
  test.slow();

  const ladder = 'Extending ladder, three sections';

  // Marion signs in and posts the ladder.
  await signInAs(Members.marion.emailAddress);

  const board = new BoardPage(page);
  const chooseKind = new ChooseKindPage(page);
  const postLend = new PostLendPage(page);
  const posted = new ListingPostedPage(page);

  await board.postAListing.click();
  await expect(chooseKind.heading).toBeVisible();

  await chooseKind.chooseLend();
  await expect(postLend.heading).toBeVisible();

  await postLend.fill({
    title: ladder,
    description: 'Sound, both spreaders lock. Kept in the garage.',
    returnBy: '2026-10-01',
  });
  await postLend.submit();

  await expect(posted.heading).toBeVisible();
  await expect(posted.viewYourListing).toBeVisible();
  await expect(posted.postAnother).toBeVisible();
  await expect(posted.backToTheBoard).toBeVisible();

  await posted.backToTheBoard.click();
  await expect(board.placard(ladder)).toBeVisible();

  // Priya signs in and asks to borrow it.
  await signInAs(Members.priya.emailAddress);

  const detail = new ListingDetailPage(page);
  const requestLend = new RequestLendPage(page);
  const sent = new RequestSentPage(page);

  await board.open(ladder);
  await expect(detail.title).toHaveText(ladder);

  await detail.ask();
  await requestLend.fillAndSend({
    message: 'Painting the back bedroom the weekend after next.',
    pickupOn: '2026-09-13',
    returnBy: '2026-09-20',
  });

  await expect(sent.heading).toContainText(Members.marion.displayName);
  await expect(sent.noPaymentOrDelivery).toBeVisible();
  await expect(sent.seeMyRequests).toBeVisible();

  // Marion accepts, and a thread opens.
  await signInAs(Members.marion.emailAddress);

  const incoming = new IncomingRequestsPage(page);
  const accepted = new RequestAcceptedPage(page);
  const thread = new ThreadPage(page);

  await incoming.goto();
  await incoming.accept(Members.priya.displayName).click();

  await expect(accepted.heading).toContainText(Members.priya.displayName);
  await accepted.openTheThread.click();

  await thread.say('Thursday after choir suits me. It will be by the side door.');
  await expect(thread.message('Thursday after choir suits me.')).toBeVisible();

  // Priya replies in the same thread.
  await signInAs(Members.priya.emailAddress);

  const threads = new ThreadsPage(page);
  const outgoing = new OutgoingRequestsPage(page);

  await outgoing.goto();
  await expect(outgoing.statusOf(ladder)).toHaveText('Accepted');
  await outgoing.threadFor(ladder).click();

  await expect(thread.message('Thursday after choir suits me.')).toBeVisible();
  await thread.say('Thursday it is. Thank you, Marion.');
  await expect(thread.message('Thursday it is.')).toBeVisible();

  await threads.goto();
  await expect(threads.with(Members.marion.displayName)).toBeVisible();

  // Marion marks it taken, and it leaves the board.
  await signInAs(Members.marion.emailAddress);

  const myListings = new MyListingsPage(page);

  await myListings.goto();
  await myListings.closeOut(ladder).click();

  await page.getByRole('button', { name: 'Mark it' }).click();

  await expect(myListings.row(ladder)).toHaveCount(0);

  await board.goto();
  await expect(board.placard(ladder)).toHaveCount(0);
});
