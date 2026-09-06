import { Injectable, inject, signal } from '@angular/core';
import { ApiError, MODERATION_SERVICE, ReportReason } from '@barnabas/api';

/**
 * Reporting a listing, which is a member's action rather than a moderator's.
 *
 * Separate from the queue store because the two have nothing in common but a table: every member
 * can report, and one screen in the product reads the queue. Loading the queue's state into every
 * listing page would be asking for the thing a member is not allowed to see.
 */
@Injectable({ providedIn: 'root' })
export class ReportStore {
  private readonly moderation = inject(MODERATION_SERVICE);

  readonly sending = signal(false);
  readonly failed = signal(false);

  /** True when this member has already reported this listing. */
  readonly alreadyReported = signal(false);

  async report(listingId: string, reason: ReportReason, note: string | null): Promise<boolean> {
    if (this.sending()) {
      return false;
    }

    this.sending.set(true);
    this.failed.set(false);
    this.alreadyReported.set(false);

    try {
      await this.moderation.report(listingId, reason, note?.trim() ? note.trim() : null);

      return true;
    } catch (error) {
      // Reported once, permanently. Saying so is kinder than a generic failure, because the member
      // did nothing wrong and their complaint is already on a moderator's list.
      if (error instanceof ApiError && error.status === 409) {
        this.alreadyReported.set(true);
      } else {
        this.failed.set(true);
      }

      return false;
    } finally {
      this.sending.set(false);
    }
  }

  reset(): void {
    this.sending.set(false);
    this.failed.set(false);
    this.alreadyReported.set(false);
  }
}

