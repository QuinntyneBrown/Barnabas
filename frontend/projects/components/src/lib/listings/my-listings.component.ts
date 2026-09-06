import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MyListing } from '@barnabas/api';
import { MyListingsStore } from '@barnabas/domain';

import { ConfirmDialogComponent } from '../dialogs/confirm-dialog.component';

/**
 * The listings a member owns, and what is waiting on each.
 *
 * The open-request count is the reason this screen is not just a filtered board: what an owner
 * needs to know is which of their listings somebody is waiting on. It is a link rather than a
 * label, and it leads to the requests themselves.
 *
 * The close-out button reads in the vocabulary of the listing's kind. The wording is derived
 * from the kind here and the outcome is derived from the kind on the server, independently, so
 * the screen can never talk a listing into an outcome the domain would not have chosen.
 */
@Component({
  selector: 'bar-my-listings',
  imports: [RouterLink, ConfirmDialogComponent],
  templateUrl: './my-listings.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MyListingsComponent {
  private readonly store = inject(MyListingsStore);

  readonly mine = this.store.mine;
  readonly loading = this.store.loading;

  /** The listing the open dialog is about. */
  readonly closing = signal<MyListing | null>(null);

  constructor() {
    void this.store.load();
  }

  /** L2-037: Sell closes as sold, Give and Lend as taken, Help as booked. */
  closeOutLabel(listing: MyListing): string {
    switch (listing.kind) {
      case 'Sell':
        return 'Mark as sold';
      case 'Help':
        return 'Mark as booked';
      default:
        return 'Mark as taken';
    }
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
}
