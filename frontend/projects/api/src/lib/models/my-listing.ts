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
}
