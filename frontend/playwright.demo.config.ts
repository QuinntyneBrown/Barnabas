import { defineConfig, devices } from '@playwright/test';

/**
 * The demonstration recording.
 *
 * Separate from the acceptance configuration for three reasons. It records video, which the
 * ordinary suite has no reason to pay for. It runs deliberately slowly, because a recording nobody
 * can follow demonstrates nothing. And it must never gate a commit: it is a document, not a test,
 * even though it is a real browser driving the real product against a real database.
 *
 *     npm run demo
 *
 * The recording lands in `docs/demo/`. What it shows happened — there is no compositing, no
 * re-take and no mock: every screen in it was rendered by the application, and every rule it
 * demonstrates was enforced by the API.
 */
const port = 4300;

export default defineConfig({
  testDir: './tests/e2e/demo',
  fullyParallel: false,
  workers: 1,
  retries: 0,
  reporter: [['list']],

  // The walkthrough is minutes long by design; the ordinary thirty seconds is for a test.
  timeout: 12 * 60_000,

  outputDir: './test-results/demo',

  use: {
    baseURL: `http://localhost:${port}`,

    // 720p: large enough to read the product's own type, small enough that the file is something
    // somebody can be sent.
    viewport: { width: 1280, height: 720 },
    video: { mode: 'on', size: { width: 1280, height: 720 } },

    // A rhythm somebody can follow. Every click, every keystroke and every navigation is slowed to
    // roughly the pace a person works at, which is what makes the recording legible.
    launchOptions: { slowMo: 120 },
  },

  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'], viewport: { width: 1280, height: 720 } },
    },
  ],

  webServer: [
    {
      command: 'dotnet run --project ../backend/src/Barnabas.Api --no-launch-profile',
      url: 'http://localhost:5003/health',
      reuseExistingServer: true,
      timeout: 120_000,
      env: {
        ASPNETCORE_ENVIRONMENT: 'Development',
        ASPNETCORE_URLS: 'http://localhost:5003',
        Database__ConnectionString:
          process.env['BARNABAS_E2E_SQL']
          ?? String.raw`Server=.\SQLEXPRESS;Database=Barnabas_E2E;Trusted_Connection=True;TrustServerCertificate=True`,
        Database__ResetOnStart: 'true',
        Database__Seed: 'true',
        SignInLink__UrlTemplate: `http://localhost:${port}/sign-in/{token}`,
      },
    },
    {
      command: `npx ng serve --port ${port}`,
      url: `http://localhost:${port}`,
      reuseExistingServer: true,
      timeout: 180_000,
    },
  ],
});
