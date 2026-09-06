/**
 * One applicant, as the moderator deciding about them sees it.
 *
 * The reason they gave is here and nowhere else. It is written for the moderators rather than for
 * the congregation, so the directory and the public profile do not carry it.
 */
export interface PendingMember {
  readonly memberId: string;
  readonly displayName: string;
  readonly neighbourhood: string;
  readonly reasonForJoining: string | null;
}
