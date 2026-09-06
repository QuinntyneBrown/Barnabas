import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';

import { NavDestination } from './nav-destinations';

/**
 * The destinations in the header, from the medium band upward.
 *
 * It renders from the one ordered list rather than a copy of it, which is what makes L2-109
 * structurally true instead of something two components have to be kept in step about.
 */
@Component({
  selector: 'bar-primary-nav',
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './primary-nav.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PrimaryNavComponent {
  readonly destinations = input.required<readonly NavDestination[]>();

  /**
   * Whether this navigation is the one the viewport is not using.
   *
   * Both navigations are in the document at every width and CSS decides which is seen. Without
   * this, assistive technology would find two navigations both called Primary and the keyboard
   * would stop at ten destinations instead of five.
   */
  readonly hidden = input(false);
}
