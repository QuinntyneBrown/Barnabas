/**
 * What a member fills in to ask for something being given away.
 *
 * A message and when they could collect it. No return date, because ownership transfers, and
 * nothing about money, because a gift has no price.
 */
export interface MakeGiftRequest {
  readonly message: string;
  readonly pickupAt: string | null;
}
