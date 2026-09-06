/**
 * Where a listing stands. Only `Active` appears on the board.
 *
 * The four closed-out statuses are not one neutral status: a member looking at last
 * spring's listings should be able to see which things were sold and which were given.
 */
export type ListingStatus = 'Active' | 'Archived' | 'Sold' | 'GivenAway' | 'Completed';
