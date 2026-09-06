import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { ThreadStore } from '@barnabas/domain';

import { InboxChipsComponent } from '../shell/inbox-chips.component';

/**
 * The member's conversations.
 *
 * Unread is announced as well as drawn. The dot is hidden from assistive technology and the word
 * is put in the row's own text instead, so a member who cannot see the dot is told the same
 * thing rather than a different one.
 */
@Component({
  selector: 'bar-threads',
  imports: [RouterLink, InboxChipsComponent],
  templateUrl: './threads.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ThreadsComponent {
  private readonly store = inject(ThreadStore);

  readonly mine = this.store.mine;
  readonly loading = this.store.loading;

  constructor() {
    void this.store.load();
  }
}
