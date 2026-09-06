import { ChangeDetectionStrategy, Component, effect, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { LISTING_SERVICE, ListingDetail, REQUEST_SERVICE } from '@barnabas/api';

import { FieldErrors, focusFirstInvalid } from '@barnabas/domain';

/**
 * Asking to buy something.
 *
 * The same two fields a gift request collects, and one sentence more: that the price is settled
 * in person. Barnabas takes no payment and holds no deposit, so the form renders no payment
 * field and the API refuses one by name.
 */
@Component({
  selector: 'bar-request-sell',
  imports: [FormsModule, RouterLink],
  templateUrl: './request-sell.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RequestSellComponent {
  private static readonly FieldOrder = ['message', 'pickupAt'];

  private readonly listings = inject(LISTING_SERVICE);
  private readonly requests = inject(REQUEST_SERVICE);
  private readonly router = inject(Router);

  readonly listingId = input.required<string>();

  readonly listing = signal<ListingDetail | null>(null);

  readonly message = signal('');
  readonly pickupAt = signal('');

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
      const made = await this.requests.askToBuy(this.listingId(), {
        message: this.message(),
        pickupAt: this.pickupAt() === '' ? null : new Date(this.pickupAt()).toISOString(),
      });

      await this.router.navigate(['/requests', made.requestId, 'sent']);
    } catch (failure) {
      const errors = FieldErrors.from(failure);

      this.errors.set(errors);

      if (errors.any) {
        focusFirstInvalid(errors, RequestSellComponent.FieldOrder);
      } else if (failure instanceof Error) {
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
