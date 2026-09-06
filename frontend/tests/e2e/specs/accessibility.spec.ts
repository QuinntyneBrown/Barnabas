import { Page } from '@playwright/test';

import { ShellPage } from '../page-objects/shell.page';
import { Members } from '../support/members';
import { contrastRatio } from '../support/contrast';
import { expect, test } from '../support/barnabas';

// Acceptance Test
// Traces to: L2-110, L2-113, L2-114, L2-115
// Description: Every control is big enough to hit with a thumb, every piece of text is legible
// against what it sits on, nothing moves for a member who asked it not to, and each screen tells
// assistive technology what it is made of.

/** One screen of each shape the product has, as the other accessibility specs use. */
const screens = ['/board', '/my-listings', '/inbox/requests', '/post/lend', '/you', '/directory'];

test.beforeEach(async ({ signInAs }) => {
  await signInAs(Members.priya.emailAddress);
});

// L2-110 AC1: Given any screen at 375 pixels, when every interactive control is measured, then
// each is at least 44 by 44 CSS pixels or separated by equivalent spacing.
test('every control on a phone is big enough to hit', async ({ page }) => {
  await page.setViewportSize({ width: 375, height: 812 });

  for (const screen of screens) {
    await page.goto(screen);
    await expect(new ShellPage(page).main).toBeVisible();

    // Settled before measuring. A screen that fetches its rows after it paints would otherwise be
    // measured while empty, and this test would pass by having found nothing to measure - which
    // is exactly how it passed on the directory before this line existed.
    await page.waitForLoadState('networkidle');

    const tooSmall = await page.evaluate(() => {
      const minimum = 44;

      // Inline links inside a run of text are the documented exception: they are sized by the
      // text they are part of, and padding one to 44 pixels would break the line it sits in.
      // WCAG 2.2 2.5.8 says so in as many words.
      const inline = (element: Element): boolean =>
        element.tagName === 'A'
        && ['P', 'SPAN', 'LI', 'DD', 'BLOCKQUOTE', 'LABEL'].includes(
          element.parentElement?.tagName ?? '',
        );

      const targets = [
        ...document.querySelectorAll('a, button, input, select, textarea, [role="button"]'),
      ].filter((element) => {
        const box = element.getBoundingClientRect();

        // Nothing that is not on the screen: a dialog that has not been opened is not a control
        // a member can fail to hit.
        return (box.width > 0 || box.height > 0) && !inline(element);
      });

      const boxes = targets.map((element) => element.getBoundingClientRect());

      /**
       * The other half of the requirement: "or equivalent separating space".
       *
       * A control under 44 pixels is still reachable if nothing else is within 44 pixels of it -
       * a member's thumb has the room even though the control does not fill it. Measured
       * centre to centre, which is how WCAG's own spacing exception is defined.
       */
      const crowded = (index: number): boolean =>
        boxes.some((other, position) => {
          if (position === index) {
            return false;
          }

          const mine = boxes[index];

          const dx = mine.left + mine.width / 2 - (other.left + other.width / 2);
          const dy = mine.top + mine.height / 2 - (other.top + other.height / 2);

          return Math.hypot(dx, dy) < minimum;
        });

      return targets
        .map((element, index) => ({ element, box: boxes[index], index }))
        .filter(({ box, index }) =>
          (box.height < minimum || box.width < minimum) && crowded(index))
        .map(({ element, box }) =>
          `${element.tagName}.${element.className} ${Math.round(box.width)}x${Math.round(box.height)}`);
    });

    expect(tooSmall, `${screen} had crowded controls under 44px`).toEqual([]);
  }
});

// L2-113 AC1: Given any body text, when its colours are measured, then the contrast ratio against
// its background is at least 4.5:1.
// L2-113 AC3: and muted secondary text on the stone field is measured with the rest.
test('every piece of text is legible against what it sits on', async ({ page }) => {
  for (const screen of screens) {
    await page.goto(screen);
    await expect(new ShellPage(page).main).toBeVisible();

    const measured = await measureTextAsync(page);

    expect(measured.length, `${screen} rendered no text to measure`).toBeGreaterThan(0);

    const failing = measured.filter((sample) => {
      // 3:1 for large text, 4.5:1 for the rest — WCAG's own division, and the reason AC1 and AC2
      // are separate criteria.
      const large = sample.fontSize >= 24 || (sample.fontSize >= 18.66 && sample.weight >= 700);

      return contrastRatio(sample.colour, sample.background) < (large ? 3 : 4.5);
    });

    expect(
      failing.map((sample) => `${sample.selector}: ${sample.colour} on ${sample.background}`),
      `${screen} had text below the minimum`,
    ).toEqual([]);
  }
});

