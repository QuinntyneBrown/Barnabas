import { Locator, Page } from '@playwright/test';

/** Where somebody types the code they were given. */
export class JoinPage {
  constructor(private readonly page: Page) {}

  get heading(): Locator {
    return this.page.getByRole('heading', { name: 'Join your congregation' });
  }

  get code(): Locator {
    return this.page.getByLabel('Invite code');
  }

  get continueOn(): Locator {
    return this.page.getByRole('button', { name: 'Continue' });
  }

  async goto(): Promise<void> {
    await this.page.goto('/join');
  }

  async redeem(code: string): Promise<void> {
    await this.code.fill(code);
    await this.continueOn.click();
  }
}

/**
 * What a code that did not work says.
 *
 * The heading is the whole point of the screen: which of the three refusals it was decides what
 * the member should do next, and a single "that failed" would leave them guessing.
 */
export class InviteInvalidPage {
  constructor(private readonly page: Page) {}

  get unrecognised(): Locator {
    return this.page.getByRole('heading', { name: 'That code was not recognised' });
  }

  get spent(): Locator {
    return this.page.getByRole('heading', { name: 'That code can no longer be used' });
  }

  /** Who to ask, since the screen cannot say why the code is dead. */
  get askWhoeverInvitedYou(): Locator {
    return this.page.getByText(/Ask whoever invited you/);
  }

  get tryAnother(): Locator {
    return this.page.getByRole('link', { name: 'Try another code' });
  }
}

/** The profile somebody supplies to finish joining. */
export class CreateProfilePage {
  constructor(private readonly page: Page) {}

  get heading(): Locator {
    return this.page.getByRole('heading', { name: 'Your profile' });
  }

  get displayName(): Locator {
    return this.page.getByLabel('What should the congregation call you?');
  }

  get emailAddress(): Locator {
    return this.page.getByLabel('Email address');
  }

  get neighbourhood(): Locator {
    return this.page.getByLabel('Neighbourhood');
  }

  get reason(): Locator {
    return this.page.getByLabel('Anything you would like the moderators to know?');
  }

  get join(): Locator {
    return this.page.getByRole('button', { name: 'Join' });
  }

  fieldError(field: string): Locator {
    return this.page.locator(`#${field}-error`);
  }

  /** The neighbourhoods on offer, which are this congregation's and no other's. */
  get neighbourhoodOptions(): Locator {
    return this.neighbourhood.locator('option');
  }

  async fillAndJoin(profile: { displayName: string; emailAddress: string }): Promise<void> {
    await this.displayName.fill(profile.displayName);
    await this.emailAddress.fill(profile.emailAddress);
    await this.join.click();
  }
}

/** Where a member waits for a moderator. */
export class AwaitingApprovalPage {
  constructor(private readonly page: Page) {}

  get heading(): Locator {
    return this.page.getByRole('heading', { name: 'You are on the list' });
  }

  /** The one thing somebody waiting needs to know: which parish they are waiting on. */
  get congregation(): Locator {
    return this.page.getByText(/is with the moderators/);
  }
}

/** A moderator's own page: the code to hand somebody. */
export class InviteSomeonePage {
  constructor(private readonly page: Page) {}

  get heading(): Locator {
    return this.page.getByRole('heading', { name: 'Invite someone' });
  }

  get issue(): Locator {
    return this.page.getByRole('button', { name: /^Issue a?n?o?t?h?e?r? ?code$/ });
  }

  get code(): Locator {
    return this.page.locator('.display');
  }

  async goto(): Promise<void> {
    await this.page.goto('/moderation/invite');
  }
}
