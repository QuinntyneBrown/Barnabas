import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  inject,
  input,
  output,
  signal,
  viewChild,
} from '@angular/core';
import { REPORT_REASONS, ReportReason } from '@barnabas/api';

import { ReportStore } from '../report.store';

/**
 * Reporting a listing, in the one place a member is looking at the listing.
 *
 * A native `dialog`, for the same reasons the confirm dialog is one: `showModal` traps focus and
 * closes on Escape, and it stays shut and harmless when scripting is unavailable. It carries a
 * form rather than two buttons, so it is its own component rather than a use of that one.
 *
 * It navigates nowhere. A region says what happened and the page decides where that leads, which
 * is what keeps routing out of this library.
 */
@Component({
  selector: 'bar-report-listing-dialog',
  templateUrl: './report-listing-dialog.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ReportListingDialogComponent {
  private readonly reports = inject(ReportStore);

  readonly listingId = input.required<string>();

  /** Named so the dialogue can promise, by name, that they are not told who reported it. */
  readonly ownerDisplayName = input.required<string>();

  readonly sent = output<void>();

  readonly reasons = REPORT_REASONS;
  readonly reason = signal<ReportReason>('NotAllowed');
  readonly note = signal('');

  readonly sending = this.reports.sending;
  readonly failed = this.reports.failed;
  readonly alreadyReported = this.reports.alreadyReported;

  private readonly dialog = viewChild.required<ElementRef<HTMLDialogElement>>('dialog');

  private opener: HTMLElement | null = null;

  open(): void {
    this.reports.reset();
    this.reason.set('NotAllowed');
    this.note.set('');
    this.opener = document.activeElement as HTMLElement | null;

    this.dialog().nativeElement.showModal();
  }

  cancel(): void {
    this.dialog().nativeElement.close();
  }

  onClosed(): void {
    this.opener?.focus();
    this.opener = null;
  }

  chooseReason(value: string): void {
    this.reason.set(value as ReportReason);
  }

  async send(): Promise<void> {
    const sent = await this.reports.report(this.listingId(), this.reason(), this.note());

    if (!sent) {
      // Left open, holding what they wrote. A dialogue that closed on failure would lose the
      // note and leave them guessing whether it went.
      return;
    }

    this.dialog().nativeElement.close();

    this.sent.emit();
  }
}
