import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { LISTING_SERVICE } from '@barnabas/api';

import { FieldErrors, focusFirstInvalid } from '@barnabas/domain';

/**
 * Step two of selling something.
 *
 * The only form of the four that carries a price, and the price is a stated asking figure in
 * Canadian dollars. Barnabas neither collects nor holds it: the two members settle it in person,
 * which the form says rather than leaves to be assumed.
 *
 * A blank price travels as null rather than as an empty string, so a member who leaves it out
 * gets the field marked rather than the serializer complaining about a value.
 */
@Component({
  selector: 'bar-post-sell',
  imports: [FormsModule, RouterLink],
  templateUrl: './post-sell.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PostSellComponent {
  /** The order the form declares its controls, which is the order faults are reported in. */
  private static readonly FieldOrder = ['title', 'category', 'description', 'condition', 'price', 'hood'];

  private readonly listings = inject(LISTING_SERVICE);
  private readonly router = inject(Router);

  readonly title = signal('');
  readonly description = signal('');
  readonly category = signal('Outdoor');
  readonly neighbourhood = signal('Riverdale');
  readonly condition = signal('Good');
  readonly price = signal('');

  readonly categories = ['Tools', 'Household', 'Kids & family', 'Electronics', 'Furniture', 'Outdoor'];

  readonly conditions = ['New', 'As new', 'Good', 'Fair', 'For parts'];

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
      const posted = await this.listings.postSell({
        title: this.title(),
        description: this.description(),
        category: this.category(),
        neighbourhood: this.neighbourhood(),
        condition: this.condition(),
        price: this.price() === '' ? null : Number(this.price()),
      });

      await this.router.navigate(['/listings', posted.listingId, 'posted']);
    } catch (failure) {
      const errors = FieldErrors.from(failure);

      this.errors.set(errors);

      focusFirstInvalid(errors, PostSellComponent.FieldOrder);
    } finally {
      this.posting.set(false);
    }
  }
}
