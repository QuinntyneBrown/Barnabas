import {
  DirectoryPage,
  MemberProfilePage,
  ProfileSettingsPage,
} from '../page-objects/profile.page';
import { Members, SeededListings } from '../support/members';
import { expect, test } from '../support/barnabas';

// Acceptance Test
// Traces to: L2-020, L2-021, L2-022, L2-023, L2-024, L2-025, L2-069, L2-076, L2-078
// Description: A member keeps their own profile, reads another's, and finds people through the
// directory. Leaving is confirmed and can be declined.

// L2-020 AC2: Given a signed-in member, when they open profile and settings, then their current
// name, neighbourhood, description, and help tags are shown populated.
// L2-022 AC2: every neighbourhood option belongs to their congregation.
test('profile and settings opens filled in, from this congregation', async ({ page, signInAs }) => {
  await signInAs(Members.priya.emailAddress);

  const settings = new ProfileSettingsPage(page);

  await settings.goto();

  await expect(settings.heading).toBeVisible();
  await expect(settings.displayName).toHaveValue(Members.priya.displayName);
  await expect(settings.neighbourhood).toHaveValue(Members.priya.neighbourhood);

  // St. Aidan's list, and not St. Brigid's. The options come back with the profile rather than
  // being written into the form.
  await expect(settings.neighbourhoodOptions.first()).toHaveText('Riverdale');
  await expect(settings.neighbourhoodOptions).not.toHaveText([/Parkdale/]);

  // Shown, and not editable: it is the only way back into the product.
  await expect(settings.emailAddress).toHaveValue(Members.priya.emailAddress);
  await expect(settings.emailAddress).toBeDisabled();
});

// L2-021 AC3: Given a member editing their profile, when they save, then a confirmation is shown
// without leaving the screen.
// L2-023: help tags are declared here and shown on the public profile.
test('saving confirms without leaving, and the tags reach the profile', async ({
  page,
  signInAs,
}) => {
  await signInAs(Members.priya.emailAddress);

  const settings = new ProfileSettingsPage(page);

  await settings.goto();

  await settings.description.fill('Two doors up from the church hall.');
  await settings.helpTag('Rides').check();
  await settings.save.click();

  // Still here. A member correcting a description has not finished with the screen.
  await expect(settings.saved).toBeVisible();
  await expect(settings.heading).toBeVisible();

  // L2-021 AC1: and it stayed saved.
  await page.reload();
  await expect(settings.description).toHaveValue('Two doors up from the church hall.');
  await expect(settings.helpTag('Rides')).toBeChecked();
});

// L2-024 AC3: Given a member viewing another's profile, when the screen loads, then that member's
// active listings are shown and each opens its listing.
// L2-069 AC2: and nothing offers to message them directly.
test("another member's profile shows what they have on the board", async ({ page, signInAs }) => {
  await signInAs(Members.priya.emailAddress);

  const directory = new DirectoryPage(page);
  const profile = new MemberProfilePage(page);

  await directory.goto();
  await directory.row(Members.marion.displayName).getByRole('link').first().click();

  await expect(profile.heading(Members.marion.displayName)).toBeVisible();
  await expect(profile.listing(SeededListings.ladder)).toBeVisible();

  // Absent, not disabled. Asking happens against a listing, and the thread follows the
  // acceptance.
  await expect(profile.messageAction).toHaveCount(0);

  await profile.listing(SeededListings.ladder).click();

  await expect(page).toHaveURL(/\/listings\//);
});

// L2-076 AC2: Given the directory, when it is displayed, then each member's help tags are shown.
// L2-078 AC2: and every row leads to a profile; none is a dead end.
test('the directory lists the congregation, and every row leads somewhere', async ({
  page,
  signInAs,
}) => {
  await signInAs(Members.priya.emailAddress);

  const settings = new ProfileSettingsPage(page);

  await settings.goto();
  await settings.helpTag('Tutoring').check();
  await settings.save.click();
  await expect(settings.saved).toBeVisible();

  const directory = new DirectoryPage(page);

  await directory.goto();

  await expect(directory.row(Members.marion.displayName)).toBeVisible();
  await expect(directory.row(Members.grace.displayName)).toBeVisible();

  // The tags are on the row, which is what makes the directory answer "who can help with this".
  await expect(directory.helpTagOn(Members.priya.displayName, 'Tutoring')).toBeVisible();

  // Every row, not merely the first. A name with no way to act on it is a dead end.
  const names = await directory.names.count();

  expect(names).toBeGreaterThan(0);

  for (let index = 0; index < names; index += 1) {
    await expect(directory.names.nth(index)).toHaveAttribute('href', /\/members\//);
  }
});

// L2-078 AC1: Given the directory, when a member selects a row, then that member's public profile
// is shown.
test('a directory row opens that member', async ({ page, signInAs }) => {
  await signInAs(Members.priya.emailAddress);

  const directory = new DirectoryPage(page);
  const profile = new MemberProfilePage(page);

  await directory.goto();
  await directory.row(Members.grace.displayName).getByRole('link').first().click();

  await expect(profile.heading(Members.grace.displayName)).toBeVisible();
});

// L2-025 AC2: Given a member on profile and settings, when they choose to leave, then a
// confirmation is required before the action proceeds.
// L2-025 AC3: Given a member who cancels the confirmation, then they remain a member and their
// listings are unchanged.
test('leaving is confirmed, and cancelling keeps everything', async ({ page, board, signInAs }) => {
  await signInAs(Members.marion.emailAddress);

  const settings = new ProfileSettingsPage(page);

  await settings.goto();
  await settings.leave.click();

  await expect(settings.leaveDialog).toBeVisible();

  await settings.cancelLeave.click();

  // Still a member, and the ladder is still on the board.
  await expect(settings.heading).toBeVisible();

  await board.goto();
  await expect(board.placard(SeededListings.ladder)).toBeVisible();

  // And then, having meant it.
  await settings.goto();
  await settings.leave.click();
  await settings.confirmLeave.click();

  // Signed out, because the session goes with the membership.
  await expect(page).toHaveURL(/\/$/);
});
