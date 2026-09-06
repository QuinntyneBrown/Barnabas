/**
 * What a member fills in to ask for help.
 *
 * The chosen window rather than a time of their own: help is offered when the owner is actually
 * free, so the form offers only the windows the listing declared.
 */
export interface MakeHelpRequest {
  readonly message: string;
  readonly availabilityWindowId: string | null;
}
