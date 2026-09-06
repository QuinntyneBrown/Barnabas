import { ChangeDetectionStrategy, Component, effect, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { LISTING_SERVICE, ListingDetail, REQUEST_SERVICE } from '@barnabas/api';

import { FieldErrors, focusFirstInvalid } from '@barnabas/domain';

/**
 * Asking for something being given away.
 *
 * Less is asked of the requester than for a loan: ownership transfers, so there is no return
 * date and no acknowledgement to make. What is left is a message and when they could collect it.
 */
@Component({
  selector: 'bar-request-give',
  imports: [FormsModule, RouterLink],
  templateUrl: './request-give.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RequestGiveComponent {
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
      const made = await this.requests.askForGift(this.listingId(), {
        message: this.message(),
        pickupAt: this.pickupAt() === '' ? null : new Date(this.pickupAt()).toISOString(),
      });

      await this.router.navigate(['/requests', made.requestId, 'sent']);
    } catch (failure) {
      const errors = FieldErrors.from(failure);

      this.errors.set(errors);

      if (errors.any) {
        focusFirstInvalid(errors, RequestGiveComponent.FieldOrder);
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
