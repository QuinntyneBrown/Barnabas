/**
 * Which of the four a listing is. The four are not interchangeable: Lend, Give and Sell
 * offer goods, Help offers time, and each collects the information its kind needs.
 */
export type ListingKind = 'Lend' | 'Give' | 'Sell' | 'Help';

/** Every kind, in the order the interface presents them. */
export const LISTING_KINDS: readonly ListingKind[] = ['Lend', 'Give', 'Sell', 'Help'];
