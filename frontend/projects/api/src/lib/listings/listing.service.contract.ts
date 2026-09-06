import { InjectionToken } from '@angular/core';

import { BoardPage } from '../models/board-page';
import { ClosedOutListing } from '../models/closed-out-listing';
import { ListingDetail } from '../models/listing-detail';
import { ListingKind } from '../models/listing-kind';
import { MyListing } from '../models/my-listing';
import { PostLendListing } from '../models/post-lend-listing';
import { PostedListing } from '../models/posted-listing';

/** The board, and the listings on it. */
export interface IListingService {
  board(kind?: ListingKind, cursor?: string): Promise<BoardPage>;

  get(listingId: string): Promise<ListingDetail>;

  mine(): Promise<readonly MyListing[]>;

  /** Only Lend can be posted in this slice. The other three kinds are a later one. */
  postLend(listing: PostLendListing): Promise<PostedListing>;

  /** Records that it has gone. The outcome is the listing's own kind to decide, not this one's. */
  closeOut(listingId: string): Promise<ClosedOutListing>;
}

/**
 * What a consumer injects.
 *
 * No default factory. The application binds this to an implementation, and a factory here would
 * quietly make that binding optional - a host that forgot to bind would get a working service
 * instead of an error, which is the one thing a seam must not do.
 */
export const LISTING_SERVICE = new InjectionToken<IListingService>('LISTING_SERVICE');
