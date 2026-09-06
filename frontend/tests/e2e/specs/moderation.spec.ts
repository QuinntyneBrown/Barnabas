import { Page } from '@playwright/test';

import { BoardPage } from '../page-objects/board.page';
import { ListingDetailPage } from '../page-objects/listing-detail.page';
import {
  ModerationQueuePage,
  ReportListingDialog,
  ReportSentPage,
} from '../page-objects/moderation.page';
import { NotificationsPage } from '../page-objects/notifications.page';
import { AwaitingApprovalPage, CreateProfilePage, InviteSomeonePage, JoinPage } from '../page-objects/joining.page';
import { Members, SeededListings } from '../support/members';
import { expect, test } from '../support/barnabas';

// Acceptance Test
// Traces to: L2-080, L2-081, L2-082, L2-083, L2-084, L2-085, L2-086, L2-087
// Description: A member reports a listing and is told a moderator will look at it and that the
// poster is not told who reported it. A moderator sees the queue, opens what is in it, and either
// leaves a listing up or removes it behind a confirmation. The same page holds the members waiting
// to join, and approving one lets them onto the board.

// L2-080 AC3: Given a listing, when a member reports it, then a confirmation states a moderator
// will look at it and that the poster is not told who reported it.
test('reporting a listing says a moderator will look and that the poster is not told', async ({
  board,
  page,
  signInAs,
}) => {
  await signInAs(Members.priya.emailAddress);

  const listing = new ListingDetailPage(page);
  const dialog = new ReportListingDialog(page);
  const sent = new ReportSentPage(page);

  await board.goto();
  await board.open(SeededListings.ladder);

  await listing.report.click();

  // Said before they choose anything. The promise is the reason somebody hesitating goes ahead.
  await expect(dialog.anonymityPromise).toContainText(Members.marion.displayName);

  await dialog.report('The description is not accurate', 'A rung is cracked.');

  await expect(sent.heading).toBeVisible();
  await expect(sent.anonymity).toBeVisible();

  // And it stays up meanwhile - reporting flags a listing for review rather than removing it.
  await expect(sent.stillUp).toBeVisible();

  await sent.backToTheBoard.click();

  await expect(board.placard(SeededListings.ladder)).toBeVisible();
});

// The second attempt says so plainly rather than failing. The member did nothing wrong and their
// complaint is already on a moderator's list.
test('reporting the same listing twice says it is already with a moderator', async ({
  board,
  page,
  signInAs,
}) => {
  await signInAs(Members.priya.emailAddress);

  const listing = new ListingDetailPage(page);
  const dialog = new ReportListingDialog(page);

  await board.goto();
  await board.open(SeededListings.ladder);
  await listing.report.click();
  await dialog.report('It does not belong on the board');

  await expect(new ReportSentPage(page).heading).toBeVisible();

  await board.goto();
  await board.open(SeededListings.ladder);
  await listing.report.click();
  await dialog.report('It is already gone');

  await expect(dialog.alreadyReported).toBeVisible();
});

// L2-081 AC3: Given a member whose listing was reported, when they view it, then no indication of
// who reported it is present.
test('the owner of a reported listing sees no sign that anybody reported it', async ({
  board,
  page,
  signInAs,
}) => {
  await signInAs(Members.priya.emailAddress);

  const listing = new ListingDetailPage(page);
  const dialog = new ReportListingDialog(page);

  await board.goto();
  await board.open(SeededListings.ladder);
  await listing.report.click();
  await dialog.report('The description is not accurate', 'A rung is cracked.');

  await expect(new ReportSentPage(page).heading).toBeVisible();

  await signInAs(Members.marion.emailAddress);

  await board.goto();
  await board.open(SeededListings.ladder);

  await expect(listing.title).toHaveText(SeededListings.ladder);

  // Nothing about a report, nobody's name but her own, and not even the fact of it. An owner is
  // told when a moderator acts, and not before.
  await expect(page.getByText(/report/i)).toHaveCount(0);
  await expect(page.getByText(/flag/i)).toHaveCount(0);
  await expect(page.getByText('A rung is cracked.')).toHaveCount(0);

  // And the owner is not offered the chance to report their own listing.
  await expect(listing.report).toHaveCount(0);
});

// L2-082 AC3: Given a moderator viewing the queue, when a flagged listing is shown, then its title
// opens the listing and its poster's name opens their profile.
test('the queue opens what it names', async ({ board, page, signInAs }) => {
  // Two members, a report, and three screens before it can assert anything. It fits the default
  // timeout alone and has exceeded it in a full run, which is a slow test rather than a failing
  // one.
  test.slow();

  await signInAs(Members.priya.emailAddress);

  await reportTheLadder(page, board);

  await signInAs(Members.marion.emailAddress);

  const queue = new ModerationQueuePage(page);

  await queue.goto();
  await queue.waitForRows();

  const row = queue.flagged(SeededListings.ladder);

  // The reason and who gave it, which is what a moderator is deciding on.
  await expect(row).toContainText(Members.priya.displayName);
  await expect(row).toContainText('The description is not accurate');

  await row.getByRole('link', { name: SeededListings.ladder }).click();

  await expect(new ListingDetailPage(page).title).toHaveText(SeededListings.ladder);

  await queue.goto();
  await queue.waitForRows();

  await queue.flagged(SeededListings.ladder).getByRole('link', { name: Members.marion.displayName }).click();

  await expect(page.getByRole('heading', { name: Members.marion.displayName })).toBeVisible();
});

