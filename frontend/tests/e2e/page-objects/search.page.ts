import { Locator, Page } from '@playwright/test';

/** Finding something the board is too long to scroll for. */
export class SearchPage {
  constructor(private readonly page: Page) {}

  get heading(): Locator {
    return this.page.getByRole('heading', { name: 'Search the board' });
  }

  get term(): Locator {
    return this.page.getByLabel('What are you looking for?');
  }

  get submit(): Locator {
    return this.page.getByRole('button', { name: 'Search' });
  }

  get results(): Locator {
    return this.page.locator('.list-row');
  }

  result(title: string): Locator {
    return this.results.filter({ hasText: title });
  }

  /** The line that says how many, and for what. */
  get summary(): Locator {
    return this.page.locator('.muted').first();
  }

  get kinds(): Locator {
    return this.page.getByRole('navigation', { name: 'Narrow the results by kind' });
  }

  /** The kind currently in force, which the screen marks as pressed. */
  get activeKind(): Locator {
    return this.kinds.locator('button[aria-pressed="true"]');
  }

  get neighbourhood(): Locator {
    return this.page.getByLabel('Neighbourhood');
  }

  get neighbourhoodOptions(): Locator {
    return this.neighbourhood.locator('option');
  }

  get maxPrice(): Locator {
    return this.page.getByLabel('Up to');
  }

  /**
   * What a search that found nothing says.
   *
   * It carries the term, because there is nothing else on that screen to say what happened.
   */
  get nothingFound(): Locator {
    return this.page.getByRole('heading', { name: /^Nothing found for/ });
  }

  get clearFilters(): Locator {
    return this.page.getByRole('button', { name: 'Clear the filters' });
  }

  async goto(): Promise<void> {
    await this.page.goto('/search');
  }

  async searchFor(term: string): Promise<void> {
    await this.term.fill(term);
    await this.submit.click();
  }

  async narrowTo(label: string): Promise<void> {
    await this.kinds.getByRole('button', { name: label }).click();
  }
}
