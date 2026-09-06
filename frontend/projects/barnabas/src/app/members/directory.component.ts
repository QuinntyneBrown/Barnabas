import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { DirectoryMember, MEMBER_SERVICE } from '@barnabas/api';

/**
 * Who is in the congregation, and what they can help with.
 *
 * The board answers "what is on offer"; this answers "who is here". Searching is done by the API
 * rather than by filtering rows in the browser, so a search reaches every member rather than the
 * ones already on screen.
 *
 * Every row opens a profile. A row that led nowhere would be a name and no way to act on it.
 */
@Component({
  selector: 'bar-directory',
  imports: [FormsModule, RouterLink],
  templateUrl: './directory.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DirectoryComponent {
  private readonly members = inject(MEMBER_SERVICE);

  readonly rows = signal<readonly DirectoryMember[]>([]);
  readonly loading = signal(false);
  readonly failed = signal(false);

  readonly term = signal('');
  readonly helpTag = signal<string | null>(null);

  constructor() {
    void this.search();
  }

  async search(): Promise<void> {
    this.loading.set(true);
    this.failed.set(false);

    try {
      this.rows.set(
        await this.members.directory({
          term: this.term() || undefined,
          helpTag: this.helpTag() ?? undefined,
        }),
      );
    } catch {
      this.failed.set(true);
      this.rows.set([]);
    } finally {
      this.loading.set(false);
    }
  }

  /** Narrows to one kind of help, or widens again if it was already in force. */
  async filterBy(tag: string): Promise<void> {
    this.helpTag.update((current) => (current === tag ? null : tag));

    await this.search();
  }

  async clear(): Promise<void> {
    this.term.set('');
    this.helpTag.set(null);

    await this.search();
  }
}