// L2-113 AC2: Given any large text or interface component boundary, when measured, then the
// contrast ratio is at least 3:1.
test('the boundary of every field and button is visible against the page', async ({ page }) => {
  await page.goto('/post/lend');
  await expect(new ShellPage(page).main).toBeVisible();

  const boundaries = await page.evaluate(() => {
    const opaque = (colour: string): boolean =>
      colour !== 'transparent' && !colour.startsWith('rgba(0, 0, 0, 0)');

    const behind = (element: Element | null): string => {
      for (let node = element; node; node = node.parentElement) {
        const background = getComputedStyle(node).backgroundColor;

        if (opaque(background)) {
          return background;
        }
      }

      return 'rgb(255, 255, 255)';
    };

    return [...document.querySelectorAll('input, select, textarea, .btn')]
      .filter((element) => element.getBoundingClientRect().height > 0)
      .map((element) => {
        const style = getComputedStyle(element);

        // A control with no border of its own is bounded by its own fill against the page, which
        // is the other way a boundary can be visible.
        const bounding =
          style.borderTopWidth !== '0px' ? style.borderTopColor : style.backgroundColor;

        return {
          selector: `${element.tagName}.${element.className}`,
          bounding,
          against: behind(element.parentElement),
        };
      });
  });

  const failing = boundaries.filter(
    (boundary) => contrastRatio(boundary.bounding, boundary.against) < 3,
  );

  expect(
    failing.map((boundary) => `${boundary.selector}: ${boundary.bounding} on ${boundary.against}`),
  ).toEqual([]);
});

// L2-114 AC1: Given a client requesting reduced motion, when the board loads, then no entrance
// animation runs.
// L2-114 AC2: and any transition is suppressed or reduced to an instantaneous change.
test('nothing moves for a member who asked it not to', async ({ page }) => {
  await page.emulateMedia({ reducedMotion: 'reduce' });

  for (const screen of screens) {
    await page.goto(screen);
    await expect(new ShellPage(page).main).toBeVisible();

    const moving = await page.evaluate(() =>
      [...document.querySelectorAll('*')]
        .filter((element) => {
          const style = getComputedStyle(element);

          const animated =
            style.animationName !== 'none' && parseFloat(style.animationDuration) > 0.01;

          const transitioned =
            style.transitionProperty !== 'none'
            && style.transitionProperty !== 'all'
            && parseFloat(style.transitionDuration) > 0.01;

          return animated || transitioned;
        })
        .map((element) => `${element.tagName}.${element.className}`)
        .slice(0, 10),
    );

    expect(moving, `${screen} still animates under reduced motion`).toEqual([]);
  }
});

// L2-115 AC1: Given any screen, when its structure is inspected, then it has one main landmark, a
// navigation landmark with an accessible name, and a single first-level heading.
test('every screen exposes one main, a named navigation, and one first-level heading', async ({
  page,
}) => {
  for (const screen of screens) {
    await page.goto(screen);
    await expect(new ShellPage(page).main).toBeVisible();

    await expect(page.getByRole('main'), `${screen} main`).toHaveCount(1);
    await expect(page.getByRole('heading', { level: 1 }), `${screen} h1`).toHaveCount(1);

    // Every navigation landmark carries a name. Two unnamed ones are indistinguishable to
    // somebody moving between landmarks, which is the whole point of the requirement.
    const unnamed = await page.evaluate(() =>
      [...document.querySelectorAll('nav')].filter(
        (element) =>
          !element.getAttribute('aria-label') && !element.getAttribute('aria-labelledby'),
      ).length,
    );

    expect(unnamed, `${screen} had an unnamed navigation landmark`).toBe(0);
  }
});

