import { ChangeDetectionStrategy, Component, inject, input } from '@angular/core';
import { RouterLink } from '@angular/router';
import { SessionStore } from '@barnabas/domain';

/**
 * What a member sees after asking for a link.
 *
 * It names the address, because the commonest reason a link never arrives is that it went
 * somewhere else. It is also identical whether or not the address belongs to a member: this
 * screen is what keeps the sign-in endpoint from being a way to discover who is in the parish.
 */
@Component({
  selector: 'bar-check-your-email',
  imports: [RouterLink],
  templateUrl: './check-your-email.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CheckYourEmailComponent {
  private readonly session = inject(SessionStore);

  /** Bound from the query string by the router. */
  readonly address = input('');

  async sendAgain(): Promise<void> {
    if (this.address()) {
      await this.session.requestLink(this.address());
    }
  }
}
