import { ListingKind } from './listing-kind';

/**
 * One placard on the board.
 *
 * `kind` is carried as its own name rather than as a colour, because the placard renders it as
 * text as well as a field colour. Colour alone cannot carry that meaning for a member with a
 * colour vision deficiency, or for anyone reading in sunlight.
 */
export interface BoardListing {
  readonly listingId: string;
  readonly kind: ListingKind;
  readonly title: string;
  readonly ownerDisplayName: string;
  readonly neighbourhood: string;
  readonly price: number | null;
  readonly offerInOwnWords: string | null;

  /** The days a Help offer is open. Null on every other kind. */
  readonly availability: string | null;
}