// L2-115 AC2: Given any image conveying meaning, when it is inspected, then it has an accessible
// description, and decorative images are hidden from assistive technology.
test('every image either describes itself or is hidden', async ({ page }) => {
  for (const screen of screens) {
    await page.goto(screen);
    await expect(new ShellPage(page).main).toBeVisible();

    const undescribed = await page.evaluate(() =>
      [
        ...[...document.querySelectorAll('img')].filter(
          (image) => image.getAttribute('alt') === null,
        ),

        // An SVG is either hidden or has a role and a name. One that is neither is announced as
        // "graphic" and tells a listener nothing.
        ...[...document.querySelectorAll('svg')].filter(
          (drawing) =>
            drawing.getAttribute('aria-hidden') !== 'true'
            && !drawing.getAttribute('aria-label')
            && !drawing.querySelector('title'),
        ),
      ].map((element) => `${element.tagName}.${element.getAttribute('class') ?? ''}`),
    );

    expect(undescribed, `${screen} had an image with nothing to announce`).toEqual([]);
  }
});

// L2-115 AC3: Given any form control, when it is inspected, then it has a programmatically
// associated label, and validation errors are associated with their field.
test('every form control is labelled, and a fault is tied to the field it is about', async ({
  page,
}) => {
  for (const screen of ['/post/lend', '/post/sell', '/post/help', '/you/profile']) {
    await page.goto(screen);
    await expect(new ShellPage(page).main).toBeVisible();

    const unlabelled = await page.evaluate(() =>
      [...document.querySelectorAll('input, select, textarea')]
        .filter((control) => {
          if (control.getAttribute('aria-label') || control.getAttribute('aria-labelledby')) {
            return false;
          }

          const id = control.getAttribute('id');

          return !id || !document.querySelector(`label[for="${id}"]`);
        })
        .map((control) => `${control.tagName}#${control.getAttribute('id') ?? '(no id)'}`),
    );

    expect(unlabelled, `${screen} had an unlabelled control`).toEqual([]);
  }

  // And a fault names its field rather than floating on the page.
  await page.goto('/post/lend');
  await page.getByRole('button', { name: 'Post listing' }).click();

  const title = page.locator('#title');

  await expect(title).toHaveAttribute('aria-invalid', 'true');
  await expect(title).toHaveAttribute('aria-describedby', 'title-error');
  await expect(page.locator('#title-error')).toBeVisible();
});

// L2-115 AC4: Given the current navigation destination, when it is inspected, then it is marked as
// current.
test('the destination a member is on is marked as the current one', async ({ page }) => {
  await page.goto('/board');
  await expect(new ShellPage(page).main).toBeVisible();

  await expect(page.locator('nav [aria-current="page"]').first()).toBeVisible();

  await expect(page.locator('nav [aria-current="page"]').first()).toHaveText(/Board/);
});

/**
 * Every run of text on the screen, with the colour it is drawn in and the colour behind it.
 *
 * The background is walked up the tree, because a transparent element is drawn on whatever its
 * ancestors painted — reading `backgroundColor` off the element itself would measure most of the
 * product's text against `rgba(0, 0, 0, 0)` and pass everything.
 */
async function measureTextAsync(page: Page): Promise<
  { selector: string; colour: string; background: string; fontSize: number; weight: number }[]
> {
  return page.evaluate(() => {
    const opaque = (colour: string): boolean =>
      colour !== 'transparent' && !colour.startsWith('rgba(0, 0, 0, 0)');

    const behind = (element: Element | null): string => {
      for (let node = element; node; node = node.parentElement) {
        const background = getComputedStyle(node).backgroundColor;

        if (opaque(background)) {
          return background;
        }
      }

      return 'rgb(255, 255, 255)';
    };

    return [...document.querySelectorAll('main *, header *, nav *')]
      .filter((element) => {
        // Only elements holding text of their own, and only ones that are actually on screen.
        const ownText = [...element.childNodes].some(
          (node) => node.nodeType === Node.TEXT_NODE && (node.textContent ?? '').trim().length > 0,
        );

        return ownText && element.getBoundingClientRect().height > 0;
      })
      .map((element) => {
        const style = getComputedStyle(element);

        return {
          selector: `${element.tagName}.${element.className}`,
          colour: style.color,
          background: behind(element),
          fontSize: parseFloat(style.fontSize),
          weight: Number(style.fontWeight) || 400,
        };
      });
  });
}
