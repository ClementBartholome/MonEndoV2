import { test, expect } from './fixtures';
import { simulerParametresSansNotifications } from './mocks/parametres';

test.describe('Navigation principale', () => {
  test("l'accueil est la page courante, avec les rubriques du quotidien à portée", async ({ navigationPage, page }) => {
    await page.goto('/');

    await expect(navigationPage.lien('Accueil')).toHaveAttribute('aria-current', 'page');
    for (const rubrique of ['Bilan', 'Douleurs', 'Cycle']) {
      await expect(navigationPage.lien(rubrique)).toBeVisible();
    }
  });

  test('les paramètres sont accessibles depuis la navigation', async ({ navigationPage, serveur, page }) => {
    simulerParametresSansNotifications(serveur);
    await page.goto('/');

    await navigationPage.ouvrirRubrique('Paramètres');

    await expect(page).toHaveURL(/\/parametres$/);
    await expect(navigationPage.menuPlus).toBeHidden();
  });

  test('le menu « Plus » regroupe les autres rubriques et se ferme', async ({ navigationPage, page }) => {
    test.skip(!navigationPage.estMobile, 'menu propre au mobile');
    await page.goto('/');

    await navigationPage.principale.getByRole('button', { name: 'Plus' }).click();

    for (const rubrique of ['Traitements', 'Activité', 'Agenda', 'Préparer un rendez-vous', 'Transit', 'Suggestion ou bug', 'Paramètres']) {
      await expect(navigationPage.menuPlus.getByRole('link', { name: rubrique })).toBeVisible();
    }
    await navigationPage.menuPlus.getByRole('button', { name: 'Fermer' }).click();
    await expect(navigationPage.menuPlus).toBeHidden();
  });
});
