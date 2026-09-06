import { Injectable, computed, inject, signal } from '@angular/core';
import { FlaggedListing, MODERATION_SERVICE, PendingMember } from '@barnabas/api';

/**
 * The two queues a moderator works through.
 *
 * One store rather than two, because the moderator tools are one screen and a member who has just
 * approved a listing should not see a stale count of members beside it. Both lists load together
 * and both reload after any decision.
 */
@Injectable({ providedIn: 'root' })
export class ModerationStore {
  private readonly moderation = inject(MODERATION_SERVICE);

  private readonly flagged = signal<readonly FlaggedListing[]>([]);
  private readonly pending = signal<readonly PendingMember[]>([]);

  readonly flaggedListings = this.flagged.asReadonly();
  readonly pendingMembers = this.pending.asReadonly();

  readonly loading = signal(false);
  readonly failed = signal(false);
  readonly deciding = signal<string | null>(null);

  readonly nothingWaiting = computed(
    () => this.flagged().length === 0 && this.pending().length === 0,
  );

  async load(): Promise<void> {
    this.loading.set(true);
    this.failed.set(false);

    try {
      const [flagged, pending] = await Promise.all([
        this.moderation.flaggedListings(),
        this.moderation.pendingMembers(),
      ]);

      this.flagged.set(flagged);
      this.pending.set(pending);
    } catch {
      this.failed.set(true);
    } finally {
      this.loading.set(false);
    }
  }

  approveListing(listingId: string): Promise<void> {
    return this.decide(listingId, () => this.moderation.approveListing(listingId));
  }

  removeListing(listingId: string): Promise<void> {
    return this.decide(listingId, () => this.moderation.removeListing(listingId));
  }

  approveMember(memberId: string): Promise<void> {
    return this.decide(memberId, () => this.moderation.approveMember(memberId));
  }

  declineMember(memberId: string): Promise<void> {
    return this.decide(memberId, () => this.moderation.declineMember(memberId));
  }

  /**
   * Applies one decision and reads both queues back.
   *
   * Reloading rather than splicing the row out. A decision can change the other list too — a
   * removed listing is one its owner is now notified about — and a moderator working a queue
   * wants what is there now, not what was there when the page opened.
   */
  private async decide(subjectId: string, act: () => Promise<void>): Promise<void> {
    if (this.deciding()) {
      return;
    }

    this.deciding.set(subjectId);
    this.failed.set(false);

    try {
      await act();
      await this.load();
    } catch {
      this.failed.set(true);
    } finally {
      this.deciding.set(null);
    }
  }
}
