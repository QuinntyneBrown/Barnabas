import { BoardPage } from '../page-objects/board.page';
import { CheckYourEmailPage } from '../page-objects/check-your-email.page';
import { LinkExpiredPage } from '../page-objects/link-expired.page';
import { SignInPage } from '../page-objects/sign-in.page';
import { YouPage } from '../page-objects/you.page';
import { Members } from '../support/members';
import { expect, signInLinkFor, test } from '../support/barnabas';

// Acceptance Test
// Traces to: L2-012, L2-014, L2-015, L2-018, L2-019
// Description: A member signs in by following an emailed link, is refused a link that no longer
// works, and is returned to the public screens when their session ends.

// L2-012 AC3: Given the sign-in screen, when a member submits their email address, then the
// check-your-email screen is shown and it names the address the link was sent to.
test('the check-your-email screen names the address the link went to', async ({ page, signIn, checkYourEmail }) => {
  await signIn.goto();
  await signIn.askForALink(Members.priya.emailAddress);

  await expect(checkYourEmail.heading).toBeVisible();
  await expect(checkYourEmail.message).toContainText(Members.priya.emailAddress);
});

// L2-012 AC2 in its visible half: there is nowhere on this screen to type a password, because
// there is no password to type.
test('the sign-in screen asks for no password', async ({ signIn }) => {
  await signIn.goto();

  await expect(signIn.emailAddress).toBeVisible();
  await expect(signIn.passwordFields).toHaveCount(0);
});

// L2-014 AC3: Given a member following a valid sign-in link, when the page loads, then the board
// is shown.
test('following a valid link opens the board', async ({ page, signIn, board }) => {
  await signIn.goto();
  await signIn.askForALink(Members.priya.emailAddress);
  await expect(new CheckYourEmailPage(page).heading).toBeVisible();

  await page.goto(await signInLinkFor(page, Members.priya.emailAddress));

  await expect(board.heading).toBeVisible();
  await expect(page).toHaveURL(/\/board$/);
});

// L2-015 AC3: Given a sign-in link that no longer works, when a member follows it, then a screen
// explains it and offers to send another.
//
// Exercised here by using a link twice rather than by waiting fifteen minutes. Expiry and reuse
// are deliberately indistinguishable - the API answers 410 for both - so this is the same screen
// by the same path. The expiry clock itself is driven in the API suite, where it can be moved.
test('a link that has already been used offers to send another', async ({ page, signIn }) => {
  const expired = new LinkExpiredPage(page);

  await signIn.goto();
  await signIn.askForALink(Members.priya.emailAddress);
  await expect(new CheckYourEmailPage(page).heading).toBeVisible();

  const link = await signInLinkFor(page, Members.priya.emailAddress);

  await page.goto(link);
  await expect(page).toHaveURL(/\/board$/);

  await page.goto(link);

  await expect(expired.heading).toBeVisible();
  await expect(expired.sendAnother).toBeVisible();
});

// L2-018 AC3: Given a member whose session has expired, when they open the board, then the
// sign-in screen is shown.
//
// The session is ended by discarding what the browser holds rather than by waiting it out: the
// access token lives in memory and the refresh token in a cookie, and losing both is exactly the
// state a member is in when their session has lapsed.
test('a member whose session has lapsed is sent to sign in', async ({ page, signInAs, board }) => {
  await signInAs(Members.priya.emailAddress);
  await expect(board.heading).toBeVisible();

  await page.context().clearCookies();
  await page.goto('/board');

  await expect(page).toHaveURL(/\/sign-in$/);
  await expect(new SignInPage(page).submit).toBeVisible();
});

// L2-019 AC2: Given a signed-in member on the You screen, when they choose to sign out and
// confirm, then the public landing screen is shown.
// L2-019 AC3: Given a member who has signed out, when they navigate back to the board, then the
// sign-in screen is shown.
test('signing out is confirmed, and afterwards the board is closed', async ({ page, signInAs }) => {
  const you = new YouPage(page);

  await signInAs(Members.priya.emailAddress);

  await you.goto();
  await you.signOut.click();

  // Confirmed rather than immediate: a member on a shared machine choosing it means it.
  await expect(you.confirmDialog).toBeVisible();
  await you.confirmSignOut.click();

  await expect(page).toHaveURL('http://localhost:4300/');

  await page.goto('/board');
  await expect(page).toHaveURL(/\/sign-in$/);
});

// Cancelling the dialog leaves the member exactly where they were.
test('cancelling sign-out keeps the member signed in', async ({ page, signInAs, board }) => {
  const you = new YouPage(page);

  await signInAs(Members.priya.emailAddress);

  await you.goto();
  await you.signOut.click();
  await you.cancel.click();

  await expect(you.confirmDialog).toBeHidden();

  await board.goto();
  await expect(board.heading).toBeVisible();
});
