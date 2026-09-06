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
    return this.page.locator('.detail__title');
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

  /**
   * The close-out action, whatever this kind calls it.
   *
   * Named by pattern rather than by one label, because the wording is the requirement: an owner
   * marks a sale *sold* and a gift *given away*. A locator fixed on "Mark as taken" could not
   * see the other three.
   */
  get closeOut(): Locator {
    return this.page.getByRole('button', { name: /^Mark as / });
  }

  /** The sentence that says money changes hands between the members, not here. */
  get paymentNote(): Locator {
    return this.page.getByText(/arranged directly between members/i);
  }

  /** The windows a Help offer declares. */
  get availability(): Locator {
    return this.page.getByRole('term', { name: /is free/ });
  }

  /**
   * The call to action, whatever this kind calls it.
   *
   * "Request to borrow" on a loan, "Request to buy" on a sale. Matched by pattern for the same
   * reason the close-out action is: the wording is the requirement, not an incidental label.
   */
  get askAction(): Locator {
    return this.page.getByRole('link', { name: /^Request/ });
  }

  /** Any member but the owner may report it. The owner is not offered it at all. */
  get report(): Locator {
    return this.page.getByRole('button', { name: 'Report this listing' });
  }

  async ask(): Promise<void> {
    await this.askAction.click();
  }
}
