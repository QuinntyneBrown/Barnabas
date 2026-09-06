/**
 * The decision, and the thread it opened.
 *
 * The thread comes back with the decision so the confirmation can offer the conversation
 * directly. Accepting is the moment two members are put in touch, and making the owner go and
 * find the thread afterwards would be a strange place to stop.
 */
export interface AcceptedRequest {
  readonly requestId: string;
  readonly threadId: string;
}
