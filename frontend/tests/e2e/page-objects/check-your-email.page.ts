import { Locator, Page } from '@playwright/test';

/** The screen that names the address a link went to. */
export class CheckYourEmailPage {
  constructor(private readonly page: Page) {}

  get heading(): Locator {
    return this.page.getByRole('heading', { name: 'Check your email' });
  }

  /** The whole sentence, so a spec can assert the address appears within it. */
  get message(): Locator {
    return this.page.getByText(/A sign-in link is on its way to/);
  }
}
