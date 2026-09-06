/*
 * Public API of @barnabas/components — the shell, the presentational components, and
 * the routed screens.
 *
 * This library may depend on @barnabas/domain and on the contracts in @barnabas/api.
 * Components stay presentational: behaviour belongs in services, state in signals.
 */

export * from './lib/shell/nav-destinations';
export * from './lib/shell/nav-icon.component';
export * from './lib/shell/skip-link.component';
export * from './lib/shell/primary-nav.component';
export * from './lib/shell/bottom-nav.component';
export * from './lib/shell/app-shell.component';
export * from './lib/shell/public-shell.component';

export * from './lib/dialogs/confirm-dialog.component';

export * from './lib/forms/field-errors';
export * from './lib/forms/focus-first-invalid';

export * from './lib/access/landing.component';
export * from './lib/access/sign-in.component';
export * from './lib/access/check-your-email.component';
export * from './lib/access/sign-in-landing.component';
export * from './lib/access/link-expired.component';

export * from './lib/board/board.component';
export * from './lib/board/placard.component';

export * from './lib/listings/choose-kind.component';
export * from './lib/listings/post-lend.component';
export * from './lib/listings/listing-posted.component';
export * from './lib/listings/listing-detail.component';
export * from './lib/listings/my-listings.component';

export * from './lib/requests/request-lend.component';
export * from './lib/requests/request-sent.component';
export * from './lib/requests/request-accepted.component';

export * from './lib/inbox/inbox-chips.component';
export * from './lib/inbox/incoming-requests.component';
export * from './lib/inbox/outgoing-requests.component';

export * from './lib/messaging/threads.component';
export * from './lib/messaging/thread.component';

export * from './lib/you/you.component';

export * from './lib/placeholders/coming-soon.component';
