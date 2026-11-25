import { defineConfig, devices } from '@playwright/test';

/**
 * Playwright E2E konfiguráció. A tesztek a futó rendszer ellen futnak (alapból a Docker Compose-os
 * gateway, http://localhost), nem indítanak saját szervert. A bejelentkezés token-injektálással történik
 * (lásd e2e/auth.ts), nem a Keycloak űrlapján át, így a tesztek gyorsak és stabilak.
 *
 * A cél cím a E2E_BASE_URL környezeti változóval írható felül (pl. CI-ban).
 */
const baseURL = process.env.E2E_BASE_URL ?? 'http://localhost';

export default defineConfig({
  testDir: './e2e',
  // A tesztek sorban fussanak: közös rendszerállapotot (demóadatok, kapcsolatok) módosítanak.
  fullyParallel: false,
  workers: 1,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 1 : 0,
  reporter: process.env.CI ? [['list'], ['html', { open: 'never' }]] : 'list',
  timeout: 30_000,
  expect: { timeout: 10_000 },
  use: {
    baseURL,
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
  },
  projects: [
    { name: 'chromium', use: { ...devices['Desktop Chrome'] } },
    // A firefox és a webkit (Safari-motor) projekt előkészítve; a CI-ban alapból a chromium fut,
    // a többi az E2E_ALL_BROWSERS beállításával vagy helyben kézzel indítható.
    ...(process.env.E2E_ALL_BROWSERS
      ? [
          { name: 'firefox', use: { ...devices['Desktop Firefox'] } },
          { name: 'webkit', use: { ...devices['Desktop Safari'] } },
        ]
      : []),
  ],
});
