import { BoardListing } from './board-listing';
import { ListingKind } from './listing-kind';

/** A page of the board, with the counts the filter chips display. */
export interface BoardPage {
  readonly listings: readonly BoardListing[];
  readonly nextCursor: string | null;
  readonly counts: Readonly<Partial<Record<ListingKind, number>>>;
}
