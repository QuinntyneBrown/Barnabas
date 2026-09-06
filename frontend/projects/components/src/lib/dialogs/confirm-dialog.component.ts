import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  input,
  output,
  viewChild,
} from '@angular/core';

/**
 * A decision the member has to mean.
 *
 * A native `dialog` rather than a div with a backdrop. `showModal` traps focus and closes on
 * Escape without a line of code, which is two thirds of what L2-111 asks of a dialog; the third
 * is returning focus to whatever opened it, and that is the part the platform does not do.
 *
 * Being native also means it stays shut and harmless when scripting is unavailable, rather than
 * appearing as a stray block of text in the middle of the page.
 */
@Component({
  selector: 'bar-confirm-dialog',
  templateUrl: './confirm-dialog.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ConfirmDialogComponent {
  readonly heading = input.required<string>();
  readonly body = input.required<string>();
  readonly confirmLabel = input('Confirm');
  readonly cancelLabel = input('Cancel');

  readonly confirmed = output<void>();

  private readonly dialog = viewChild.required<ElementRef<HTMLDialogElement>>('dialog');

  /** Remembered so focus can go back where it came from. */
  private opener: HTMLElement | null = null;

  open(): void {
    this.opener = document.activeElement as HTMLElement | null;

    this.dialog().nativeElement.showModal();
  }

  confirm(): void {
    this.dialog().nativeElement.close();

    this.confirmed.emit();
  }

  cancel(): void {
    this.dialog().nativeElement.close();
  }

  /** Fires for the buttons and for Escape alike, so both restore focus. */
  onClosed(): void {
    this.opener?.focus();
    this.opener = null;
  }
}
