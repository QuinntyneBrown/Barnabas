import { Injectable, computed, inject, signal } from '@angular/core';
import { MEMBER_SERVICE, MyProfile } from '@barnabas/api';

/**
 * A member's own profile, and the change they are making to it.
 *
 * The saved profile and the draft are held apart. A member who typed a new description and then
 * hit a validation failure should still be looking at what they typed, not at what was there
 * before — and a screen that reloaded on every save would lose it.
 */
@Injectable({ providedIn: 'root' })
export class ProfileStore {
  private readonly members = inject(MEMBER_SERVICE);

  private readonly saved = signal<MyProfile | null>(null);

  readonly profile = this.saved.asReadonly();
  readonly loading = signal(false);
  readonly saving = signal(false);

  /** Shown after a save, and cleared as soon as the member changes anything. */
  readonly savedJustNow = signal(false);

  readonly neighbourhoods = computed(() => this.saved()?.neighbourhoods ?? []);
  readonly availableHelpTags = computed(() => this.saved()?.availableHelpTags ?? []);

  async load(): Promise<void> {
    this.loading.set(true);

    try {
      this.saved.set(await this.members.me());
    } finally {
      this.loading.set(false);
    }
  }

  async save(change: {
    displayName: string;
    neighbourhood: string;
    description: string | null;
    helpTags: readonly string[];
  }): Promise<void> {
    this.saving.set(true);
    this.savedJustNow.set(false);

    try {
      this.saved.set(await this.members.edit(change));
      this.savedJustNow.set(true);
    } finally {
      this.saving.set(false);
    }
  }

  /** Forgets the confirmation, so it does not linger over an unsaved change. */
  touched(): void {
    this.savedJustNow.set(false);
  }

  leave(): Promise<void> {
    return this.members.leave();
  }
}
