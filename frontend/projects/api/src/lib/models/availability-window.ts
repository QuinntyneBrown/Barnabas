/**
 * One window a Help listing declares.
 *
 * The identifier is here because a request names the window it wants. Two windows holding the
 * same day and hours are still two different offers, so they cannot be told apart by value.
 */
export interface AvailabilityWindow {
  readonly availabilityWindowId: string;
  readonly day: string;
  readonly startsAt: string;
  readonly endsAt: string;
}
