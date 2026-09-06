import { ChangeDetectionStrategy, Component, effect, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { LISTING_SERVICE, ListingDetail, REQUEST_SERVICE } from '@barnabas/api';

import { FieldErrors, focusFirstInvalid } from '@barnabas/domain';

/**
 * Asking for help.
 *
 * The only request form with no pickup on it: help is time rather than a thing to collect. What
 * the requester chooses instead is one of the windows the offer declared, and only those are
 * shown — naming an hour of their own would be asking for time the owner never offered.
 */
@Component({
  selector: 'bar-request-help',
  imports: [FormsModule, RouterLink],
  templateUrl: './request-help.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RequestHelpComponent {
  private static readonly FieldOrder = ['message', 'availabilityWindowId'];

  private readonly listings = inject(LISTING_SERVICE);
  private readonly requests = inject(REQUEST_SERVICE);
  private readonly router = inject(Router);

  readonly listingId = input.required<string>();

  readonly listing = signal<ListingDetail | null>(null);

  readonly message = signal('');
  readonly availabilityWindowId = signal('');

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
      const made = await this.requests.askForHelp(this.listingId(), {
        message: this.message(),
        availabilityWindowId: this.availabilityWindowId() || null,
      });

      await this.router.navigate(['/requests', made.requestId, 'sent']);
    } catch (failure) {
      const errors = FieldErrors.from(failure);

      this.errors.set(errors);

      if (errors.any) {
        focusFirstInvalid(errors, RequestHelpComponent.FieldOrder);
      } else if (failure instanceof Error) {
        this.refused.set(failure.message);
      }
    } finally {
      this.sending.set(false);
    }
  }

  private async load(listingId: string): Promise<void> {
    try {
      const detail = await this.listings.get(listingId);

      this.listing.set(detail);

      // Preselect the first window so the form is never submitted with nothing chosen by
      // accident. It is still the member's choice; there is simply always one made.
      this.availabilityWindowId.set(detail.availabilityWindows[0]?.availabilityWindowId ?? '');
    } catch {
      this.listing.set(null);
    }
  }
}
