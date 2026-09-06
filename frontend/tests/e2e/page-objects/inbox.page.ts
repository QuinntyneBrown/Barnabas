import { Locator, Page } from '@playwright/test';

/** What has been asked of the member. */
export class IncomingRequestsPage {
  constructor(private readonly page: Page) {}

  get rows(): Locator {
    return this.page.locator('.list-row');
  }

  from(requester: string): Locator {
    return this.rows.filter({ hasText: requester });
  }

  accept(requester: string): Locator {
    return this.from(requester).getByRole('button', { name: 'Accept' });
  }

  decline(requester: string): Locator {
    return this.from(requester).getByRole('button', { name: 'Decline' });
  }

  async goto(): Promise<void> {
    await this.page.goto('/inbox/requests');
  }
}

/** What the member asked for, and what became of it. */
export class OutgoingRequestsPage {
  constructor(private readonly page: Page) {}

  get rows(): Locator {
    return this.page.locator('.list-row');
  }

  forListing(title: string): Locator {
    return this.rows.filter({ hasText: title });
  }

  statusOf(title: string): Locator {
    return this.forListing(title).locator('.status');
  }

  threadFor(title: string): Locator {
    return this.forListing(title).getByRole('link', { name: 'Open the message thread' });
  }

  async goto(): Promise<void> {
    await this.page.goto('/inbox/my-requests');
  }
}
