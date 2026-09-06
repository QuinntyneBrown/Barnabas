import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter, withComponentInputBinding, withRouterConfig } from '@angular/router';
import {
  CONGREGATION_SERVICE,
  CongregationService,
  INVITATION_SERVICE,
  INVITE_SERVICE,
  InvitationService,
  InviteService,
  LISTING_SERVICE,
  MEMBER_SERVICE,
  MemberService,
  SEARCH_SERVICE,
  SearchService,
  ListingService,
  REQUEST_SERVICE,
  RequestService,
  SESSION_SERVICE,
  SessionService,
  THREAD_SERVICE,
  TOKEN_SERVICE,
  ThreadService,
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

    // The one place in the workspace that names both a contract and an implementation. Nothing
    // beneath the host knows which class answers a token, which is what makes the seam a real
    // one: a test host binds a mock here and nothing else changes.
    { provide: SESSION_SERVICE, useClass: SessionService },
    { provide: CONGREGATION_SERVICE, useClass: CongregationService },
    { provide: INVITATION_SERVICE, useClass: InvitationService },
    { provide: INVITE_SERVICE, useClass: InviteService },
    { provide: LISTING_SERVICE, useClass: ListingService },
    { provide: MEMBER_SERVICE, useClass: MemberService },
    { provide: SEARCH_SERVICE, useClass: SearchService },
    { provide: REQUEST_SERVICE, useClass: RequestService },
    { provide: THREAD_SERVICE, useClass: ThreadService },

    // The api library declares that it needs a token and a way to renew one; the domain library
    // implements it. Binding them here is what keeps that dependency running the right way.
    { provide: TOKEN_SERVICE, useExisting: SessionStore },
  ],
};
