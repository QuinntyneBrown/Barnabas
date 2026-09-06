import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { JoiningStore } from '@barnabas/domain';

/**
 * A code that did not work.
 *
 * Three refusals and three screens' worth of wording, because what the member should do next
 * differs: a code nobody recognises invites another try, a dead one sends them back to whoever
 * invited them, and a throttled one asks them to wait.
 *
 * Which of expired, spent and revoked a dead code was is not said, and cannot be — the API
 * answers all three alike on purpose, so as not to disclose anything about a code the caller is
 * not entitled to know about.
 */
@Component({
  selector: 'bar-invite-invalid',
  imports: [RouterLink],
  templateUrl: './invite-invalid.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InviteInvalidComponent {
  private readonly joining = inject(JoiningStore);

  readonly refusal = this.joining.refusedBecause;

  readonly heading = computed(() => {
    switch (this.refusal()) {
      case 'unrecognised':
        return 'That code was not recognised';
      case 'spent':
        return 'That code can no longer be used';
      case 'throttled':
        return 'Too many tries';
      default:
        return 'That code did not work';
    }
  });
}
