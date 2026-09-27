import { test, expect } from './fixtures';
import { simulerParametresSansNotifications } from './mocks/parametres';

test.describe('Documents légaux — sans session', () => {
  test.use({ connectee: false });

  test('la politique de confidentialité est lisible depuis la connexion, sans compte', async ({ documentsLegauxPage, page }) => {
    await documentsLegauxPage.ouvrirConnexion();

    await documentsLegauxPage.lienPied('Confidentialité').click();

    await expect(page).toHaveURL(/\/confidentialite$/);
    await expect(documentsLegauxPage.titre).toHaveText('Politique de confidentialité');
    await expect(documentsLegauxPage.section('Tes droits')).toBeVisible();

    await documentsLegauxPage.lienRetour().click();
    await expect(page).toHaveURL(/\/login$/);
  });

  test('les mentions légales sont accessibles directement, sans compte', async ({ documentsLegauxPage, page }) => {
    await page.goto('/mentions-legales');

    await expect(documentsLegauxPage.titre).toHaveText('Mentions légales');
    await expect(documentsLegauxPage.section('Hébergement')).toBeVisible();
  });
});

test.describe('Documents légaux — connectée', () => {
  test('les paramètres mènent aux mentions légales, avec retour à l\'accueil', async ({ documentsLegauxPage, serveur }) => {
    simulerParametresSansNotifications(serveur);
    await documentsLegauxPage.ouvrirParametres();

    await documentsLegauxPage.lienPied('Mentions légales').click();

    await expect(documentsLegauxPage.titre).toHaveText('Mentions légales');
    await expect(documentsLegauxPage.lienRetour()).toHaveText(/Retour à l'accueil/);
  });
});
