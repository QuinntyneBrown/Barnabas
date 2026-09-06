import { Injectable, inject, signal } from '@angular/core';
import { IRequestsApi, IncomingRequest, MyRequest } from '@barnabas/api';

/**
 * Both sides of the inbox.
 *
 * One store rather than two, because the three inbox chips are one screen with three views and
 * the counts on them have to agree.
 */
@Injectable({ providedIn: 'root' })
export class RequestStore {
  private readonly requests = inject(IRequestsApi);

  private readonly incomingRequests = signal<readonly IncomingRequest[]>([]);
  private readonly outgoingRequests = signal<readonly MyRequest[]>([]);

  readonly loading = signal(false);
  readonly incoming = this.incomingRequests.asReadonly();
  readonly outgoing = this.outgoingRequests.asReadonly();

  async loadIncoming(): Promise<void> {
    this.loading.set(true);

    try {
      this.incomingRequests.set(await this.requests.incoming());
    } finally {
      this.loading.set(false);
    }
  }

  async loadOutgoing(): Promise<void> {
    this.loading.set(true);

    try {
      this.outgoingRequests.set(await this.requests.mine());
    } finally {
      this.loading.set(false);
    }
  }

  /** Returns the thread the acceptance opened, so the screen can offer it straight away. */
  async accept(requestId: string): Promise<string> {
    const accepted = await this.requests.accept(requestId);

    await this.loadIncoming();

    return accepted.threadId;
  }

  async decline(requestId: string): Promise<void> {
    await this.requests.decline(requestId);

    await this.loadIncoming();
  }
}