// L2-083 AC2: Given a moderator approving a flagged listing, when the action completes, then the
// queue is shown without that listing.
test('leaving a listing up clears it from the queue and leaves it on the board', async ({
  board,
  page,
  signInAs,
}) => {
  await signInAs(Members.priya.emailAddress);

  await reportTheLadder(page, board);

  await signInAs(Members.marion.emailAddress);

  const queue = new ModerationQueuePage(page);

  await queue.goto();
  await queue.waitForRows();

  await queue.leaveItUp(SeededListings.ladder);

  await expect(queue.flagged(SeededListings.ladder)).toHaveCount(0);

  // Still where it was. Approving clears the flag and changes nothing else.
  await board.goto();

  await expect(board.placard(SeededListings.ladder)).toBeVisible();
});

// L2-084 AC3: Given a moderator removing a listing, when they choose to remove, then a
// confirmation is required before it proceeds.
test('removing a listing is confirmed first, and its owner is told', async ({
  board,
  page,
  signInAs,
}) => {
  // Two members, a report, a confirmation, the board and the notification list. Slow rather than
  // failing.
  test.slow();

  await signInAs(Members.priya.emailAddress);

  await reportTheLadder(page, board);

  await signInAs(Members.marion.emailAddress);

  const queue = new ModerationQueuePage(page);

  await queue.goto();
  await queue.waitForRows();

  await queue.beginRemoving(SeededListings.ladder);

  // Nothing has happened yet. The dialog is the step between choosing and doing.
  await expect(queue.removeDialog).toBeVisible();
  await expect(queue.flagged(SeededListings.ladder)).toHaveCount(1);

  await queue.confirmRemove.click();

  await expect(queue.flagged(SeededListings.ladder)).toHaveCount(0);

  // Off the board, and its owner told what happened to it.
  await board.goto();

  await expect(board.placard(SeededListings.ladder)).toHaveCount(0);

  const notifications = new NotificationsPage(page);

  await notifications.goto();
  await notifications.waitForRows();

  await expect(notifications.rows.first()).toContainText(/took .* off the board/);

  // And nothing about who objected to it.
  await expect(page.getByText(Members.priya.displayName)).toHaveCount(0);
});

// L2-085 AC2 and L2-086 AC3: a moderator sees who is waiting and why, lets one in, and that member
// reaches the board.
test('a moderator lets somebody in and they reach the board', async ({ page, board, signInAs }) => {
  // A code issued, redeemed, a profile filled in, an approval, and a third sign-in.
  test.slow();

  await signInAs(Members.marion.emailAddress);

  await joinAsAsync(page, {
    displayName: 'Nora Almeida',
    emailAddress: 'nora@example.com',
    reason: 'Came to the parish newcomer lunch.',
  });

  await signInAs(Members.marion.emailAddress);

  const queue = new ModerationQueuePage(page);

  await queue.goto();
  await queue.waitForRows();

  const row = queue.applicant('Nora Almeida');

  await expect(row).toContainText('Riverdale');
  await expect(row).toContainText('Came to the parish newcomer lunch.');

  await queue.approve('Nora Almeida');

  await expect(queue.applicant('Nora Almeida')).toHaveCount(0);

  // The board, rather than the screen that says a moderator has to let them in.
  await signInAs('nora@example.com');

  await board.goto();

  await expect(board.heading).toHaveText("St. Aidan's board");
});

// L2-087 AC2: Given a moderator declining a member, when they choose to decline, then a
// confirmation is required before it proceeds.
test('declining somebody is confirmed first, and leaves them off the board', async ({
  page,
  signInAs,
}) => {
  await signInAs(Members.marion.emailAddress);

  await joinAsAsync(page, {
    displayName: 'Rowan Doyle',
    emailAddress: 'rowan@example.com',
    reason: 'Passing through.',
  });

  await signInAs(Members.marion.emailAddress);

  const queue = new ModerationQueuePage(page);

  await queue.goto();
  await queue.waitForRows();

  await queue.beginDeclining('Rowan Doyle');

  await expect(queue.declineDialog).toBeVisible();
  await expect(queue.applicant('Rowan Doyle')).toHaveCount(1);

  await queue.confirmDecline.click();

  await expect(queue.applicant('Rowan Doyle')).toHaveCount(0);
});

// The queue is honest about being empty rather than showing two blank headings.
test('a moderator with nothing waiting is told so', async ({ page, signInAs }) => {
  await signInAs(Members.marion.emailAddress);

  const queue = new ModerationQueuePage(page);

  await queue.goto();

  await expect(queue.nothingWaiting).toBeVisible();
});

/** Reports the ladder as the signed-in member, and waits for the product's own confirmation. */
async function reportTheLadder(page: Page, board: BoardPage): Promise<void> {
  const listing = new ListingDetailPage(page);
  const dialog = new ReportListingDialog(page);

  await board.goto();
  await board.open(SeededListings.ladder);
  await listing.report.click();
  await dialog.report('The description is not accurate', 'A rung is cracked.');

  // Waits for the screen that says it went, rather than navigating with the write in flight.
  await expect(new ReportSentPage(page).heading).toBeVisible();
}

/** Walks somebody all the way in, up to the point a moderator has to let them on. */
async function joinAsAsync(
  page: Page,
  applicant: { displayName: string; emailAddress: string; reason: string },
): Promise<void> {
  const invite = new InviteSomeonePage(page);

  await invite.goto();
  await invite.issue.click();

  await expect(invite.code).toHaveText(/^[A-Z2-9]{8}$/);

  const code = (await invite.code.textContent())?.trim() ?? '';

  await page.context().clearCookies();

  const join = new JoinPage(page);
  const profile = new CreateProfilePage(page);

  await join.goto();
  await join.redeem(code);

  await expect(profile.heading).toBeVisible();

  await profile.displayName.fill(applicant.displayName);
  await profile.emailAddress.fill(applicant.emailAddress);
  await profile.reason.fill(applicant.reason);
  await profile.join.click();

  await expect(new AwaitingApprovalPage(page).heading).toBeVisible();
}
