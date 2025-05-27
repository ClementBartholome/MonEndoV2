import { test, expect } from '@playwright/test';

test.describe('CRUD for ActivitePage', () => {
    test.beforeEach(async ({ page }) => {
        await page.goto('https://monendoapp.fr/login');
        await page.fill('input[placeholder="mail@gmail.com"]', 'testuser@gmail.com');
        await page.fill('input[placeholder="********"]', 'Password123$');
        await page.click('button:has-text("Connexion")');
        await expect(page).toHaveURL('https://monendoapp.fr/', { timeout: 10000 });
    });

    test('should create a new entry', async ({ page }) => {
        await page.goto('https://monendoapp.fr/activite');
        await page.click('text=Ajouter une session');
        await page.fill('input[placeholder="Course à pied"]', 'Test playwright');
        const today = new Date().toISOString().split('T')[0];
        await page.fill('input[type="date"]', today);
        await page.fill('input[type="time"]', '10:00');
        await page.fill('input[placeholder="Durée en minutes"]', '30');
        await page.click('text=Enregistrer');
        await expect(page.locator('text=Test playwright')).toBeVisible();
        console.log('New entry created:', 'Test playwright');
    });

    test('should read an entry', async ({ page }) => {
        await page.goto('https://monendoapp.fr/activite');
        const entry = page.locator('text=Test playwright').first();
        await expect(entry).toBeVisible();
        console.log('Entry found:', 'Test playwright');
    });

    test('should delete an entry', async ({ page }) => {
        await page.goto('https://monendoapp.fr/activite');
        const entry = page.locator('text=Test playwright').first();
        await entry.scrollIntoViewIfNeeded();
        await entry.click({ force: true });
        await page.click('span.material-symbols-outlined.delete-btn');
        await page.waitForSelector('text=La session a été supprimée avec succès', { timeout: 10000 });
        await expect(entry).not.toBeVisible({ timeout: 10000 });
        console.log('Entry deleted:', 'Test playwright');
    });
});