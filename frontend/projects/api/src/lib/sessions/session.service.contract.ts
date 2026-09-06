import { InjectionToken } from '@angular/core';

import { Session } from '../models/session';

/** Signing in, renewing, and signing out. */
export interface ISessionService {
  /** Asks for a sign-in link. Answers the same way whether or not the address is registered. */
  requestLink(emailAddress: string): Promise<void>;

  /** Exchanges the secret from a link for a session. */
  exchange(token: string): Promise<Session>;

  /** Renews without another trip to the inbox. The refresh token travels as a cookie. */
  refresh(): Promise<Session>;

  signOut(): Promise<void>;
}

export const SESSION_SERVICE = new InjectionToken<ISessionService>('SESSION_SERVICE');
