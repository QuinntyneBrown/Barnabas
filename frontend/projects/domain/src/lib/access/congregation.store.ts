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

  readonly congregation = this.current.asReadonly();

  /** The parish's name, or nothing at all rather than a guess at one. */
  readonly name = computed(() => this.current()?.name ?? '');

  readonly neighbourhoods = computed(() => this.current()?.neighbourhoods ?? []);

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
