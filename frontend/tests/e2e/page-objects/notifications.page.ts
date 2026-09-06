import { Locator, Page, expect } from '@playwright/test';

/** What has happened that concerns this member. */
export class NotificationsPage {
  constructor(private readonly page: Page) {}

  get heading(): Locator {
    return this.page.getByRole('heading', { name: 'Notifications' });
  }

  get rows(): Locator {
    return this.page.locator('.list-row');
  }

  get markAllRead(): Locator {
    return this.page.getByRole('button', { name: 'Mark all read' });
  }

  /**
   * The member a notification names, which opens their profile.
   *
   * Exact, because the row's own title also contains their name — "Priya K. asked about the
   * ladder" is the destination for the event, and this is the destination for the person.
   */
  member(name: string): Locator {
    return this.rows.getByRole('link', { name, exact: true });
  }

  get nothingYet(): Locator {
    return this.page.getByRole('heading', { name: 'Nothing yet' });
  }

  /**
   * One of the switches on the settings screen.
   *
   * They live there rather than here because that is where L2-075 puts them: deciding what to
   * hear about is the same sort of act as deciding what the congregation sees of you.
   */
  preference(kind: string): Locator {
    return this.page.locator(`#notify-${kind}`);
  }

  async goto(): Promise<void> {
    await this.page.goto('/notifications');
  }

  /**
   * Waits for the list to arrive.
   *
   * The screen renders and then fetches, so anything reading the rows imperatively - counting
   * them, walking them - has to wait first or it counts an empty list. An `expect(locator)`
   * assertion retries on its own; a `count()` does not.
   */
  async waitForRows(): Promise<void> {
    await expect(this.rows.first()).toBeVisible();
  }
}
