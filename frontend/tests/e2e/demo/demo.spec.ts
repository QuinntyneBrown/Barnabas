import { Page } from '@playwright/test';

import { BoardPage } from '../page-objects/board.page';
import { CheckYourEmailPage } from '../page-objects/check-your-email.page';
import { DirectoryPage, ProfileSettingsPage } from '../page-objects/profile.page';
import { IncomingRequestsPage } from '../page-objects/inbox.page';
import {
  AwaitingApprovalPage,
  CreateProfilePage,
  InviteSomeonePage,
  JoinPage,
} from '../page-objects/joining.page';
import { ListingDetailPage } from '../page-objects/listing-detail.page';
import {
  ChooseKindPage,
  ListingPostedPage,
  PostHelpPage,
  PostSellPage,
} from '../page-objects/post-listing.page';
import {
  ModerationQueuePage,
  ReportListingDialog,
  ReportSentPage,
} from '../page-objects/moderation.page';
import { MyListingsPage } from '../page-objects/my-listings.page';
import { NotificationsPage } from '../page-objects/notifications.page';
import { RequestAcceptedPage, RequestSellPage, RequestSentPage } from '../page-objects/request.page';
import { SearchPage } from '../page-objects/search.page';
import { ShellPage } from '../page-objects/shell.page';
import { SignInPage } from '../page-objects/sign-in.page';
import { ThreadPage } from '../page-objects/messages.page';
import { aPhotograph } from '../support/images';
import { Members, SeededListings } from '../support/members';
import { expect, signInLinkFor, test } from '../support/barnabas';
import { beat, chapter, quiet, reveal, say } from './narrate';

// A demonstration, not a test.
//
// It asserts as it goes — every `expect` here is the recording refusing to show something that did
// not happen — but its purpose is to be watched. It drives the real client against the real API
// and a real database, in one continuous take, with no compositing and no second attempt.
//
// The scenes follow the walkthrough in the README: provision, invite, join, approve, post, request,
// accept, message, close out, moderate. Roughly five minutes.
//
//     npm run demo
//
// It is excluded from the acceptance suites by living outside `tests/e2e/specs`, and it gates
// nothing.

/** The thing posted during the recording, so every later scene can find it again. */
const KETTLE = 'Copper stovetop kettle';

