import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { RouterLink } from '@angular/router';

/**
 * The owner has accepted, and a thread is open.
 *
 * It names the requester and offers the thread directly, which is why accepting returns the
 * thread's identifier with the decision: the two members have just been put in touch, and making
 * the owner go and find the conversation would be a strange place to stop.
 */
@Component({
  selector: 'bar-request-accepted',
  imports: [RouterLink],
  templateUrl: './request-accepted.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RequestAcceptedComponent {
  readonly threadId = input.required<string>();

  readonly requester = input('');
}
