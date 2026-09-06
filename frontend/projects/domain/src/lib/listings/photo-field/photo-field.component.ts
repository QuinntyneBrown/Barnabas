import { ChangeDetectionStrategy, Component, input, output, signal } from '@angular/core';

/**
 * Choosing one photograph for a listing.
 *
 * A file input and nothing more clever. A drag target that is not also an input is unreachable
 * from a keyboard, and this is a parish noticeboard rather than a photo library — the platform's
 * own control is what a phone opens the camera from.
 *
 * It emits the file and stores nothing. The form that owns it decides when to send it, because a
 * photo goes on a listing that already exists and there is no listing until the form has posted.
 *
 * It lives in `domain` rather than `components` only because the wording is Barnabas's own: the
 * three kinds that take a photo, and the one that does not, are this product's vocabulary.
 */
@Component({
  selector: 'bar-photo-field',
  templateUrl: './photo-field.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PhotoFieldComponent {
  /** What the file input accepts, and what the API will re-encode. */
  readonly accept = 'image/jpeg,image/png,image/webp';

  readonly label = input('Photo');

  readonly chosen = output<File | null>();

  readonly name = signal<string | null>(null);

  /** A local preview, so somebody knows they picked the right one before they post. */
  readonly preview = signal<string | null>(null);

  choose(files: FileList | null): void {
    const file = files?.item(0) ?? null;

    this.release();

    this.name.set(file?.name ?? null);
    this.preview.set(file ? URL.createObjectURL(file) : null);

    this.chosen.emit(file);
  }

  /**
   * Lets go of the previous preview.
   *
   * An object URL is held until it is revoked or the document goes, so choosing a different photo
   * five times would otherwise keep five files alive for the life of the page.
   */
  private release(): void {
    const previous = this.preview();

    if (previous) {
      URL.revokeObjectURL(previous);
    }
  }
}
