import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { LISTING_SERVICE } from '@barnabas/api';

import { FieldErrors, focusFirstInvalid } from '@barnabas/domain';

/**
 * Step two of giving something away.
 *
 * The shortest of the four forms, and deliberately so. A gift needs nothing a listing does not
 * already have: no price, because it is free, and no return date, because it is not coming back.
 * That it is free and collected in person is said on the form rather than collected by it.
 */
@Component({
  selector: 'bar-post-give',
  imports: [FormsModule, RouterLink],
  templateUrl: './post-give.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PostGiveComponent {
  /** The order the form declares its controls, which is the order faults are reported in. */
  private static readonly FieldOrder = ['title', 'category', 'description', 'hood'];

  private readonly listings = inject(LISTING_SERVICE);
  private readonly router = inject(Router);

  readonly title = signal('');
  readonly description = signal('');
  readonly category = signal('Household');
  readonly neighbourhood = signal('Riverdale');

  readonly categories = ['Tools', 'Household', 'Kids & family', 'Electronics', 'Furniture', 'Outdoor'];

  readonly neighbourhoods = [
    'Riverdale',
    'Leslieville',
    'The Danforth',
    'Scarborough',
    'The Beaches',
    'East York',
    'Cabbagetown',
    'North York',
  ];

  readonly errors = signal(FieldErrors.none());
  readonly posting = signal(false);

  async submit(): Promise<void> {
    if (this.posting()) {
      return;
    }

    this.posting.set(true);
    this.errors.set(FieldErrors.none());

    try {
      const posted = await this.listings.postGive({
        title: this.title(),
        description: this.description(),
        category: this.category(),
        neighbourhood: this.neighbourhood(),
      });

      await this.router.navigate(['/listings', posted.listingId, 'posted']);
    } catch (failure) {
      const errors = FieldErrors.from(failure);

      this.errors.set(errors);

      focusFirstInvalid(errors, PostGiveComponent.FieldOrder);
    } finally {
      this.posting.set(false);
    }
  }
}
