import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { FlaggedListing, PendingMember, REPORT_REASONS, ReportReason } from '@barnabas/api';

import { ConfirmDialogComponent } from '@barnabas/components';
import { ModerationStore } from '@barnabas/domain';

/**
 * Reported listings and members waiting to join, on one page.
 *
 * Two queues rather than two screens because they are one job, done in one sitting, by the same
 * two or three people in a parish. Splitting them would mean a moderator checking two places to
 * find out whether anything needed them.
 *
 * Every title and name is a link to the thing it names. A moderator deciding about a listing
 * needs to read the listing, and one deciding about a person needs to see who they are —
 * L2-082 AC3 and L2-085 AC2.
 *
 * The two destructive actions are confirmed and the two safe ones are not. Approving a listing
 * leaves it exactly where it was and approving a member lets them in; removing and declining are
 * the ones a moderator should have to mean — L2-084 AC3 and L2-087 AC2.
 */
@Component({
  selector: 'bar-moderation-queue',
  imports: [RouterLink, ConfirmDialogComponent],
  templateUrl: './moderation-queue.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ModerationQueueComponent {
  private readonly moderation = inject(ModerationStore);

  readonly flaggedListings = this.moderation.flaggedListings;
  readonly pendingMembers = this.moderation.pendingMembers;
  readonly loading = this.moderation.loading;
  readonly failed = this.moderation.failed;
  readonly nothingWaiting = this.moderation.nothingWaiting;

  /** What the confirm dialog is about, set as it opens. */
  readonly listingBeingRemoved = signal<FlaggedListing | null>(null);
  readonly memberBeingDeclined = signal<PendingMember | null>(null);

  constructor() {
    void this.moderation.load();
  }

  /** The reason in the words the member who reported it was offered. */
  wordsFor(reason: ReportReason): string {
    return REPORT_REASONS.find((choice) => choice.reason === reason)?.words ?? 'Something else';
  }

  askToRemove(listing: FlaggedListing, dialog: ConfirmDialogComponent): void {
    this.listingBeingRemoved.set(listing);

    dialog.open();
  }

  askToDecline(member: PendingMember, dialog: ConfirmDialogComponent): void {
    this.memberBeingDeclined.set(member);

    dialog.open();
  }

  approveListing(listingId: string): Promise<void> {
    return this.moderation.approveListing(listingId);
  }

  async removeListing(): Promise<void> {
    const listing = this.listingBeingRemoved();

    if (listing) {
      await this.moderation.removeListing(listing.listingId);
    }

    this.listingBeingRemoved.set(null);
  }

  approveMember(memberId: string): Promise<void> {
    return this.moderation.approveMember(memberId);
  }

  async declineMember(): Promise<void> {
    const member = this.memberBeingDeclined();

    if (member) {
      await this.moderation.declineMember(member.memberId);
    }

    this.memberBeingDeclined.set(null);
  }
}
