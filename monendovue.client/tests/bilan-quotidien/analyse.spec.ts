import { expect, test } from '../fixtures';
import { simulerBilans } from '../mocks/bilan-quotidien';

// Aujourd'hui (MAINTENANT) : mardi 15 septembre 2026. L'analyse porte sur le mois affiché (septembre).
test.describe('Bilan quotidien — analyse et tendances', () => {
  test('calcule les moyennes sans compter les mesures non renseignées', async ({ bilanQuotidienPage: bilanPage, serveur }) => {
    simulerBilans(serveur, {
      bilans: [
        { jour: '2026-09-13', douleurMoyenne: 2, fatigue: 2 },
        { jour: '2026-09-14', douleurMoyenne: 4, fatigue: null },
        { jour: '2026-09-15', douleurMoyenne: 6, fatigue: 4 },
      ],
    });
    await bilanPage.ouvrir();

    await bilanPage.afficherOnglet('Tendances');

    await expect(bilanPage.indicateur('Douleur moyenne')).toContainText('4/10');
    // Fatigue : (2 + 4) / 2 = 3, et non (2 + 0 + 4) / 3 = 2 si le jour non renseigné comptait pour 0.
    await expect(bilanPage.indicateur('Fatigue moyenne')).toContainText('3/5');
    // Jamais renseignée sur la période ni sur la précédente : pas de tuile plutôt qu'une valeur à 0.
    await expect(bilanPage.indicateur('Hydratation')).toHaveCount(0);
    await expect(bilanPage.page.getByText('Moyennes de la période (aucun bilan le mois précédent pour comparer).'))
      .toBeVisible();
  });

  test('compare au mois précédent', async ({ bilanQuotidienPage: bilanPage, serveur }) => {
    simulerBilans(serveur, {
      bilans: [
        { jour: '2026-08-20', douleurMoyenne: 7 },
        { jour: '2026-08-21', douleurMoyenne: 7 },
        { jour: '2026-09-14', douleurMoyenne: 3 },
        { jour: '2026-09-15', douleurMoyenne: 3 },
      ],
    });
    await bilanPage.ouvrir();

    await bilanPage.afficherOnglet('Tendances');

    await expect(bilanPage.page.getByText('Moyennes de la période, comparées au mois précédent.')).toBeVisible();
    await expect(bilanPage.indicateur('Douleur moyenne')).toContainText('3/10');
    await expect(bilanPage.indicateur('Douleur moyenne')).toContainText('−4');
  });

  test('rapproche la douleur des jours de règles', async ({ bilanQuotidienPage: bilanPage, serveur }) => {
    simulerBilans(serveur, {
      bilans: [
        { jour: '2026-09-07', douleurMoyenne: 8 },
        { jour: '2026-09-08', douleurMoyenne: 6 },
        { jour: '2026-09-12', douleurMoyenne: 2 },
        { jour: '2026-09-15', douleurMoyenne: 2 },
      ],
      joursRegles: ['2026-09-07', '2026-09-08'],
    });
    await bilanPage.ouvrir();

    await bilanPage.afficherOnglet('Tendances');

    await expect(bilanPage.page.getByText('Douleur moyenne pendant tes règles : 7/10, contre 2/10 les autres jours.'))
      .toBeVisible();
  });
});
