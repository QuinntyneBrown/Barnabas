import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';

import { ITokenSource } from '../contracts/token-source';

/** Attaches the access token, when there is one to attach. */
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const token = inject(ITokenSource).token();

  if (!token) {
    return next(request);
  }

  return next(request.clone({ setHeaders: { Authorization: `Bearer ${token}` } }));
};
