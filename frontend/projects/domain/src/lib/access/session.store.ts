import { Injectable, computed, inject, signal } from '@angular/core';
import { ISessionsApi, ITokenSource } from '@barnabas/api';

/**
 * The member's session, and the only thing that knows the access token.
 *
 * The token is held in a signal and nowhere else - not in `localStorage`, not in a cookie this
 * code can read. It is short-lived by design, and the long-lived half of the pair is an HttpOnly
 * cookie the browser returns on its own, which is what keeps it out of reach of script on the
 * page.
 *
 * This implements `ITokenSource` so the refresh interceptor in `@barnabas/api` can reach it
 * without that library depending on this one. The dependency runs the right way round: the api
 * library declares what it needs, this implements it, and the application binds the two.
 */
@Injectable({ providedIn: 'root' })
export class SessionStore extends ITokenSource {
  private readonly sessions = inject(ISessionsApi);
  private readonly accessToken = signal<string | null>(null);

  /** The renewal in flight, shared by everyone who asks for one while it runs. */
  private renewal: Promise<boolean> | null = null;

  readonly isSignedIn = computed(() => this.accessToken() !== null);

  token(): string | null {
    return this.accessToken();
  }

  /** Asks for a sign-in link. The screen that follows says the same thing either way. */
  requestLink(emailAddress: string): Promise<void> {
    return this.sessions.requestLink(emailAddress);
  }

  /** Exchanges the secret from a followed link for a session. */
  async signIn(token: string): Promise<void> {
    const session = await this.sessions.exchange(token);

    this.accessToken.set(session.accessToken);
  }

  /**
   * Recovers a session from the refresh cookie, if the browser still holds one.
   *
   * This is what makes a reload keep the member signed in. The access token lives in memory and
   * is gone the moment the page is; the cookie is not.
   */
  async restore(): Promise<boolean> {
    return this.isSignedIn() || (await this.refresh());
  }

  /** @inheritdoc */
  async refresh(): Promise<boolean> {
    // One renewal at a time. Several requests can meet a 401 in the same instant, and because
    // the server rotates the refresh token on use, a second renewal would invalidate the first
    // and sign the member out in the middle of a session.
    this.renewal ??= this.renew();

    try {
      return await this.renewal;
    } finally {
      this.renewal = null;
    }
  }

  clear(): void {
    this.accessToken.set(null);
  }

  /** Ends the session, and forgets it locally whatever the server said. */
  async signOut(): Promise<void> {
    try {
      await this.sessions.signOut();
    } finally {
      this.clear();
    }
  }

  private async renew(): Promise<boolean> {
    try {
      const session = await this.sessions.refresh();

      this.accessToken.set(session.accessToken);

      return true;
    } catch {
      this.clear();

      return false;
    }
  }
}
