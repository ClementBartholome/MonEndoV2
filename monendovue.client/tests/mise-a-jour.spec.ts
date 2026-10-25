import type { Page } from '@playwright/test';
import { expect, test } from './fixtures';

/** Ce que Vite émet quand un fichier chargé à la demande n'existe plus (nouvelle version déployée) ; vrai si l'app l'a pris en charge. */
const fichierIntrouvable = (page: Page) => page.evaluate(() => {
  const evenement = new Event('vite:preloadError', { cancelable: true });
  window.dispatchEvent(evenement);
  return evenement.defaultPrevented;
});

test.describe('Après une mise à jour de l\'application', () => {
  test.use({ connectee: false });

  test('un fichier de l\'ancienne version introuvable recharge la page une fois, pas en boucle', async ({ page }) => {
    await page.goto('/confidentialite');
    await expect(page.getByRole('heading', { level: 1 })).toBeVisible();

    const rechargement = page.waitForEvent('load');
    expect(await fichierIntrouvable(page)).toBe(true);
    await rechargement;
    await expect(page.getByRole('heading', { level: 1 })).toBeVisible();

    // Juste après, une nouvelle erreur n'est plus prise en charge : ce n'est pas une mise à jour, l'erreur suit son cours.
    expect(await fichierIntrouvable(page)).toBe(false);
  });

  test('hors connexion, la page n\'est pas rechargée', async ({ page, context }) => {
    await page.goto('/confidentialite');
    await expect(page.getByRole('heading', { level: 1 })).toBeVisible();
    await context.setOffline(true);

    expect(await fichierIntrouvable(page)).toBe(false);
    await context.setOffline(false);
  });
});
