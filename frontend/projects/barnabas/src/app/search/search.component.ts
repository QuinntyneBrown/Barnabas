import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ListingKind, SEARCH_SERVICE, SearchResult } from '@barnabas/api';
import { CongregationStore, wordsFor } from '@barnabas/domain';

/**
 * Finding something the board is too long to scroll for.
 *
 * The searching is done by the API rather than by filtering what is already on screen, so a
 * search reaches the whole board and a filtered result still pages. The term comes back with the
 * results, which is what lets a screen that found nothing say what was looked for.
 *
 * The neighbourhood control offers this congregation's own list, read from the congregation
 * rather than written into the template — the same list the profile and the post forms offer.
 */
@Component({
  selector: 'bar-search',
  imports: [FormsModule, RouterLink],
  templateUrl: './search.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SearchComponent {
  private readonly search = inject(SEARCH_SERVICE);
  private readonly congregations = inject(CongregationStore);

  readonly term = signal('');
  readonly kind = signal<ListingKind | null>(null);
  readonly neighbourhood = signal<string>('');
  readonly maxPrice = signal<string>('');

  readonly results = signal<readonly SearchResult[]>([]);
  readonly searchedFor = signal<string | null>(null);
  readonly searching = signal(false);
  readonly failed = signal(false);

  readonly neighbourhoods = this.congregations.neighbourhoods;

  readonly kinds: readonly { readonly kind: ListingKind; readonly label: string }[] = (
    ['Lend', 'Give', 'Sell', 'Help'] as const
  ).map((kind) => ({ kind, label: wordsFor(kind).board }));

  /** Whether anything is narrowing the results, which decides what an empty screen offers. */
  readonly filtered = computed(
    () => this.kind() !== null || this.neighbourhood() !== '' || this.maxPrice() !== '',
  );

  constructor() {
    void this.congregations.load();
  }

  labelFor(kind: ListingKind): string {
    return wordsFor(kind).board;
  }

  async submit(): Promise<void> {
    if (this.term().trim() === '') {
      return;
    }

    this.searching.set(true);
    this.failed.set(false);

    try {
      const page = await this.search.search(this.term().trim(), {
        kind: this.kind() ?? undefined,
        neighbourhood: this.neighbourhood() || undefined,
        maxPrice: this.maxPrice() === '' ? undefined : Number(this.maxPrice()),
      });

      this.results.set(page.results);
      this.searchedFor.set(page.term);
    } catch {
      this.failed.set(true);
      this.results.set([]);
    } finally {
      this.searching.set(false);
    }
  }

  async narrowTo(kind: ListingKind): Promise<void> {
    this.kind.update((current) => (current === kind ? null : kind));

    await this.submit();
  }

  /** Drops every filter and searches the same term again. */
  async clearFilters(): Promise<void> {
    this.kind.set(null);
    this.neighbourhood.set('');
    this.maxPrice.set('');

    await this.submit();
  }
}
