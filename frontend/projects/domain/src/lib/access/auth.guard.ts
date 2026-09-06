import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { SessionStore } from './session.store';

/**
 * Keeps the signed-in screens signed-in.
 *
 * It tries to recover a session before turning anyone away, because the access token lives in
 * memory: a member who reloads the board has no token but very likely still has a refresh
 * cookie, and sending them to sign in again would be wrong.
 */
export const authGuard: CanActivateFn = async () => {
  const session = inject(SessionStore);
  const router = inject(Router);

  if (await session.restore()) {
    return true;
  }

  return router.createUrlTree(['/sign-in']);
};

/**
 * Sends a member a moderator has not let in yet to the screen that says so.
 *
 * The API refuses them the board with 403 either way — a route guard is presentation, never the
 * rule. What this saves them is arriving at a board that answers nothing and being left to work
 * out why.
 *
 * The status comes back with the session and is renewed on every refresh, and `restore` refreshes
 * whenever there is no token in memory - so a moderator's approval takes effect on the member's
 * next visit rather than their next sign-in.
 */
export const approvedGuard: CanActivateFn = async () => {
  const session = inject(SessionStore);
  const router = inject(Router);

  if (!(await session.restore())) {
    return router.createUrlTree(['/sign-in']);
  }

  return session.isApproved() ? true : router.createUrlTree(['/join/pending']);
};
