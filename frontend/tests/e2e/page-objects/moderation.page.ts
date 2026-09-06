import { Locator, Page, expect } from '@playwright/test';

/** The dialogue a member reports a listing through. */
export class ReportListingDialog {
  constructor(private readonly page: Page) {}

  get dialog(): Locator {
    return this.page.getByRole('dialog', { name: 'Report this listing' });
  }

  /** The promise the member is given before they choose anything. */
  get anonymityPromise(): Locator {
    return this.dialog.getByText(/is not told who reported it/);
  }

  get reason(): Locator {
    return this.dialog.getByLabel('What is wrong with it?');
  }

  get note(): Locator {
    return this.dialog.getByLabel('Anything to add?');
  }

  get send(): Locator {
    return this.dialog.getByRole('button', { name: 'Send report' });
  }

  get alreadyReported(): Locator {
    return this.dialog.getByText(/already reported this listing/);
  }

  async report(reason: string, note?: string): Promise<void> {
    await this.reason.selectOption({ label: reason });

    if (note) {
      await this.note.fill(note);
    }

    await this.send.click();
  }
}

/** What a member is told once a report is on its way. */
export class ReportSentPage {
  constructor(private readonly page: Page) {}

  get heading(): Locator {
    return this.page.getByRole('heading', { name: /a moderator will look at this/ });
  }

  /** The promise, repeated where it can be read after the dialogue has gone. */
  get anonymity(): Locator {
    return this.page.getByText(/is not told who reported it/);
  }

  /** The thing people get wrong: reporting flags a listing, it does not remove it. */
  get stillUp(): Locator {
    return this.page.getByText(/rather than removing it/);
  }

  get backToTheBoard(): Locator {
    return this.page.getByRole('link', { name: 'Back to the board' });
  }
}

/** The two queues a moderator works through, on one page. */
export class ModerationQueuePage {
  constructor(private readonly page: Page) {}

  get heading(): Locator {
    return this.page.getByRole('heading', { name: 'Moderator tools' });
  }

  get reportedListings(): Locator {
    return this.page.getByRole('region', { name: 'Reported listings' });
  }

  get waitingToJoin(): Locator {
    return this.page.getByRole('region', { name: 'Members waiting to join' });
  }

  get flaggedRows(): Locator {
    return this.reportedListings.locator('.list-row');
  }

  get pendingRows(): Locator {
    return this.waitingToJoin.locator('.list-row');
  }

  get nothingWaiting(): Locator {
    return this.page.getByRole('heading', { name: 'Nothing is waiting' });
  }

  /** The row for one flagged listing, found by the title a moderator would recognise. */
  flagged(title: string): Locator {
    return this.flaggedRows.filter({ hasText: title });
  }

  /** The row for one applicant. */
  applicant(displayName: string): Locator {
    return this.pendingRows.filter({ hasText: displayName });
  }

  async leaveItUp(title: string): Promise<void> {
    await this.flagged(title).getByRole('button', { name: 'Leave it up' }).click();
  }

  async beginRemoving(title: string): Promise<void> {
    await this.flagged(title).getByRole('button', { name: 'Remove it' }).click();
  }

  async approve(displayName: string): Promise<void> {
    await this.applicant(displayName).getByRole('button', { name: 'Approve' }).click();
  }

  async beginDeclining(displayName: string): Promise<void> {
    await this.applicant(displayName).getByRole('button', { name: 'Decline' }).click();
  }

  get removeDialog(): Locator {
    return this.page.getByRole('dialog', { name: 'Remove this listing?' });
  }

  get declineDialog(): Locator {
    return this.page.getByRole('dialog', { name: 'Decline this member?' });
  }

  get confirmRemove(): Locator {
    return this.removeDialog.getByRole('button', { name: 'Remove it' });
  }

  get confirmDecline(): Locator {
    return this.declineDialog.getByRole('button', { name: 'Decline' });
  }

  async goto(): Promise<void> {
    await this.page.goto('/moderation/queue');
  }

  /**
   * Waits for the queues to arrive.
   *
   * The screen renders and then fetches, so anything reading the rows imperatively - counting
   * them, filtering them - has to wait first or it reads an empty page. An `expect(locator)`
   * assertion retries on its own; a `count()` does not.
   */
  async waitForRows(): Promise<void> {
    await expect(this.page.locator('.list-row').first()).toBeVisible();
  }
}
