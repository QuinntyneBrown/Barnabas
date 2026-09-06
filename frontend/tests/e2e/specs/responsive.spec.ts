import { ShellPage } from '../page-objects/shell.page';
import { Members } from '../support/members';
import { expect, test } from '../support/barnabas';

// Acceptance Test
// Traces to: L2-108, L2-109
// Description: Every screen lays out correctly at each of the five bands, and all five primary
// destinations - and the notification count - are reachable at every one of them.

/** The five widths L2-108 names, at their boundaries. */
const bands = [
  { name: 'extra small', width: 375 },
  { name: 'small', width: 576 },
  { name: 'medium', width: 768 },
  { name: 'large', width: 992 },
  { name: 'extra large', width: 1200 },
];

/** One screen of each shape the product has. */
const screens = ['/board', '/my-listings', '/inbox/requests', '/post/lend', '/you'];

test.beforeEach(async ({ signInAs }) => {
  await signInAs(Members.priya.emailAddress);
});

for (const band of bands) {
  // L2-108 AC1: Given any screen at each band, when it renders, then no horizontal scrolling of
  // the page body occurs.
  test(`no screen scrolls sideways at ${band.name}`, async ({ page }) => {
    await page.setViewportSize({ width: band.width, height: 900 });

    for (const screen of screens) {
      await page.goto(screen);

      const overflow = await page.evaluate(
        () => document.documentElement.scrollWidth - document.documentElement.clientWidth,
      );

      expect(overflow, `${screen} scrolls sideways at ${band.width}px`).toBeLessThanOrEqual(1);
    }
  });

  // L2-109 AC1 and AC2: all five destinations are reachable at every width, in the same order.
  // L2-109 AC3: and so is the notification count.
  test(`all five destinations are reachable at ${band.name}`, async ({ page }) => {
    const shell = new ShellPage(page);

    await page.setViewportSize({ width: band.width, height: 900 });
    await page.goto('/board');

    // Exactly one navigation is exposed, whichever the stylesheet is showing. Both are in the
    // document at every width, and the other is inert.
    await expect(shell.navigation).toHaveCount(1);

    await expect(shell.navigation.getByRole('link')).toHaveText([
      'Board',
      'Search',
      'Post',
      'Inbox',
      'You',
    ]);

    await expect(shell.notifications).toBeVisible();
  });
}

// L2-108 AC3: Given the board at 375 pixels, when it renders, then listings are readable in a
// single column.
test('the board is a single column on a phone', async ({ page, board }) => {
  await page.setViewportSize({ width: 375, height: 900 });
  await board.goto();

  expect(await columnsInTheMosaic(page)).toBe(1);
});

// L2-108 AC4: Given the board at 1200 pixels, when it renders, then at least three listings are
// shown per row.
test('the board shows at least three listings per row on a wide screen', async ({ page, board, signInAs }) => {
  // The seeded board holds two listings, and three per row cannot be observed with two of them.
  await signInAs(Members.marion.emailAddress);

  for (const title of ['Wheelbarrow', 'Extending ladder', 'Garden shears']) {
    await page.goto('/post/lend');
    await page.getByLabel('Title').fill(title);
    await page.getByLabel('Description').fill('Kept in the garage.');
    await page.getByLabel('Back with you by').fill('2026-10-01');
    await page.getByRole('button', { name: 'Post listing' }).click();
    await expect(page.getByRole('heading', { name: 'Your listing is on the board' })).toBeVisible();
  }

  await page.setViewportSize({ width: 1200, height: 900 });
  await board.goto();

  await expect(board.placards).toHaveCount(5);

  expect(await columnsInTheMosaic(page)).toBeGreaterThanOrEqual(3);
});

/** How many placards share the top edge of the first one, which is the width of a row. */
async function columnsInTheMosaic(page: import('@playwright/test').Page): Promise<number> {
  return page.evaluate(() => {
    const placards = Array.from(document.querySelectorAll('.mosaic .placard'));

    if (placards.length === 0) {
      return 0;
    }

    const top = placards[0].getBoundingClientRect().top;

    return placards.filter((placard) => Math.abs(placard.getBoundingClientRect().top - top) < 2).length;
  });
}
