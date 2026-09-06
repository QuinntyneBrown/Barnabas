import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { BoardStore } from '@barnabas/domain';

import { PlacardComponent } from './placard.component';

/**
 * The board: everything the congregation currently has on offer.
 *
 * Three states beyond the ordinary one are rendered rather than left to chance. A congregation
 * that has posted nothing sees an invitation; a board still arriving announces the wait to
 * assistive technology as well as drawing skeletons; a board that failed says so plainly and
 * offers a retry.
 */
@Component({
  selector: 'bar-board',
  imports: [RouterLink, PlacardComponent],
  templateUrl: './board.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BoardComponent {
  private readonly store = inject(BoardStore);

  readonly placards = this.store.placards;
  readonly loading = this.store.loading;
  readonly failed = this.store.failed;
  readonly isEmpty = this.store.isEmpty;
  readonly total = this.store.total;

  constructor() {
    void this.store.load(null);
  }

  retry(): void {
    void this.store.load();
  }
}
