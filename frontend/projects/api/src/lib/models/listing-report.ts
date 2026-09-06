import { ListingKind } from './listing-kind';
import { ReportReason } from './report-reason';

/**
 * One complaint, as a moderator sees it.
 *
 * The only model in the workspace that names a reporter. Nothing a member sees carries one, which
 * is what makes L2-081 a property of the types rather than a rule somebody has to remember.
 */
export interface ListingReport {
  readonly reportId: string;
  readonly reason: ReportReason;
  readonly note: string | null;
  readonly reporterId: string;
  readonly reporterDisplayName: string;
  readonly reportedAt: string;
}

/** A flagged listing, its poster, and every open complaint about it. */
export interface FlaggedListing {
  readonly listingId: string;
  readonly kind: ListingKind;
  readonly title: string;
  readonly description: string;
  readonly ownerId: string;
  readonly ownerDisplayName: string;
  readonly flaggedAt: string;
  readonly reports: readonly ListingReport[];
}

/** What the member who reported it is told back. Nothing about the outcome. */
export interface ReportedListing {
  readonly reportId: string;
  readonly listingId: string;
}
