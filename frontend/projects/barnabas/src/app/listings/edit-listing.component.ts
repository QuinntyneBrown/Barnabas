import { ChangeDetectionStrategy, Component, effect, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { LISTING_SERVICE, ListingDetail } from '@barnabas/api';

import { CongregationStore, FieldErrors, focusFirstInvalid } from '@barnabas/domain';

/**
 * Correcting a listing already on the board.
 *
 * One form for all four kinds, because what may be corrected is the four fields every kind
 * carries. The per-kind terms — a loan's return date, a sale's price, an offer's windows — are
 * what the kinds differ by, and no acceptance criterion asks for them to be editable yet.
 *
 * The kind itself is not editable at all, and the screen says so rather than leaving its absence
 * to be noticed. Requests already made against a listing were made on the strength of its kind.
 */
@Component({
  selector: 'bar-edit-listing',
  imports: [FormsModule, RouterLink],
  templateUrl: './edit-listing.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EditListingComponent {
  private static readonly FieldOrder = ['title', 'category', 'description', 'hood'];

  private readonly listings = inject(LISTING_SERVICE);
  private readonly router = inject(Router);
  private readonly congregations = inject(CongregationStore);

  readonly listingId = input.required<string>();

  readonly listing = signal<ListingDetail | null>(null);
  readonly missing = signal(false);

  readonly title = signal('');
  readonly description = signal('');
  readonly category = signal('');
  readonly neighbourhood = signal('');

  readonly categories = ['Tools', 'Household', 'Kids & family', 'Electronics', 'Furniture', 'Outdoor'];

  /**
   * The congregation's own neighbourhoods, read from the API rather than written here.
   *
   * L2-022 asks a member to choose from their parish's list and no other's, and a literal cannot
   * promise that - it was St. Aidan's eight for every member of every parish, and the API would
   * have refused a Parkdale member's own neighbourhood as one their congregation does not offer.
   */
  readonly neighbourhoods = this.congregations.neighbourhoods;

  readonly errors = signal(FieldErrors.none());
  readonly saving = signal(false);

  constructor() {
    // The neighbourhoods are the congregation's, and this is not the screen a member arrives on.
    void this.congregations.ensureLoaded();

    effect(() => {
      void this.load(this.listingId());
    });
  }

  async save(): Promise<void> {
    if (this.saving()) {
      return;
    }

    this.saving.set(true);
    this.errors.set(FieldErrors.none());

    try {
      await this.listings.edit(this.listingId(), {
        title: this.title(),
        description: this.description(),
        category: this.category(),
        neighbourhood: this.neighbourhood(),
      });

      await this.router.navigate(['/listings', this.listingId()]);
    } catch (failure) {
      const errors = FieldErrors.from(failure);

      this.errors.set(errors);

      focusFirstInvalid(errors, EditListingComponent.FieldOrder);
    } finally {
      this.saving.set(false);
    }
  }

  /**
   * Fills the form from the listing as it stands.
   *
   * An edit screen that opened empty would invite a member to retype what they already wrote, and
   * a blank field submitted is a field cleared.
   */
  private async load(listingId: string): Promise<void> {
    this.missing.set(false);

    try {
      const detail = await this.listings.get(listingId);

      this.listing.set(detail);
      this.title.set(detail.title);
      this.description.set(detail.description);
      this.category.set(detail.category);
      this.neighbourhood.set(detail.neighbourhood);
    } catch {
      this.listing.set(null);
      this.missing.set(true);
    }
  }
}
