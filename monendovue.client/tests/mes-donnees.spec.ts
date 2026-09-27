import { test, expect } from './fixtures';
import { simulerExportDonnees, simulerParametresSansNotifications } from './mocks/parametres';

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
