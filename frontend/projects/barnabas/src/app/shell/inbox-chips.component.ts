import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';

/**
 * The three views of the inbox.
 *
 * Requests to me, my requests, and messages are one screen seen three ways, so the chips live in
 * one component that all three render rather than in each of them.
 */
@Component({
  selector: 'bar-inbox-chips',
  imports: [RouterLink, RouterLinkActive],
  templateUrl: './inbox-chips.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class InboxChipsComponent {}
