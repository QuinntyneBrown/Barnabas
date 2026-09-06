import { Locator, Page } from '@playwright/test';

/** The member's own corner, and the way out. */
export class YouPage {
  constructor(private readonly page: Page) {}

  get signOut(): Locator {
    return this.page.getByRole('button', { name: 'Sign out' });
  }

  /** The dialog that gates signing out, so a member on a shared machine has to mean it. */
  get confirmDialog(): Locator {
    return this.page.getByRole('dialog', { name: 'Sign out of Barnabas?' });
  }

  get confirmSignOut(): Locator {
    return this.confirmDialog.getByRole('button', { name: 'Sign out' });
  }

  get cancel(): Locator {
    return this.confirmDialog.getByRole('button', { name: 'Cancel' });
  }

  async goto(): Promise<void> {
    await this.page.goto('/you');
  }
}
