import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';

/**
 * A link that no longer works.
 *
 * There is no mock for this screen; it is designed here, because L2-015 requires a member
 * following an expired link to be told so and offered another. Expiry and reuse land here alike:
 * the API answers the same for both, deliberately, so neither tells a stranger which happened.
 */
@Component({
  selector: 'bar-link-expired',
  imports: [RouterLink],
  templateUrl: './link-expired.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LinkExpiredComponent {}
