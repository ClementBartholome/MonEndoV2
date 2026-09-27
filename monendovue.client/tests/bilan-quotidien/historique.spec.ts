import { expect, test } from '../fixtures';
import { simulerBilans } from '../mocks/bilan-quotidien';

// Aujourd'hui (MAINTENANT) : mardi 15 septembre 2026.
test.describe('Bilan quotidien — historique', () => {
  test.beforeEach(async ({ serveur }) => {
    simulerBilans(serveur, {
      bilans: [
        { jour: '2026-09-15', douleurMoyenne: 3 },
        { jour: '2026-09-08', douleurMoyenne: 7 },
        { jour: '2026-09-09', douleurMoyenne: 5, fatigue: null },
      ],
      joursRegles: ['2026-09-08', '2026-09-09'],
    });
  });

  test('affiche le mois en cours et décrit chaque jour du calendrier', async ({ bilanQuotidienPage: bilanPage, serveur }) => {
    await bilanPage.ouvrir();

    await expect(bilanPage.titrePeriode(/septembre 2026/i)).toBeVisible();
    await expect(bilanPage.modePeriode('Mois')).toHaveAttribute('aria-pressed', 'true');
    await expect(bilanPage.jour(new Date(2026, 8, 8))).toHaveAccessibleName('mardi 8 septembre, douleur 7 sur 10, règles');
    await expect(bilanPage.jour(new Date(2026, 8, 10))).toHaveAccessibleName('jeudi 10 septembre, pas de bilan');
    await expect(bilanPage.jour(new Date(2026, 8, 20))).toHaveAccessibleName('dimanche 20 septembre, à venir');

    const periodes = serveur.appelsVers('GET', /^BilanQuotidien\/periode$/).map((a) => a.parametres.toString());
    expect(periodes).toContain('du=2026-09-01&au=2026-09-30');
  });

  test('passe au mois précédent', async ({ bilanQuotidienPage: bilanPage, serveur }) => {
    await bilanPage.ouvrir();

    await bilanPage.periodePrecedente();

    await expect(bilanPage.titrePeriode(/août 2026/i)).toBeVisible();
    await expect(bilanPage.jour(new Date(2026, 7, 31))).toBeVisible();
    const periodes = serveur.appelsVers('GET', /^BilanQuotidien\/periode$/).map((a) => a.parametres.toString());
    expect(periodes).toContain('du=2026-08-01&au=2026-08-31');
  });

  test('affiche la semaine en cours', async ({ bilanQuotidienPage: bilanPage }) => {
    await bilanPage.ouvrir();

    await bilanPage.modePeriode('Semaine').click();

    await expect(bilanPage.titrePeriode('14 - 20 sept. 2026')).toBeVisible();
    await expect(bilanPage.calendrier.getByRole('button')).toHaveCount(7);
    await expect(bilanPage.jour(new Date(2026, 8, 8))).toHaveCount(0);
  });

  test('affiche le détail du jour choisi', async ({ bilanQuotidienPage: bilanPage }) => {
    await bilanPage.ouvrir();

    await bilanPage.selectionnerJour(new Date(2026, 8, 9));

    await expect(bilanPage.jour(new Date(2026, 8, 9))).toHaveAttribute('aria-pressed', 'true');
    await expect(bilanPage.titreDuJour('mercredi 9 septembre')).toBeVisible();
    await expect(bilanPage.boutonModifier).toBeVisible();
  });
});