test('Barnabas, end to end', async ({ page }) => {
  const board = new BoardPage(page);
  const shell = new ShellPage(page);

  // ── Title ────────────────────────────────────────────────────────────────────────────────────
  await page.goto('/');

  await chapter(
    page,
    'A demonstration',
    'Barnabas',
    'A private lending, giving, selling and helping board, scoped to one congregation. '
      + 'Everything that follows is the real application, driving itself.',
  );

  await quiet(page);
  await beat(page, 1_400);

  await say(
    page,
    'It begins with a parish, not a marketplace',
    'A ladder in one garage, a crib in another loft. Barnabas brokers the introduction.',
  );

  // ── 1. Signing in ────────────────────────────────────────────────────────────────────────────
  await chapter(page, 'One', 'Signing in', 'There is no password. There never was one to steal.');

  await signInSlowly(page, Members.marion.emailAddress, {
    heading: 'Identity is possession of the mailbox',
    detail: 'No password is stored, so there is none to steal.',
  });

  // ── 2. The board ─────────────────────────────────────────────────────────────────────────────
  await chapter(
    page,
    'Two',
    'The board',
    'Four kinds of listing, and they are not interchangeable.',
  );

  await board.goto();
  await board.waitForPlacards();

  await say(
    page,
    'The board names its own congregation',
    'Read from the API. A member of another parish sees theirs, never this one.',
  );

  await expect(board.heading).toHaveText("St. Aidan's board");

  await reveal(page, 380);

  await say(
    page,
    'Every placard carries its kind in words',
    'Colour alone cannot carry that on a phone in sunlight.',
  );

  await expect(board.kindOf(SeededListings.ladder)).toHaveText('Lending');

  await say(
    page,
    'Filtering happens on the server',
    'The count beside a chip is the number of placards behind it.',
  );

  await board.filterBy('For sale');
  await beat(page, 1_800);

  await expect(board.activeFilter).toContainText('For sale');
  await expect(board.placard(SeededListings.drill)).toBeVisible();

  await board.filterBy('Everything');
  await beat(page, 1_200);

  // ── 3. Posting, with a photograph ────────────────────────────────────────────────────────────
  await chapter(
    page,
    'Three',
    'Posting something',
    'The kind is chosen first, because the kind decides the form.',
  );

  const chooseKind = new ChooseKindPage(page);
  const sell = new PostSellPage(page);

  await chooseKind.goto();

  await say(
    page,
    'Four kinds, and nothing to fill in yet',
    'The kind is chosen before a single question is asked about the thing.',
  );

  await chooseKind.chooseSell();
  await expect(sell.heading).toBeVisible();

  await say(
    page,
    'Only the Sell form asks a price',
    'A submitted price on a gift is refused by name, not quietly dropped.',
  );

  await sell.title.fill(KETTLE);
  await sell.description.fill(
    'Two litres, barely used. It whistles properly. Collection from Riverdale any evening.',
  );
  await sell.price.fill('35');

  await say(
    page,
    'One photograph, and it is never stored as it arrived',
    'Re-encoded from pixels, so no location tag survives.',
  );

  await sell.photo.setInputFiles({
    name: 'kettle.png',
    mimeType: 'image/png',
    buffer: aPhotograph(1200, 900),
  });

  await expect(sell.photoPreview).toBeVisible();
  await beat(page, 1_600);

  await sell.submit();

  await expect(new ListingPostedPage(page).heading).toBeVisible();

  await say(
    page,
    'A confirmation that leads somewhere',
    'View it, post another, or go back. Never a dead end.',
  );

  await new ListingPostedPage(page).viewYourListing.click();

  const detail = new ListingDetailPage(page);

  await expect(detail.title).toHaveText(KETTLE);

  await say(
    page,
    'The price never stands on its own',
    'The screen carrying the figure says money changes hands in person.',
  );

  await expect(detail.paymentNote).toBeVisible();
  await beat(page, 1_600);

  // Help is the kind that is not a thing, and the difference is worth seeing rather than being
  // told about: a different form, a different shape of answer, and no photograph at all.
  const help = new PostHelpPage(page);

  await chooseKind.goto();
  await chooseKind.chooseHelp();

  await expect(help.heading).toBeVisible();

  await say(
    page,
    'Help is time, not a thing',
    'So this form asks when you are free — and offers nowhere to attach a photograph.',
  );

  await expect(help.photo).toHaveCount(0);

  await help.title.fill('Lifts to the Sunday service');
  await help.description.fill('Room for three, anywhere along the Danforth. I go every week anyway.');

  await help.dayOf(0).selectOption({ label: 'Sunday' });
  await help.fromOf(0).fill('08:30');
  await help.untilOf(0).fill('10:30');

  await say(
    page,
    'At least one window, or it is not an offer',
    'An offer of time nobody can name a moment for is not one.',
  );

  await help.submit();

  await expect(new ListingPostedPage(page).heading).toBeVisible();
  await beat(page, 1_200);

  // ── 4. Asking for it ─────────────────────────────────────────────────────────────────────────
  await chapter(
    page,
    'Four',
    'Asking for it',
    'Now as Priya, a different member of the same congregation.',
  );

  await signInQuickly(page, Members.priya.emailAddress);

  await board.goto();
  await board.waitForPlacards();

  await reveal(page, 380);

  await say(
    page,
    'The photograph reached the board',
    'At board size, loaded lazily. A mosaic never fetches thirty full-size pictures.',
  );

  await expect(board.placard(KETTLE).locator('img')).toHaveAttribute('loading', 'lazy');

  await say(
    page,
    'And the offer of help carries words instead',
    'Nothing to photograph, so the placard shows what was said and when it is open.',
  );

  await expect(board.imagesOn('Lifts to the Sunday service')).toHaveCount(0);
  await expect(board.metaOf('Lifts to the Sunday service').first()).toContainText('Sunday');
  await beat(page, 1_600);

  await board.open(KETTLE);

  await say(
    page,
    'The call to action is in the words of the kind',
    'You <em>borrow</em> a loan and <em>buy</em> a sale.',
  );

  await expect(detail.askAction).toHaveText('Request to buy');

  await detail.ask();

  const requestSell = new RequestSellPage(page);

  await expect(requestSell.heading).toBeVisible();

  await say(
    page,
    'The request form carries no card field',
    'Nowhere to pay, and that is enforced rather than merely absent.',
  );

  await expect(requestSell.payment).toHaveCount(0);

  await requestSell.message.fill(
    'Is it still going? I could collect on Thursday evening if that suits.',
  );
  await requestSell.pickupAt.fill('2026-09-17T18:30');
  await requestSell.send.click();

  await expect(new RequestSentPage(page).heading).toBeVisible();

  await say(
    page,
    'And the confirmation says it again',
    'No payment, no delivery, no deposit.',
  );

  await expect(new RequestSentPage(page).noPaymentOrDelivery).toBeVisible();

  // ── 5. Accepting, and the thread it opens ────────────────────────────────────────────────────
  await chapter(
    page,
    'Five',
    'Accepting',
    'Back to Marion, who has been told somebody asked.',
  );

  await signInQuickly(page, Members.marion.emailAddress);

  await board.goto();
  await board.waitForPlacards();

  await say(
    page,
    'The count sits on every screen, at every width',
    'Held once, so no two copies of it can disagree.',
  );

  await expect(shell.unreadCount).toHaveText('1');
  await beat(page, 1_200);

  const notifications = new NotificationsPage(page);

  await notifications.goto();
  await notifications.waitForRows();

  await say(
    page,
    'Every notification leads somewhere',
    'One that led nowhere could not be constructed.',
  );

  await expect(notifications.rows.first()).toContainText(Members.priya.displayName);

  const incoming = new IncomingRequestsPage(page);

  await incoming.goto();

  await say(
    page,
    'The owner decides',
    'Accepting opens a thread. Nothing else starts a conversation.',
  );

  await incoming.accept(Members.priya.displayName).click();

  const accepted = new RequestAcceptedPage(page);

  await expect(accepted.openTheThread).toBeVisible();
  await beat(page, 1_200);

  await accepted.openTheThread.click();

  const thread = new ThreadPage(page);

  await say(
    page,
    'One thread, and it leads back',
    'To the listing it concerns, and to the member on the other side.',
  );

  await expect(thread.listing).toContainText(KETTLE);

  await thread.say('Thursday evening is fine. It is the blue door, by the side gate.');

  await beat(page, 1_600);

  // ── 6. Closing out, in the words of the kind ─────────────────────────────────────────────────
  await chapter(
    page,
    'Six',
    'Closing out',
    'A sale is sold. A gift is taken. A loan comes back.',
  );

  const mine = new MyListingsPage(page);

  await mine.goto();

  await say(
    page,
    'The verb belongs to the kind',
    'A gift is taken; help is booked. The server picks the outcome, not the button.',
  );

  await expect(mine.closeOut(KETTLE)).toHaveText('Mark as sold');

  await mine.closeOut(KETTLE).click();

  const confirm = mine.dialog('Mark this as gone?');

  await expect(confirm).toBeVisible();
  await beat(page, 1_400);

  await confirm.getByRole('button', { name: 'Mark it' }).click();

  await say(
    page,
    'Off the board, keeping its outcome',
    'It stays <em>sold</em>, not some neutral word that lost the distinction.',
  );

  await mine.putAwayTab.click();

  await reveal(page, 240);
  await beat(page, 1_600);

  // ── 7. Moderation ────────────────────────────────────────────────────────────────────────────
  await chapter(
    page,
    'Seven',
    'Moderation',
    'Somebody objects to a listing, and a moderator decides.',
  );

  await signInQuickly(page, Members.priya.emailAddress);

  await board.goto();
  await board.waitForPlacards();
  await board.open(SeededListings.drill);

  await say(
    page,
    'Any member can report a listing',
    'And is told first that the poster will not learn who did.',
  );

  await detail.report.click();

  const reportDialog = new ReportListingDialog(page);

  await expect(reportDialog.anonymityPromise).toContainText(Members.marion.displayName);
  await beat(page, 1_400);

  await reportDialog.report('The description is not accurate', 'The second battery is missing.');

  await expect(new ReportSentPage(page).heading).toBeVisible();

  await say(
    page,
    'Reporting flags a listing — it does not remove it',
    'It stays up until a moderator has looked.',
  );

  await expect(new ReportSentPage(page).stillUp).toBeVisible();

  await signInQuickly(page, Members.marion.emailAddress);

  const queue = new ModerationQueuePage(page);

  await queue.goto();
  await queue.waitForRows();

  await reveal(page, 300);

  await say(
    page,
    'The moderator sees the reason and who gave it',
    'The only screen in the product that names a reporter.',
  );

  await expect(queue.flagged(SeededListings.drill)).toContainText(Members.priya.displayName);
  await beat(page, 1_600);

  await say(
    page,
    'Removing is confirmed; leaving it up is not',
    'The destructive decision is the one to mean.',
  );

  await queue.beginRemoving(SeededListings.drill);

  await expect(queue.removeDialog).toBeVisible();
  await beat(page, 1_600);

  await queue.confirmRemove.click();

  await expect(queue.flagged(SeededListings.drill)).toHaveCount(0);

  await say(
    page,
    'Off the board, and its owner is told',
    'The one notification a member cannot switch off.',
  );

  await beat(page, 1_400);

  // ── 8. Letting somebody in ───────────────────────────────────────────────────────────────────
  await chapter(
    page,
    'Eight',
    'Letting somebody in',
    'Barnabas is invitation only. This is the whole way in.',
  );

  const invite = new InviteSomeonePage(page);

  await invite.goto();

  await say(
    page,
    'A code, drawn from an alphabet nobody misreads',
    'No I, O, 0 or 1 — it is read aloud and typed by hand.',
  );

  await invite.issue.click();

  await expect(invite.code).toHaveText(/^[A-Z2-9]{8}$/);

  const code = (await invite.code.textContent())?.trim() ?? '';

  await beat(page, 1_800);

  // Start from nobody: joining is anonymous, and doing it while still signed in as the moderator
  // would be demonstrating something else entirely.
  await page.context().clearCookies();

  const join = new JoinPage(page);
  const profile = new CreateProfilePage(page);

  await join.goto();

  await say(
    page,
    'Redeeming it is anonymous',
    'Nobody is signed in. The code says which congregation.',
  );

  await join.redeem(code);

  await expect(profile.heading).toBeVisible();

  await say(
    page,
    'And it offers this parish’s neighbourhoods',
    'Not free text, and not another parish’s list.',
  );

  await profile.displayName.fill('Nora Almeida');
  await profile.emailAddress.fill('nora@example.com');
  await profile.reason.fill('Came to the newcomers’ lunch. I can drive, and I bake.');

  await beat(page, 1_200);

  await profile.join.click();

  await expect(new AwaitingApprovalPage(page).heading).toBeVisible();

  await say(
    page,
    'A code is not a way in on its own',
    'It creates a member awaiting approval.',
  );

  await signInQuickly(page, Members.marion.emailAddress);

  await queue.goto();
  await queue.waitForRows();

  await reveal(page, 420);

  await say(
    page,
    'The moderator sees who is waiting, and why',
    'The reason somebody gave is on this screen alone.',
  );

  await expect(queue.applicant('Nora Almeida')).toContainText('newcomers');
  await beat(page, 1_600);

  await queue.approve('Nora Almeida');

  await expect(queue.applicant('Nora Almeida')).toHaveCount(0);

  await say(
    page,
    'And she is in',
    'Status is read from the record each request, so approval lands on the next visit.',
  );

  await signInQuickly(page, 'nora@example.com');

  await board.goto();
  await board.waitForPlacards();

  await expect(board.heading).toHaveText("St. Aidan's board");
  await beat(page, 1_600);

  // ── 9. Finding people and things ─────────────────────────────────────────────────────────────
  await chapter(
    page,
    'Nine',
    'Finding things, and people',
    'A board is only useful if you can find what is on it.',
  );

  const search = new SearchPage(page);

  await search.goto();

  await say(
    page,
    'Search is scoped before it is run',
    'There is no term that reaches another parish.',
  );

  await search.searchFor('ladder');

  await expect(search.result(SeededListings.ladder)).toBeVisible();

  await reveal(page, 260);
  await beat(page, 1_600);

  const directory = new DirectoryPage(page);

  await directory.goto();

  await say(
    page,
    'The directory carries no email addresses',
    'The type describing another member has no field for one.',
  );

  await expect(directory.rows.first()).toBeVisible();

  await reveal(page, 300);
  await beat(page, 1_600);

  const settings = new ProfileSettingsPage(page);

  await settings.goto();

  await say(
    page,
    'Your own is a different matter',
    'A member can export everything held about them, or ask to be forgotten.',
  );

  await expect(settings.emailAddress).toBeVisible();
  await beat(page, 2_000);

  // ── Closing ──────────────────────────────────────────────────────────────────────────────────
  await chapter(
    page,
    'Barnabas',
    'Eighteen requirements. One hundred and twenty criteria.',
    'Every one named by an acceptance test — 306 against the API, 112 through the browser, none '
      + 'skipped. This recording is one of them, driving the same product.',
  );
});

