import { InjectionToken } from '@angular/core';

/**
 * Where the API lives.
 *
 * Same-origin in every environment: the refresh token is an HttpOnly cookie, and same-origin
 * removes CORS credentials, `SameSite=None`, and the whole class of cookie-in-the-browser
 * problems at once. In development a proxy maps `/api` onto the ASP.NET Core host.
 */
export const API_BASE_URL = new InjectionToken<string>('API_BASE_URL', {
  providedIn: 'root',
  factory: () => '/api',
});
