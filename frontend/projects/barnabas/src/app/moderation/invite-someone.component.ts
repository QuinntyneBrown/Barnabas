import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { INVITE_SERVICE, IssuedInvite } from '@barnabas/api';

/**
 * A moderator's own page: the code to give somebody.
 *
 * It names no congregation, because the API takes that from the session — a moderator cannot
 * issue a code into a parish they do not belong to, and the screen has nothing to choose.
 *
 * Reachable only by a moderator, and the API refuses an ordinary member regardless. Hiding the
 * entry is presentation; the 403 is the rule.
 */
@Component({
  selector: 'bar-invite-someone',
  imports: [RouterLink],
  templateUrl: './invite-someone.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InviteSomeoneComponent {
  private readonly invites = inject(INVITE_SERVICE);

  readonly issued = signal<IssuedInvite | null>(null);
  readonly issuing = signal(false);
  readonly failed = signal(false);

  async issue(): Promise<void> {
    if (this.issuing()) {
      return;
    }

    this.issuing.set(true);
    this.failed.set(false);

    try {
      this.issued.set(await this.invites.issue());
    } catch {
      this.failed.set(true);
    } finally {
      this.issuing.set(false);
    }
  }
}
