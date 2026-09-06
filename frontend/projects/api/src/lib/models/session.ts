/**
 * What signing in returns.
 *
 * There is no refresh token here, and that is the contract rather than an omission: it travels
 * as an HttpOnly cookie the browser sends back on its own, out of reach of any script on the
 * page. The access token is short-lived and is held in memory, never in storage.
 */
export interface Session {
  readonly sessionId: string;
  readonly accessToken: string;
  readonly expiresOn: string;

  /**
   * Whether the board is open to this member yet.
   *
   * Renewed on every refresh, so a moderator's approval takes effect on the member's next visit
   * rather than their next sign-in.
   */
  readonly status: string;

  /**
   * What this member may do beyond being a member.
   *
   * Read from the record on every refresh, so a moderator grant works on the member's next visit
   * rather than their next sign-in.
   */
  readonly role: string;
}
