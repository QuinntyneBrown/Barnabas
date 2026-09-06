import { APIRequestContext, Page, expect, test as base } from '@playwright/test';

import { BoardPage } from '../page-objects/board.page';
import { CheckYourEmailPage } from '../page-objects/check-your-email.page';
import { ShellPage } from '../page-objects/shell.page';
import { SignInPage } from '../page-objects/sign-in.page';

/**
 * What every spec starts from: an empty board of seeded listings, and a way in.
 *
 * The reset runs before each test rather than once for the suite. A spec that depended on what
 * the spec before it left behind would pass in one order and fail in another, and the failure
 * would look like a product defect rather than a test one.
 */
export const test = base.extend<{
  board: BoardPage;
  shell: ShellPage;
  signIn: SignInPage;
  checkYourEmail: CheckYourEmailPage;
  signInAs: (emailAddress: string) => Promise<void>;
}>({
  page: async ({ page, request }, use) => {
    await reset(request);

    if (process.env['BARNABAS_TRACE_API']) {
      page.on('console', (m) => console.log(`[console:${m.type()}] ${m.text()}`));
      page.on('pageerror', (e) => console.log(`[pageerror] ${e.message}`));
      page.on('request', (r) => {
        if (r.url().includes('/api/')) {
          console.log(`--> ${r.method()} ${r.url()}`);
        }
      });
      page.on('response', async (r) => {
        if (!r.url().includes('/api/')) {
          return;
        }

        let body = '';

        try {
          body = (await r.text()).slice(0, 400);
        } catch {
          body = '<unreadable>';
        }

        console.log(`<-- ${r.status()} ${r.url()} ${body}`);
      });
    }

    await use(page);
  },

  board: async ({ page }, use) => use(new BoardPage(page)),
  shell: async ({ page }, use) => use(new ShellPage(page)),
  signIn: async ({ page }, use) => use(new SignInPage(page)),
  checkYourEmail: async ({ page }, use) => use(new CheckYourEmailPage(page)),

  signInAs: async ({ page, request }, use) => {
    await use(async (emailAddress: string) => {
      const signInPage = new SignInPage(page);

      // Let whatever the previous member did finish first. Handing the browser from one member
      // to another is not something a member ever does, and doing it with a request in flight
      // aborts that request - which reads later as the product having lost the write rather
      // than as the test having raced it.
      await page.waitForLoadState('networkidle').catch(() => undefined);

      // Then start from nobody. Leaving the previous refresh cookie in place would make which
      // identity the next page load recovers a matter of timing.
      await page.context().clearCookies();

      await signInPage.goto();
      await signInPage.askForALink(emailAddress);

      // Wait for the screen that says the link is on its way, rather than reading the outbox the
      // instant the button is clicked. The dispatch is what this is waiting on, and the
      // confirmation is the product's own signal that it happened.
      await expect(new CheckYourEmailPage(page).heading).toBeVisible();

      // Follow the link out of the outbox, exactly as a member follows it out of their inbox.
      await page.goto(await latestSignInLink(request, emailAddress));

      await expect(page).toHaveURL(/\/board$/);
    });
  },
});

export { expect };

async function reset(request: APIRequestContext): Promise<void> {
  if (process.env['BARNABAS_TRACE_API']) {
    console.log(`[reset] ${new Date().toISOString()}`);
  }

  const response = await request.post('/api/dev/reset');

  if (!response.ok()) {
    throw new Error(`Could not reset the board: the API answered ${response.status()}.`);
  }
}

async function latestSignInLink(request: APIRequestContext, emailAddress: string): Promise<string> {
  const route = `/api/dev/sign-in-links/${encodeURIComponent(emailAddress)}`;

  // Polled rather than read once. The dispatch and the navigation that follows it are two
  // separate things, and which lands first is not something a test should depend on.
  await expect
    .poll(async () => (await request.get(route)).status(), { timeout: 5_000 })
    .toBe(200);

  return (await request.get(route)).json().then((message) => message.url as string);
}

/** Reads the link a member would have received, for the specs that follow one by hand. */
export async function signInLinkFor(page: Page, emailAddress: string): Promise<string> {
  return latestSignInLink(page.request, emailAddress);
}
