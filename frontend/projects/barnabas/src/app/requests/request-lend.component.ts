import { ChangeDetectionStrategy, Component, effect, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { LISTING_SERVICE, REQUEST_SERVICE, ListingDetail } from '@barnabas/api';

import { FieldErrors } from '@barnabas/domain';
import { focusFirstInvalid } from '@barnabas/domain';

/**
 * Asking to borrow something.
 *
 * A loan needs more of the requester than the other kinds do: when they would collect it, when
 * they would bring it back, and an acknowledgement that it stays the owner's. The last is not
 * decoration - it is the misunderstanding the whole form exists to prevent, and the API refuses
 * a request without it.
 */
@Component({
  selector: 'bar-request-lend',
  imports: [FormsModule, RouterLink],
  templateUrl: './request-lend.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RequestLendComponent {
  private static readonly FieldOrder = ['message', 'pickupOn', 'returnBy', 'loanAcknowledged'];

  private readonly listings = inject(LISTING_SERVICE);
  private readonly requests = inject(REQUEST_SERVICE);
  private readonly router = inject(Router);

  readonly listingId = input.required<string>();

  readonly listing = signal<ListingDetail | null>(null);

  readonly message = signal('');
  readonly pickupOn = signal('');
  readonly returnBy = signal('');
  readonly acknowledged = signal(false);

  readonly errors = signal(FieldErrors.none());
  readonly sending = signal(false);
  readonly refused = signal<string | null>(null);

  constructor() {
    effect(() => {
      void this.load(this.listingId());
    });
  }

  async submit(): Promise<void> {
    if (this.sending()) {
      return;
    }

    this.sending.set(true);
    this.errors.set(FieldErrors.none());
    this.refused.set(null);

    try {
      const made = await this.requests.askToBorrow(this.listingId(), {
        message: this.message(),
        pickupOn: this.pickupOn() || null,
        returnBy: this.returnBy() || null,
        loanAcknowledged: this.acknowledged(),
      });

      await this.router.navigate(['/requests', made.requestId, 'sent']);
    } catch (failure) {
      const errors = FieldErrors.from(failure);

      this.errors.set(errors);

      if (errors.any) {
        focusFirstInvalid(errors, RequestLendComponent.FieldOrder);
      } else if (failure instanceof Error) {
        // Not a field fault: the listing has gone, or this member already has an open request
        // against it. Either way the member needs to be told rather than left on a dead button.
        this.refused.set(failure.message);
      }
    } finally {
      this.sending.set(false);
    }
  }

  private async load(listingId: string): Promise<void> {
    try {
      this.listing.set(await this.listings.get(listingId));
    } catch {
      this.listing.set(null);
    }
  }
}
