/**
 * WCAG relative-luminance contrast, computed from what the browser actually painted.
 *
 * L2-112 asks for a focus indicator at 3:1 against its field, and that is a number rather than a
 * feeling - so the suite reads the two colours off the element and works it out, instead of
 * asserting that some outline or other exists.
 */
export function contrastRatio(foreground: string, background: string): number {
  const first = relativeLuminance(parse(foreground));
  const second = relativeLuminance(parse(background));

  const lighter = Math.max(first, second);
  const darker = Math.min(first, second);

  return (lighter + 0.05) / (darker + 0.05);
}

function parse(colour: string): [number, number, number] {
  const parts = colour.match(/[\d.]+/g);

  if (!parts || parts.length < 3) {
    throw new Error(`Could not read a colour from "${colour}".`);
  }

  return [Number(parts[0]), Number(parts[1]), Number(parts[2])];
}

function relativeLuminance([red, green, blue]: [number, number, number]): number {
  const [r, g, b] = [red, green, blue].map((channel) => {
    const value = channel / 255;

    return value <= 0.03928 ? value / 12.92 : ((value + 0.055) / 1.055) ** 2.4;
  });

  return 0.2126 * r + 0.7152 * g + 0.0722 * b;
}
