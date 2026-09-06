import { Locator, Page } from '@playwright/test';

/**
 * One listing, seen by a visitor or by its owner.
 *
 * The two are different screens, and the page object exposes both sets of controls so a spec can
 * assert that the wrong ones are absent.
 */
export class ListingDetailPage {
  constructor(private readonly page: Page) {}

  get title(): Locator {
    return this.page.getByRole('heading', { level: 1 });
  }

  get kind(): Locator {
    return this.page.locator('.kind');
  }

  get requestToBorrow(): Locator {
    return this.page.getByRole('link', { name: 'Request to borrow' });
  }

  get markAsTaken(): Locator {
    return this.page.getByRole('button', { name: 'Mark as taken' });
  }

  get price(): Locator {
    return this.page.locator('.detail__price');
  }

  async ask(): Promise<void> {
    await this.requestToBorrow.click();
  }
}
