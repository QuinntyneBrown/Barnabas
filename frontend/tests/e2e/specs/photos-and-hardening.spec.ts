import { Page } from '@playwright/test';

import { BoardPage } from '../page-objects/board.page';
import {
  ChooseKindPage,
  ListingPostedPage,
  PostGivePage,
  PostHelpPage,
} from '../page-objects/post-listing.page';
import { ListingDetailPage } from '../page-objects/listing-detail.page';
import { aJpeg } from '../support/images';
import { Members } from '../support/members';
import { expect, test } from '../support/barnabas';

// Acceptance Test
// Traces to: L2-032, L2-097, L2-106
// Description: A member attaches a photograph to a goods listing and it appears on the board and
// on the listing, at a size chosen for each. Nothing a member typed executes, whatever they typed.
// The document the browser loads carries a policy that forbids inline script.

// L2-106 AC2: Given the board, when images below the fold are inspected, then they are lazily
// loaded.
// L2-032: and the photograph a member attached is the one that appears.
test('a photograph reaches the board, and the board loads it lazily', async ({
  page,
  board,
  signInAs,
}) => {
  await signInAs(Members.marion.emailAddress);

  await postWithAPhotoAsync(page, 'Cast iron frying pan');

  await board.goto();
  await board.waitForPlacards();

  const photo = board.placard('Cast iron frying pan').locator('img');

  await expect(photo).toHaveCount(1);

  // Lazy, so a mosaic that is mostly below the fold does not fetch thirty photographs on the
  // first paint. And dimensioned, so the ones that do arrive do not shove their neighbours down.
  await expect(photo).toHaveAttribute('loading', 'lazy');
  await expect(photo).toHaveAttribute('width', /\d+/);
  await expect(photo).toHaveAttribute('height', /\d+/);

  // L2-106 AC1: the board asks for the board's rendition, not the listing screen's.
  await expect(photo).toHaveAttribute('src', /\/photos\/[0-9a-f-]+\/board$/);

  // And it actually loaded.
  //
  // This is the assertion that matters, and the one this test first went without: the src pattern
  // and the loading attribute were both right while the picture itself was never fetched. The
  // API answers with a path of its own, the client is served from a different root, and the
  // browser resolved it against the wrong origin - so every listing showed its alt text. An
  // attribute is what the markup says; naturalWidth is what the browser got.
  await expect
    .poll(async () => photo.evaluate((image: HTMLImageElement) => image.naturalWidth))
    .toBeGreaterThan(0);

  // The listing screen asks for the other one.
  await board.open('Cast iron frying pan');

  const detail = new ListingDetailPage(page);

  await expect(detail.title).toHaveText('Cast iron frying pan');
  await expect(detail.photo).toHaveAttribute('src', /\/photos\/[0-9a-f-]+$/);

  await expect
    .poll(async () => detail.photo.evaluate((image: HTMLImageElement) => image.naturalWidth))
    .toBeGreaterThan(0);
});

// Help offers time rather than a thing, so its form does not ask for a photograph at all.
test('the help form asks for no photograph', async ({ page, signInAs }) => {
  await signInAs(Members.marion.emailAddress);

  const chooseKind = new ChooseKindPage(page);

  await chooseKind.goto();
  await chooseKind.chooseHelp();

  await expect(new PostHelpPage(page).heading).toBeVisible();
  await expect(new PostHelpPage(page).photo).toHaveCount(0);
});

// L2-097 AC2: Given a listing whose title contains a script tag, when another member views the
// board, then the text is displayed literally and no script executes.
test('a script tag in a title is read out as text and never runs', async ({
  page,
  board,
  signInAs,
}) => {
  const title = "Ladder <script>window.__barnabas_xss = true</script>";

  await signInAs(Members.marion.emailAddress);

  const chooseKind = new ChooseKindPage(page);
  const form = new PostGivePage(page);

  await chooseKind.goto();
  await chooseKind.chooseGive();

  await expect(form.heading).toBeVisible();

  await form.fill({ title, description: 'It is sound, and the brackets are part of the joke.' });
  await form.submit();

  await expect(new ListingPostedPage(page).heading).toBeVisible();

  // Read by somebody else, which is where the requirement points.
  await signInAs(Members.priya.emailAddress);

  await board.goto();
  await board.waitForPlacards();

  // Displayed literally: the angle brackets are characters on a placard, not markup.
  await expect(board.placard(title).locator('.placard__title')).toHaveText(title);

  // And nothing ran. The script would have set this had it been parsed as an element.
  const ran = await page.evaluate(() => '__barnabas_xss' in window);

  expect(ran).toBe(false);

  // Nor is there a script element carrying it, which is the other half of "not as markup".
  const injected = await page.locator('script').filter({ hasText: '__barnabas_xss' }).count();

  expect(injected).toBe(0);
});

// L2-097 AC3: Given any page, when its headers are inspected, then a Content-Security-Policy is
// present that forbids inline script.
test('the document carries a policy that forbids inline script', async ({ page }) => {
  const response = await page.goto('/');

  const policy = (await response!.headerValue('content-security-policy')) ?? '';

  expect(policy).toContain("script-src 'self'");

  // The half that matters. A policy naming script-src and then allowing inline would be present
  // and useless.
  expect(policy).not.toContain("script-src 'self' 'unsafe-inline'");
  expect(policy).toContain("object-src 'none'");
  expect(policy).toContain("frame-ancestors 'none'");
});

/** Posts a Give listing with a photograph attached, and waits for the product to confirm it. */
async function postWithAPhotoAsync(page: Page, title: string): Promise<void> {
  const chooseKind = new ChooseKindPage(page);
  const form = new PostGivePage(page);

  await chooseKind.goto();
  await chooseKind.chooseGive();

  await expect(form.heading).toBeVisible();

  await form.fill({ title, description: 'Seasoned, heavy, and surplus to requirements.' });

  // A real JPEG, made rather than checked in — a binary fixture is a file nobody can read in a
  // diff.
  await form.photo.setInputFiles({
    name: 'pan.jpg',
    mimeType: 'image/jpeg',
    buffer: aJpeg(),
  });

  // The local preview appears before anything is sent, so somebody knows they picked the right
  // one before they post.
  await expect(form.photoPreview).toBeVisible();

  await form.submit();

  // Waits for the product's own confirmation rather than navigating with the upload in flight.
  await expect(new ListingPostedPage(page).heading).toBeVisible();
}
