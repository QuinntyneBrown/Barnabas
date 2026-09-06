import {
  AwaitingApprovalPage,
  CreateProfilePage,
  InviteInvalidPage,
  InviteSomeonePage,
  JoinPage,
} from '../page-objects/joining.page';
import { YouPage } from '../page-objects/you.page';
import { Members } from '../support/members';
import { expect, test } from '../support/barnabas';

// Acceptance Test
// Traces to: L2-003, L2-004, L2-005, L2-006, L2-007, L2-008, L2-010, L2-011
// Description: A moderator issues a code and somebody joins with it, ending on the screen that
// says a moderator has to let them in. A code that does not work says which kind of not working
// it was, because that decides what the member does next.

// L2-003 AC3: Given a signed-in moderator, when they open the You screen, then a moderator tools
// entry is shown.
test('a moderator is offered their own tools', async ({ page, signInAs }) => {
  await signInAs(Members.marion.emailAddress);

  const you = new YouPage(page);

  await page.goto('/you');

  await expect(page.getByRole('link', { name: /Moderator tools/ })).toBeVisible();
  await expect(you.signOut).toBeVisible();
});

// L2-003 AC4: Given a signed-in member who is not a moderator, when they open the You screen,
// then no moderator tools entry is shown.
test('an ordinary member is not', async ({ page, signInAs }) => {
  await signInAs(Members.priya.emailAddress);

  await page.goto('/you');

  await expect(page.getByRole('link', { name: /Moderator tools/ })).toHaveCount(0);
});

// L2-005: a moderator issues a code, and it is theirs to read out.
test('a moderator issues a code to hand somebody', async ({ page, signInAs }) => {
  await signInAs(Members.marion.emailAddress);

  const invite = new InviteSomeonePage(page);

  await invite.goto();
  await expect(invite.heading).toBeVisible();

  await invite.issue.click();

  await expect(invite.code).toHaveText(/^[A-Z2-9]{8}$/);
});

// L2-006, L2-010, L2-011: the whole way in, walked.
test('somebody joins with a code and waits for a moderator', async ({ page, signInAs }) => {
  await signInAs(Members.marion.emailAddress);

  const invite = new InviteSomeonePage(page);

  await invite.goto();
  await invite.issue.click();

  const code = (await invite.code.textContent())?.trim() ?? '';

  // Start from nobody. Joining is anonymous, and doing it while still signed in as the moderator
  // would prove something other than what this is about.
  await page.context().clearCookies();

  const join = new JoinPage(page);
  const profile = new CreateProfilePage(page);
  const waiting = new AwaitingApprovalPage(page);

  await join.goto();
  await join.redeem(code);

  await expect(profile.heading).toBeVisible();

  // L2-002 AC3: only this congregation's neighbourhoods are offered, and they came back with the
  // code rather than being written into the form.
  await expect(profile.neighbourhoodOptions.first()).toHaveText('Riverdale');
  await expect(profile.neighbourhoodOptions).not.toHaveText([/Parkdale/]);

  await profile.fillAndJoin({
    displayName: 'Newcomer N.',
    emailAddress: 'newcomer@example.com',
  });

  await expect(waiting.heading).toBeVisible();

  // L2-011 AC2: it names the congregation they are waiting on.
  await expect(waiting.congregation).toContainText("St. Aidan's");
});

// L2-010 AC2: a profile missing a required field is refused naming the field, and the member can
// correct it - the code is not spent on a typo.
test('an incomplete profile is corrected rather than costing the code', async ({ page, signInAs }) => {
  await signInAs(Members.marion.emailAddress);

  const invite = new InviteSomeonePage(page);

  await invite.goto();
  await invite.issue.click();

  const code = (await invite.code.textContent())?.trim() ?? '';

  await page.context().clearCookies();

  const join = new JoinPage(page);
  const profile = new CreateProfilePage(page);

  await join.goto();
  await join.redeem(code);

  await profile.emailAddress.fill('newcomer@example.com');
  await profile.join.click();

  await expect(profile.fieldError('displayName')).toBeVisible();

  // Still here, and still able to finish. Burning the code on a typo would send them back to a
  // moderator for a fresh one.
  await profile.fillAndJoin({
    displayName: 'Newcomer N.',
    emailAddress: 'newcomer@example.com',
  });

  await expect(new AwaitingApprovalPage(page).heading).toBeVisible();
});

// L2-007 AC2: an unrecognised code says so, and invites another try.
test('a code nobody recognises invites another try', async ({ page }) => {
  const join = new JoinPage(page);
  const invalid = new InviteInvalidPage(page);

  await join.goto();
  await join.redeem('ZZZZ9999');

  await expect(invalid.unrecognised).toBeVisible();
  await expect(invalid.tryAnother).toBeVisible();
});

// L2-008 AC2: a code that has been used says who to ask for another, and does not say why it is
// dead - expired, spent and revoked read alike on purpose.
test('a spent code sends the member back to whoever invited them', async ({ page, signInAs }) => {
  await signInAs(Members.marion.emailAddress);

  const invite = new InviteSomeonePage(page);

  await invite.goto();
  await invite.issue.click();

  const code = (await invite.code.textContent())?.trim() ?? '';

  await page.context().clearCookies();

  const join = new JoinPage(page);
  const profile = new CreateProfilePage(page);

  await join.goto();
  await join.redeem(code);
  await expect(profile.heading).toBeVisible();

  // The same code again, now that it has been spent.
  await join.goto();
  await join.redeem(code);

  const invalid = new InviteInvalidPage(page);

  await expect(invalid.spent).toBeVisible();
  await expect(invalid.askWhoeverInvitedYou).toBeVisible();
});

// L2-004 AC1: the board heading names the member's own congregation, read from the API rather
// than written into the template.
test('the board names the congregation it belongs to', async ({ board, signInAs }) => {
  await signInAs(Members.marion.emailAddress);

  await board.goto();

  await expect(board.heading).toHaveText("St. Aidan's board");
});
