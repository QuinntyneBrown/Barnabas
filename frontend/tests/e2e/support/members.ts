/**
 * The members the API seeds, named so a spec can say who is doing something.
 *
 * These mirror the seed in the backend. Feature slice 1 seeds its congregation rather than
 * provisioning one through the product: two real identities are what make authorisation and
 * tenant scoping mean anything, and the administrative surface that creates them is a later
 * slice.
 */
export const Members = {
  /** Owns the ladder. Every ownership assertion is written from her side. */
  marion: { emailAddress: 'marion@example.com', displayName: 'Marion T.', neighbourhood: 'Riverdale' },

  /** Asks to borrow it. */
  priya: { emailAddress: 'priya@example.com', displayName: 'Priya K.', neighbourhood: 'Leslieville' },

  /** Neither owner nor requester, which is what makes her useful. */
  grace: { emailAddress: 'grace@example.com', displayName: 'Grace Papadopoulos', neighbourhood: 'The Danforth' },
} as const;

export const SeededListings = {
  ladder: '6ft aluminum step ladder',
  drill: 'DeWalt cordless drill, two batteries',
} as const;
