import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { SessionStore } from '@barnabas/domain';

import { ConfirmDialogComponent } from '@barnabas/components';

/**
 * The member's own corner of the board.
 *
 * Signing out is confirmed rather than immediate: a member on a shared machine choosing it means
 * it, and a member who hit it by accident on a phone did not.
 */
@Component({
  selector: 'bar-you',
  imports: [RouterLink, ConfirmDialogComponent],
  templateUrl: './you.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class YouComponent {
  private readonly session = inject(SessionStore);

  /**
   * Whether to offer the moderator's own tools.
   *
   * Hiding the entry is presentation and nothing more: the API refuses an ordinary member the
   * queue and the invite endpoint regardless. L2-003 asks for both halves.
   */
  readonly isModerator = this.session.isModerator;
  private readonly router = inject(Router);

  async signOut(): Promise<void> {
    await this.session.signOut();

    await this.router.navigate(['/']);
  }
}
