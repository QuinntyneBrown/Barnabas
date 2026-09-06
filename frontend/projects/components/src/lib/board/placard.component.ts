import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { BoardListing } from '@barnabas/api';

/**
 * One listing as a solid field of colour, butted against its neighbours into the mosaic.
 *
 * The kind is written on it in words as well as carried by the colour of the field. That is
 * L2-044, and it is not decoration: a member with a colour vision deficiency, or one reading a
 * phone in sunlight, gets the same information from the text that everyone else gets from both.
 */
@Component({
  selector: 'bar-placard',
  imports: [RouterLink],
  templateUrl: './placard.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PlacardComponent {
  readonly listing = input.required<BoardListing>();

  readonly fieldClass = computed(() => `placard placard--${this.listing().kind.toLowerCase()}`);

  /** Help offers time rather than a thing, so it carries words where the others carry a drawing. */
  readonly isHelp = computed(() => this.listing().kind === 'Help');
}
