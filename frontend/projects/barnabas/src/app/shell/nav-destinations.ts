import { NavIcon } from '@barnabas/components';

export interface NavDestination {
  readonly label: string;
  readonly path: string;
  readonly icon: NavIcon;
  /** Applied by the header nav only; the Post link is the one filled control in the bar. */
  readonly cssClass?: string;
}

/**
 * The five primary destinations, in order, read by both navigations.
 *
 * `L2-109` requires all five to be reachable at every viewport width, and that a
 * destination not exist only above a breakpoint. Both the header nav and the bottom bar
 * render from this one constant, so the requirement holds structurally rather than by
 * anyone remembering to keep two lists in step.
 *
 * Search routes to a placeholder in this slice: `L1-008` is deferred, and a nav entry
 * that vanishes until then would be exactly the breakpoint-shaped gap `L2-109` forbids.
 */
export const NAV_DESTINATIONS: readonly NavDestination[] = [
  { label: 'Board', path: '/board', icon: 'board' },
  { label: 'Search', path: '/search', icon: 'search' },
  { label: 'Post', path: '/post', icon: 'post', cssClass: 'nav-post' },
  { label: 'Inbox', path: '/inbox', icon: 'inbox' },
  { label: 'You', path: '/you', icon: 'person' },
];
