import { defineConfig, devices } from '@playwright/test';

/**
 * Tests E2E des parcours de l'interface, avec une API simulée (tests/support/faux-serveur.ts) : ni serveur .NET, ni base,
 * ni compte. Le serveur et ses règles sont couverts par les tests xUnit (voir docs/tests.md).
 *
 * L'application tourne sur un serveur Vite dédié (port 5174) avec VITE_DOCKER=true : ses appels API sont relatifs et
 * interceptés par le faux serveur. Lancement : `npm run test:e2e` ; en CI, job `e2e` sur les PR vers main.
 */
const PORT = 5174;

export default defineConfig({
  testDir: './tests',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 1 : 0,
  workers: process.env.CI ? 2 : undefined,
  reporter: process.env.CI ? [['list'], ['html', { open: 'never' }]] : [['list']],
  use: {
    baseURL: `https://localhost:${PORT}`,
    ignoreHTTPSErrors: true,
    locale: 'fr-FR',
    timezoneId: 'Europe/Paris',
    trace: 'retain-on-failure',
  },
  // Chromium seulement, en deux formats : l'application est pensée mobile d'abord (375px), le desktop l'enrichit.
  projects: [
    {
      name: 'mobile',
      use: { ...devices['Pixel 5'], viewport: { width: 375, height: 812 } },
    },
    {
      name: 'desktop',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
  webServer: {
    command: `npx vite --port ${PORT} --strictPort`,
    url: `https://localhost:${PORT}`,
    ignoreHTTPSErrors: true,
    reuseExistingServer: !process.env.CI,
    env: { VITE_DOCKER: 'true' },
  },
});
