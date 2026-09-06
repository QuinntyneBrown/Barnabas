import { ListingKind } from '@barnabas/api';

/**
 * The words each listing kind is spoken about in.
 *
 * The four kinds are not interchangeable, and the difference shows most plainly in the verbs: a
 * loan is *returned*, a gift is *taken*, a sale is *sold*, an offer of time is *completed*. Using
 * one neutral wording for all four would lose exactly the distinction the product rests on.
 *
 * Gathered here rather than spelled out in each template so the board, the listing screen and the
 * request forms cannot drift into calling the same thing by different names. The server derives
 * the close-out outcome from the kind independently; this is the wording, not the rule.
 */
export interface ListingWords {
  /** The call to action a member sees on someone else's listing. */
  readonly ask: string;

  /**
   * The close-out action an owner sees on their own.
   *
   * The verb, not the resulting status. L2-037 is precise about the difference: a Give listing is
   * marked *taken* and becomes `Given away`; a Help offer is marked *booked* and becomes
   * `Completed`. Labelling the button with the status would be describing the result of the act
   * rather than the act itself.
   */
  readonly closeOut: string;

  /** The heading of the dialog confirming that close-out. */
  readonly closeOutHeading: string;

  /** How the board and the listing screen describe what is on offer. */
  readonly offer: string;

  /** The path segment of the request form for this kind. */
  readonly requestPath: string;

  /** How the board names this kind, on a placard and on its filter chip. */
  readonly board: string;
}

const WORDS: Readonly<Record<ListingKind, ListingWords>> = {
  Lend: {
    ask: 'Request to borrow',
    closeOut: 'Mark as taken',
    closeOutHeading: 'Mark this as gone?',
    offer: 'lends this',
    requestPath: 'lend',
    board: 'Lending',
  },
  Give: {
    ask: 'Request this',
    closeOut: 'Mark as taken',
    closeOutHeading: 'Mark this as taken?',
    offer: 'is giving this away',
    requestPath: 'give',
    board: 'Giving away',
  },
  Sell: {
    ask: 'Request to buy',
    closeOut: 'Mark as sold',
    closeOutHeading: 'Mark this as sold?',
    offer: 'is selling this',
    requestPath: 'sell',
    board: 'For sale',
  },
  Help: {
    ask: 'Request this help',
    closeOut: 'Mark as booked',
    closeOutHeading: 'Mark this as booked?',
    offer: 'offers this',
    requestPath: 'help',
    board: 'Offers of help',
  },
};

export function wordsFor(kind: ListingKind): ListingWords {
  return WORDS[kind] ?? WORDS.Lend;
}
