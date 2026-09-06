import { ListingStatus } from './listing-status';

/** The outcome the listing's own kind chose for it. */
export interface ClosedOutListing {
  readonly listingId: string;
  readonly status: ListingStatus;
}
