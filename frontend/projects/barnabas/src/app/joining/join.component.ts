import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { JoiningStore } from '@barnabas/domain';

/**
 * Step one of joining: the code from the bulletin.
 *
 * A code is spoken aloud and typed by hand, so it is read the way somebody types it — lower case,
 * and with the space they put in the middle. The API normalises; this only has to not get in the
 * way.
 */
@Component({
  selector: 'bar-join',
  imports: [FormsModule, RouterLink],
  templateUrl: './join.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class JoinComponent {
  private readonly joining = inject(JoiningStore);
  private readonly router = inject(Router);

  readonly code = signal('');
  readonly redeeming = signal(false);

  async submit(): Promise<void> {
    if (this.redeeming()) {
      return;
    }

    this.redeeming.set(true);

    try {
      const accepted = await this.joining.redeem(this.code());

      await this.router.navigate([accepted ? '/join/profile' : '/join/invalid']);
    } finally {
      this.redeeming.set(false);
    }
  }
}
