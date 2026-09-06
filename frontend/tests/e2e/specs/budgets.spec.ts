import { gzipSync } from 'node:zlib';
import { readFile, readdir } from 'node:fs/promises';
import { join } from 'node:path';

import { BoardPage } from '../page-objects/board.page';
import { Members } from '../support/members';
import { expect, test } from '../support/barnabas';

// Acceptance Test
// Traces to: L2-104
// Description: The board paints quickly on a slow connection, does not shove itself about as it
// arrives, and does not ask a phone to download more script than it needs.
//
// Excluded from the ordinary run and run against a *production* build, for two reasons that both
// matter. These are wall-clock assertions, which can fail on a developer machine because a build
// was running in another window; and a development server serves a two-megabyte unminified bundle,
// so measuring it would fail a budget the deployment meets several times over. Run them
// deliberately:
//
//     npm run build
//     npx playwright test --config playwright.budgets.config.ts
//
// Every one reports the number it measured, so a run that passes still says by how much.

/** What L2-104 asks for, in one place, so a failure can say what it was measured against. */
const budgets = {
  largestContentfulPaintMs: 2_500,
  cumulativeLayoutShift: 0.1,
  compressedJavaScriptBytes: 300 * 1024,
};

test.describe('@budget page weight and paint', () => {
  // These load a page under a throttled connection and wait for it to settle, which the default
  // thirty seconds does not allow for.
  test.slow();

  // L2-104 AC1: Given the board on a simulated 4G connection, when it loads, then Largest
  // Contentful Paint occurs within 2.5 seconds.
  test('the board paints within budget on a simulated 4G connection', async ({ page, signInAs }) => {
    await signInAs(Members.priya.emailAddress);

    // Regular 4G, as Chrome's own device emulation defines it: 9 Mbps down, 170 ms of latency.
    const session = await page.context().newCDPSession(page);

    await session.send('Network.enable');
    await session.send('Network.emulateNetworkConditions', {
      offline: false,
      latency: 170,
      downloadThroughput: (9 * 1024 * 1024) / 8,
      uploadThroughput: (9 * 1024 * 1024) / 8,
    });

    await page.goto('/board');

    await new BoardPage(page).waitForPlacards();

    const paint = await page.evaluate(
      () =>
        new Promise<number>((resolve) => {
          // The entries already buffered, plus anything that lands while we wait. LCP is only
          // final when the page stops changing, so this takes the last one it sees.
          let latest = 0;

          const observer = new PerformanceObserver((list) => {
            for (const entry of list.getEntries()) {
              latest = Math.max(latest, entry.startTime);
            }
          });

          observer.observe({ type: 'largest-contentful-paint', buffered: true });

          setTimeout(() => {
            observer.disconnect();
            resolve(latest);
          }, 1_500);
        }),
    );

    console.log(`  measured: largest contentful paint ${Math.round(paint)} ms`);

    expect(paint).toBeGreaterThan(0);
    expect(paint).toBeLessThan(budgets.largestContentfulPaintMs);
  });

  // L2-104 AC2: Given the board, when it loads, then Cumulative Layout Shift is below 0.1.
  test('the board does not shove itself about as it arrives', async ({ page, signInAs }) => {
    await signInAs(Members.priya.emailAddress);

    await page.goto('/board');

    await new BoardPage(page).waitForPlacards();

    const shift = await page.evaluate(
      () =>
        new Promise<number>((resolve) => {
          let total = 0;

          const observer = new PerformanceObserver((list) => {
            for (const entry of list.getEntries() as (PerformanceEntry & {
              value: number;
              hadRecentInput: boolean;
            })[]) {
              // A shift the member caused by tapping something is not a shift they mind. The
              // metric excludes it, and so does this.
              if (!entry.hadRecentInput) {
                total += entry.value;
              }
            }
          });

          observer.observe({ type: 'layout-shift', buffered: true });

          setTimeout(() => {
            observer.disconnect();
            resolve(total);
          }, 1_500);
        }),
    );

    console.log(`  measured: cumulative layout shift ${shift.toFixed(4)}`);

    expect(shift).toBeLessThan(budgets.cumulativeLayoutShift);
  });

  // L2-104 AC3: Given any screen, when its initial transfer is measured, then compressed
  // JavaScript is under 300 KB.
  test('a screen does not ask a phone for more script than it needs', async () => {
    // Measured from the built artefact rather than over the wire, and deliberately.
    //
    // Compression is a property of whatever serves the files - a CDN, a reverse proxy, Kestrel's
    // own response compression - and no development server does it. Reading `content-length` off
    // a dev server would measure the uncompressed bundle and fail a budget the deployment meets
    // comfortably; gzipping the artefact measures the bytes a phone on a train actually waits for.
    const chunks = await javascriptChunksAsync();

    expect(chunks.length, 'no built javascript was found - run `npm run build` first').toBeGreaterThan(0);

    const compressed = chunks.reduce((total, chunk) => total + chunk.gzipped, 0);

    for (const chunk of chunks) {
      console.log(
        `  measured: ${chunk.name} ${Math.round(chunk.raw / 1024)} KB raw, ` +
          `${Math.round(chunk.gzipped / 1024)} KB gzipped`,
      );
    }

    console.log(`  measured: compressed javascript ${Math.round(compressed / 1024)} KB in total`);

    expect(compressed).toBeLessThan(budgets.compressedJavaScriptBytes);
  });
});

/**
 * Every JavaScript chunk the production build emitted, raw and gzipped.
 *
 * Level 9, because that is what a CDN serves a static asset at - it compresses once and caches
 * the result, so there is no reason for it to hurry.
 */
async function javascriptChunksAsync(): Promise<
  { name: string; raw: number; gzipped: number }[]
> {
  const root = join(process.cwd(), 'dist', 'barnabas', 'browser');

  let names: string[];

  try {
    names = (await readdir(root)).filter((name) => name.endsWith('.js'));
  } catch {
    return [];
  }

  return Promise.all(
    names.map(async (name) => {
      const bytes = await readFile(join(root, name));

      return { name, raw: bytes.length, gzipped: gzipSync(bytes, { level: 9 }).length };
    }),
  );
}
