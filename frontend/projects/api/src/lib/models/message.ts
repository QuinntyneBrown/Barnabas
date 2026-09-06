/**
 * One message, attributed.
 *
 * `sentByCaller` is answered by the API so the screen can set the member's own words apart from
 * the other party's without holding its own identifier to compare against.
 */
export interface Message {
  readonly messageId: string;
  readonly senderId: string;
  readonly senderDisplayName: string;
  readonly body: string;
  readonly sentAt: string;
  readonly sentByCaller: boolean;
}
