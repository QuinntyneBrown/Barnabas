import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MyListing } from '@barnabas/api';
import { MyListingsStore, wordsFor } from '@barnabas/domain';

import { ConfirmDialogComponent } from '@barnabas/components';

/** Which of the member's own listings they are looking at. */
export type MyListingsTab = 'active' | 'archived';

/**
 * The listings a member owns, and what is waiting on each.
 *
 * The open-request count is the reason this screen is not just a filtered board: what an owner
 * needs to know is which of their listings somebody is waiting on. It is a link rather than a
 * label, and it leads to the requests themselves.
 *
 * Two tabs rather than two screens, because a member putting something away and finding it again
 * is doing one thing. Whether an archived listing can come back is a property of that listing —
 * a returned loan cannot — and the API answers it, so the row shows a restore action only where
 * there is one to offer.
 *
 * Every verb here comes from the shared vocabulary. It used to be a switch in this class, which
 * disagreed with the same switch elsewhere about what a Give listing is marked as.
 *
 * The close-out dialog's heading is fixed rather than named after the kind. A native `dialog` is
 * put into the top layer by `showModal()` in the same tick as the signal that would change its
 * heading, and the binding has not flushed by then - so a per-kind heading showed the previous
 * one. The verb belongs on the button, which is where L2-037 asks for it.
 */
@Component({
  selector: 'bar-my-listings',
  imports: [RouterLink, ConfirmDialogComponent],
  templateUrl: './my-listings.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MyListingsComponent {
  private readonly store = inject(MyListingsStore);

  readonly loading = this.store.loading;

  readonly tab = signal<MyListingsTab>('active');

  readonly showing = computed(() =>
    this.tab() === 'active' ? this.store.active() : this.store.archived(),
  );

  readonly activeCount = computed(() => this.store.active().length);
  readonly archivedCount = computed(() => this.store.archived().length);

  /** The listing the open dialog is about. */
  readonly closing = signal<MyListing | null>(null);
  readonly archiving = signal<MyListing | null>(null);
  readonly deleting = signal<MyListing | null>(null);

  constructor() {
    void this.store.load();
  }

  show(tab: MyListingsTab): void {
    this.tab.set(tab);
  }

  /** L2-037: the verb belonging to the kind — taken, sold, or booked. */
  closeOutLabel(listing: MyListing): string {
    return wordsFor(listing.kind).closeOut;
  }

  requestLabel(listing: MyListing): string {
    return listing.openRequestCount === 1 ? '1 request' : `${listing.openRequestCount} requests`;
  }

  async confirmCloseOut(): Promise<void> {
    const listing = this.closing();

    if (listing) {
      await this.store.closeOut(listing.listingId);
    }

    this.closing.set(null);
  }

  async confirmArchive(): Promise<void> {
    const listing = this.archiving();

    if (listing) {
      await this.store.archive(listing.listingId);
    }

    this.archiving.set(null);
  }

  async confirmDelete(): Promise<void> {
    const listing = this.deleting();

    if (listing) {
      await this.store.remove(listing.listingId);
    }

    this.deleting.set(null);
  }

  async restore(listing: MyListing): Promise<void> {
    await this.store.restore(listing.listingId);

    // Back to where it now is, so the member sees it land rather than watching it vanish.
    this.tab.set('active');
  }
}
