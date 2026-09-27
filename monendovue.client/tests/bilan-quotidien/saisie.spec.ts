import { expect, test } from '../fixtures';
import { simulerBilans } from '../mocks/bilan-quotidien';

// Aujourd'hui (MAINTENANT) : mardi 15 septembre 2026.
test.describe('Bilan quotidien — saisie du jour', () => {
  test("ouvre directement la saisie quand le bilan du jour n'est pas rempli", async ({ bilanQuotidienPage: bilanPage, serveur }) => {
    simulerBilans(serveur);

    await bilanPage.ouvrir();

    await expect(bilanPage.titreSaisie).toHaveText("Bilan d'aujourd'hui");
    await expect(bilanPage.boutonEnregistrer).toBeDisabled();
    await expect(bilanPage.aideSaisie).toHaveText('À renseigner : douleur, une émotion');
  });

  test('enregistre un bilan avec seulement la douleur et une émotion', async ({ bilanQuotidienPage: bilanPage, page, serveur }) => {
    const bilans = simulerBilans(serveur);
    await bilanPage.ouvrir();

    await bilanPage.remplirEssentiel(4, 'Calme');
    await expect(bilanPage.boutonEnregistrer).toBeEnabled();
    await bilanPage.enregistrer();

    await expect(page.getByText('Bilan enregistré', { exact: true })).toBeVisible();
    await expect(bilanPage.titreDuJour("Aujourd'hui")).toBeVisible();
    await expect(bilanPage.boutonModifier).toBeVisible();
    // Enregistré pour le bon jour, et les réponses facultatives restent vides (null) au lieu de valoir 0.
    expect(bilans).toHaveLength(1);
    expect(bilans[0]).toMatchObject({
      douleurMoyenne: 4,
      emotions: [{ emotion: 'Calme' }],
      fatigue: null,
      stressPro: null,
      stressPerso: null,
      pas: null,
      hydratation: null,
    });
    expect(String(bilans[0].date)).toMatch(/^2026-09-15T/);
  });

  test('limite le choix à trois émotions', async ({ bilanQuotidienPage: bilanPage, serveur }) => {
    simulerBilans(serveur);
    await bilanPage.ouvrir();

    await bilanPage.choisirEmotions('Joie', 'Calme', 'Motivation');

    await expect(bilanPage.emotion('Tristesse')).toBeDisabled();
    await expect(bilanPage.emotion('Joie')).toHaveAttribute('aria-pressed', 'true');
  });

  test('« Comme hier » reprend le corps de la veille', async ({ bilanQuotidienPage: bilanPage, serveur }) => {
    const bilans = simulerBilans(serveur, {
      bilans: [{ jour: '2026-09-14', douleurMoyenne: 6, pas: 7500, hydratation: 1.5, gluten: true }],
    });
    await bilanPage.ouvrir();

    await bilanPage.boutonCommeHier.click();
    await bilanPage.remplirEssentiel(2, 'Joie');
    await bilanPage.enregistrer();

    await expect(bilanPage.boutonModifier).toBeVisible();
    const bilanDuJour = bilans.find((b) => String(b.date).startsWith('2026-09-15'));
    // Le corps (pas, hydratation, alimentation) vient de la veille ; la douleur et les émotions sont celles du jour.
    expect(bilanDuJour).toMatchObject({
      douleurMoyenne: 2,
      emotions: [{ emotion: 'Joie' }],
      pas: 7500,
      hydratation: 1.5,
      gluten: true,
    });
  });

  test('demande confirmation avant de quitter une saisie commencée', async ({ bilanQuotidienPage: bilanPage, serveur }) => {
    const bilans = simulerBilans(serveur);
    await bilanPage.ouvrir();

    await bilanPage.choisirNiveau('Douleur', 5);
    await bilanPage.boutonAnnuler.click();

    await expect(bilanPage.confirmationSortie).toBeVisible();
    await bilanPage.confirmationSortie.getByRole('button', { name: 'Continuer la saisie' }).click();
    await expect(bilanPage.pastille('Douleur', 5)).toHaveAttribute('aria-pressed', 'true');

    await bilanPage.boutonAnnuler.click();
    await bilanPage.confirmationSortie.getByRole('button', { name: 'Quitter sans enregistrer' }).click();
    await expect(bilanPage.titreSaisie).toBeHidden();
    expect(bilans).toHaveLength(0);
  });

  test("affiche le message du serveur si l'enregistrement est refusé", async ({ bilanQuotidienPage: bilanPage, page, serveur }) => {
    simulerBilans(serveur);
    serveur.on('POST', /^BilanQuotidien$/, () => ({
      status: 409,
      body: { message: "Un bilan existe déjà pour ce jour : modifie-le plutôt que d'en créer un second." },
    }));
    await bilanPage.ouvrir();

    await bilanPage.remplirEssentiel(3, 'Calme');
    await bilanPage.enregistrer();

    await expect(page.getByText("Un bilan existe déjà pour ce jour : modifie-le plutôt que d'en créer un second.", { exact: true }))
      .toBeVisible();
    await expect(bilanPage.titreSaisie).toBeVisible();
  });
});
