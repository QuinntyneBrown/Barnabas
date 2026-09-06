import { ListingKind } from './listing-kind';

/** One listing a search found. */
export interface SearchResult {
  readonly listingId: string;
  readonly kind: ListingKind;
  readonly title: string;
  readonly ownerDisplayName: string;
  readonly neighbourhood: string;
  readonly price: number | null;
}

/**
 * A page of results, with the term that produced them.
 *
 * The term comes back rather than being remembered by the screen, so a search that found nothing
 * can still say what was searched — which is the whole of what that screen has to work with.
 */
export interface SearchPage {
  readonly term: string;
  readonly results: readonly SearchResult[];
  readonly nextCursor: string | null;
}

/** What narrows a search. */
export interface SearchFilters {
  readonly kind?: ListingKind;
  readonly neighbourhood?: string;
  readonly maxPrice?: number;
}
