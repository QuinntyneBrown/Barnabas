import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';

/**
 * A destination that exists so it can be reached, and says plainly that it is not built.
 *
 * Search is one of the five primary destinations, and L2-109 requires all five to be reachable
 * at every width - so it cannot simply be left out until search is built. Notifications and a
 * member's profile are linked to from screens that are in this slice. Each of them lands here.
 *
 * A destination that vanished until its feature arrived would be exactly the breakpoint-shaped
 * gap the requirement forbids, and a link that went nowhere would be worse than either.
 */
@Component({
  selector: 'bar-coming-soon',
  imports: [RouterLink],
  templateUrl: './coming-soon.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ComingSoonComponent {
  readonly heading = input('Not built yet');

  readonly body = input('This part of Barnabas is on its way.');
}
