import { ChangeDetectionStrategy, Component, effect, inject, input } from '@angular/core';
import { Router } from '@angular/router';
import { SessionStore } from '@barnabas/domain';

/**
 * What the emailed link opens.
 *
 * It exchanges the secret for a session and goes straight to the board. A link that has expired
 * or has already been used answers the same way, and the member is sent somewhere that offers to
 * send another rather than somewhere implying they mistyped something.
 */
@Component({
  selector: 'bar-sign-in-landing',
  templateUrl: './sign-in-landing.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SignInLandingComponent {
  private readonly session = inject(SessionStore);
  private readonly router = inject(Router);

  readonly token = input.required<string>();

  constructor() {
    effect(() => {
      void this.exchange(this.token());
    });
  }

  private async exchange(token: string): Promise<void> {
    try {
      await this.session.signIn(token);

      await this.router.navigate(['/board']);
    } catch {
      await this.router.navigate(['/sign-in/expired']);
    }
  }
}
