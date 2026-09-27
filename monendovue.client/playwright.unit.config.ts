import { defineConfig } from '@playwright/test';

/**
 * Tests unitaires du client : fonctions pures (utils/, config/), sans navigateur, sans serveur et sans compte.
 * Fichiers : src/**\/__tests__/*.spec.ts. Lancement : `npm run test:unit` (aussi exécuté par la CI, bloquant).
 *
 * Limite : le code est chargé par Node, pas par Vite. Un module qui utilise `import.meta.env`, importe un fichier `.vue`
 * ou `apiService` ne se charge pas ici : il se teste par les E2E (playwright.config.ts).
 */
export default defineConfig({
  testDir: './src',
  testMatch: '**/__tests__/**/*.spec.ts',
  // Résout l'alias `@/` (paths de ce tsconfig) dans les tests comme dans le code testé.
  tsconfig: './tsconfig.unit.json',
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  reporter: 'list',
});
