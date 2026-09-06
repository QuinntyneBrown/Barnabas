import { Injectable, computed, inject, signal } from '@angular/core';
import { LISTING_SERVICE, MyListing } from '@barnabas/api';

/**
 * The listings a member owns, active and put away, and what they can do with each.
 *
 * Both views come from one read. A member's own listings are a handful rather than a board's
 * worth, so fetching them once and splitting by status here costs less than two round trips and
 * keeps the two tabs from disagreeing about what was just archived.
 */
@Injectable({ providedIn: 'root' })
export class MyListingsStore {
  private readonly listings = inject(LISTING_SERVICE);

  private readonly own = signal<readonly MyListing[]>([]);

  readonly loading = signal(false);
  readonly mine = this.own.asReadonly();

  readonly active = computed(() => this.own().filter((listing) => listing.status === 'Active'));

  /**
   * Everything no longer on the board — shelved and closed out alike.
   *
   * The two are not separated here. A member looking at what they have put away is looking at one
   * list; whether a particular listing can come back is a property of that listing, and the row
   * says so rather than the tab implying it.
   */
  readonly archived = computed(() => this.own().filter((listing) => listing.status !== 'Active'));

  async load(): Promise<void> {
    this.loading.set(true);

    try {
      this.own.set(await this.listings.mine(true));
    } finally {
      this.loading.set(false);
    }
  }

  async closeOut(listingId: string): Promise<void> {
    await this.listings.closeOut(listingId);

    await this.load();
  }

  async archive(listingId: string): Promise<void> {
    await this.listings.archive(listingId);

    await this.load();
  }

  async restore(listingId: string): Promise<void> {
    await this.listings.restore(listingId);

    await this.load();
  }

  async remove(listingId: string): Promise<void> {
    await this.listings.remove(listingId);

    await this.load();
  }
}