/**
 * Signs in the way a member does, narrated.
 *
 * Shown in full once, because the passwordless flow is the part people most expect to be a
 * password box. Afterwards `signInQuickly` does the same thing without the commentary.
 */
async function signInSlowly(
  page: Page,
  emailAddress: string,
  caption: { heading: string; detail: string },
): Promise<void> {
  const signIn = new SignInPage(page);

  await page.context().clearCookies();
  await signIn.goto();

  await say(page, caption.heading, caption.detail);

  await signIn.askForALink(emailAddress);

  await expect(new CheckYourEmailPage(page).heading).toBeVisible();

  await say(
    page,
    'The link is on its way',
    'To a development outbox here; to their inbox in a deployment.',
  );

  await page.goto(await signInLinkFor(page, emailAddress));

  await expect(page).toHaveURL(/\/board$/);
}

/** The same journey, at the pace of somebody who has done it before. */
async function signInQuickly(page: Page, emailAddress: string): Promise<void> {
  const signIn = new SignInPage(page);

  await page.waitForLoadState('networkidle').catch(() => undefined);
  await page.context().clearCookies();

  await signIn.goto();
  await signIn.askForALink(emailAddress);

  await expect(new CheckYourEmailPage(page).heading).toBeVisible();

  await page.goto(await signInLinkFor(page, emailAddress));

  await expect(page).toHaveURL(/\/board$/);
}
