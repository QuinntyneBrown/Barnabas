import { ChangeDetectionStrategy, Component, input } from '@angular/core';

/**
 * The first focusable thing on every screen.
 *
 * It sits off canvas until it takes focus, so it costs a sighted member nothing and saves a
 * keyboard member the whole navigation on every page. L2-111 asks for it by name.
 */
@Component({
  selector: 'bar-skip-link',
  templateUrl: './skip-link.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SkipLinkComponent {
  readonly targetId = input('main');
}
