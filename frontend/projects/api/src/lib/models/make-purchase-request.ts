/**
 * What a member fills in to ask to buy something.
 *
 * The same two fields a gift request collects. The price is on the listing and is settled
 * between the two members in person, so there is no payment field here and the API refuses one.
 */
export interface MakePurchaseRequest {
  readonly message: string;
  readonly pickupAt: string | null;
}
