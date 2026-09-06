import { Locator, Page } from '@playwright/test';

/** A member's own profile and settings. */
export class ProfileSettingsPage {
  constructor(private readonly page: Page) {}

  get heading(): Locator {
    return this.page.getByRole('heading', { name: 'Profile and settings' });
  }

  get displayName(): Locator {
    return this.page.getByLabel('What the congregation calls you');
  }

  get neighbourhood(): Locator {
    return this.page.getByLabel('Neighbourhood');
  }

  get neighbourhoodOptions(): Locator {
    return this.neighbourhood.locator('option');
  }

  get description(): Locator {
    return this.page.getByLabel('A few words about you');
  }

  helpTag(tag: string): Locator {
    return this.page.getByRole('checkbox', { name: tag });
  }

  get save(): Locator {
    return this.page.getByRole('button', { name: 'Save changes' });
  }

  /** The confirmation that a save landed, without leaving the screen. */
  get saved(): Locator {
    return this.page.getByText('Saved', { exact: true });
  }

  /** Shown and not editable: it is the only way back into the product. */
  get emailAddress(): Locator {
    return this.page.getByLabel('Email address');
  }

  get leave(): Locator {
    return this.page.getByRole('button', { name: /^Leave/ });
  }

  get leaveDialog(): Locator {
    return this.page.getByRole('dialog', { name: 'Leave the board?' });
  }

  get confirmLeave(): Locator {
    return this.leaveDialog.getByRole('button', { name: 'Leave' });
  }

  get cancelLeave(): Locator {
    return this.leaveDialog.getByRole('button', { name: 'Cancel' });
  }

  fieldError(field: string): Locator {
    return this.page.locator(`#${field}-error`);
  }

  async goto(): Promise<void> {
    await this.page.goto('/you/profile');
  }
}

/** Another member, as the congregation sees them. */
export class MemberProfilePage {
  constructor(private readonly page: Page) {}

  heading(name: string): Locator {
    return this.page.getByRole('heading', { name, level: 1 });
  }

  get helpTags(): Locator {
    return this.page.locator('.chip-row .chip');
  }

  get listings(): Locator {
    return this.page.locator('.list-row');
  }

  listing(title: string): Locator {
    return this.page.getByRole('link', { name: title });
  }

  /**
   * Anything offering to write to this member.
   *
   * Exposed so a spec can assert there is none. A page object with no way to look for one could
   * not state `L2-069`.
   */
  get messageAction(): Locator {
    return this.page.getByRole('button', { name: /message|write|contact/i });
  }
}

/** Who is in the congregation. */
export class DirectoryPage {
  constructor(private readonly page: Page) {}

  get heading(): Locator {
    return this.page.getByRole('heading', { name: 'Congregation directory' });
  }

  get rows(): Locator {
    return this.page.locator('.list-row');
  }

  row(name: string): Locator {
    return this.rows.filter({ hasText: name });
  }

  /** Every row's name, which is also its way to that member's profile. */
  get names(): Locator {
    return this.rows.locator('.list-row__title');
  }

  get search(): Locator {
    return this.page.getByLabel('Search by name');
  }

  get showEveryone(): Locator {
    return this.page.getByRole('button', { name: 'Show everyone' });
  }

  helpTagOn(name: string, tag: string): Locator {
    return this.row(name).getByRole('button', { name: tag });
  }

  async goto(): Promise<void> {
    await this.page.goto('/directory');
  }
}
