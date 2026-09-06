import { InjectionToken } from '@angular/core';

import { FlaggedListing, ReportedListing } from '../models/listing-report';
import { PendingMember } from '../models/pending-member';
import { ReportReason } from '../models/report-reason';

/**
 * Reporting a listing, and the two queues a moderator works through.
 *
 * One contract for both halves because they are one feature, and because the seam a test binds a
 * mock to should be the feature rather than the endpoint. Reporting is something any member does;
 * everything else here answers 403 to anybody but a moderator, regardless of what the screen chose
 * to show.
 */
export interface IModerationService {
  report(listingId: string, reason: ReportReason, note: string | null): Promise<ReportedListing>;

  flaggedListings(): Promise<readonly FlaggedListing[]>;

  /** Clears the flag. The listing stays exactly where it was. */
  approveListing(listingId: string): Promise<void>;

  /** Takes it off the board and tells its owner. */
  removeListing(listingId: string): Promise<void>;

  pendingMembers(): Promise<readonly PendingMember[]>;

  approveMember(memberId: string): Promise<void>;

  declineMember(memberId: string): Promise<void>;
}

export const MODERATION_SERVICE = new InjectionToken<IModerationService>('MODERATION_SERVICE');
