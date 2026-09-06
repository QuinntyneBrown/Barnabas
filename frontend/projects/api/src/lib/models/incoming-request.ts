import { ListingKind } from './listing-kind';
import { RequestStatus } from './request-status';

/**
 * One request made against a listing the member owns.
 *
 * The message is here because it is the whole basis for the decision, and the terms because a
 * loan is decided on when the thing comes back.
 */
export interface IncomingRequest {
  readonly requestId: string;
  readonly requesterId: string;
  readonly requesterDisplayName: string;
  readonly listingId: string;
  readonly listingTitle: string;
  readonly kind: ListingKind;
  readonly message: string;
  readonly terms: string;
  readonly status: RequestStatus;
  readonly madeAt: string;
}
