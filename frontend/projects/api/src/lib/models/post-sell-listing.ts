/**
 * The fields a Sell listing collects.
 *
 * The price is a stated asking figure in Canadian dollars, settled between the two members in
 * person. Barnabas neither takes it nor holds it.
 *
 * A price left blank travels as null rather than as an empty string, for the same reason a blank
 * date does: an empty string is not a number, and sending one would fail during deserialization
 * so the member would see the serializer complain about a value instead of the form marking a
 * field.
 */
export interface PostSellListing {
  readonly title: string;
  readonly description: string;
  readonly category: string;
  readonly neighbourhood: string;
  readonly condition: string;
  readonly price: number | null;
}
