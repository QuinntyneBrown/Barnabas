/**
 * What comes back from redeeming a code: which congregation, and the right to finish joining.
 *
 * The joining token is a bearer credential rather than the congregation's identifier. It names
 * the one congregation this code belonged to, so the profile step has nothing to choose.
 */
export interface RedeemedInvite {
  readonly congregationName: string;
  readonly neighbourhoods: readonly string[];
  readonly joiningToken: string;
  readonly expiresAt: string;
}

/** What somebody fills in to finish joining. */
export interface JoiningProfile {
  readonly joiningToken: string;
  readonly emailAddress: string;
  readonly displayName: string;
  readonly neighbourhood: string;
  readonly reasonForJoining: string | null;
}

/** Where they stand once they have joined: waiting on a moderator. */
export interface JoinedCongregation {
  readonly memberId: string;
  readonly congregationName: string;
  readonly status: string;
}
