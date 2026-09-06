import { ChangeDetectionStrategy, Component, input } from '@angular/core';

import { NavIcon } from './nav-destinations';

/**
 * The drawing beside a destination in the bottom bar.
 *
 * Inline rather than a sprite or a font, so the stroke takes `currentColor` and the icon
 * inverts with its label when the destination is the current one. Every one is hidden from
 * assistive technology: the label beside it already says where it goes.
 */
@Component({
  selector: 'bar-nav-icon',
  templateUrl: './nav-icon.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NavIconComponent {
  readonly icon = input.required<NavIcon>();
}
