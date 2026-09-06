import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { BoardListing } from '@barnabas/api';

import { wordsFor } from '../../listings/listing-words';

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

  /**
   * The kind in the board's own words - "Giving away", not "Give".
   *
   * L2-028 asks that a gift be identified as being given away rather than sold, and the bare
   * enum name does not do that: "Give" beside a placard with no price leaves the reader to
   * infer it. The same vocabulary labels the filter chips, so the two cannot drift.
   */
  readonly kindLabel = computed(() => wordsFor(this.listing().kind).board);

  /** Help offers time rather than a thing, so it carries words where the others carry a drawing. */
  readonly isHelp = computed(() => this.listing().kind === 'Help');
}
