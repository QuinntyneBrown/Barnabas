import { Locator, Page } from '@playwright/test';

/**
 * The chrome every signed-in screen shares.
 *
 * Both navigations are in the document at every width, and exactly one of them is exposed. This
 * object reaches for the exposed one, which is what a member - and a screen reader - can
 * actually reach.
 */
export class ShellPage {
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
