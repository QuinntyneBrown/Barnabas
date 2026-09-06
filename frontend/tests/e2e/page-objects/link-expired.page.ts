import { Locator, Page } from '@playwright/test';

/** What a member sees when a link has been used, or has gone stale. */
export class LinkExpiredPage {
  constructor(private readonly page: Page) {}

  get heading(): Locator {
    return this.page.getByRole('heading', { name: 'That link has expired' });
  }

  get sendAnother(): Locator {
    return this.page.getByRole('link', { name: 'Send me another link' });
  }
}
