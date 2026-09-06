import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter, withComponentInputBinding, withRouterConfig } from '@angular/router';
import {
  IListingsApi,
  IRequestsApi,
  ISessionsApi,
  IThreadsApi,
  ITokenSource,
  ListingsApi,
  RequestsApi,
  SessionsApi,
  ThreadsApi,
  authInterceptor,
  problemDetailsInterceptor,
  refreshInterceptor,
} from '@barnabas/api';
import { SessionStore } from '@barnabas/domain';

import { routes } from './app.routes';

/**
 * Where the abstractions the screens depend on are bound to implementations.
 *
 * The order of the interceptors is the order they wrap the request in. Auth attaches the token;
 * refresh sits outside it so a retry gets a fresh one; problem details sits outermost so every
 * failure reaching a screen is already an ApiError, whichever stage produced it.
 */
export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),

    provideRouter(
      routes,
      withComponentInputBinding(),
      withRouterConfig({ paramsInheritanceStrategy: 'always' }),
    ),

    provideHttpClient(
      withFetch(),
      withInterceptors([problemDetailsInterceptor, refreshInterceptor, authInterceptor]),
    ),

    { provide: ISessionsApi, useClass: SessionsApi },
    { provide: IListingsApi, useClass: ListingsApi },
    { provide: IRequestsApi, useClass: RequestsApi },
    { provide: IThreadsApi, useClass: ThreadsApi },

    // The api library declares that it needs a token and a way to renew one; the domain library
    // implements it. Binding them here is what keeps that dependency running the right way.
    { provide: ITokenSource, useExisting: SessionStore },
  ],
};
