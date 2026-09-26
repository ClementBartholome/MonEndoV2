import { test as setup, expect } from '@playwright/test';

const authFile = 'playwright/.auth/user.json';

// Compte de test d'un environnement local : identifiants fournis par variables
// d'environnement, jamais versionnés. Ne jamais lancer ces tests contre la production.
const email = process.env.E2E_EMAIL;
const password = process.env.E2E_PASSWORD;

setup('authenticate', async ({ page }) => {
    if (!email || !password) {
        throw new Error('Définir E2E_EMAIL et E2E_PASSWORD pour lancer les tests E2E.');
    }

    await page.goto('/login');
    await page.fill('input[placeholder="mail@gmail.com"]', email);
    await page.fill('input[placeholder="********"]', password);
    await page.click('button:has-text("Connexion")');
    await expect(page).toHaveURL('/', { timeout: 10000 });

    await page.context().storageState({ path: authFile });
});
