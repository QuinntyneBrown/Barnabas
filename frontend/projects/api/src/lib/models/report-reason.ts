/**
 * Why somebody reported a listing.
 *
 * Four, matching the four the report dialogue offers. A reason nobody can choose is a value the
 * queue would have to render and never would.
 */
export type ReportReason = 'NotAllowed' | 'AlreadyGone' | 'Misleading' | 'Other';

/** What a member is asked, in the words they would use. */
export const REPORT_REASONS: readonly { readonly reason: ReportReason; readonly words: string }[] = [
  { reason: 'NotAllowed', words: 'It does not belong on the board' },
  { reason: 'AlreadyGone', words: 'It is already gone' },
  { reason: 'Misleading', words: 'The description is not accurate' },
  { reason: 'Other', words: 'Something else' },
];
