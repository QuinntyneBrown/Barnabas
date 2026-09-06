import { BoardPage } from '../page-objects/board.page';
import { ListingDetailPage } from '../page-objects/listing-detail.page';
import {
  ChooseKindPage,
  ListingPostedPage,
  PostGivePage,
  PostHelpPage,
  PostSellPage,
} from '../page-objects/post-listing.page';
import { Members } from '../support/members';
import { expect, test } from '../support/barnabas';

// Acceptance Test
// Traces to: L2-026, L2-028, L2-029, L2-030, L2-034, L2-037
// Description: All four kinds can be posted, each form asks only for what its kind needs, and
// each kind is identified on the board in its own words. A sale says where the money changes
// hands; an offer of time says when it is on offer.

test.beforeEach(async ({ signInAs }) => {
  await signInAs(Members.marion.emailAddress);
});

// L2-026 AC1: Given a member posting a listing, when they open the post flow, then all four
// kinds are offered and no detail fields are shown.
test('posting begins with four kinds and no detail fields', async ({ page }) => {
  const chooseKind = new ChooseKindPage(page);

  await chooseKind.goto();

  await expect(chooseKind.heading).toBeVisible();
  await expect(chooseKind.kinds).toHaveCount(4);

  await expect(chooseKind.lend).toBeVisible();
  await expect(chooseKind.give).toBeVisible();
  await expect(chooseKind.sell).toBeVisible();
  await expect(chooseKind.help).toBeVisible();

  // Nothing to fill in yet. The kind is chosen before any detail is entered, so it can never be
  // defaulted underneath a half-completed form.
  await expect(chooseKind.detailFields).toHaveCount(0);
});

// L2-026 AC3: Given the kind chooser, when the member chooses Sell, then the Sell detail form is
// shown and it offers a price field.
test('choosing sell shows the sell form with a price field', async ({ page }) => {
  const chooseKind = new ChooseKindPage(page);
  const postSell = new PostSellPage(page);

  await chooseKind.goto();
  await chooseKind.chooseSell();

  await expect(postSell.heading).toBeVisible();
  await expect(postSell.price).toBeVisible();
  await expect(postSell.condition).toBeVisible();
});

// L2-028 AC3: Given a Give listing on the board, when it is displayed, then it is identified as
// being given away rather than sold.
test('a give listing is identified as being given away', async ({ page, board }) => {
  const chooseKind = new ChooseKindPage(page);
  const postGive = new PostGivePage(page);

  await chooseKind.goto();
  await chooseKind.chooseGive();

  // Absent, not hidden. A gift is free, and it is not coming back.
  await expect(postGive.price).toHaveCount(0);
  await expect(postGive.returnBy).toHaveCount(0);

  await postGive.fill({
    title: 'Wooden high chair',
    description: 'Our youngest has outgrown it. Wipes clean.',
  });
  await postGive.submit();

  await expect(new ListingPostedPage(page).heading).toBeVisible();

  await board.goto();

  await expect(board.kindOf('Wooden high chair')).toHaveText('Giving away');

  // No price, because a gift has none. Read as an absence rather than as an empty string.
  await expect(board.priceOf('Wooden high chair')).toHaveCount(0);
});

// L2-029 AC4: Given a Sell listing on the board, when it is displayed, then its price is shown.
// L2-034 AC2: Given a Sell listing, when it is viewed, then the price is accompanied by a
// statement that payment is arranged directly between members.
test('a sell listing shows its price, and says where it is paid', async ({ page, board }) => {
  const chooseKind = new ChooseKindPage(page);
  const postSell = new PostSellPage(page);
  const detail = new ListingDetailPage(page);

  await chooseKind.goto();
  await chooseKind.chooseSell();

  await postSell.fill({
    title: 'Raleigh three-speed',
    description: 'Rides well, new tyres last spring.',
    price: '45',
  });
  await postSell.submit();

  await expect(new ListingPostedPage(page).heading).toBeVisible();

  await board.goto();

  await expect(board.kindOf('Raleigh three-speed')).toHaveText('For sale');
  await expect(board.priceOf('Raleigh three-speed')).toContainText('45');

  await board.open('Raleigh three-speed');

  await expect(detail.price).toContainText('45');

  // The figure never stands on its own. Barnabas brokers the introduction; the money changes
  // hands between the two members.
  await expect(detail.paymentNote).toBeVisible();
});

// L2-030 AC4: Given a Help listing on the board, when it is displayed, then its availability is
// shown and no photo placeholder is present.
test('a help listing shows its availability and carries no photo', async ({ page, board }) => {
  const chooseKind = new ChooseKindPage(page);
  const postHelp = new PostHelpPage(page);

  await chooseKind.goto();
  await chooseKind.chooseHelp();

  // No price field: time is not sold here.
  await expect(postHelp.price).toHaveCount(0);

  // One window to begin with, because an offer with none cannot be posted.
  await expect(postHelp.windows).toHaveCount(1);

  await postHelp.fill({
    title: 'Lifts to appointments',
    description: 'Happy to drive within the east end.',
  });

  await postHelp.addWindow.click();
  await expect(postHelp.windows).toHaveCount(2);

  await postHelp.dayOf(0).selectOption('Tuesday');
  await postHelp.dayOf(1).selectOption('Thursday');

  await postHelp.submit();

  await expect(new ListingPostedPage(page).heading).toBeVisible();

  await board.goto();

  await expect(board.kindOf('Lifts to appointments')).toHaveText('Offers of help');
  await expect(board.metaOf('Lifts to appointments').first()).toContainText('Tuesday');

  // Nothing to photograph, so nothing pretending there is.
  await expect(board.imagesOn('Lifts to appointments')).toHaveCount(0);
});

// L2-037 AC5: Given an owner's own Sell listing, when they view it, then the close-out action
// reads "Mark as sold", not generic wording.
test('an owner closes out a sale in the words of its kind', async ({ page, board }) => {
  const chooseKind = new ChooseKindPage(page);
  const postSell = new PostSellPage(page);
  const detail = new ListingDetailPage(page);

  await chooseKind.goto();
  await chooseKind.chooseSell();

  await postSell.fill({
    title: 'Raleigh three-speed',
    description: 'Rides well.',
    price: '45',
  });
  await postSell.submit();

  await new ListingPostedPage(page).viewYourListing.click();

  // "Mark as sold", because a sale is sold. A neutral "close out" would lose the distinction the
  // four kinds exist to carry.
  await expect(detail.closeOut).toHaveText('Mark as sold');

  // The owner sees what they can do with it, and no way to ask themselves for it.
  await expect(detail.askAction).toHaveCount(0);

  await board.goto();

  await board.open('Raleigh three-speed');
  await expect(detail.closeOut).toHaveText('Mark as sold');
});
