import { ChangeDetectionStrategy, Component, effect, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

import { ConfirmDialogComponent } from '@barnabas/components';
import { FieldErrors, ProfileStore, SessionStore, focusFirstInvalid } from '@barnabas/domain';
import { NOTIFICATION_SERVICE, NotificationPreference } from '@barnabas/api';

/**
 * Profile and settings, on one screen.
 *
 * Saving confirms without leaving, because a member correcting a description has not finished
 * with the screen — and being thrown back to the board after every change would make small
 * corrections expensive.
 *
 * The neighbourhoods and the kinds of help are the congregation's own, returned with the profile.
 * Free text is refused here and at the API: what a parish recognises is not the same as what a
 * form will take.
 */
@Component({
  selector: 'bar-profile-settings',
  imports: [FormsModule, RouterLink, ConfirmDialogComponent],
  templateUrl: './profile-settings.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProfileSettingsComponent {
  private static readonly FieldOrder = ['displayName', 'hood', 'description'];

  private readonly store = inject(ProfileStore);
  private readonly notifications = inject(NOTIFICATION_SERVICE);
  private readonly session = inject(SessionStore);
  private readonly router = inject(Router);

  readonly profile = this.store.profile;
  readonly loading = this.store.loading;
  readonly saving = this.store.saving;
  readonly savedJustNow = this.store.savedJustNow;
  readonly neighbourhoods = this.store.neighbourhoods;
  readonly availableHelpTags = this.store.availableHelpTags;

  readonly displayName = signal('');
  readonly neighbourhood = signal('');
  readonly description = signal('');
  readonly helpTags = signal<readonly string[]>([]);

  readonly errors = signal(FieldErrors.none());

  /**
   * Which kinds of notification this member wants.
   *
   * On the settings screen because that is where L2-075 puts it - a member deciding what to hear
   * about is doing the same sort of thing as deciding what the congregation sees of them.
   */
  readonly preferences = signal<readonly NotificationPreference[]>([]);
  readonly preferencesSaved = signal(false);

  constructor() {
    void this.store.load();
    void this.loadPreferences();

    effect(() => {
      const profile = this.profile();

      if (profile === null) {
        return;
      }

      this.displayName.set(profile.displayName);
      this.neighbourhood.set(profile.neighbourhood);
      this.description.set(profile.description ?? '');
      this.helpTags.set(profile.helpTags);
    });
  }

  /** The words a member reads, rather than the enum name the API speaks. */
  labelFor(kind: string): string {
    const said: Record<string, string> = {
      RequestReceived: 'Somebody asks for something of mine',
      RequestAccepted: 'A request of mine is accepted',
      RequestDeclined: 'A request of mine is declined',
      MessageReceived: 'Somebody writes to me',
    };

    return said[kind] ?? kind;
  }

  async togglePreference(kind: string, enabled: boolean): Promise<void> {
    this.preferencesSaved.set(false);

    const next = this.preferences().map((preference) =>
      preference.kind === kind ? { ...preference, enabled } : preference,
    );

    this.preferences.set(next);
    this.preferences.set(await this.notifications.setPreferences(next));
    this.preferencesSaved.set(true);
  }

  offers(tag: string): boolean {
    return this.helpTags().includes(tag);
  }

  toggle(tag: string): void {
    this.store.touched();

    this.helpTags.update((tags) =>
      tags.includes(tag) ? tags.filter((declared) => declared !== tag) : [...tags, tag],
    );
  }

  touched(): void {
    this.store.touched();
  }

  async save(): Promise<void> {
    if (this.saving()) {
      return;
    }

    this.errors.set(FieldErrors.none());

    try {
      await this.store.save({
        displayName: this.displayName(),
        neighbourhood: this.neighbourhood(),
        description: this.description() || null,
        helpTags: this.helpTags(),
      });
    } catch (failure) {
      const errors = FieldErrors.from(failure);

      this.errors.set(errors);

      focusFirstInvalid(errors, ProfileSettingsComponent.FieldOrder);
    }
  }

  private async loadPreferences(): Promise<void> {
    try {
      this.preferences.set(await this.notifications.preferences());
    } catch {
      this.preferences.set([]);
    }
  }

  /** They leave, and the session goes with them, so the landing page is where they end up. */
  async leave(): Promise<void> {
    await this.store.leave();

    this.session.clear();

    await this.router.navigate(['/']);
  }
}
