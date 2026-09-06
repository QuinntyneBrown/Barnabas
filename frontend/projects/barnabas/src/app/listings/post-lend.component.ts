import { ChangeDetectionStrategy, Component, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { LISTING_SERVICE } from '@barnabas/api';

import { CongregationStore, FieldErrors, PhotoFieldComponent, focusFirstInvalid } from '@barnabas/domain';

/**
 * Step two of posting a loan.
 *
 * It renders only what a loan needs, and there is no price field on it - not hidden, not
 * disabled, absent. A loan is not a sale, and the API rejects a price outright rather than
 * quietly dropping one.
 *
 * On a refusal the failing fields are marked, each message is associated to its control, and
 * focus moves to the first of them. What the member typed is kept.
 */
@Component({
  selector: 'bar-post-lend',
  imports: [FormsModule, RouterLink, PhotoFieldComponent],
  templateUrl: './post-lend.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PostLendComponent {
  /** The order the form declares its controls, which is the order faults are reported in. */
  private static readonly FieldOrder = ['title', 'category', 'description', 'returnBy', 'hood'];

  private readonly listings = inject(LISTING_SERVICE);
  private readonly router = inject(Router);
  private readonly congregations = inject(CongregationStore);

  readonly title = signal('');
  readonly description = signal('');
  readonly category = signal('Tools');
  readonly neighbourhood = signal('');
  readonly returnBy = signal('');

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
  readonly posting = signal(false);

  /**
   * The photograph, if they chose one.
   *
   * Held rather than sent as it is picked, because a photo goes on a listing that already exists
   * and there is no listing until this form has posted.
   */
  readonly photo = signal<File | null>(null);

  constructor() {
    // The congregation's neighbourhoods are not written into this form, so it has to ask for
    // them - and it is not the screen a member arrives on, so nothing else will have.
    void this.congregations.ensureLoaded();

    effect(() => {
      const offered = this.neighbourhoods();

      if (offered.length > 0 && this.neighbourhood() === '') {
        this.neighbourhood.set(offered[0]);
      }
    });
  }
  /** Whichever they picked, or the first their congregation offers if the form has not settled. */
  private chosenNeighbourhood(): string {
    return this.neighbourhood() || this.neighbourhoods()[0] || '';
  }

  async submit(): Promise<void> {
    if (this.posting()) {
      return;
    }

    this.posting.set(true);
    this.errors.set(FieldErrors.none());

    try {
      // The neighbourhoods are the congregation's and arrive from the API, so a member who
      // submitted before they landed would send an empty one and be refused a field they were
      // never shown as blank. Waiting here is the only place that cannot race.
      await this.congregations.ensureLoaded();

      const posted = await this.listings.postLend({
        title: this.title(),
        description: this.description(),
        category: this.category(),
        neighbourhood: this.chosenNeighbourhood(),
        returnBy: this.returnBy() || null,
      });

      // After the listing exists, and before the confirmation. A photo that failed to attach
      // must not leave the member on a screen saying everything went well - the listing is
      // posted either way, and the error names which half did not.
      const photo = this.photo();

      if (photo) {
        await this.listings.attachPhoto(posted.listingId, photo);
      }

      await this.router.navigate(['/listings', posted.listingId, 'posted']);
    } catch (failure) {
      const errors = FieldErrors.from(failure);

      this.errors.set(errors);

      focusFirstInvalid(errors, PostLendComponent.FieldOrder);
    } finally {
      this.posting.set(false);
    }
  }
}
