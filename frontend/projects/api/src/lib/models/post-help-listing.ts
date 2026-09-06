/**
 * The fields a Help listing collects.
 *
 * Help offers time, so it carries no price and no photo, and it declares the windows in which
 * the offer actually holds. The windows are submitted without identifiers: those are minted by
 * the API when the listing is created, which is what stops a request naming a window that was
 * never declared.
 */
export interface AvailabilityWindowInput {
  readonly day: string;
  readonly startsAt: string;
  readonly endsAt: string;
}

export interface PostHelpListing {
  readonly title: string;
  readonly description: string;
  readonly category: string;
  readonly neighbourhood: string;
  readonly windows: readonly AvailabilityWindowInput[];
}
