/**
 * The photo now on a listing, and where to fetch it from.
 *
 * Two addresses, not one. The board asks for the smaller rendition and the listing screen for the
 * larger, and the server decides which is which — a client that built the second address by
 * appending to the first would be guessing at somebody else's routing.
 */
export interface AttachedPhoto {
  readonly listingId: string;
  readonly photoId: string;
  readonly url: string;
  readonly boardUrl: string;
}
