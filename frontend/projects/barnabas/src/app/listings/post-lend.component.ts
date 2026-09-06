import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { LISTING_SERVICE } from '@barnabas/api';

import { FieldErrors } from '@barnabas/domain';
import { focusFirstInvalid } from '@barnabas/domain';

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
  imports: [FormsModule, RouterLink],
  templateUrl: './post-lend.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PostLendComponent {
  /** The order the form declares its controls, which is the order faults are reported in. */
  private static readonly FieldOrder = ['title', 'category', 'description', 'returnBy', 'hood'];

  private readonly listings = inject(LISTING_SERVICE);
  private readonly router = inject(Router);

  readonly title = signal('');
  readonly description = signal('');
  readonly category = signal('Tools');
  readonly neighbourhood = signal('Riverdale');
  readonly returnBy = signal('');

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
      const posted = await this.listings.postLend({
        title: this.title(),
        description: this.description(),
        category: this.category(),
        neighbourhood: this.neighbourhood(),
        returnBy: this.returnBy() || null,
      });

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
