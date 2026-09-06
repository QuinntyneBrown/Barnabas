import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';

/**
 * Step one of posting: which of the four this is.
 *
 * The kind is chosen before any detail is entered and fixed thereafter, because the four are not
 * interchangeable. An earlier iteration sent Lend, Give and Sell to one shared form carrying a
 * price, which made a gift look like a sale and lost the return date a loan depends on.
 *
 * Only Lend is buildable in this slice. The other three are shown as what they are - coming -
 * rather than hidden, because the choice is the point of the screen.
 */
@Component({
  selector: 'bar-choose-kind',
  imports: [RouterLink],
  templateUrl: './choose-kind.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ChooseKindComponent {}
