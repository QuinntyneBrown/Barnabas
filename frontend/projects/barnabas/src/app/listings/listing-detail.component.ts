import { ChangeDetectionStrategy, Component, effect, inject, input, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { LISTING_SERVICE, ListingDetail } from '@barnabas/api';

import { ConfirmDialogComponent } from '@barnabas/components';
import { ReportListingDialogComponent, wordsFor } from '@barnabas/domain';

/**
 * One listing, shown two ways.
 *
 * A visitor sees a way to ask for it. The owner sees what they can do with it and no way to ask
 * for it at all - which is both L2-041 and L2-062: a member cannot request their own listing,
 * and the screen does not offer them the chance to try.
 *
 * Which of the two is not decided here by comparing identifiers. The API says whose it is.
 *
 * Neither of the two is told the listing has been reported. The owner learns nothing about a
 * complaint until a moderator acts on it, and a visitor who reported it sees the same screen as
 * everybody else - L2-081.
 *
 * Every verb on the screen comes from the listing's kind rather than being written into the
 * template: an owner marks a sale *sold* and a gift *given away*, and a visitor asks to *borrow*
 * a loan but to *buy* a sale. The server derives the outcome from the kind by the same rule.
 */
@Component({
  selector: 'bar-listing-detail',
  imports: [RouterLink, ConfirmDialogComponent, ReportListingDialogComponent],
  templateUrl: './listing-detail.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ListingDetailComponent {
  private readonly listings = inject(LISTING_SERVICE);
  private readonly router = inject(Router);

  readonly listingId = input.required<string>();

  readonly listing = signal<ListingDetail | null>(null);
  readonly missing = signal(false);

  /**
   * The wording a kind is spoken about in, for the template to bind through `@let`.
   *
   * Exposed as the function rather than as a computed because the template already has the
   * listing narrowed inside its `@if`, and a computed would have to answer null outside it for
   * no reader's benefit.
   */
  readonly wordsFor = wordsFor;

  constructor() {
    effect(() => {
      void this.load(this.listingId());
    });
  }

  /** Where a sent report leads. The dialogue says what happened; the page says where to go. */
  async reported(): Promise<void> {
    await this.router.navigate(['/listings', this.listingId(), 'reported']);
  }

  async closeOut(): Promise<void> {
    await this.listings.closeOut(this.listingId());

    await this.router.navigate(['/my-listings']);
  }

  private async load(listingId: string): Promise<void> {
    this.missing.set(false);

    try {
      this.listing.set(await this.listings.get(listingId));
    } catch {
      this.listing.set(null);
      this.missing.set(true);
    }
  }
}
