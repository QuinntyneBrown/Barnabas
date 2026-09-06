/**
 * The drawings the interface has.
 *
 * It lives here, beside the component that renders it, rather than with the destinations that
 * ask for one. A destination is a Barnabas idea and belongs with the pages; the set of shapes an
 * icon can be is not, and this library may not reach for anything of ours.
 */
export type NavIcon = 'board' | 'search' | 'post' | 'inbox' | 'person';
