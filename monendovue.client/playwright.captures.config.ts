import { defineConfig, devices } from '@playwright/test';
import base from './playwright.config';

/**
 * Captures d'écran du README (docs/images/) : mêmes parcours simulés que les tests E2E, données génériques.
 * Lancement : `npm run captures` (pas exécuté par la CI). Voir tests/captures/readme.capture.ts.
 */
export default defineConfig({
  ...base,
  testDir: './tests/captures',
  testMatch: '**/*.capture.ts',
  fullyParallel: false,
  workers: 1,
  retries: 0,
  projects: [
    { name: 'mobile', use: { ...devices['Pixel 5'], viewport: { width: 375, height: 1000 }, deviceScaleFactor: 2 } },
    { name: 'desktop', use: { ...devices['Desktop Chrome'], viewport: { width: 1280, height: 900 }, deviceScaleFactor: 1.5 } },
  ],
});
