/**
 * The congregation a member belongs to.
 *
 * Read from the API rather than assumed, so no screen hard-codes a parish. `L2-004` requires the
 * heading to name the member's own congregation and never another's, which a literal in a
 * template cannot promise.
 */
export interface Congregation {
  readonly congregationId: string;
  readonly name: string;
  readonly slug: string;
  readonly neighbourhoods: readonly string[];
}
