import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { RequestStore } from '@barnabas/domain';

import { InboxChipsComponent } from '../shell/inbox-chips.component';

/**
 * What the member asked for, and what became of it.
 *
 * A pending row says it is waiting. An accepted row offers the thread. A declined row says so
 * and offers nothing, because declining opens no thread - and the row knows that from the data
 * it already has rather than by asking again.
 */
@Component({
  selector: 'bar-outgoing-requests',
  imports: [RouterLink, InboxChipsComponent],
  templateUrl: './outgoing-requests.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class OutgoingRequestsComponent {
  private readonly store = inject(RequestStore);

  readonly outgoing = this.store.outgoing;
  readonly loading = this.store.loading;

  constructor() {
    void this.store.loadOutgoing();
  }
}
