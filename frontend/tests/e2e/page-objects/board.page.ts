import { Locator, Page, expect } from '@playwright/test';

/**
 * The board, as a mosaic of placards.
 *
 * A placard is located by its title, and its kind is read from the text inside it rather than
 * from the colour of the field it sits on. That is not a convenience: L2-044 requires the kind
 * to be determinable without colour, and a page object that read the field colour would be
 * asserting the opposite of the requirement.
 */
export class BoardPage {
  constructor(private readonly page: Page) {}

  get heading(): Locator {
    return this.page.getByRole('heading', { name: /board/i, level: 1 });
  }

  get placards(): Locator {
    return this.page.locator('.mosaic .placard');
  }

  placard(title: string): Locator {
    return this.placards.filter({ hasText: title });
  }

  /** The kind as words, which is how the placard carries it besides the colour. */
  kindOf(title: string): Locator {
    return this.placard(title).locator('.placard__kind');
  }

  /** The price on a placard, which only a Sell listing carries. */
  priceOf(title: string): Locator {
    return this.placard(title).locator('.placard__price');
  }

  /** Everything a placard says besides its title, so a spec can read the availability off it. */
  metaOf(title: string): Locator {
    return this.placard(title).locator('.placard__meta');
  }

  /** Any image on a placard, exposed so a spec can assert a Help placard carries none. */
  imagesOn(title: string): Locator {
    return this.placard(title).locator('img, .placard__art');
  }

  get filters(): Locator {
    return this.page.getByRole('navigation', { name: 'Filter the board by kind' });
  }

  filter(label: string): Locator {
    return this.filters.getByRole('button', { name: new RegExp(`^${label}`) });
  }

  /** The chip currently in force, which the board marks as pressed. */
  get activeFilter(): Locator {
    return this.filters.locator('button[aria-pressed="true"]');
  }

  async filterBy(label: string): Promise<void> {
    await this.filter(label).click();
  }

  /**
   * What a filtered board says when nothing of that kind is posted.
   *
   * Distinct from the empty board. A member who filtered to Help and found none has not
   * discovered that the congregation has posted nothing; telling them so would be false.
   */
  get nothingOfThatKind(): Locator {
    return this.page.getByRole('heading', { name: 'Nothing of that kind just now' });
  }

  get emptyBoard(): Locator {
    return this.page.getByRole('heading', { name: 'The board is empty' });
  }

  get postAListing(): Locator {
    return this.page.getByRole('link', { name: 'Post a listing' });
  }

  async goto(): Promise<void> {
    await this.page.goto('/board');
  }

  /**
   * Waits for the mosaic to arrive.
   *
   * The board renders and then fetches, so anything that reads the placards imperatively -
   * counting them, or measuring where they sit - has to wait first or it measures an empty
   * mosaic. A `expect(locator)` assertion retries on its own and needs none of this; a `count()`
   * or a `page.evaluate` does not.
   */
  async waitForPlacards(): Promise<void> {
    await expect(this.placards.first()).toBeVisible();
  }

  async open(title: string): Promise<void> {
    await this.placard(title).click();
  }
}
