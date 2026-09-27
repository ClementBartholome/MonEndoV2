import { expect, test } from '../fixtures';
import { simulerBilans } from '../mocks/bilan-quotidien';

// Aujourd'hui (MAINTENANT) : mardi 15 septembre 2026. Le bilan du jour est déjà rempli dans ces parcours.
const BILAN_DU_JOUR = { jour: '2026-09-15', douleurMoyenne: 3 };

test.describe('Bilan quotidien — modification et jours passés', () => {
  test('affiche le bilan du jour déjà rempli sans rouvrir la saisie', async ({ bilanQuotidienPage: bilanPage, serveur }) => {
    simulerBilans(serveur, { bilans: [BILAN_DU_JOUR] });

    await bilanPage.ouvrir();

    await expect(bilanPage.titreDuJour("Aujourd'hui")).toBeVisible();
    await expect(bilanPage.titreSaisie).toBeHidden();
    await expect(bilanPage.boutonModifier).toBeVisible();
  });

  test('modifie le bilan du jour', async ({ bilanQuotidienPage: bilanPage, page, serveur }) => {
    const bilans = simulerBilans(serveur, { bilans: [BILAN_DU_JOUR] });
    await bilanPage.ouvrir();

    await bilanPage.boutonModifier.click();
    await expect(bilanPage.titreSaisie).toHaveText("Modifier le bilan d'aujourd'hui");
    await expect(bilanPage.pastille('Douleur', 3)).toHaveAttribute('aria-pressed', 'true');
    await bilanPage.choisirNiveau('Douleur', 7);
    await bilanPage.enregistrer();

    await expect(page.getByText('Bilan mis à jour', { exact: true })).toBeVisible();
    expect(bilans).toHaveLength(1);
    expect(bilans[0]).toMatchObject({ id: 1, douleurMoyenne: 7 });
  });

  test('modifie un bilan passé choisi dans le calendrier', async ({ bilanQuotidienPage: bilanPage, serveur }) => {
    const bilans = simulerBilans(serveur, {
      bilans: [BILAN_DU_JOUR, { jour: '2026-09-10', douleurMoyenne: 5, emotions: ['Tristesse'] }],
    });
    await bilanPage.ouvrir();

    await bilanPage.selectionnerJour(new Date(2026, 8, 10));
    await expect(bilanPage.titreDuJour('jeudi 10 septembre')).toBeVisible();
    await bilanPage.boutonModifier.click();
    await expect(bilanPage.titreSaisie).toHaveText('Modifier le bilan du 10 septembre');
    await bilanPage.choisirEmotions('Calme');
    await bilanPage.enregistrer();

    await expect(bilanPage.titreDuJour('jeudi 10 septembre')).toBeVisible();
    const bilanDu10 = bilans.find((b) => b.id === 2);
    // Le bilan garde son jour : la modification ne le déplace pas.
    expect(String(bilanDu10?.date)).toMatch(/^2026-09-10T/);
    expect(bilanDu10?.emotions).toEqual([{ emotion: 'Tristesse' }, { emotion: 'Calme' }]);
  });

  test('remplit le bilan oublié d\'un jour passé', async ({ bilanQuotidienPage: bilanPage, serveur }) => {
    const bilans = simulerBilans(serveur, { bilans: [BILAN_DU_JOUR] });
    await bilanPage.ouvrir();

    await bilanPage.selectionnerJour(new Date(2026, 8, 12));
    await expect(bilanPage.page.getByText('Aucun bilan pour ce jour')).toBeVisible();
    await bilanPage.boutonRemplir.click();
    await expect(bilanPage.titreSaisie).toHaveText('Bilan du 12 septembre');
    await bilanPage.remplirEssentiel(2, 'Soulagement');
    await bilanPage.enregistrer();

    await expect(bilanPage.titreDuJour('samedi 12 septembre')).toBeVisible();
    const bilanDu12 = bilans.find((b) => String(b.date).startsWith('2026-09-12'));
    expect(bilanDu12).toMatchObject({ douleurMoyenne: 2, emotions: [{ emotion: 'Soulagement' }] });
  });

  test('ne permet pas de choisir un jour à venir', async ({ bilanQuotidienPage: bilanPage, serveur }) => {
    simulerBilans(serveur, { bilans: [BILAN_DU_JOUR] });
    await bilanPage.ouvrir();

    await expect(bilanPage.jour(new Date(2026, 8, 16))).toBeDisabled();
    await expect(bilanPage.jour(new Date(2026, 8, 15))).toBeEnabled();
  });
});
