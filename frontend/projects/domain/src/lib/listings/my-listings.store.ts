import { Injectable, inject, signal } from '@angular/core';
import { LISTING_SERVICE, MyListing } from '@barnabas/api';

/** The listings a member owns, and the close-out that takes one off the board. */
@Injectable({ providedIn: 'root' })
export class MyListingsStore {
  private readonly listings = inject(LISTING_SERVICE);

  private readonly own = signal<readonly MyListing[]>([]);

  readonly loading = signal(false);
  readonly mine = this.own.asReadonly();

  async load(): Promise<void> {
    this.loading.set(true);

    try {
      this.own.set(await this.listings.mine());
    } finally {
      this.loading.set(false);
    }
  }

  async closeOut(listingId: string): Promise<void> {
    await this.listings.closeOut(listingId);

    await this.load();
  }
}
