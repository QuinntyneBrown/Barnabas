import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { SessionStore } from '@barnabas/domain';

/**
 * Where a member asks for a sign-in link.
 *
 * There is no password field because there is no password. What the screen collects is the
 * address the parish office already has, and what it promises is a link that works once.
 */
@Component({
  selector: 'bar-sign-in',
  imports: [FormsModule],
  templateUrl: './sign-in.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SignInComponent {
  private readonly session = inject(SessionStore);
  private readonly router = inject(Router);

  readonly emailAddress = signal('');
  readonly sending = signal(false);

  async submit(): Promise<void> {
    const address = this.emailAddress().trim();

    if (address.length === 0 || this.sending()) {
      return;
    }

    this.sending.set(true);

    try {
      await this.session.requestLink(address);
    } catch {
      // Deliberately swallowed. The answer is the same whether or not the address belongs to a
      // member, and a failure shown here would be a way to tell the two apart.
    } finally {
      this.sending.set(false);
    }

    await this.router.navigate(['/check-your-email'], { queryParams: { address } });
  }
}
