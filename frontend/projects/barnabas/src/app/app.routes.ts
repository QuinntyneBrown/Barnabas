import { Routes } from '@angular/router';
import { AppShellComponent } from './shell/app-shell.component';
import { PublicShellComponent } from './shell/public-shell.component';

import { CheckYourEmailComponent } from './access/check-your-email.component';
import { LandingComponent } from './access/landing.component';
import { LinkExpiredComponent } from './access/link-expired.component';
import { SignInComponent } from './access/sign-in.component';
import { SignInLandingComponent } from './access/sign-in-landing.component';

import { BoardComponent } from './board/board.component';

import { AwaitingApprovalComponent } from './joining/awaiting-approval.component';
import { CreateProfileComponent } from './joining/create-profile.component';
import { InviteInvalidComponent } from './joining/invite-invalid.component';
import { JoinComponent } from './joining/join.component';

import { ChooseKindComponent } from './listings/choose-kind.component';
import { EditListingComponent } from './listings/edit-listing.component';
import { PostGiveComponent } from './listings/post-give.component';
import { PostHelpComponent } from './listings/post-help.component';
import { PostSellComponent } from './listings/post-sell.component';
import { ListingDetailComponent } from './listings/listing-detail.component';
import { ListingPostedComponent } from './listings/listing-posted.component';
import { MyListingsComponent } from './listings/my-listings.component';
import { PostLendComponent } from './listings/post-lend.component';

import { RequestAcceptedComponent } from './requests/request-accepted.component';
import { RequestGiveComponent } from './requests/request-give.component';
import { RequestHelpComponent } from './requests/request-help.component';
import { RequestLendComponent } from './requests/request-lend.component';
import { RequestSellComponent } from './requests/request-sell.component';
import { RequestSentComponent } from './requests/request-sent.component';

import { IncomingRequestsComponent } from './inbox/incoming-requests.component';
import { OutgoingRequestsComponent } from './inbox/outgoing-requests.component';

import { ThreadComponent } from './messaging/thread.component';
import { ThreadsComponent } from './messaging/threads.component';

import { InviteSomeoneComponent } from './moderation/invite-someone.component';

import { DirectoryComponent } from './members/directory.component';
import { SearchComponent } from './search/search.component';
import { MemberProfileComponent } from './members/member-profile.component';

import { ProfileSettingsComponent } from './you/profile-settings.component';
import { YouComponent } from './you/you.component';

import { ComingSoonComponent } from './placeholders/coming-soon.component';
import { NotFoundComponent } from './placeholders/not-found.component';
import { approvedGuard } from '@barnabas/domain';

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

      // Joining is public: nobody is signed in, and the code is what reveals which congregation
      // is being joined.
      { path: 'join', component: JoinComponent, title: 'Join · Barnabas' },
      { path: 'join/profile', component: CreateProfileComponent, title: 'Your profile · Barnabas' },
      {
        path: 'join/invalid',
        component: InviteInvalidComponent,
        title: 'That code did not work · Barnabas',
      },

      // Reached two ways: straight after joining, with no session at all, and by a pending member
      // signing in later. Public, so the first of those works.
      {
        path: 'join/pending',
        component: AwaitingApprovalComponent,
        title: 'Waiting for approval · Barnabas',
      },
    ],
  },
  {
    path: '',
    component: AppShellComponent,
    canActivate: [approvedGuard],
    children: [
      { path: 'board', component: BoardComponent, title: 'Board · Barnabas' },

      { path: 'post', component: ChooseKindComponent, title: 'Post · Barnabas' },
      { path: 'post/lend', component: PostLendComponent, title: 'Post — Lend · Barnabas' },
      { path: 'post/give', component: PostGiveComponent, title: 'Post — Give · Barnabas' },
      { path: 'post/sell', component: PostSellComponent, title: 'Post — Sell · Barnabas' },
      { path: 'post/help', component: PostHelpComponent, title: 'Post — Help · Barnabas' },

      {
        path: 'listings/:listingId/edit',
        component: EditListingComponent,
        title: 'Edit your listing · Barnabas',
      },

      // Before the detail route, so "mine" is not read as an identifier.
      { path: 'my-listings', component: MyListingsComponent, title: 'My listings · Barnabas' },
      {
        path: 'listings/:listingId/posted',
        component: ListingPostedComponent,
        title: 'Listing posted · Barnabas',
      },
      // A route per kind, for the same reason the API has an endpoint per kind: the four collect
      // different terms, and the kind is part of the address rather than something the form has
      // to work out after it loads.
      {
        path: 'listings/:listingId/request/lend',
        component: RequestLendComponent,
        title: 'Request to borrow · Barnabas',
      },
      {
        path: 'listings/:listingId/request/give',
        component: RequestGiveComponent,
        title: 'Request this · Barnabas',
      },
      {
        path: 'listings/:listingId/request/sell',
        component: RequestSellComponent,
        title: 'Request to buy · Barnabas',
      },
      {
        path: 'listings/:listingId/request/help',
        component: RequestHelpComponent,
        title: 'Request this help · Barnabas',
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
      {
        path: 'you/profile',
        component: ProfileSettingsComponent,
        title: 'Profile and settings · Barnabas',
      },

      {
        path: 'moderation/invite',
        component: InviteSomeoneComponent,
        title: 'Invite someone · Barnabas',
      },

      // Reachable so the five destinations are five at every width, and honest about being empty.
      { path: 'search', component: SearchComponent, title: 'Search · Barnabas' },
      {
        path: 'notifications',
        component: ComingSoonComponent,
        title: 'Notifications · Barnabas',
        data: {
          heading: 'Notifications are on their way',
          body: 'Requests and replies are in your inbox in the meantime.',
        },
      },
      { path: 'directory', component: DirectoryComponent, title: 'Directory · Barnabas' },
      { path: 'members/:memberId', component: MemberProfileComponent, title: 'Member · Barnabas' },
    ],
  },
  // Says so rather than redirecting. A member who mistyped an address was being shown the
  // landing page, which looks like being signed out.
  { path: '**', component: NotFoundComponent, title: 'Not found · Barnabas' },
];
