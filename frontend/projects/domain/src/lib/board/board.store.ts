import { Injectable, computed, inject, signal } from '@angular/core';
import { ApiError, BoardListing, IListingsApi, ListingKind } from '@barnabas/api';

/**
 * The board, as three signals the screen reads rather than subscribes to.
 *
 * Loading and failure are states of the board rather than exceptions the component catches: a
 * congregation that has posted nothing sees an invitation, a board still arriving holds its
 * geometry and announces the wait, and a board that could not be fetched says so and offers a
 * retry. All three are ordinary, so all three are modelled.
 */
@Injectable({ providedIn: 'root' })
export class BoardStore {
  private readonly listings = inject(IListingsApi);

  private readonly all = signal<readonly BoardListing[]>([]);
  private readonly counts = signal<Readonly<Partial<Record<ListingKind, number>>>>({});
  private readonly filter = signal<ListingKind | null>(null);

  readonly loading = signal(false);
  readonly failed = signal(false);

  readonly placards = this.all.asReadonly();
  readonly kind = this.filter.asReadonly();

  readonly isEmpty = computed(() => !this.loading() && !this.failed() && this.all().length === 0);

  readonly total = computed(() =>
    Object.values(this.counts()).reduce((running, count) => running + count, 0),
  );

  async load(kind: ListingKind | null = this.filter()): Promise<void> {
    this.filter.set(kind);
    this.loading.set(true);
    this.failed.set(false);

    try {
      const page = await this.listings.board(kind ?? undefined);

      this.all.set(page.listings);
      this.counts.set(page.counts);
    } catch (failure) {
      this.failed.set(failure instanceof ApiError || failure instanceof Error);
      this.all.set([]);
    } finally {
      this.loading.set(false);
    }
  }
}
