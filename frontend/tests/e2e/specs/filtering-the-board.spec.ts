import {
  ChooseKindPage,
  ListingPostedPage,
  PostGivePage,
  PostHelpPage,
} from '../page-objects/post-listing.page';
import { Members, SeededListings } from '../support/members';
import { expect, test } from '../support/barnabas';

// Acceptance Test
// Traces to: L2-043
// Description: The board narrows to one kind, says which one is in force, and can be widened
// again. The filter is applied by the API rather than by hiding placards, so the counts on the
// chips describe the whole board rather than what happens to be on screen.

test.beforeEach(async ({ signInAs }) => {
  await signInAs(Members.marion.emailAddress);
});

// L2-043 AC2: Given the board filtered to one kind, when it is displayed, then the active filter
// is evident.
test('filtering to one kind shows only that kind, and says which', async ({ page, board }) => {
  const chooseKind = new ChooseKindPage(page);
  const postGive = new PostGivePage(page);

  // A gift, so the board holds more than one kind and filtering has something to do.
  await chooseKind.goto();
  await chooseKind.chooseGive();
  await postGive.fill({ title: 'Wooden high chair', description: 'Our youngest has outgrown it.' });
  await postGive.submit();
  await expect(new ListingPostedPage(page).heading).toBeVisible();

  await board.goto();
  await board.waitForPlacards();

  // Everything, to begin with.
  await expect(board.activeFilter).toContainText('Everything');
  await expect(board.placard(SeededListings.ladder)).toBeVisible();
  await expect(board.placard('Wooden high chair')).toBeVisible();

  await board.filterBy('Giving away');

  // The gift stays; the ladder goes. The filter is applied server-side, so this is the board
  // being narrower rather than placards being hidden.
  await expect(board.placard('Wooden high chair')).toBeVisible();
  await expect(board.placard(SeededListings.ladder)).toHaveCount(0);

  // Evident to a reader who cannot see which chip is darker, as well as to one who can.
  await expect(board.activeFilter).toContainText('Giving away');
});

// L2-043 AC3: Given a filtered board, when the member clears the filter, then listings of all
// four kinds are shown.
test('a filter can be cleared, and all four kinds come back', async ({ page, board }) => {
  const chooseKind = new ChooseKindPage(page);

  // The seed holds a Lend and a Sell. A gift and an offer of help make the board all four, which
  // is what the criterion actually asks about.
  await chooseKind.goto();
  await chooseKind.chooseGive();

  const postGive = new PostGivePage(page);

  await postGive.fill({ title: 'Wooden high chair', description: 'Our youngest has outgrown it.' });
  await postGive.submit();
  await expect(new ListingPostedPage(page).heading).toBeVisible();

  await chooseKind.goto();
  await chooseKind.chooseHelp();

  const postHelp = new PostHelpPage(page);

  await postHelp.fill({ title: 'Lifts to appointments', description: 'Happy to drive locally.' });
  await postHelp.submit();
  await expect(new ListingPostedPage(page).heading).toBeVisible();

  await board.goto();
  await board.waitForPlacards();

  await board.filterBy('For sale');

  await expect(board.activeFilter).toContainText('For sale');
  await expect(board.placard(SeededListings.drill)).toBeVisible();
  await expect(board.placard(SeededListings.ladder)).toHaveCount(0);

  await board.filterBy('Everything');

  await expect(board.activeFilter).toContainText('Everything');

  // All four kinds, not merely more than one.
  await expect(board.placard(SeededListings.ladder)).toBeVisible();
  await expect(board.placard(SeededListings.drill)).toBeVisible();
  await expect(board.placard('Wooden high chair')).toBeVisible();
  await expect(board.placard('Lifts to appointments')).toBeVisible();
});

// A filtered board with nothing in it says so in its own terms, rather than claiming the whole
// board is empty. Not an acceptance criterion of its own; it is the honest reading of L2-045,
// which is about a board with nothing posted at all.
test('a kind with nothing posted says so without claiming the board is empty', async ({ board }) => {
  await board.goto();
  await board.waitForPlacards();

  await board.filterBy('Offers of help');

  await expect(board.nothingOfThatKind).toBeVisible();

  // And not the other message: the congregation has posted plenty, just none of this kind.
  await expect(board.emptyBoard).toHaveCount(0);
});
