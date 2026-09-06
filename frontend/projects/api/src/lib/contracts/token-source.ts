/**
 * The access token, and the ability to renew it.
 *
 * This abstraction exists to keep the dependency direction honest. The refresh
 * interceptor lives in `@barnabas/api` and needs the token; the session store lives in
 * `@barnabas/domain` and needs `ISessionsApi`. Importing the store from the api library
 * would reverse the direction. Instead the api library declares what it needs, the
 * domain library implements it, and the application binds the two together.
 */
export abstract class ITokenSource {
  /** The current access token, or null when nobody is signed in. */
  abstract token(): string | null;

  /**
   * Renews the access token, reporting whether it succeeded.
   *
   * Implementations must share one in-flight renewal across concurrent callers. Several
   * requests can meet a 401 at the same moment, and because the server rotates the
   * refresh token on use, a second renewal would invalidate the first and sign the
   * member out mid-session.
   */
  abstract refresh(): Promise<boolean>;

  /** Forgets the token, locally, whatever the server said. */
  abstract clear(): void;
}
