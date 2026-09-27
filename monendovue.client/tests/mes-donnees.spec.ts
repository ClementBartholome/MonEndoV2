import { test, expect } from './fixtures';
import { simulerExportDonnees, simulerParametresSansNotifications, simulerSuppressionCompte } from './mocks/parametres';

test.describe('Mes données — export', () => {
  test.beforeEach(({ serveur }) => {
    simulerParametresSansNotifications(serveur);
  });

  test('télécharge une archive de toutes ses données depuis les paramètres', async ({ parametresPage, serveur, page }) => {
    simulerExportDonnees(serveur);
    await parametresPage.ouvrir();

    const telechargement = page.waitForEvent('download');
    await parametresPage.boutonExport.click();

    expect((await telechargement).suggestedFilename()).toBe('monendo-mes-donnees-2026-09-15.zip');
  });

  test("signale un export qui n'a pas pu être préparé", async ({ parametresPage, serveur }) => {
    simulerExportDonnees(serveur, false);
    await parametresPage.ouvrir();

    await parametresPage.boutonExport.click();

    await expect(parametresPage.erreur).toContainText("L'export n'a pas pu être préparé");
  });
});

test.describe('Mes données — suppression du compte', () => {
  test.beforeEach(({ serveur }) => {
    simulerParametresSansNotifications(serveur);
    simulerSuppressionCompte(serveur);
  });

  test('supprime le compte après confirmation par le mot de passe, puis ferme la session', async ({ parametresPage, serveur, page }) => {
    await parametresPage.ouvrir();

    await parametresPage.boutonSuppression.click();
    await expect(parametresPage.boutonConfirmerSuppression).toBeDisabled();
    await parametresPage.champMotDePasse.fill('MotDePasse1!');
    await parametresPage.boutonConfirmerSuppression.click();

    await expect(page).toHaveURL(/\/login$/);
    await expect(page.getByText('Ton compte et toutes tes données ont été supprimés.', { exact: true })).toBeVisible();
    expect(serveur.appelsVers('POST', /^DonneesPersonnelles\/suppression-compte$/)[0].corps).toEqual({ password: 'MotDePasse1!' });
    expect(await page.evaluate(() => localStorage.getItem('user'))).toBeNull();
  });

  test('un mauvais mot de passe ne supprime rien et le dit', async ({ parametresPage, page }) => {
    await parametresPage.ouvrir();

    await parametresPage.boutonSuppression.click();
    await parametresPage.champMotDePasse.fill('Faux');
    await parametresPage.boutonConfirmerSuppression.click();

    await expect(parametresPage.fenetreSuppression.getByRole('alert')).toHaveText('Le mot de passe est incorrect.');
    await expect(page).toHaveURL(/\/parametres$/);
  });
});
