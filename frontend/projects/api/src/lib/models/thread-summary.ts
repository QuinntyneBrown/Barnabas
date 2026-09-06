/**
 * One conversation, as a row in the member's messages.
 *
 * `unread` is computed for whoever is reading. Two members looking at the same conversation can
 * legitimately disagree about whether it is unread.
 */
export interface ThreadSummary {
  readonly threadId: string;
  readonly otherMemberId: string;
  readonly otherMemberDisplayName: string;
  readonly listingId: string;
  readonly listingTitle: string;
  readonly latestMessage: string;
  readonly latestAt: string | null;
  readonly unread: boolean;
}
