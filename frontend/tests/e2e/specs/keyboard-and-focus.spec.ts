import { ShellPage } from '../page-objects/shell.page';
import { YouPage } from '../page-objects/you.page';
import { Members } from '../support/members';
import { contrastRatio } from '../support/contrast';
import { expect, test } from '../support/barnabas';

// Acceptance Test
// Traces to: L2-111, L2-112
// Description: Every screen is operable by keyboard in a logical order and offers a skip link, an
// open dialog traps focus and gives it back, and the focus indicator is visible on each of the
// four field colours controls sit on.

const screens = ['/board', '/my-listings', '/inbox/requests', '/post/lend', '/you'];

test.beforeEach(async ({ signInAs }) => {
  await signInAs(Members.priya.emailAddress);
});

// L2-111 AC2: Given any screen, when the first tab is pressed, then a skip link to the main
// content is offered.
test('the first tab stop on every screen is the skip link', async ({ page }) => {
  const shell = new ShellPage(page);

  for (const screen of screens) {
    await page.goto(screen);

    // Wait for the screen to be there before typing at it. A member does not press Tab into a
    // page that has not painted, and doing so here would test the loading order rather than the
    // tab order.
    await expect(shell.main).toBeVisible();

    await page.keyboard.press('Tab');

    await expect(shell.skipLink, `${screen} did not offer a skip link first`).toBeFocused();
  }
});

// L2-111 AC1: Given any screen, when it is traversed by keyboard alone, then every interactive
// control is reachable and operable in a logical order.
test('a member can reach the board and post a listing by keyboard alone', async ({ page }) => {
  const shell = new ShellPage(page);

  await page.goto('/board');
  await expect(shell.main).toBeVisible();

  await page.keyboard.press('Tab');
  await expect(shell.skipLink).toBeFocused();

  // The skip link goes straight to the main landmark rather than through the navigation.
  await page.keyboard.press('Enter');
  await expect(page).toHaveURL(/#main$/);

  await page.goto('/board');
  await expect(shell.main).toBeVisible();

  // Then the brand, then the five destinations, in the order they are declared. Reading order.
  await page.keyboard.press('Tab');
  await page.keyboard.press('Tab');

  for (const label of ['Board', 'Search', 'Post', 'Inbox', 'You']) {
    await page.keyboard.press('Tab');

    await expect(shell.destination(label), `${label} was not the next tab stop`).toBeFocused();
  }

  // And the one the member stopped on is the one that opens.
  await shell.destination('Post').focus();
  await page.keyboard.press('Enter');

  await expect(page.getByRole('heading', { name: 'What are you posting?' })).toBeVisible();
});

// L2-111 AC3: Given an open dialog, when it is traversed by keyboard, then focus is trapped
// within it and Escape closes it, returning focus to the control that opened it.
test('a dialog traps focus, closes on escape, and gives focus back', async ({ page }) => {
  const you = new YouPage(page);

  await you.goto();
  await you.signOut.focus();
  await page.keyboard.press('Enter');

  await expect(you.confirmDialog).toBeVisible();

  // Tabbing round it never reaches a control outside it. A native modal dialog gives this for
  // nothing, which is most of why it is one.
  //
  // The cycle wraps through the document body between the last control and the first, which is
  // Chromium's own behaviour for a modal dialog and is not an escape: body is not something a
  // member can operate, and the next press is back inside. What would be an escape is focus
  // landing on the navigation or on the screen behind, and that is what this asserts.
  for (let press = 0; press < 8; press += 1) {
    await page.keyboard.press('Tab');

    const where = await page.evaluate(() => {
      const dialog = document.querySelector('dialog[open]');
      const active = document.activeElement;

      return {
        insideDialog: dialog !== null && dialog.contains(active),
        isWrapPoint: active === document.body || active === document.documentElement,
        modal: dialog instanceof HTMLDialogElement && dialog.matches(':modal'),
      };
    });

    expect(where.modal, 'the dialog was not opened modally').toBe(true);

    expect(
      where.insideDialog || where.isWrapPoint,
      'focus reached a control outside the open dialog',
    ).toBe(true);
  }

  await page.keyboard.press('Escape');
  await expect(you.confirmDialog).toBeHidden();

  // Back to the control that opened it - the one part the platform does not do on its own.
  await expect(you.signOut).toBeFocused();
});

// L2-112 AC1: Given a control on each of the four field colours, when it receives keyboard focus,
// then a focus indicator is visible with a contrast ratio of at least 3:1 against its field.
// L2-112 AC2: and the indicator is not suppressed.
//
// The ring is drawn from currentColor, so one rule covers indigo, white, stone, and cream. This
// checks that the rule actually resolves on each of them rather than trusting that it does.
test('the focus indicator is visible on every field colour', async ({ page }) => {
  const controls = [
    { screen: '/board', find: () => page.getByRole('link', { name: 'Barnabas' }) },
    { screen: '/board', find: () => page.getByRole('link', { name: 'Post a listing' }) },
    { screen: '/my-listings', find: () => page.getByRole('link', { name: 'You', exact: false }).last() },
    { screen: '/post/lend', find: () => page.getByLabel('Title') },
  ];

  for (const control of controls) {
    await page.goto(control.screen);

    const element = control.find();

    await element.focus();

    const drawn = await element.evaluate((node) => {
      const style = getComputedStyle(node);

      let field: Element | null = node;
      let background = 'rgb(255, 255, 255)';

      // The nearest ancestor that paints something is the field the control sits on.
      while (field) {
        const colour = getComputedStyle(field).backgroundColor;

        if (colour && colour !== 'rgba(0, 0, 0, 0)' && colour !== 'transparent') {
          background = colour;
          break;
        }

        field = field.parentElement;
      }

      return {
        outlineStyle: style.outlineStyle,
        outlineWidth: Number.parseFloat(style.outlineWidth),
        outlineColor: style.outlineColor,
        background,
      };
    });

    expect(drawn.outlineStyle, `${control.screen} suppressed its focus outline`).not.toBe('none');
    expect(drawn.outlineWidth, `${control.screen} drew no focus outline`).toBeGreaterThan(0);

    expect(
      contrastRatio(drawn.outlineColor, drawn.background),
      `focus ring on ${control.screen} against ${drawn.background}`,
    ).toBeGreaterThanOrEqual(3);
  }
});
