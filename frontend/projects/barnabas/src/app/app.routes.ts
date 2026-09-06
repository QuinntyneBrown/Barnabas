import { Routes } from '@angular/router';
import {
  AppShellComponent,
  BoardComponent,
  CheckYourEmailComponent,
  ChooseKindComponent,
  ComingSoonComponent,
  IncomingRequestsComponent,
  LandingComponent,
  LinkExpiredComponent,
  ListingDetailComponent,
  ListingPostedComponent,
  MyListingsComponent,
  OutgoingRequestsComponent,
  PostLendComponent,
  PublicShellComponent,
  RequestAcceptedComponent,
  RequestLendComponent,
  RequestSentComponent,
  SignInComponent,
  SignInLandingComponent,
  ThreadComponent,
  ThreadsComponent,
  YouComponent,
} from '@barnabas/components';
import { authGuard } from '@barnabas/domain';

/**
 * Two shells, chosen by the route.
 *
 * The public one carries the brand and nothing else; the signed-in one carries the five
 * destinations. Which a screen gets is a property of the route rather than something the root
 * component decides, so no screen has to know which of the two it is inside.
 *
 * `withComponentInputBinding` is what lets the screens declare route and query parameters as
 * inputs instead of reading a snapshot.
 */
export const routes: Routes = [
  {
    path: '',
    component: PublicShellComponent,
    children: [
      { path: '', component: LandingComponent, title: 'Barnabas' },
      { path: 'sign-in', component: SignInComponent, title: 'Sign in · Barnabas' },
      {
        path: 'check-your-email',
        component: CheckYourEmailComponent,
        title: 'Check your email · Barnabas',
      },

      // Before the token route, or "expired" would be read as a token and exchanged.
      {
        path: 'sign-in/expired',
        component: LinkExpiredComponent,
        title: 'That link has expired · Barnabas',
      },
      { path: 'sign-in/:token', component: SignInLandingComponent, title: 'Signing you in · Barnabas' },
    ],
  },
  {
    path: '',
    component: AppShellComponent,
    canActivate: [authGuard],
    children: [
      { path: 'board', component: BoardComponent, title: 'Board · Barnabas' },

      { path: 'post', component: ChooseKindComponent, title: 'Post · Barnabas' },
      { path: 'post/lend', component: PostLendComponent, title: 'Post — Lend · Barnabas' },

      // Before the detail route, so "mine" is not read as an identifier.
      { path: 'my-listings', component: MyListingsComponent, title: 'My listings · Barnabas' },
      {
        path: 'listings/:listingId/posted',
        component: ListingPostedComponent,
        title: 'Listing posted · Barnabas',
      },
      {
        path: 'listings/:listingId/request',
        component: RequestLendComponent,
        title: 'Request to borrow · Barnabas',
      },
      { path: 'listings/:listingId', component: ListingDetailComponent, title: 'Listing · Barnabas' },

      { path: 'inbox', pathMatch: 'full', redirectTo: 'inbox/requests' },
      {
        path: 'inbox/requests',
        component: IncomingRequestsComponent,
        title: 'Requests to me · Barnabas',
      },
      {
        path: 'inbox/my-requests',
        component: OutgoingRequestsComponent,
        title: 'My requests · Barnabas',
      },
      { path: 'inbox/messages', component: ThreadsComponent, title: 'Messages · Barnabas' },

      {
        path: 'requests/:requestId/sent',
        component: RequestSentComponent,
        title: 'Request sent · Barnabas',
      },
      {
        path: 'requests/:requestId/accepted',
        component: RequestAcceptedComponent,
        title: 'Request accepted · Barnabas',
      },

      { path: 'threads/:threadId', component: ThreadComponent, title: 'Messages · Barnabas' },

      { path: 'you', component: YouComponent, title: 'You · Barnabas' },

      // Reachable so the five destinations are five at every width, and honest about being empty.
      {
        path: 'search',
        component: ComingSoonComponent,
        title: 'Search · Barnabas',
        data: {
          heading: 'Search is on its way',
          body: 'For now the whole board fits on one screen.',
        },
      },
      {
        path: 'notifications',
        component: ComingSoonComponent,
        title: 'Notifications · Barnabas',
        data: {
          heading: 'Notifications are on their way',
          body: 'Requests and replies are in your inbox in the meantime.',
        },
      },
      {
        path: 'members/:memberId',
        component: ComingSoonComponent,
        title: 'Member · Barnabas',
        data: {
          heading: 'Member profiles are on their way',
          body: 'You can see who posted a listing on the listing itself.',
        },
      },
    ],
  },
  { path: '**', redirectTo: '' },
];
