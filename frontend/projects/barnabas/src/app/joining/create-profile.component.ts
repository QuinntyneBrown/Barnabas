import { ChangeDetectionStrategy, Component, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';

import { FieldErrors, JoiningStore, focusFirstInvalid } from '@barnabas/domain';

/**
 * Step two of joining: who you are, and where in the parish.
 *
 * The neighbourhoods are the ones the congregation actually offers, returned when the code was
 * redeemed. Free text is not accepted, here or at the API — a parish's neighbourhoods are what
 * its members recognise, not what a form will take.
 *
 * An invalid field does not cost the member their code: the API validates before spending the
 * joining session, so a typo is corrected and submitted again rather than sending somebody back
 * to a moderator.
 */
@Component({
  selector: 'bar-create-profile',
  imports: [FormsModule],
  templateUrl: './create-profile.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CreateProfileComponent {
  private static readonly FieldOrder = ['displayName', 'emailAddress', 'neighbourhood', 'reason'];

  private readonly joining = inject(JoiningStore);
  private readonly router = inject(Router);

  readonly congregationName = this.joining.congregationName;
  readonly neighbourhoods = this.joining.neighbourhoods;

  readonly displayName = signal('');
  readonly emailAddress = signal('');
  readonly neighbourhood = signal('');
  readonly reasonForJoining = signal('');

  readonly errors = signal(FieldErrors.none());
  readonly joiningNow = signal(false);

  constructor() {
    effect(() => {
      // Reloading this page loses the joining token, which lived only in memory. Saying so beats
      // a form that submits into nothing.
      if (!this.joining.canSupplyAProfile()) {
        void this.router.navigate(['/join']);

        return;
      }

      const offered = this.neighbourhoods();

      if (offered.length > 0 && this.neighbourhood() === '') {
        this.neighbourhood.set(offered[0]);
      }
    });
  }

  async submit(): Promise<void> {
    if (this.joiningNow()) {
      return;
    }

    this.joiningNow.set(true);
    this.errors.set(FieldErrors.none());

    try {
      await this.joining.join({
        emailAddress: this.emailAddress(),
        displayName: this.displayName(),
        neighbourhood: this.neighbourhood(),
        reasonForJoining: this.reasonForJoining() || null,
      });

      await this.router.navigate(['/join/pending']);
    } catch (failure) {
      const errors = FieldErrors.from(failure);

      this.errors.set(errors);

      focusFirstInvalid(errors, CreateProfileComponent.FieldOrder);
    } finally {
      this.joiningNow.set(false);
    }
  }
}
