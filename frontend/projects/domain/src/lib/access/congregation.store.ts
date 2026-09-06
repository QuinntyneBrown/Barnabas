import { Injectable, computed, inject, signal } from '@angular/core';
import { CONGREGATION_SERVICE, Congregation } from '@barnabas/api';

/**
 * The congregation the signed-in member belongs to.
 *
 * Held once and read by every screen that names the parish. `L2-004` requires the heading to name
 * the member's own congregation and never another's, which a literal in a template cannot
 * promise — and a second member signing in on the same browser would otherwise still be looking
 * at the first one's parish.
 */
@Injectable({ providedIn: 'root' })
export class CongregationStore {
  private readonly congregations = inject(CONGREGATION_SERVICE);

  private readonly current = signal<Congregation | null>(null);

  /** The attempt in progress, so five screens asking at once make one call. */
  private inFlight: Promise<void> | null = null;

  readonly congregation = this.current.asReadonly();

  /** The parish's name, or nothing at all rather than a guess at one. */
  readonly name = computed(() => this.current()?.name ?? '');

  readonly neighbourhoods = computed(() => this.current()?.neighbourhoods ?? []);

  /**
   * Loads it once, and shares the attempt with whoever asks meanwhile.
   *
   * For screens that need the parish without owning it — the four post forms and the edit form
   * all offer its neighbourhoods, and none of them is the screen a member arrives on. Without
   * this each would either fetch it again or render an empty list depending on where the member
   * came from.
   */
  async ensureLoaded(): Promise<void> {
    if (this.current()) {
      return;
    }

    this.inFlight ??= this.load().finally(() => {
      this.inFlight = null;
    });

    await this.inFlight;
  }

  async load(): Promise<void> {
    try {
      this.current.set(await this.congregations.current());
    } catch {
      // A member who cannot read their congregation is either signed out or waiting on a
      // moderator. Both are handled by the screens themselves; a heading with no parish in it is
      // better than one naming the wrong parish.
      this.current.set(null);
    }
  }

  /** Forgets it, so the next member to sign in does not see the last one's parish. */
  clear(): void {
    this.current.set(null);
  }
}
