import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { IncomingRequest } from '@barnabas/api';
import { RequestStore } from '@barnabas/domain';

import { ConfirmDialogComponent } from '@barnabas/components';
import { InboxChipsComponent } from '../shell/inbox-chips.component';

/**
 * What has been asked of the member.
 *
 * Each row carries the requester, the listing, the message, and the terms the kind needs -
 * everything the decision turns on, so the owner never has to go somewhere else to make it.
 *
 * Accepting goes straight to the confirmation with the thread it opened. Declining asks first:
 * the dialog gates the call rather than the response, so cancelling leaves the request pending
 * and nothing has been sent.
 */
@Component({
  selector: 'bar-incoming-requests',
  imports: [RouterLink, ConfirmDialogComponent, InboxChipsComponent],
  templateUrl: './incoming-requests.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class IncomingRequestsComponent {
  private readonly store = inject(RequestStore);
  private readonly router = inject(Router);

  readonly incoming = this.store.incoming;
  readonly loading = this.store.loading;

  /** The request the open dialog is about. */
  readonly declining = signal<IncomingRequest | null>(null);

  constructor() {
    void this.store.loadIncoming();
  }

  async accept(request: IncomingRequest): Promise<void> {
    const threadId = await this.store.accept(request.requestId);

    await this.router.navigate(['/requests', request.requestId, 'accepted'], {
      queryParams: { threadId, requester: request.requesterDisplayName },
    });
  }

  async confirmDecline(): Promise<void> {
    const request = this.declining();

    if (request) {
      await this.store.decline(request.requestId);
    }

    this.declining.set(null);
  }
}
