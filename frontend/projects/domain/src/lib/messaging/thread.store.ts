import { Injectable, inject, signal } from '@angular/core';
import { THREAD_SERVICE, Message, ThreadDetail, ThreadSummary } from '@barnabas/api';

/**
 * A member's conversations, and the one they are reading.
 *
 * Sending appends to the open thread rather than reloading it. Confirmation of a send is the
 * message appearing, and nothing else happened worth telling anybody about.
 */
@Injectable({ providedIn: 'root' })
export class ThreadStore {
  private readonly threads = inject(THREAD_SERVICE);

  private readonly summaries = signal<readonly ThreadSummary[]>([]);
  private readonly open = signal<ThreadDetail | null>(null);

  readonly loading = signal(false);
  readonly mine = this.summaries.asReadonly();
  readonly thread = this.open.asReadonly();

  async load(): Promise<void> {
    this.loading.set(true);

    try {
      this.summaries.set(await this.threads.mine());
    } finally {
      this.loading.set(false);
    }
  }

  async openThread(threadId: string): Promise<void> {
    this.loading.set(true);

    try {
      this.open.set(await this.threads.get(threadId));
    } finally {
      this.loading.set(false);
    }
  }

  async send(threadId: string, body: string): Promise<Message> {
    const sent = await this.threads.send(threadId, body);

    const thread = this.open();

    if (thread?.threadId === threadId) {
      this.open.set({ ...thread, messages: [...thread.messages, sent] });
    }

    return sent;
  }
}
