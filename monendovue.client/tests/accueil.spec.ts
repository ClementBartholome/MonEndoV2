import { test, expect } from './fixtures';
import { simulerAccueil, traitement } from './mocks/accueil';

test.describe('Accueil « Aujourd\'hui »', () => {
  test('sans données : le bilan est à faire, rien d\'autre ne s\'affiche à vide', async ({ accueilPage, page }) => {
    await accueilPage.ouvrir();

    await expect(page.getByRole('heading', { level: 1 })).toHaveText('Mardi 15 septembre');
    await expect(accueilPage.bilan.getByRole('link', { name: 'Faire mon bilan' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Douleur', exact: true })).toBeVisible();
    await expect(accueilPage.traitements).toHaveCount(0);
    await expect(accueilPage.semaine).toHaveCount(0);
  });

  test('résume la journée : règles, bilan fait, semaine', async ({ accueilPage, serveur }) => {
    simulerAccueil(serveur, {
      cycle: { enRegles: true, jourDeRegles: 2, jourDuCycle: 2 },
      bilan: { douleurMoyenne: 4, emotions: ['Calme'], fatigue: 3 },
      semaine: { joursAvecDouleur: 4, joursAvecDouleurPendantRegles: 2, fatigueMoyenne: 2.5, fatigueMoyennePrecedente: 3.5 },
    });
    await accueilPage.ouvrir();

    await expect(accueilPage.pastilleCycle('Règles · jour 2')).toBeVisible();
    await expect(accueilPage.bilan).toContainText('Bilan du jour fait');
    await expect(accueilPage.bilan).toContainText('4/10');
    await expect(accueilPage.bilan).toContainText('Calme');
    await expect(accueilPage.semaine).toContainText('Douleur notée 4 jours sur 7, dont 2 pendant les règles.');
    await expect(accueilPage.semaine).toContainText('Fatigue moyenne plus basse que la semaine précédente.');
  });

  test('une prise de traitement se note en un geste, à l\'heure locale', async ({ accueilPage, serveur }) => {
    simulerAccueil(serveur, { traitements: [traitement({ id: 7, nom: 'Diénogest 2 mg' })] });
    await accueilPage.ouvrir();

    await accueilPage.boutonPrise('Diénogest 2 mg').click();

    await expect(accueilPage.traitements).toContainText('Pris à 10 h 00');
    expect(serveur.appelsVers('POST', /^DonneesMedicament$/)[0].corps).toMatchObject({
      medicamentId: 7,
      nombreComprimes: 1,
      date: '2026-09-15T10:00:00',
    });
  });

  test('signale un accueil qui n\'a pas pu être chargé', async ({ accueilPage, serveur, page }) => {
    serveur.on('GET', /^Accueil\/aujourdhui$/, () => ({ status: 500 }));
    await accueilPage.ouvrir();

    await expect(page.getByRole('alert')).toContainText('L\'accueil n\'a pas pu être chargé.');
    await expect(page.getByRole('button', { name: 'Réessayer' })).toBeVisible();
  });
});
