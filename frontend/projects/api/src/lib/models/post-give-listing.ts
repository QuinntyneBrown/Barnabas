/**
 * The fields a Give listing collects.
 *
 * The same four every listing carries, and nothing else. That a gift is free and collected in
 * person follows from its kind rather than from a field, so the form says it and sends nothing.
 */
export interface PostGiveListing {
  readonly title: string;
  readonly description: string;
  readonly category: string;
  readonly neighbourhood: string;
}
