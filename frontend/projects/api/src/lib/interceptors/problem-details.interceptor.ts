import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { catchError, throwError } from 'rxjs';

import { ApiError } from '../models/api-error';
import { ProblemDetails } from '../models/problem-details';

/**
 * Turns every failure into the one shape the screens render.
 *
 * Doing it here means no component ever handles an `HttpErrorResponse`, and no form has to know
 * that ASP.NET names its fields in PascalCase. A form marks the fields `ApiError` names and
 * moves focus to the first of them.
 */
export const problemDetailsInterceptor: HttpInterceptorFn = (request, next) =>
  next(request).pipe(
    catchError((failure: unknown) => {
      if (!(failure instanceof HttpErrorResponse)) {
        return throwError(() => failure);
      }

      const problem = isProblemDetails(failure.error) ? failure.error : null;

      return throwError(() => ApiError.fromProblemDetails(failure.status, problem));
    }),
  );

function isProblemDetails(body: unknown): body is ProblemDetails {
  return typeof body === 'object' && body !== null;
}
