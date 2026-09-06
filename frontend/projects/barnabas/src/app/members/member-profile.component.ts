import { ChangeDetectionStrategy, Component, effect, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { MEMBER_SERVICE, MemberProfile } from '@barnabas/api';

/**
 * Another member, as the congregation sees them.
 *
 * Their listings are here because a profile is a way to what somebody is offering, not a
 * biography. Each opens its listing, which is where asking for it happens.
 *
 * There is no way to message them from here, and the API has no endpoint that would. A request
 * against a listing, once accepted, is the only thing that opens a thread — L2-069.
 */
@Component({
  selector: 'bar-member-profile',
  imports: [RouterLink],
  templateUrl: './member-profile.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MemberProfileComponent {
  private readonly members = inject(MEMBER_SERVICE);

  readonly memberId = input.required<string>();

  readonly member = signal<MemberProfile | null>(null);
  readonly missing = signal(false);

  constructor() {
    effect(() => {
      void this.load(this.memberId());
    });
  }

  private async load(memberId: string): Promise<void> {
    this.missing.set(false);

    try {
      this.member.set(await this.members.profile(memberId));
    } catch {
      // Another congregation's member, one still waiting on a moderator, and one who has left all
      // answer the same way. Telling them apart would say who belongs where.
      this.member.set(null);
      this.missing.set(true);
    }
  }
}
