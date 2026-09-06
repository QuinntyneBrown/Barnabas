import { InjectionToken } from '@angular/core';

/**
 * The access token, and the ability to renew it.
 *
 * A contract with no implementation beside it, and that is the point of it. The refresh
 * interceptor lives in `@barnabas/api` and needs the token; the session store lives in
 * `@barnabas/domain` and needs `ISessionService`. Importing the store from here would reverse the
 * direction. Instead this library declares what it needs, the domain library implements it, and
 * the application binds the two together.
 */
export interface ITokenService {
  /** The current access token, or null when nobody is signed in. */
  token(): string | null;

  /**
   * Renews the access token, reporting whether it succeeded.
   *
   * Implementations must share one in-flight renewal across concurrent callers. Several requests
   * can meet a 401 at the same moment, and because the server rotates the refresh token on use, a
   * second renewal would invalidate the first and sign the member out mid-session.
   */
  refresh(): Promise<boolean>;

  /** Forgets the token, locally, whatever the server said. */
  clear(): void;
}

export const TOKEN_SERVICE = new InjectionToken<ITokenService>('TOKEN_SERVICE');
