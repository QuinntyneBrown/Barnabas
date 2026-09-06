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

  get give(): Locator {
    return this.page.getByRole('link', { name: 'Give', exact: false });
  }

  get sell(): Locator {
    return this.page.getByRole('link', { name: 'Sell', exact: false });
  }

  get help(): Locator {
    return this.page.getByRole('link', { name: 'Help', exact: false });
  }

  /** The four choices, so a spec can assert that all four are offered. */
  get kinds(): Locator {
    return this.page.getByRole('navigation', { name: 'Choose a listing kind' }).getByRole('link');
  }

  /**
   * Anything that would collect a detail.
   *
   * Exposed so a spec can assert there is nothing here to fill in. L2-026 requires the kind to
   * be chosen before any detail is entered, and a page object with no way to look for a field
   * could not state that.
   */
  get detailFields(): Locator {
    return this.page.locator('input, textarea, select');
  }

  async goto(): Promise<void> {
    await this.page.goto('/post');
  }

  async chooseLend(): Promise<void> {
    await this.lend.click();
  }

  async chooseGive(): Promise<void> {
    await this.give.click();
  }

  async chooseSell(): Promise<void> {
    await this.sell.click();
  }

  async chooseHelp(): Promise<void> {
    await this.help.click();
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

/**
 * The Give form.
 *
 * `price` and `returnBy` are exposed so a spec can assert they are absent. A gift is free and it
 * is not coming back, and a page object with no way to look for either field could not say so.
 */
export class PostGivePage {
  constructor(private readonly page: Page) {}

  get heading(): Locator {
    return this.page.getByRole('heading', { name: 'What are you giving away?' });
  }

  get title(): Locator {
    return this.page.getByLabel('Title');
  }

  get description(): Locator {
    return this.page.getByLabel('Description');
  }

  get price(): Locator {
    return this.page.getByLabel(/price/i);
  }

  get returnBy(): Locator {
    return this.page.getByLabel(/back with you by/i);
  }

  get post(): Locator {
    return this.page.getByRole('button', { name: 'Post listing' });
  }

  async fill(listing: { title: string; description: string }): Promise<void> {
    await this.title.fill(listing.title);
    await this.description.fill(listing.description);
  }

  async submit(): Promise<void> {
    await this.post.click();
  }
}

/** The Sell form: the only one of the four that carries a price. */
export class PostSellPage {
  constructor(private readonly page: Page) {}

  get heading(): Locator {
    return this.page.getByRole('heading', { name: 'What are you selling?' });
  }

  get title(): Locator {
    return this.page.getByLabel('Title');
  }

  get description(): Locator {
    return this.page.getByLabel('Description');
  }

  get condition(): Locator {
    return this.page.getByLabel('Condition');
  }

  get price(): Locator {
    return this.page.getByLabel('Asking price');
  }

  get post(): Locator {
    return this.page.getByRole('button', { name: 'Post listing' });
  }

  fieldError(field: string): Locator {
    return this.page.locator(`#${field}-error`);
  }

  async fill(listing: { title: string; description: string; price: string }): Promise<void> {
    await this.title.fill(listing.title);
    await this.description.fill(listing.description);
    await this.price.fill(listing.price);
  }

  async submit(): Promise<void> {
    await this.post.click();
  }
}

/** The Help form: the only one that collects a repeating structure rather than a set of fields. */
export class PostHelpPage {
  constructor(private readonly page: Page) {}

  get heading(): Locator {
    return this.page.getByRole('heading', { name: 'What help are you offering?' });
  }

  get title(): Locator {
    return this.page.getByLabel('Title');
  }

  get description(): Locator {
    return this.page.getByLabel('Description');
  }

  get price(): Locator {
    return this.page.getByLabel(/price/i);
  }

  /** One row per window the offer declares. */
  get windows(): Locator {
    return this.page.getByRole('group', { name: 'When you are free' }).locator('.row');
  }

  get addWindow(): Locator {
    return this.page.getByRole('button', { name: 'Add another window' });
  }

  dayOf(index: number): Locator {
    return this.page.locator(`#day-${index}`);
  }

  fromOf(index: number): Locator {
    return this.page.locator(`#from-${index}`);
  }

  untilOf(index: number): Locator {
    return this.page.locator(`#until-${index}`);
  }

  get post(): Locator {
    return this.page.getByRole('button', { name: 'Post listing' });
  }

  async fill(listing: { title: string; description: string }): Promise<void> {
    await this.title.fill(listing.title);
    await this.description.fill(listing.description);
  }

  async submit(): Promise<void> {
    await this.post.click();
  }
}
