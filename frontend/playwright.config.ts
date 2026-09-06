import { defineConfig, devices } from '@playwright/test';

/**
 * The acceptance suite drives the real product: the Angular application, the ASP.NET API behind
 * a proxy, and a real database.
 *
 * The API runs on SQLite so the suite needs no container runtime, and in Development so the two
 * endpoints the suite depends on exist - the one that reads back a dispatched sign-in link, and
 * the one that puts the board back between specs. Neither is registered outside Development.
 *
 * Sign-in is passwordless, so there is no way to shortcut it: the suite asks for a link, reads
 * it out of the outbox, and follows it, exactly as a member does. That is deliberate. The
 * sign-in screens carry acceptance criteria of their own, and a suite that minted sessions
 * directly would leave them untested.
 */
const port = 4300;

export default defineConfig({
  testDir: './tests/e2e/specs',
  fullyParallel: false,
  workers: 1,
  forbidOnly: !!process.env['CI'],
  retries: process.env['CI'] ? 1 : 0,
  reporter: process.env['CI'] ? [['github'], ['list']] : [['list']],

  use: {
    baseURL: `http://localhost:${port}`,
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
  },

  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'], viewport: { width: 1280, height: 900 } },
    },
  ],

  webServer: [
    {
      command: 'dotnet run --project ../backend/src/Barnabas.Api --no-launch-profile',
      url: 'http://localhost:5003/health',
      reuseExistingServer: !process.env['CI'],
      timeout: 120_000,
      env: {
        ASPNETCORE_ENVIRONMENT: 'Development',
        ASPNETCORE_URLS: 'http://localhost:5003',
        BARNABAS_TEST_DB: 'sqlite',
        Database__ConnectionString: 'Data Source=barnabas-e2e.db',
        Database__ResetOnStart: 'true',
        Database__Seed: 'true',
        SignInLink__UrlTemplate: `http://localhost:${port}/sign-in/{token}`,
      },
    },
    {
      command: `npx ng serve --port ${port}`,
      url: `http://localhost:${port}`,
      reuseExistingServer: !process.env['CI'],
      timeout: 180_000,
    },
  ],
});
