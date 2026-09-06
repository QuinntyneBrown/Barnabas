import { ChangeDetectionStrategy, Component, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AvailabilityWindowInput, LISTING_SERVICE } from '@barnabas/api';

import { CongregationStore, FieldErrors, focusFirstInvalid } from '@barnabas/domain';

/** One row of the windows editor, before it is worth sending. */
interface WindowRow {
  day: string;
  startsAt: string;
  endsAt: string;
}

/**
 * Step two of offering help.
 *
 * The only form of the four that collects a repeating structure rather than a set of fields.
 * Help offers time, so the offer has to say when that time actually is; a requester later
 * chooses one of these windows rather than naming an hour of their own.
 *
 * There is no price field and no photo field. Time is not sold here, and there is nothing to
 * photograph.
 */
@Component({
  selector: 'bar-post-help',
  imports: [FormsModule, RouterLink],
  templateUrl: './post-help.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PostHelpComponent {
  /** The order the form declares its controls, which is the order faults are reported in. */
  private static readonly FieldOrder = ['title', 'category', 'description', 'windows', 'hood'];

  private readonly listings = inject(LISTING_SERVICE);
  private readonly router = inject(Router);
  private readonly congregations = inject(CongregationStore);

  readonly title = signal('');
  readonly description = signal('');
  readonly category = signal('Rides');
  readonly neighbourhood = signal('');

  /** One window to begin with, because an offer with none cannot be posted. */
  readonly windows = signal<readonly WindowRow[]>([{ day: 'Tuesday', startsAt: '09:00', endsAt: '12:00' }]);

  readonly categories = ['Rides', 'Tutoring', 'Moving help', 'Tech support', 'Companionship', 'Minor repairs'];

  readonly days = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'];

  /**
   * The congregation's own neighbourhoods, read from the API rather than written here.
   *
   * L2-022 asks a member to choose from their parish's list and no other's, and a literal cannot
   * promise that - it was St. Aidan's eight for every member of every parish, and the API would
   * have refused a Parkdale member's own neighbourhood as one their congregation does not offer.
   */
  readonly neighbourhoods = this.congregations.neighbourhoods;

  readonly errors = signal(FieldErrors.none());
  readonly posting = signal(false);

  addWindow(): void {
    this.windows.update((windows) => [...windows, { day: 'Thursday', startsAt: '09:00', endsAt: '12:00' }]);
  }

  /**
   * Removes a window, unless it is the last one.
   *
   * An offer with no windows cannot be posted, so the form does not let a member arrive at one
   * and then be told so by the server.
   */
  removeWindow(index: number): void {
    this.windows.update((windows) => (windows.length === 1 ? windows : windows.filter((_, at) => at !== index)));
  }

  setDay(index: number, day: string): void {
    this.updateWindow(index, (window) => ({ ...window, day }));
  }

  setStartsAt(index: number, startsAt: string): void {
    this.updateWindow(index, (window) => ({ ...window, startsAt }));
  }

  setEndsAt(index: number, endsAt: string): void {
    this.updateWindow(index, (window) => ({ ...window, endsAt }));
  }

  constructor() {
    // The congregation's neighbourhoods are not written into this form, so it has to ask for
    // them - and it is not the screen a member arrives on, so nothing else will have.
    void this.congregations.ensureLoaded();

    effect(() => {
      const offered = this.neighbourhoods();

      if (offered.length > 0 && this.neighbourhood() === '') {
        this.neighbourhood.set(offered[0]);
      }
    });
  }
  async submit(): Promise<void> {
    if (this.posting()) {
      return;
    }

    this.posting.set(true);
    this.errors.set(FieldErrors.none());

    try {
      const posted = await this.listings.postHelp({
        title: this.title(),
        description: this.description(),
        category: this.category(),
        neighbourhood: this.neighbourhood(),
        windows: this.windows().map(
          (window): AvailabilityWindowInput => ({
            day: window.day,
            startsAt: window.startsAt,
            endsAt: window.endsAt,
          }),
        ),
      });

      await this.router.navigate(['/listings', posted.listingId, 'posted']);
    } catch (failure) {
      const errors = FieldErrors.from(failure);

      this.errors.set(errors);

      focusFirstInvalid(errors, PostHelpComponent.FieldOrder);
    } finally {
      this.posting.set(false);
    }
  }

  private updateWindow(index: number, change: (window: WindowRow) => WindowRow): void {
    this.windows.update((windows) => windows.map((window, at) => (at === index ? change(window) : window)));
  }
}
