import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterLink } from '@angular/router';

/**
 * What happens now that a report has been sent.
 *
 * It answers the three things a member who has just reported somebody's listing wants to know:
 * that a moderator reads it, that the poster is not told who reported it, and that the listing
 * stays up meanwhile. The last of those is L2-080 AC3 and is the one people get wrong - reporting
 * flags a listing for review; it does not remove it.
 */
@Component({
  selector: 'bar-report-sent',
  imports: [RouterLink],
  templateUrl: './report-sent.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ReportSentComponent {}
