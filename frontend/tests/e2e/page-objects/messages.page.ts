import { Locator, Page, expect } from '@playwright/test';

/** The member's conversations. */
export class ThreadsPage {
  constructor(private readonly page: Page) {}

  get rows(): Locator {
    return this.page.locator('.list-row');
  }

  with(member: string): Locator {
    return this.rows.filter({ hasText: member });
  }

  /**
   * Unread as a reader is told it, not as it is drawn.
   *
   * The dot is hidden from assistive technology and the word is in the row's own text, so this
   * asserts the thing a member who cannot see the dot is actually given.
   */
  unreadMarker(member: string): Locator {
    return this.with(member).getByText('Unread.', { exact: false });
  }

  async goto(): Promise<void> {
    await this.page.goto('/inbox/messages');
  }

  async open(member: string): Promise<void> {
    await this.with(member).click();
  }
}

/** One conversation, and the composer under it. */
export class ThreadPage {
  constructor(private readonly page: Page) {}

  get heading(): Locator {
    return this.page.getByRole('heading', { level: 1 });
  }

  get messages(): Locator {
    return this.page.locator('.message');
  }

  message(body: string): Locator {
    return this.messages.filter({ hasText: body });
  }

  attributionOf(body: string): Locator {
    return this.message(body).locator('.message__meta');
  }

  get composer(): Locator {
    return this.page.getByLabel(/^Reply to/);
  }

  get send(): Locator {
    return this.page.getByRole('button', { name: 'Send' });
  }

  /**
   * Says something, and waits for it to be there.
   *
   * The waiting is not politeness: confirmation of a send in this product is the message
   * appearing, so returning before it does would leave a spec free to navigate away and abort
   * the request it just made.
   */
  async say(body: string): Promise<void> {
    await this.composer.fill(body);
    await this.send.click();

    await expect(this.message(body)).toBeVisible();
  }
}
