import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { CongregationStore, JoiningStore } from '@barnabas/domain';

/**
 * Where a member waits.
 *
 * Reached two ways, and it has to name the congregation on both. A member signing in later reads
 * it from the API, which still answers them while they wait. Somebody who has only just joined
 * has no session at all, and for them it is the name that came back with the code.
 *
 * The one thing somebody waiting needs to know is which parish they are waiting on.
 */
@Component({
  selector: 'bar-awaiting-approval',
  imports: [RouterLink],
  templateUrl: './awaiting-approval.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AwaitingApprovalComponent {
  private readonly congregations = inject(CongregationStore);
  private readonly joining = inject(JoiningStore);

  readonly congregationName = computed(
    () => this.congregations.name() || this.joining.congregationName(),
  );

  constructor() {
    void this.congregations.load();
  }
}
