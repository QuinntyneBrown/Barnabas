import { ListingDetailPage } from '../page-objects/listing-detail.page';
import {
  ChooseKindPage,
  ListingPostedPage,
  PostGivePage,
  PostHelpPage,
  PostSellPage,
} from '../page-objects/post-listing.page';
import {
  RequestGivePage,
  RequestHelpPage,
  RequestSellPage,
} from '../page-objects/request.page';
import { Members } from '../support/members';
import { expect, test } from '../support/barnabas';

// Acceptance Test
// Traces to: L2-055, L2-056, L2-057, L2-063
// Description: Each kind is asked for in its own words, on a form carrying only the terms that
// kind needs. None of the four takes payment, delivery, or a deposit.

/** Marion posts one of each kind, so Priya has something of every sort to ask for. */
async function marionPosts(page: import('@playwright/test').Page, kind: 'give' | 'sell' | 'help') {
  const chooseKind = new ChooseKindPage(page);

  await chooseKind.goto();

  if (kind === 'give') {
    await chooseKind.chooseGive();

    const form = new PostGivePage(page);

    await form.fill({ title: 'Wooden high chair', description: 'Our youngest has outgrown it.' });
    await form.submit();
  } else if (kind === 'sell') {
    await chooseKind.chooseSell();

    const form = new PostSellPage(page);

    await form.fill({ title: 'Raleigh three-speed', description: 'Rides well.', price: '45' });
    await form.submit();
  } else {
    await chooseKind.chooseHelp();

    const form = new PostHelpPage(page);

    await form.fill({ title: 'Lifts to appointments', description: 'Happy to drive locally.' });
    await form.addWindow.click();
    await form.dayOf(0).selectOption('Tuesday');
    await form.dayOf(1).selectOption('Thursday');
    await form.submit();
  }

  await expect(new ListingPostedPage(page).heading).toBeVisible();
}

// L2-055 AC2: Given a Give listing, when a member chooses to request it, then the action reads
// "Request this" and the form offers no return date.
test('a gift is asked for in its own words, with no return date', async ({ page, board, signInAs }) => {
  await signInAs(Members.marion.emailAddress);
  await marionPosts(page, 'give');

  await signInAs(Members.priya.emailAddress);

  const detail = new ListingDetailPage(page);
  const request = new RequestGivePage(page);

  await board.goto();
  await board.open('Wooden high chair');

  await expect(detail.askAction).toHaveText('Request this');
  await detail.ask();

  await expect(request.heading).toBeVisible();
  await expect(request.send).toHaveText('Request this');
  await expect(request.pickupAt).toBeVisible();

  // Absent, not hidden: ownership transfers, so there is nothing to bring back.
  await expect(request.returnBy).toHaveCount(0);
});

// L2-056 AC2: Given a Sell listing priced 45, when a member opens the request form, then the
// action reads "Request to buy" and the form states the price is paid in person.
test('a sale is asked for in its own words, and says where it is paid', async ({
  page,
  board,
  signInAs,
}) => {
  await signInAs(Members.marion.emailAddress);
  await marionPosts(page, 'sell');

  await signInAs(Members.priya.emailAddress);

  const detail = new ListingDetailPage(page);
  const request = new RequestSellPage(page);

  await board.goto();
  await board.open('Raleigh three-speed');

  await expect(detail.askAction).toHaveText('Request to buy');
  await detail.ask();

  await expect(request.heading).toBeVisible();
  await expect(request.send).toHaveText('Request to buy');

  // The price is stated, and so is the fact that Barnabas has nothing to do with collecting it.
  await expect(request.paymentNote).toContainText('45');
  await expect(request.paymentNote).toBeVisible();
});

// L2-057 AC3: Given a Help listing, when a member opens the request form, then the action reads
// "Request this help" and only the declared windows are offered.
test('help is asked for in one of the windows it declared', async ({ page, board, signInAs }) => {
  await signInAs(Members.marion.emailAddress);
  await marionPosts(page, 'help');

  await signInAs(Members.priya.emailAddress);

  const detail = new ListingDetailPage(page);
  const request = new RequestHelpPage(page);

  await board.goto();
  await board.open('Lifts to appointments');

  await expect(detail.askAction).toHaveText('Request this help');
  await detail.ask();

  await expect(request.heading).toBeVisible();
  await expect(request.send).toHaveText('Request this help');

  // Two declared, two offered, and no way to name an hour the owner never offered.
  await expect(request.windows).toHaveCount(2);
  await expect(request.windowLabels.first()).toContainText('Tuesday');
  await expect(request.windowLabels.last()).toContainText('Thursday');

  // Help is time, not a thing to collect.
  await expect(request.pickupAt).toHaveCount(0);
});

// L2-063 AC2: Given a request form of any kind, when it is rendered, then it contains no
// payment, delivery, or deposit field.
test('no request form takes payment, delivery, or a deposit', async ({ page, board, signInAs }) => {
  await signInAs(Members.marion.emailAddress);

  await marionPosts(page, 'give');
  await marionPosts(page, 'sell');
  await marionPosts(page, 'help');

  await signInAs(Members.priya.emailAddress);

  const detail = new ListingDetailPage(page);

  for (const [title, form] of [
    ['Wooden high chair', new RequestGivePage(page)],
    ['Raleigh three-speed', new RequestSellPage(page)],
    ['Lifts to appointments', new RequestHelpPage(page)],
  ] as const) {
    await board.goto();
    await board.open(title);
    await detail.ask();

    await expect(form.message).toBeVisible();

    // Barnabas brokers the introduction and nothing beyond it. Read as absences, because a
    // disabled or hidden field would still be a field that exists.
    await expect(form.payment).toHaveCount(0);
    await expect(form.delivery).toHaveCount(0);
  }
});
