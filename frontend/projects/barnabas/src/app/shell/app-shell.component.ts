import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { NotificationStore, ViewportService } from '@barnabas/domain';

import { BottomNavComponent } from './bottom-nav.component';
import { NAV_DESTINATIONS } from './nav-destinations';
import { PrimaryNavComponent } from './primary-nav.component';
import { SkipLinkComponent } from '@barnabas/components';

/**
 * The shell every signed-in screen is rendered inside.
 *
 * Skip link first, then the header, then the screen in the `main` landmark, then the bottom bar.
 * That order is the tab order, and the tab order is the reading order.
 *
 * Both navigations are rendered at every width. CSS decides which is seen; `inert` and
 * `aria-hidden` decide which is reachable, so exactly one of them holds tab stops and exactly
 * one is announced - whatever the stylesheet happens to be doing.
 */
@Component({
  selector: 'bar-app-shell',
  imports: [RouterOutlet, RouterLink, SkipLinkComponent, PrimaryNavComponent, BottomNavComponent],
  templateUrl: './app-shell.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AppShellComponent {
  private readonly viewport = inject(ViewportService);
  private readonly notifications = inject(NotificationStore);

  /**
   * How many notifications are waiting.
   *
   * Read once here rather than by each screen that shows it: L2-073 puts the count on every
   * screen at every width, and several copies asking independently would disagree the moment one
   * of them went stale. The bell is in the header at every band, so this is the one place it
   * needs to be.
   */
  readonly unread = this.notifications.unread;

  readonly destinations = NAV_DESTINATIONS;

  constructor() {
    void this.notifications.refreshCount();
  }

  readonly isCompact = this.viewport.isCompactNav;

  /** The header nav is the one exposed from the medium band up, and only there. */
  readonly headerNavHidden = computed(() => this.isCompact());

  readonly bottomNavHidden = computed(() => !this.isCompact());
}
