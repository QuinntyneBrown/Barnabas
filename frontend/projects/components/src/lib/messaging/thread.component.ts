import { ChangeDetectionStrategy, Component, effect, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ThreadStore } from '@barnabas/domain';

import { FieldErrors } from '../forms/field-errors';

/**
 * One conversation, and the composer under it.
 *
 * Sending is confirmed by the message appearing. There is no toast and no banner, because
 * nothing happened beyond the thing the member can already see.
 *
 * A refused message leaves what was typed in the box. Losing three sentences to a length limit
 * would be a worse failure than the one being reported.
 */
@Component({
  selector: 'bar-thread',
  imports: [FormsModule, RouterLink],
  templateUrl: './thread.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ThreadComponent {
  private readonly store = inject(ThreadStore);

  readonly threadId = input.required<string>();

  readonly thread = this.store.thread;
  readonly loading = this.store.loading;

  readonly draft = signal('');
  readonly errors = signal(FieldErrors.none());
  readonly sending = signal(false);

  constructor() {
    effect(() => {
      void this.store.openThread(this.threadId());
    });
  }

  async send(): Promise<void> {
    if (this.sending() || this.draft().trim().length === 0) {
      return;
    }

    this.sending.set(true);
    this.errors.set(FieldErrors.none());

    try {
      await this.store.send(this.threadId(), this.draft());

      this.draft.set('');
    } catch (failure) {
      this.errors.set(FieldErrors.from(failure));
    } finally {
      this.sending.set(false);
    }
  }
}
