import { DestroyRef, Injectable, computed, inject, signal } from '@angular/core';

import { Band, NAV_SWAP_WIDTH, bandFor } from './band';

/**
 * Exposes the current viewport band as a signal, so components react to width without
 * each subscribing to a media query.
 *
 * A `matchMedia` change handler writing to a signal is simpler than an observable and,
 * under zoneless change detection, is exactly what schedules a repaint.
 *
 * The media query answers the question the shell actually asks - which navigation is in play -
 * and the resize listener alongside it keeps the band correct as a window is dragged across a
 * boundary the query does not cover.
 */
@Injectable({ providedIn: 'root' })
export class ViewportService {
  private readonly width = signal(typeof window === 'undefined' ? NAV_SWAP_WIDTH : window.innerWidth);

  readonly band = computed<Band>(() => bandFor(this.width()));

  /**
   * Whether the bottom bar carries the primary navigation, rather than the header.
   *
   * The shell renders both navigations at every width and lets CSS decide which is
   * visible; this signal drives `inert` and `aria-hidden` so exactly one of them is
   * exposed to assistive technology and holds tab stops, whatever the CSS is doing.
   */
  readonly isCompactNav = computed(() => this.width() < NAV_SWAP_WIDTH);

  constructor() {
    if (typeof window === 'undefined') {
      return;
    }

    const query = window.matchMedia(`(min-width: ${NAV_SWAP_WIDTH}px)`);
    const update = () => this.width.set(window.innerWidth);

    query.addEventListener('change', update);
    window.addEventListener('resize', update, { passive: true });

    inject(DestroyRef).onDestroy(() => {
      query.removeEventListener('change', update);
      window.removeEventListener('resize', update);
    });
  }
}
