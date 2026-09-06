import { ListingDetailPage } from '../page-objects/listing-detail.page';
import { NotificationsPage } from '../page-objects/notifications.page';
import { ProfileSettingsPage } from '../page-objects/profile.page';
import { RequestLendPage, RequestSentPage } from '../page-objects/request.page';
import { ShellPage } from '../page-objects/shell.page';
import { Members, SeededListings } from '../support/members';
import { expect, test } from '../support/barnabas';

// Acceptance Test
// Traces to: L2-073, L2-074, L2-075
// Description: A member is told what has happened, sees how much is waiting from wherever they
// are, and can say what they would rather not hear about.

/** Priya asks to borrow the ladder, which is what gives Marion something to be told about. */
async function priyaAsks(page: import('@playwright/test').Page): Promise<void> {
  const detail = new ListingDetailPage(page);
  const request = new RequestLendPage(page);

  await page.goto('/board');
  await page.getByRole('link', { name: SeededListings.ladder }).first().click();
  await detail.ask();

  await request.fillAndSend({
    message: 'Painting the back bedroom the weekend after next.',
    pickupOn: '2026-09-13',
    returnBy: '2026-09-20',
  });

  // Waited for rather than assumed. Signing in as somebody else while the request is in flight
  // aborts it, and the notification that never got written reads later as a product defect
  // rather than as a test racing itself.
  await expect(new RequestSentPage(page).heading).toBeVisible();
}

// L2-073 AC2: Given a member with unread notifications, when they view any screen at any viewport
// width, then the count is visible.
// L2-073 AC3: Given a member who marks all read, when the screen updates, then the count is
// absent.
test('the unread count follows the member around, and clears', async ({ page, signInAs }) => {
  await signInAs(Members.priya.emailAddress);
  await priyaAsks(page);

  await signInAs(Members.marion.emailAddress);

  const shell = new ShellPage(page);
  const notifications = new NotificationsPage(page);

  // Visible from the board, which is where she landed.
  await expect(shell.unreadCount).toHaveText('1');

  // And from everywhere else, because the bell is in the header at every band.
  for (const screen of ['/my-listings', '/directory', '/you']) {
    await page.goto(screen);
    await expect(shell.unreadCount).toHaveText('1');
  }

  // Narrow, where the header nav is replaced by the bottom bar and the bell stays put.
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto('/board');
  await expect(shell.unreadCount).toBeVisible();

  await page.setViewportSize({ width: 1280, height: 900 });

  await notifications.goto();
  await notifications.markAllRead.click();

  await expect(shell.unreadCount).toHaveCount(0);
});

// L2-074 AC1: Given a member's notification list, when each notification is inspected, then every
// one leads to the listing, request, or thread it concerns.
// L2-074 AC2: and the member it names opens their profile.
test('every notification leads somewhere, and names somebody you can look up', async ({
  page,
  signInAs,
}) => {
  await signInAs(Members.priya.emailAddress);
  await priyaAsks(page);

  await signInAs(Members.marion.emailAddress);

  const notifications = new NotificationsPage(page);

  await notifications.goto();
  await notifications.waitForRows();

  const rows = await notifications.rows.count();

  expect(rows).toBeGreaterThan(0);

  for (let index = 0; index < rows; index += 1) {
    await expect(notifications.rows.nth(index).locator('.list-row__title')).toHaveAttribute(
      'href',
      /.+/,
    );
  }

  // The requester's name opens their profile.
  await notifications.member(Members.priya.displayName).first().click();

  await expect(page.getByRole('heading', { name: Members.priya.displayName, level: 1 })).toBeVisible();
});

// L2-075 AC3: Given profile and settings, when the member changes a notification preference and
// saves, then a confirmation is shown and the preference persists on reload.
test('a member says what they would rather not hear about, and it sticks', async ({
  page,
  signInAs,
}) => {
  await signInAs(Members.marion.emailAddress);

  const settings = new ProfileSettingsPage(page);
  const notifications = new NotificationsPage(page);

  await settings.goto();

  await notifications.preference('RequestReceived').uncheck();

  await expect(settings.saved).toBeVisible();

  await page.reload();

  await expect(notifications.preference('RequestReceived')).not.toBeChecked();

  // And nothing is recorded for it. Turned off means never created, not hidden.
  await signInAs(Members.priya.emailAddress);
  await priyaAsks(page);

  await signInAs(Members.marion.emailAddress);

  const shell = new ShellPage(page);

  await expect(shell.unreadCount).toHaveCount(0);
});
