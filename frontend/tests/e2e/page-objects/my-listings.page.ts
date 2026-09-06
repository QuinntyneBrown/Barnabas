import { Locator, Page } from '@playwright/test';

/** The listings a member owns, with what is waiting on each. */
export class MyListingsPage {
  constructor(private readonly page: Page) {}

  get heading(): Locator {
    return this.page.getByRole('heading', { name: 'My listings' });
  }

  get rows(): Locator {
    return this.page.locator('.list-row');
  }

  row(title: string): Locator {
    return this.rows.filter({ hasText: title });
  }

  /** The count is a link to the requests themselves, not a label. */
  requestCount(title: string): Locator {
    return this.row(title).locator('.count-pill');
  }

  closeOut(title: string): Locator {
    return this.row(title).getByRole('button', { name: /^Mark as/ });
  }

  get onTheBoardTab(): Locator {
    return this.page.getByRole('button', { name: /^On the board/ });
  }

  get putAwayTab(): Locator {
    return this.page.getByRole('button', { name: /^Put away/ });
  }

  edit(title: string): Locator {
    return this.row(title).getByRole('link', { name: 'Edit' });
  }

  takeOffTheBoard(title: string): Locator {
    return this.row(title).getByRole('button', { name: 'Take off the board' });
  }

  /**
   * Offered only where the listing may actually come back.
   *
   * A closed-out loan is archived too, so a row without this control is the screen honouring a
   * rule rather than forgetting a button.
   */
  putBack(title: string): Locator {
    return this.row(title).getByRole('button', { name: 'Put back on the board' });
  }

  remove(title: string): Locator {
    return this.row(title).getByRole('button', { name: 'Delete' });
  }

  /** The dialog that gates a destructive action, so a member has to mean it. */
  dialog(heading: string): Locator {
    return this.page.getByRole('dialog', { name: heading });
  }

  confirmIn(heading: string, action: string): Locator {
    return this.dialog(heading).getByRole('button', { name: action });
  }

  cancelIn(heading: string): Locator {
    return this.dialog(heading).getByRole('button', { name: /cancel/i });
  }

  get nothingPutAway(): Locator {
    return this.page.getByRole('heading', { name: 'Nothing put away' });
  }

  async goto(): Promise<void> {
    await this.page.goto('/my-listings');
  }

  async showPutAway(): Promise<void> {
    await this.putAwayTab.click();
  }

  async showOnTheBoard(): Promise<void> {
    await this.onTheBoardTab.click();
  }
}

/** Correcting a listing already on the board. */
export class EditListingPage {
  constructor(private readonly page: Page) {}

  get heading(): Locator {
    return this.page.getByRole('heading', { name: 'Edit your listing' });
  }

  get title(): Locator {
    return this.page.getByLabel('Title');
  }

  get description(): Locator {
    return this.page.getByLabel('Description');
  }

  get save(): Locator {
    return this.page.getByRole('button', { name: 'Save changes' });
  }

  /** Exposed so a spec can assert a listing's kind is not editable. */
  get kind(): Locator {
    return this.page.getByLabel(/kind/i);
  }
}
