import { FieldErrors } from './field-errors';

/**
 * Moves focus to the first control the server refused.
 *
 * Marking a field is not enough on its own: a member using a screen reader has no idea anything
 * changed unless focus goes there, and a member on a phone would have to scroll back up to find
 * out. L2-031 asks for the first invalid field to be focused, and this is that.
 */
export function focusFirstInvalid(errors: FieldErrors, order: readonly string[]): void {
  const first = errors.firstOf(order);

  if (!first) {
    return;
  }

  const control = document.getElementById(first);

  control?.focus();
  control?.scrollIntoView({ block: 'center', behavior: 'auto' });
}
