/**
 * The fields a Lend listing collects.
 *
 * There is no price, and the form renders none. A loan is not a sale, and the API rejects a
 * price outright rather than ignoring one.
 *
 * A date left blank travels as null rather than as an empty string. An empty string is not a
 * date, and sending one would fail during deserialization - so the member would be shown the
 * serializer complaining about a value instead of the form marking a field.
 */
export interface PostLendListing {
  readonly title: string;
  readonly description: string;
  readonly category: string;
  readonly neighbourhood: string;
  readonly returnBy: string | null;
}
