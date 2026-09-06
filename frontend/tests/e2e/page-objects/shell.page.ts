import { Locator, Page } from '@playwright/test';

/**
 * The chrome every signed-in screen shares.
 *
 * Both navigations are in the document at every width, and exactly one of them is exposed. This
 * object reaches for the exposed one, which is what a member - and a screen reader - can
 * actually reach.
 */
export class ShellPage {
  /**
   * How many notifications are waiting, as drawn on the bell.
   *
   * The bell is in the header at every width, which is how L2-073's count stays reachable below
   * the band where the header nav is not.
   */
  get unreadCount(): Locator {
    return this.page.locator('.bell-count');
  }

  constructor(private readonly page: Page) {}

  /** The navigation a member can currently use, of the two that are rendered. */
  get navigation(): Locator {
    return this.page.getByRole('navigation', { name: 'Primary' });
  }

  destination(label: string): Locator {
    return this.navigation.getByRole('link', { name: label, exact: true });
  }

  get skipLink(): Locator {
    return this.page.getByRole('link', { name: 'Skip to main content' });
  }

  get main(): Locator {
    return this.page.getByRole('main');
  }

  get notifications(): Locator {
    return this.page.getByRole('link', { name: 'Notifications' });
  }

  async goTo(label: string): Promise<void> {
    await this.destination(label).click();
  }
}
