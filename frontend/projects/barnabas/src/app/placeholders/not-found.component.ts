import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';

/**
 * An address that leads nowhere.
 *
 * The catch-all used to redirect to the landing page, which told a member nothing and quietly
 * signed-out-looking screens over a mistyped URL. Saying so is better than pretending the address
 * was the one they wanted.
 */
@Component({
  selector: 'bar-not-found',
  imports: [RouterLink],
  templateUrl: './not-found.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NotFoundComponent {}
