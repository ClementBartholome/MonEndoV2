import { test, expect } from './fixtures';
import { prisePrevue, simulerAccueil } from './mocks/accueil';

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
    await expect(accueilPage.bilan).toContainText('Fait');
    await expect(accueilPage.bilan).toContainText('4/10');
    await expect(accueilPage.bilan).toContainText('Calme');
    await expect(accueilPage.semaine).toContainText('Douleur notée 4 jours sur 7, dont 2 pendant les règles.');
    await expect(accueilPage.semaine).toContainText('Fatigue moyenne plus basse que la semaine précédente.');
  });

  test('seules les prises prévues aujourd\'hui s\'affichent ; une prise se note en un geste', async ({ accueilPage, serveur, page }) => {
    simulerAccueil(serveur, {
      prisesPrevues: [
        prisePrevue({ traitementId: 7, nom: 'Diénogest 2 mg', heurePrevue: '08:00' }),
        prisePrevue({ traitementId: 8, nom: 'Vitamine D', heurePrevue: '20:00', reponse: { priseId: 3, statut: 'Ignore', date: '2026-09-15T09:00:00' } }),
      ],
      auBesoin: [{ id: 9, nom: 'Ibuprofène', dose: '400 mg', dernierePrise: null }],
    });
    await accueilPage.ouvrir();

    await expect(accueilPage.traitements).toContainText('0 sur 2');
    await expect(accueilPage.traitements).toContainText('Ignorée');
    await expect(accueilPage.traitements).toContainText('Au besoin : Ibuprofène');
    await accueilPage.boutonPrise('Diénogest 2 mg', '8 h 00').click();

    await expect(accueilPage.traitements).toContainText('Pris à 10 h 00');
    await expect(accueilPage.traitements).toContainText('1 sur 2');
    await expect(page.getByText('Diénogest 2 mg à 10 h 00', { exact: true })).toBeVisible();
    expect(serveur.appelsVers('POST', /^Traitements\/7\/prises$/)[0].corps).toEqual({
      statut: 'Pris',
      heurePrevue: '08:00:00',
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
