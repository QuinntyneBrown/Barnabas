import { defineConfig, devices } from '@playwright/test';

/**
 * The acceptance suite drives the real product: the Angular application, the ASP.NET API behind
 * a proxy, and a real database.
 *
 * The API runs against SQL Express on a database of its own, dropped and recreated on start, and
 * in Development so the two endpoints the suite depends on exist - the one that reads back a
 * dispatched sign-in link, and the one that puts the board back between specs. Neither is
 * registered outside Development.
 *
 * Sign-in is passwordless, so there is no way to shortcut it: the suite asks for a link, reads
 * it out of the outbox, and follows it, exactly as a member does. That is deliberate. The
 * sign-in screens carry acceptance criteria of their own, and a suite that minted sessions
 * directly would leave them untested.
 */
const port = 4300;

export default defineConfig({
  testDir: './tests/e2e/specs',

  /**
   * The budget suite does not gate the ordinary run.
   *
   * L2-104 asserts wall-clock paint budgets, which can fail on a developer machine for reasons
   * that have nothing to do with correctness - a build running in another window, a laptop on
   * battery. Excluded here and run deliberately with `--grep @budget`, which reports the measured
   * numbers rather than only pass or fail.
   */
  grepInvert: process.env['BARNABAS_BUDGETS'] ? undefined : /@budget/,
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
        // String.raw, because a lone backslash in a normal string literal is an unknown
        // escape and JavaScript drops it - which points the API at a server called
        // ".SQLEXPRESS" and fails with a network error that names no cause.
        Database__ConnectionString: String.raw`Server=.\SQLEXPRESS;Database=Barnabas_E2E;Trusted_Connection=True;TrustServerCertificate=True`,
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
