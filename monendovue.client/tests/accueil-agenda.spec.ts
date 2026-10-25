import { expect, test } from './fixtures';
import { simulerAccueil } from './mocks/accueil';
import { simulerLiaisonAgenda } from './mocks/agenda';

// La proposition de lier son agenda, en bas de l'accueil (la fonction est sinon cachée dans Plus › Paramètres).
test.describe('Accueil — proposer de lier son agenda', () => {
  test('agenda non lié : la proposition mène au réglage, et « Plus tard » la retire pour 30 jours', async ({ accueilPage, serveur, page }) => {
    simulerAccueil(serveur, {});
    simulerLiaisonAgenda(serveur);
    await accueilPage.ouvrir();

    const proposition = page.getByRole('region', { name: 'Tes rendez-vous médicaux' });
    await expect(proposition).toContainText('Lie ton agenda Google, en lecture seule');
    await expect(proposition.getByRole('link', { name: 'Lier mon agenda Google' })).toHaveAttribute('href', '/parametres?agenda=lier');

    await proposition.getByRole('button', { name: 'Plus tard' }).click();
    await expect(proposition).toHaveCount(0);
    await page.reload();
    await expect(accueilPage.bilan).toBeVisible();
    await expect(page.getByRole('region', { name: 'Tes rendez-vous médicaux' })).toHaveCount(0);
  });

  test('agenda lié sans calendrier choisi : propose de le choisir', async ({ accueilPage, serveur, page }) => {
    simulerAccueil(serveur, {});
    simulerLiaisonAgenda(serveur, { liee: true });
    await accueilPage.ouvrir();

    await expect(page.getByRole('region', { name: 'Tes rendez-vous médicaux' }).getByRole('link', { name: 'Choisir le calendrier' })).toBeVisible();
  });

  test('agenda lié et calendrier choisi, ou service indisponible : rien n\'est proposé', async ({ accueilPage, serveur, page }) => {
    simulerAccueil(serveur, {});
    simulerLiaisonAgenda(serveur, { liee: true, calendrierId: 'rdv' });
    await accueilPage.ouvrir();

    await expect(accueilPage.bilan).toBeVisible();
    await expect(page.getByRole('region', { name: 'Tes rendez-vous médicaux' })).toHaveCount(0);
  });

  test('depuis la proposition, les Paramètres s\'ouvrent sur le panneau de l\'agenda', async ({ parametresPage, serveur, page }) => {
    simulerLiaisonAgenda(serveur);

    await page.goto('/parametres?agenda=lier');

    await expect(parametresPage.panneau('Agenda Google').getByRole('button', { name: 'Lier mon agenda Google' })).toBeVisible();
    await expect(page).toHaveURL(/\/parametres$/);
  });
});
