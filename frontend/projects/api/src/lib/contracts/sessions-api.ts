import { Session } from '../models/session';

/**
 * Signing in, renewing, and signing out.
 *
 * An abstract class rather than an interface, because Angular needs something that exists at
 * run time to inject against. Screens depend on this; dependency injection supplies the client.
 */
export abstract class ISessionsApi {
  /** Asks for a sign-in link. Answers the same way whether or not the address is registered. */
  abstract requestLink(emailAddress: string): Promise<void>;

  /** Exchanges the secret from a link for a session. */
  abstract exchange(token: string): Promise<Session>;

  /** Renews without another trip to the inbox. The refresh token travels as a cookie. */
  abstract refresh(): Promise<Session>;

  abstract signOut(): Promise<void>;
}
