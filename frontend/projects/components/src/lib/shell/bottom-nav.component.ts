import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';

import { NavDestination } from './nav-destinations';
import { NavIconComponent } from './nav-icon.component';

/**
 * The same five destinations, in the same order, below the medium band.
 *
 * Same list, same order, different rendering. A destination that existed only on a desktop
 * header would not be a layout quirk, it would be a member who cannot reach their listings.
 */
@Component({
  selector: 'bar-bottom-nav',
  imports: [RouterLink, RouterLinkActive, NavIconComponent],
  templateUrl: './bottom-nav.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BottomNavComponent {
  readonly destinations = input.required<readonly NavDestination[]>();

  readonly hidden = input(false);
}
