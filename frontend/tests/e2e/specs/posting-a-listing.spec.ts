import { BoardPage } from '../page-objects/board.page';
import { ListingDetailPage } from '../page-objects/listing-detail.page';
import { ChooseKindPage, ListingPostedPage, PostLendPage } from '../page-objects/post-listing.page';
import { Members } from '../support/members';
import { expect, test } from '../support/barnabas';

// Acceptance Test
// Traces to: L2-026, L2-027, L2-031, L2-033
// Description: The Lend form asks for what a loan needs and no more, marks what it refuses, and
// confirms a posted listing with the three ways onward.

test.beforeEach(async ({ signInAs }) => {
  await signInAs(Members.marion.emailAddress);
});

// L2-027 AC3: Given the Lend form, when it is displayed, then no price field is present.
// L2-026 AC2: Given the kind chooser, when the member chooses Lend, then the Lend detail form is
// shown and it offers a return-by field.
test('the lend form has no price field', async ({ page }) => {
  const chooseKind = new ChooseKindPage(page);
  const postLend = new PostLendPage(page);

  await chooseKind.goto();
  await chooseKind.chooseLend();

  await expect(postLend.heading).toBeVisible();
  await expect(postLend.title).toBeVisible();
  await expect(postLend.returnBy).toBeVisible();

  // Absent, not hidden. A loan is not a sale.
  await expect(postLend.price).toHaveCount(0);
});

// L2-031 AC4: Given a member submitting an incomplete listing, when they submit, then each
// invalid field is marked and the first is focused.
test('an incomplete listing marks each faulty field and focuses the first', async ({ page }) => {
  const postLend = new PostLendPage(page);

  await page.goto('/post/lend');

  // Everything left empty, so the title and the description are both at fault.
  await postLend.submit();

  await expect(postLend.fieldError('title')).toBeVisible();
  await expect(postLend.fieldError('description')).toBeVisible();

  await expect(postLend.title).toHaveAttribute('aria-invalid', 'true');
  await expect(postLend.description).toHaveAttribute('aria-invalid', 'true');

  // The first one, so a member using a screen reader is told, and one on a phone is not left
  // scrolling to find out what happened.
  await expect(postLend.title).toBeFocused();
});

// What was typed survives a refusal.
test('a refused listing keeps what the member typed', async ({ page }) => {
  const postLend = new PostLendPage(page);

  await page.goto('/post/lend');

  await postLend.description.fill('Sound, both spreaders lock.');
  await postLend.submit();

  await expect(postLend.description).toHaveValue('Sound, both spreaders lock.');
});

// L2-033 AC1: Given a completed listing form, when the member posts it, then a confirmation
// screen states the listing is on the board.
// L2-033 AC2: it offers to view the listing, post another, and return to the board.
// L2-033 AC3: choosing to view the listing shows the listing they just created.
test('posting confirms the listing is live and offers the three ways onward', async ({ page }) => {
  const postLend = new PostLendPage(page);
  const posted = new ListingPostedPage(page);
  const detail = new ListingDetailPage(page);
  const board = new BoardPage(page);

  await page.goto('/post/lend');

  await postLend.fill({
    title: 'Wheelbarrow',
    description: 'Pneumatic tyre, recently pumped.',
    returnBy: '2026-10-01',
  });
  await postLend.submit();

  await expect(posted.heading).toBeVisible();
  await expect(posted.viewYourListing).toBeVisible();
  await expect(posted.postAnother).toBeVisible();
  await expect(posted.backToTheBoard).toBeVisible();

  await posted.viewYourListing.click();
  await expect(detail.title).toHaveText('Wheelbarrow');

  await board.goto();
  await expect(board.placard('Wheelbarrow')).toBeVisible();
});
