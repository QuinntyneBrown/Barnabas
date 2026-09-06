import { Locator, Page } from '@playwright/test';

/** Choosing what kind of thing is being posted. */
export class ChooseKindPage {
  constructor(private readonly page: Page) {}

  get heading(): Locator {
    return this.page.getByRole('heading', { name: 'What are you posting?' });
  }

  get lend(): Locator {
    return this.page.getByRole('link', { name: 'Lend', exact: false });
  }

  async goto(): Promise<void> {
    await this.page.goto('/post');
  }

  async chooseLend(): Promise<void> {
    await this.lend.click();
  }
}

/**
 * The Lend form.
 *
 * `price` is exposed so a spec can assert it is absent. A page object that simply had no way to
 * reach a price field would make that assertion impossible to write.
 */
export class PostLendPage {
  constructor(private readonly page: Page) {}

  get heading(): Locator {
    return this.page.getByRole('heading', { name: 'What are you lending?' });
  }

  get title(): Locator {
    return this.page.getByLabel('Title');
  }

  get description(): Locator {
    return this.page.getByLabel('Description');
  }

  get category(): Locator {
    return this.page.getByLabel('Category');
  }

  get returnBy(): Locator {
    return this.page.getByLabel('Back with you by');
  }

  get neighbourhood(): Locator {
    return this.page.getByLabel('Neighbourhood');
  }

  get price(): Locator {
    return this.page.getByLabel(/price/i);
  }

  get post(): Locator {
    return this.page.getByRole('button', { name: 'Post listing' });
  }

  get invalidFields(): Locator {
    return this.page.locator('[aria-invalid="true"]');
  }

  fieldError(field: string): Locator {
    return this.page.locator(`#${field}-error`);
  }

  async fill(listing: { title: string; description: string; returnBy: string }): Promise<void> {
    await this.title.fill(listing.title);
    await this.description.fill(listing.description);
    await this.returnBy.fill(listing.returnBy);
  }

  async submit(): Promise<void> {
    await this.post.click();
  }
}

/** The confirmation that a listing is live, and the three ways onward. */
export class ListingPostedPage {
  constructor(private readonly page: Page) {}

  get heading(): Locator {
    return this.page.getByRole('heading', { name: 'Your listing is on the board' });
  }

  get viewYourListing(): Locator {
    return this.page.getByRole('link', { name: 'View your listing' });
  }

  get postAnother(): Locator {
    return this.page.getByRole('link', { name: 'Post another' });
  }

  get backToTheBoard(): Locator {
    return this.page.getByRole('link', { name: 'Back to the board' });
  }
}
