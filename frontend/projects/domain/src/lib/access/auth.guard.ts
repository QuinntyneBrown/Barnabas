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
