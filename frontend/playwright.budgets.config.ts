import { defineConfig, devices } from '@playwright/test';

/**
 * The budget suite, run deliberately and against a production build.
 *
 * Separate from `playwright.config.ts` for two reasons, and both matter.
 *
 * It runs the **production** configuration of the client. `L2-104` asks about what a member on a
 * phone waits for, and a development server serves a two-megabyte unminified bundle with no
 * tree-shaking — measuring that would fail a budget the deployment meets several times over, and
 * would be measuring the build tool rather than the product.
 *
 * And it does not gate the ordinary run. These are wall-clock assertions: they can fail on a
 * developer machine because a build was running in another window or a laptop was on battery,
 * and a suite that goes red for those reasons teaches people to ignore it.
 *
 *     npx ng build
 *     npx playwright test --config playwright.budgets.config.ts
 *
 * The build is not optional: this serves `dist/barnabas/browser`, so a stale one measures a stale
 * product.
 *
 * Every test reports the number it measured, so a run that passes still says by how much.
 */
const port = 4302;

export default defineConfig({
  testDir: './tests/e2e/specs',
  testMatch: /budgets\.spec\.ts/,
  fullyParallel: false,
  workers: 1,
  reporter: [['list']],

  // A paint measured on a throttled connection takes longer than the ordinary default allows.
  timeout: 120_000,

  use: {
    baseURL: `http://localhost:${port}`,
    trace: 'retain-on-failure',
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
      reuseExistingServer: true,
      timeout: 120_000,
      env: {
        ASPNETCORE_ENVIRONMENT: 'Development',
        ASPNETCORE_URLS: 'http://localhost:5003',
        Database__ConnectionString: String.raw`Server=.\SQLEXPRESS;Database=Barnabas_E2E;Trusted_Connection=True;TrustServerCertificate=True`,
        Database__ResetOnStart: 'true',
        Database__Seed: 'true',
        SignInLink__UrlTemplate: `http://localhost:${port}/sign-in/{token}`,
      },
    },
    {
      // The built files behind something that gzips them, which is what a deployment does and
      // what the development server does not. That difference - 483 KB against 109 KB - is most
      // of the paint budget, so measuring the dev server would be measuring the build tool.
      command: `node tests/e2e/support/serve-production.mjs dist/barnabas/browser ${port} 5003`,
      url: `http://localhost:${port}`,
      reuseExistingServer: !process.env['CI'],
      timeout: 60_000,
    },
  ],
});
