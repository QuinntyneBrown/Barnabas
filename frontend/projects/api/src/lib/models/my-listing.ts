import { ListingKind } from './listing-kind';
import { ListingStatus } from './listing-status';

/** One of the member's own listings, and how many people are waiting on it. */
export interface MyListing {
  readonly listingId: string;
  readonly kind: ListingKind;
  readonly status: ListingStatus;
  readonly title: string;
  readonly price: number | null;
  readonly openRequestCount: number;

  /**
   * Whether it can go back on the board.
   *
   * Answered by the API. A closed-out loan and a shelved listing are both archived, so the
   * status cannot tell them apart and the screen must not try.
   */
  readonly canBeRestored: boolean;
}
