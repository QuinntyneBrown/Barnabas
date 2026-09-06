/** What a notification is about. */
export type NotificationKind =
  | 'RequestReceived'
  | 'RequestAccepted'
  | 'RequestDeclined'
  | 'MessageReceived'
  | 'ListingRemoved';

/**
 * Something that happened, told to the one member it concerns.
 *
 * It carries identifiers rather than a path. Where a thing lives is the client's business, and a
 * URL stored on the server would be wrong the first time a route changed — but every kind carries
 * what its destination needs, so none of them leads nowhere.
 */
export interface Notification {
  readonly notificationId: string;
  readonly kind: NotificationKind;
  readonly subjectMemberId: string | null;
  readonly subjectMemberDisplayName: string | null;
  readonly listingId: string | null;
  readonly listingTitle: string | null;
  readonly requestId: string | null;
  readonly threadId: string | null;
  readonly createdAt: string;
  readonly unread: boolean;
}

/** One kind, and whether the member wants to hear about it. */
export interface NotificationPreference {
  readonly kind: NotificationKind;
  readonly enabled: boolean;
}
