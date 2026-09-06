/**
 * What a member fills in to ask to borrow something.
 *
 * A date left blank travels as null rather than as an empty string, so the form marks the field
 * rather than the serializer refusing the value.
 */
export interface MakeLoanRequest {
  readonly message: string;
  readonly pickupOn: string | null;
  readonly returnBy: string | null;
  readonly loanAcknowledged: boolean;
}
