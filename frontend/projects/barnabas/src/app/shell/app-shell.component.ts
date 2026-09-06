import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { RouterLink, RouterOutlet } from '@angular/router';
import { ViewportService } from '@barnabas/domain';

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

  readonly destinations = NAV_DESTINATIONS;

  readonly isCompact = this.viewport.isCompactNav;

  /** The header nav is the one exposed from the medium band up, and only there. */
  readonly headerNavHidden = computed(() => this.isCompact());

  readonly bottomNavHidden = computed(() => !this.isCompact());
}
