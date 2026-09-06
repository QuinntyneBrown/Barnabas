/**
 * One of the five viewport widths the interface is required to be correct at.
 *
 * Extra small is below 576px, small from 576, medium from 768, large from 992, and
 * extra large from 1200. These are the bands `L2-108` names; they are not the same as
 * the breakpoints the stylesheet uses, and the two are deliberately kept apart.
 */
export type Band = 'xs' | 'sm' | 'md' | 'lg' | 'xl';

/** The lower bound of each band, in CSS pixels. */
export const BAND_MIN_WIDTH: Readonly<Record<Exclude<Band, 'xs'>, number>> = {
  sm: 576,
  md: 768,
  lg: 992,
  xl: 1200,
};

/** The width at which the header nav replaces the bottom bar. */
export const NAV_SWAP_WIDTH = 768;

export function bandFor(width: number): Band {
  if (width >= BAND_MIN_WIDTH.xl) return 'xl';
  if (width >= BAND_MIN_WIDTH.lg) return 'lg';
  if (width >= BAND_MIN_WIDTH.md) return 'md';
  if (width >= BAND_MIN_WIDTH.sm) return 'sm';
  return 'xs';
}
