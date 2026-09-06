import { MyListingsPage } from '../page-objects/my-listings.page';
import { Members, SeededListings } from '../support/members';
import { expect, test } from '../support/barnabas';

// Acceptance Test
// Traces to: L2-045, L2-046, L2-047
// Description: A board that is empty, one still arriving, and one that could not be fetched are
// three ordinary states rather than three kinds of nothing. Each says what it is, and the
// failure offers a way to try again.

test.beforeEach(async ({ signInAs }) => {
  await signInAs(Members.marion.emailAddress);
});

// L2-045 AC2: Given an empty board, when it loads, then it explains that nothing is posted yet
// and offers to post a listing.
test('an empty board invites the first listing', async ({ page, board }) => {
  const mine = new MyListingsPage(page);

  // Emptied through the product rather than by editing the database, so what is being tested is
  // the board a congregation would actually arrive at.
  await mine.goto();

  for (const title of [SeededListings.ladder, SeededListings.drill]) {
    await mine.takeOffTheBoard(title).click();
    await mine.confirmIn('Take this off the board?', 'Take it off').click();
    await expect(mine.row(title)).toHaveCount(0);
  }

  await board.goto();

  await expect(board.emptyBoard).toBeVisible();
  await expect(board.postTheFirstListing).toBeVisible();

  // Not a failure. A congregation that has posted nothing has not encountered a problem.
  await expect(board.failed).toHaveCount(0);
});

// L2-046 AC1: Given the board is loading, when the screen is inspected, then a live region
// announces that listings are loading.
// L2-046 AC2: Given the board has loaded, when the screen is inspected, then the loading
// announcement is gone.
test('the wait is announced, and the announcement goes when it is over', async ({ page, board }) => {
  let release: () => void = () => undefined;

  const held = new Promise<void>((resolve) => {
    release = resolve;
  });

  // Held open so the loading state is a state rather than a flicker. Announced as well as drawn:
  // a member who cannot see the skeletons is the reason the live region exists.
  await page.route('**/api/board*', async (route) => {
    await held;
    await route.continue();
  });

  await board.goto();

  await expect(board.loadingAnnouncement).toHaveText('Loading listings.');

  release();

  await board.waitForPlacards();

  await expect(board.loadingAnnouncement).toHaveCount(0);
});

// L2-047 AC1: Given the board request fails, when the screen renders, then it states that the
// board failed to load and offers to try again.
// L2-047 AC2: Given the failure screen, when the member chooses to try again, then the board is
// requested again.
test('a board that will not load says so, and tries again', async ({ page, board }) => {
  let attempts = 0;

  await page.route('**/api/board*', async (route) => {
    attempts += 1;

    // The first attempt fails, the second is allowed through, so "try again" is shown to do
    // something rather than merely to exist.
    if (attempts === 1) {
      await route.fulfill({ status: 500, contentType: 'application/json', body: '{}' });

      return;
    }

    await route.continue();
  });

  await board.goto();

  await expect(board.failed).toBeVisible();
  await expect(board.tryAgain).toBeVisible();

  await board.tryAgain.click();

  await board.waitForPlacards();

  await expect(board.placard(SeededListings.ladder)).toBeVisible();
  await expect(board.failed).toHaveCount(0);

  expect(attempts).toBe(2);
});

// The catch-all used to redirect to the landing page, which reads as being signed out. Not an
// acceptance criterion of its own; docs/mocks/404.html was a screen nobody was accountable for.
test('an address that leads nowhere says so', async ({ page }) => {
  await page.goto('/no-such-place');

  await expect(page.getByRole('heading', { name: 'Not found' })).toBeVisible();
});
