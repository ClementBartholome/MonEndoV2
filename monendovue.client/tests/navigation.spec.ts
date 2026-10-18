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

  test('le menu « Plus » regroupe les autres rubriques par thème et se ferme', async ({ navigationPage, page }) => {
    test.skip(!navigationPage.estMobile, 'menu propre au mobile');
    await page.goto('/');

    await navigationPage.principale.getByRole('button', { name: 'Plus' }).click();

    const groupes: Record<string, string[]> = {
      'Mes suivis': ['Traitements', 'Acné', 'Activité', 'Transit'],
      'Mes rendez-vous': ['Agenda', 'Préparer un rendez-vous'],
      'Aide': ["S'informer", 'Suggestion ou bug'],
    };
    for (const [groupe, rubriques] of Object.entries(groupes)) {
      await expect(navigationPage.menuPlus.getByRole('heading', { name: groupe })).toBeVisible();
      for (const rubrique of rubriques) {
        await expect(navigationPage.menuPlus.getByRole('navigation', { name: groupe }).getByRole('link', { name: new RegExp(`^${rubrique}`) })).toBeVisible();
      }
    }
    await expect(navigationPage.menuPlus.getByRole('link', { name: 'Paramètres' })).toBeVisible();
    await navigationPage.menuPlus.getByRole('button', { name: 'Fermer' }).click();
    await expect(navigationPage.menuPlus).toBeHidden();
  });
});
