/**
 * The fields a listing may be corrected in.
 *
 * The kind is not among them. A listing's kind decides its fields, its request form and its
 * close-out verb, and requests already made against it were made on the strength of that kind.
 * The API refuses a submitted kind by name rather than ignoring one.
 */
export interface EditListing {
  readonly title: string;
  readonly description: string;
  readonly category: string;
  readonly neighbourhood: string;
}
