import { BoardPage } from '../models/board-page';
import { ClosedOutListing } from '../models/closed-out-listing';
import { ListingDetail } from '../models/listing-detail';
import { ListingKind } from '../models/listing-kind';
import { MyListing } from '../models/my-listing';
import { PostLendListing } from '../models/post-lend-listing';
import { PostedListing } from '../models/posted-listing';

/** The board, and the listings on it. */
export abstract class IListingsApi {
  abstract board(kind?: ListingKind, cursor?: string): Promise<BoardPage>;

  abstract get(listingId: string): Promise<ListingDetail>;

  abstract mine(): Promise<readonly MyListing[]>;

  /** Only Lend can be posted in this slice. The other three kinds are a later one. */
  abstract postLend(listing: PostLendListing): Promise<PostedListing>;

  /** Records that it has gone. The outcome is the listing's own kind to decide, not this one's. */
  abstract closeOut(listingId: string): Promise<ClosedOutListing>;
}
