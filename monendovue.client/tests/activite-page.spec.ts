import { test, expect } from '@playwright/test';

// La session est ouverte par tests/auth.setup.ts (storageState).
test.describe('CRUD for ActivitePage', () => {
    test('should create a new entry', async ({ page }) => {
        await page.goto('/activite');
        await page.click('text=Ajouter une session');
        await page.fill('input[placeholder="Course à pied"]', 'Test playwright');
        const today = new Date().toISOString().split('T')[0];
        await page.fill('input[type="date"]', today);
        await page.fill('input[type="time"]', '10:00');
        await page.fill('input[placeholder="Durée en minutes"]', '30');
        await page.click('text=Enregistrer');
        await expect(page.locator('text=Test playwright')).toBeVisible();
    });

    test('should read an entry', async ({ page }) => {
        await page.goto('/activite');
        const entry = page.locator('text=Test playwright').first();
        await expect(entry).toBeVisible();
    });

    test('should delete an entry', async ({ page }) => {
        await page.goto('/activite');
        const entry = page.locator('text=Test playwright').first();
        await entry.scrollIntoViewIfNeeded();
        await entry.click({ force: true });
        await page.click('span.material-symbols-outlined.delete-btn');
        await page.waitForSelector('text=La session a été supprimée avec succès', { timeout: 10000 });
        await expect(entry).not.toBeVisible({ timeout: 10000 });
    });
});
