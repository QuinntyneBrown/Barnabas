import { ChangeDetectionStrategy, Component, effect, inject, input, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { IListingsApi, ListingDetail } from '@barnabas/api';

import { ConfirmDialogComponent } from '../dialogs/confirm-dialog.component';

/**
 * One listing, shown two ways.
 *
 * A visitor sees a way to ask for it. The owner sees what they can do with it and no way to ask
 * for it at all - which is both L2-041 and L2-062: a member cannot request their own listing,
 * and the screen does not offer them the chance to try.
 *
 * Which of the two is not decided here by comparing identifiers. The API says whose it is.
 */
@Component({
  selector: 'bar-listing-detail',
  imports: [RouterLink, ConfirmDialogComponent],
  templateUrl: './listing-detail.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ListingDetailComponent {
  private readonly listings = inject(IListingsApi);
  private readonly router = inject(Router);

  readonly listingId = input.required<string>();

  readonly listing = signal<ListingDetail | null>(null);
  readonly missing = signal(false);

  constructor() {
    effect(() => {
      void this.load(this.listingId());
    });
  }

  async closeOut(): Promise<void> {
    await this.listings.closeOut(this.listingId());

    await this.router.navigate(['/my-listings']);
  }

  private async load(listingId: string): Promise<void> {
    this.missing.set(false);

    try {
      this.listing.set(await this.listings.get(listingId));
    } catch {
      this.listing.set(null);
      this.missing.set(true);
    }
  }
}
