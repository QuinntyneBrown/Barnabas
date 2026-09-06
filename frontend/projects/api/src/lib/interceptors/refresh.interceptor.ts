import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, from, switchMap, throwError } from 'rxjs';

import { TOKEN_SERVICE } from '../tokens/token.service.contract';
import { SKIP_REFRESH } from './skip-refresh';

/**
 * Renews once on a 401, then retries the request that met it.
 *
 * The access token is deliberately short, so a member with a live session will meet an expired
 * one in the ordinary course of reading a screen. Renewing here means they never see it.
 *
 * Only one renewal runs at a time, and that is enforced inside the token source rather than
 * here: several requests can meet a 401 in the same instant, and because the server rotates the
 * refresh token on use, a second renewal would invalidate the first and sign the member out
 * mid-session.
 */
export const refreshInterceptor: HttpInterceptorFn = (request, next) => {
  const tokens = inject(TOKEN_SERVICE);

  if (request.context.get(SKIP_REFRESH)) {
    return next(request);
  }

  return next(request).pipe(
    catchError((failure: unknown) => {
      if (!(failure instanceof HttpErrorResponse) || failure.status !== 401) {
        return throwError(() => failure);
      }

      return from(tokens.refresh()).pipe(
        switchMap((renewed) => {
          if (!renewed) {
            return throwError(() => failure);
          }

          const token = tokens.token();

          return next(
            token ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : request,
          );
        }),
      );
    }),
  );
};
