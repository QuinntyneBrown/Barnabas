import { Locator, Page } from '@playwright/test';

/** The listings a member owns, with what is waiting on each. */
export class MyListingsPage {
  constructor(private readonly page: Page) {}

  get heading(): Locator {
    return this.page.getByRole('heading', { name: 'My listings' });
  }

  get rows(): Locator {
    return this.page.locator('.list-row');
  }

  row(title: string): Locator {
    return this.rows.filter({ hasText: title });
  }

  /** The count is a link to the requests themselves, not a label. */
  requestCount(title: string): Locator {
    return this.row(title).locator('.count-pill');
  }

  closeOut(title: string): Locator {
    return this.row(title).getByRole('button', { name: /^Mark as/ });
  }

  async goto(): Promise<void> {
    await this.page.goto('/my-listings');
  }
}
