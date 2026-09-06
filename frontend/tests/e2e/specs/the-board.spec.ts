import { ListingDetailPage } from '../page-objects/listing-detail.page';
import { Members, SeededListings } from '../support/members';
import { expect, test } from '../support/barnabas';

// Acceptance Test
// Traces to: L2-042, L2-044
// Description: The board shows the congregation's active listings, each opening its own screen,
// and each carrying its kind in words rather than only in the colour of its field.

test.beforeEach(async ({ signInAs }) => {
  await signInAs(Members.priya.emailAddress);
});

// L2-042 AC3: Given a member on the board, when they select a listing, then that listing's
// detail screen is shown.
test('a placard opens its listing', async ({ page, board }) => {
  const detail = new ListingDetailPage(page);

  await board.goto();
  await board.open(SeededListings.ladder);

  await expect(detail.title).toHaveText(SeededListings.ladder);
  await expect(page).toHaveURL(/\/listings\//);
});

// L2-044 AC1: Given any listing on the board, when it is rendered, then its kind is present as
// text within the listing.
test('every placard names its kind in words', async ({ board }) => {
  await board.goto();
  await board.waitForPlacards();

  const placards = await board.placards.count();

  expect(placards).toBeGreaterThan(0);

  for (let index = 0; index < placards; index += 1) {
    const kind = board.placards.nth(index).locator('.placard__kind');

    await expect(kind).toBeVisible();
    await expect(kind).toHaveText(/^(Lending|Giving away|For sale|Offers of help)$/);
  }
});

// L2-044 AC2: Given the board rendered with colour removed, when a listing is inspected, then its
// kind remains determinable.
//
// Colour is removed for real rather than assumed away: the page is rendered in greyscale, and
// the kind is still read off the placard. A test that only checked the text was present would
// pass even if the text were painted the same colour as the field behind it.
test('a listing keeps its kind when colour is removed', async ({ page, board }) => {
  await board.goto();

  await page.addStyleTag({ content: 'html { filter: grayscale(100%) !important; }' });

  await expect(board.kindOf(SeededListings.ladder)).toHaveText('Lending');
  await expect(board.kindOf(SeededListings.drill)).toHaveText('For sale');
});

// L2-042 AC1 in its visible half: a placard carries what a member needs to decide whether to
// open it.
test('a placard carries the title, the owner, and the neighbourhood', async ({ board }) => {
  await board.goto();

  const ladder = board.placard(SeededListings.ladder);

  await expect(ladder).toContainText(SeededListings.ladder);
  await expect(ladder).toContainText(Members.marion.displayName);
  await expect(ladder).toContainText(Members.marion.neighbourhood);
});
