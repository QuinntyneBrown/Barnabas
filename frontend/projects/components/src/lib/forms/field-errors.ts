import { ApiError } from '@barnabas/api';

/**
 * What a form knows about what the server refused.
 *
 * The mocks carry no error state, so this is the pattern the screens adopt: the failing controls
 * are marked with `aria-invalid`, each carries a message associated to it by `aria-describedby`,
 * and focus moves to the first one. L2-031 asks for exactly that, and a member who cannot see
 * the marking needs the association rather than the colour.
 */
export class FieldErrors {
  private constructor(private readonly errors: Readonly<Record<string, string>>) {}

  static none(): FieldErrors {
    return new FieldErrors({});
  }

  static from(failure: unknown): FieldErrors {
    return failure instanceof ApiError ? new FieldErrors(failure.fieldErrors) : FieldErrors.none();
  }

  get any(): boolean {
    return Object.keys(this.errors).length > 0;
  }

  /** The first field at fault, in the order the form declares its controls. */
  firstOf(order: readonly string[]): string | null {
    return order.find((field) => field in this.errors) ?? null;
  }

  messageFor(field: string): string | null {
    return this.errors[field] ?? null;
  }

  has(field: string): boolean {
    return field in this.errors;
  }
}
