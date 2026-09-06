import { Locator, Page } from '@playwright/test';

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

  async open(title: string): Promise<void> {
    await this.placard(title).click();
  }
}
