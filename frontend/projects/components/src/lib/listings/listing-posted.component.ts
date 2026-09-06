import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';

/**
 * The listing is live, and here are the ways onward.
 *
 * Three of them, which L2-033 names: view it, post another, return to the board. A confirmation
 * that only said "done" would leave the member on a dead end.
 */
@Component({
  selector: 'bar-listing-posted',
  imports: [RouterLink],
  templateUrl: './listing-posted.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ListingPostedComponent {
  readonly listingId = input.required<string>();
}
