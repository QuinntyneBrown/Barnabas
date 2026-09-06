import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';

import { TOKEN_SERVICE } from '../tokens/token.service.contract';

/** Attaches the access token, when there is one to attach. */
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const token = inject(TOKEN_SERVICE).token();

  if (!token) {
    return next(request);
  }

  return next(request.clone({ setHeaders: { Authorization: `Bearer ${token}` } }));
};
