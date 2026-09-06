import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ListingKind } from '@barnabas/api';
import { BoardStore, wordsFor } from '@barnabas/domain';

import { PlacardComponent } from '@barnabas/domain';

/**
 * The board: everything the congregation currently has on offer.
 *
 * Three states beyond the ordinary one are rendered rather than left to chance. A congregation
 * that has posted nothing sees an invitation; a board still arriving announces the wait to
 * assistive technology as well as drawing skeletons; a board that failed says so plainly and
 * offers a retry.
 *
 * The chip row filters the board to one kind. The filter is applied by the API rather than by
 * hiding placards here, so the counts on the chips describe the whole board and a filtered board
 * still pages correctly.
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
  readonly kind = this.store.kind;
  readonly showingEverything = this.store.showingEverything;

  /**
   * The chips, in the order the board names the kinds.
   *
   * Labelled from the same vocabulary the placards use, so a chip reading "Giving away" filters
   * to placards that also read "Giving away".
   */
  readonly filters: readonly { readonly kind: ListingKind; readonly label: string }[] = (
    ['Lend', 'Give', 'Sell', 'Help'] as const
  ).map((kind) => ({ kind, label: wordsFor(kind).board }));

  constructor() {
    void this.store.load(null);
  }

  countOf(kind: ListingKind): number {
    return this.store.countOf(kind);
  }

  show(kind: ListingKind | null): void {
    void this.store.load(kind);
  }

  retry(): void {
    void this.store.load();
  }
}
