/** A member's own profile, and what their congregation offers them to choose from. */
export interface MyProfile {
  readonly memberId: string;
  readonly displayName: string;

  /**
   * The caller's own address.
   *
   * Present here and on no other member's profile: this is the screen that says where a sign-in
   * link goes. It is shown and not edited — it is the only way back in.
   */
  readonly emailAddress: string;

  readonly neighbourhood: string;
  readonly description: string | null;
  readonly helpTags: readonly string[];

  /** This congregation's neighbourhoods, in the order the parish office wrote them. */
  readonly neighbourhoods: readonly string[];

  /** The kinds of help this congregation recognises. */
  readonly availableHelpTags: readonly string[];
}

/** What a member changes about themselves. */
export interface EditMyProfile {
  readonly displayName: string;
  readonly neighbourhood: string;
  readonly description: string | null;
  readonly helpTags: readonly string[];
}

/** One of a member's listings, as their profile lists it. */
export interface ProfileListing {
  readonly listingId: string;
  readonly kind: string;
  readonly title: string;
  readonly price: number | null;
}

/**
 * Another member, as the congregation sees them.
 *
 * There is no email address on this type, and no way to message them. A request against a
 * listing, once accepted, is the only thing that opens a thread.
 */
export interface MemberProfile {
  readonly memberId: string;
  readonly displayName: string;
  readonly neighbourhood: string;
  readonly description: string | null;
  readonly helpTags: readonly string[];
  readonly activeListings: readonly ProfileListing[];
}

/** One row of the congregation directory. */
export interface DirectoryMember {
  readonly memberId: string;
  readonly displayName: string;
  readonly neighbourhood: string;
  readonly helpTags: readonly string[];
}
