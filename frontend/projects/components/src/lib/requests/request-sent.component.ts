import { ChangeDetectionStrategy, Component, effect, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { IRequestsApi, MyRequest } from '@barnabas/api';

/**
 * The request is on its way.
 *
 * It names the owner, says what happens next, offers the member their own requests and the
 * board, and states plainly that Barnabas handles no payment or delivery. L2-058 asks for all
 * four, and the last one matters most: it is the sentence that sets expectations about what this
 * product does and does not do.
 */
@Component({
  selector: 'bar-request-sent',
  imports: [RouterLink],
  templateUrl: './request-sent.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RequestSentComponent {
  private readonly requests = inject(IRequestsApi);

  readonly requestId = input.required<string>();

  readonly request = signal<MyRequest | null>(null);

  constructor() {
    effect(() => {
      void this.load(this.requestId());
    });
  }

  private async load(requestId: string): Promise<void> {
    const mine = await this.requests.mine();

    this.request.set(mine.find((candidate) => candidate.requestId === requestId) ?? null);
  }
}
