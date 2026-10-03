import { test, expect } from './fixtures';

test.describe("S'informer sur l'endométriose", () => {
  test('la rubrique se trouve dans la navigation et ouvre la page', async ({ navigationPage, page }) => {
    await page.goto('/');

    await navigationPage.ouvrirRubrique("S'informer");

    await expect(page).toHaveURL(/\/s-informer$/);
    await expect(page.getByRole('heading', { level: 1, name: "S'informer sur l'endométriose" })).toBeVisible();
  });

  test('la page recharge sans afficher de JSON', async ({ page }) => {
    await page.goto('/s-informer');
    await page.reload();

    await expect(page.getByRole('heading', { level: 1, name: "S'informer sur l'endométriose" })).toBeVisible();
  });

  test("chaque source s'ouvre dans un nouvel onglet, sans référent ni paramètre", async ({ page }) => {
    await page.goto('/s-informer');
    const liens = page.locator('main a[target="_blank"]');

    expect(await liens.count()).toBeGreaterThanOrEqual(9);
    for (const lien of await liens.all()) {
      await expect(lien).toHaveAttribute('rel', 'noopener noreferrer');
      const url = new URL((await lien.getAttribute('href')) ?? '');
      expect(url.protocol).toBe('https:');
      expect(url.search).toBe('');
      await expect(lien).toContainText("s'ouvre dans un nouvel onglet");
    }
  });

  test("la fiche de l'OMS est signalée comme étant en anglais", async ({ page }) => {
    await page.goto('/s-informer');

    const lien = page.getByRole('link', { name: /Endometriosis/ });
    await expect(lien).toHaveAttribute('hreflang', 'en');
    await expect(lien).toContainText('en anglais');
  });

  test('rappelle que MonEndo ne remplace pas un soignant et mène à la préparation du rendez-vous', async ({ page }) => {
    await page.goto('/s-informer');

    await expect(page.getByText('ne pose pas de diagnostic et ne remplace pas un soignant')).toBeVisible();
    await page.getByRole('link', { name: 'Préparer mon prochain rendez-vous' }).click();

    await expect(page).toHaveURL(/\/export$/);
  });
});
