import { ListingKind } from './listing-kind';
import { RequestStatus } from './request-status';

/**
 * One request the member made, and what became of it.
 *
 * `threadId` is present only for an accepted request. A pending one has no thread because
 * nothing has been decided, and a declined one has none because declining opens none - so the
 * screen decides what to offer from the row in front of it rather than by asking again.
 */
export interface MyRequest {
  readonly requestId: string;
  readonly listingId: string;
  readonly listingTitle: string;
  readonly kind: ListingKind;
  readonly ownerDisplayName: string;
  readonly neighbourhood: string;
  readonly terms: string;
  readonly status: RequestStatus;
  readonly madeAt: string;
  readonly threadId: string | null;
}
