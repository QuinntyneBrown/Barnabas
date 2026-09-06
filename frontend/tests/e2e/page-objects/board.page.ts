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
