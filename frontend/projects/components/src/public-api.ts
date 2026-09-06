/*
 * Public API of @barnabas/components — the plain vocabulary of an interface.
 *
 * This library names nothing of ours: no @barnabas/api, no @barnabas/domain, no router. Treat it
 * as a package that ships to npm and drops into an unrelated product. That constraint is the
 * whole point of the library, and it is what keeps everything below it honest.
 */

export * from './lib/confirm-dialog/confirm-dialog.component';
export * from './lib/nav-icon/nav-icon';
export * from './lib/nav-icon/nav-icon.component';
export * from './lib/skip-link/skip-link.component';
