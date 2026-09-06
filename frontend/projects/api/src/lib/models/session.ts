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
}
