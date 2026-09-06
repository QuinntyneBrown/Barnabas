import { Message } from './message';
import { RequestStatus } from './request-status';

/**
 * One conversation, with what it is about.
 *
 * The listing and the standing of the request travel with the messages because a thread is not
 * a conversation in the abstract: it exists because somebody asked for something.
 */
export interface ThreadDetail {
  readonly threadId: string;
  readonly listingId: string;
  readonly listingTitle: string;
  readonly otherMemberId: string;
  readonly otherMemberDisplayName: string;
  readonly requestStatus: RequestStatus;
  readonly messages: readonly Message[];
}
