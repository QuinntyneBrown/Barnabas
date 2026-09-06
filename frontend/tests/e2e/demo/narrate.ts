import { Page } from '@playwright/test';

/**
 * The captions the demonstration is narrated with.
 *
 * A recording of a browser driving itself is not a demonstration — a viewer cannot tell an
 * intention from an accident, and half of what Barnabas does is a rule they cannot see. So each
 * scene says what is about to happen and why it matters, and the recording pauses long enough to
 * read it.
 *
 * The overlay is injected into the page rather than composited afterwards, because there is no
 * video editor in this toolchain and a caption that travels with the frame cannot drift out of
 * sync with it. It is `pointer-events: none` throughout, so nothing here can intercept a click
 * the demonstration is trying to make.
 *
 * Nothing in this file is used by the acceptance suites. It exists for `demo.spec.ts` alone.
 */

/** Roughly how long a reader needs, from the length of what they are being asked to read. */
function readingTime(words: number): number {
  // Captions here are short by design, so this is the pace of somebody skimming a line while
  // watching the screen behind it rather than reading prose. The floor gives a two-word caption a
  // moment to land; the ceiling stops one long caption dominating the recording.
  return Math.min(6_500, Math.max(1_500, Math.round((words / 230) * 60_000) + 700));
}

const OVERLAY_ID = 'barnabas-demo-caption';

/**
 * Puts a caption on the screen and waits for it to be read.
 *
 * Returns once the pause is over, so a scene reads as: say what is coming, then do it.
 */
export async function say(page: Page, title: string, detail: string): Promise<void> {
  await render(page, { title, detail, chapter: false });

  await page.waitForTimeout(readingTime(`${title} ${detail}`.split(/\s+/).length));
}

/**
 * A full-screen title card between chapters.
 *
 * The product's screens change abruptly when a different member signs in, and without a beat
 * between them a viewer reads the cut as a glitch rather than as a new scene.
 */
export async function chapter(page: Page, number: string, title: string, detail: string): Promise<void> {
  await render(page, { title, detail, chapter: true, number });

  await page.waitForTimeout(2_200);
}

/** Clears the caption, for the moments the screen should speak for itself. */
export async function quiet(page: Page): Promise<void> {
  await page.evaluate((id) => document.getElementById(id)?.remove(), OVERLAY_ID);
}

/**
 * Scrolls the part of the screen being talked about into view.
 *
 * Barnabas's mastheads are tall and the caption occupies the bottom of the frame, so on several
 * screens the thing being narrated starts below the fold — and a recording that describes a mosaic
 * the viewer can see only the top edge of is describing nothing.
 */
export async function reveal(page: Page, pixels = 340): Promise<void> {
  await page.evaluate((by) => window.scrollBy({ top: by, behavior: 'smooth' }), pixels);

  // Long enough for the scroll to finish, so the caption that follows lands on a still frame.
  await page.waitForTimeout(750);
}

/** A pause with the caption left standing, so an action can be watched after it is announced. */
export async function beat(page: Page, milliseconds = 1_200): Promise<void> {
  await page.waitForTimeout(milliseconds);
}

async function render(
  page: Page,
  caption: { title: string; detail: string; chapter: boolean; number?: string },
): Promise<void> {
  await page.evaluate(
    ({ id, text }) => {
      document.getElementById(id)?.remove();

      const overlay = document.createElement('div');

      overlay.id = id;
      overlay.setAttribute('aria-hidden', 'true');

      // Everything is inline so the overlay owes nothing to the application's stylesheet, and
      // cannot be restyled by the screen it happens to be sitting on.
      const shell = text.chapter
        ? `position:fixed;inset:0;z-index:2147483647;pointer-events:none;display:flex;
           align-items:center;justify-content:center;background:rgba(23,23,26,.94);
           font-family:'Archivo',system-ui,sans-serif;color:#f7f5f0;`
        : `position:fixed;left:0;right:0;bottom:0;z-index:2147483647;pointer-events:none;
           padding:20px 34px 24px;background:linear-gradient(transparent,rgba(23,23,26,.93) 26%);
           font-family:'Archivo',system-ui,sans-serif;color:#f7f5f0;`;

      overlay.setAttribute('style', shell.replace(/\s+/g, ' '));

      const body = text.chapter
        ? `<div style="max-width:56rem;padding:0 3rem;text-align:center">
             <div style="font-size:14px;letter-spacing:.32em;text-transform:uppercase;opacity:.55;
                         margin-bottom:20px">${text.number ?? ''}</div>
             <div style="font-size:52px;font-weight:700;line-height:1.1;margin-bottom:18px">
               ${text.title}
             </div>
             <div style="font-size:21px;line-height:1.5;opacity:.82;font-weight:400">
               ${text.detail}
             </div>
           </div>`
        : `<div style="max-width:64rem">
             <div style="font-size:12px;letter-spacing:.28em;text-transform:uppercase;opacity:.6;
                         margin-bottom:7px">Barnabas</div>
             <div style="font-size:27px;font-weight:700;line-height:1.2;margin-bottom:6px">
               ${text.title}
             </div>
             <div style="font-size:17px;line-height:1.45;opacity:.86">${text.detail}</div>
           </div>`;

      overlay.innerHTML = body;

      document.body.appendChild(overlay);
    },
    { id: OVERLAY_ID, text: caption },
  );
}
