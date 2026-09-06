import { AvailabilityWindow } from './availability-window';
import { ListingKind } from './listing-kind';
import { ListingStatus } from './listing-status';

/**
 * One listing, as its own screen shows it.
 *
 * `isOwnedByCaller` is answered by the API rather than worked out here by comparing identifiers.
 * The owner sees what they can do with a listing and a visitor sees a way to ask for it, and
 * those are different screens; deciding which on the server means they cannot drift apart.
 */
export interface ListingDetail {
  readonly listingId: string;
  readonly kind: ListingKind;
  readonly title: string;
  readonly description: string;
  readonly category: string;
  readonly neighbourhood: string;
  readonly status: ListingStatus;
  readonly ownerId: string;
  readonly ownerDisplayName: string;
  readonly price: number | null;
  readonly returnBy: string | null;
  readonly condition: string | null;
  readonly availabilityWindows: readonly AvailabilityWindow[];
  readonly postedAt: string;
  readonly isOwnedByCaller: boolean;
}
