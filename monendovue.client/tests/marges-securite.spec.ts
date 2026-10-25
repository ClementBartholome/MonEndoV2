import { expect, test } from './fixtures';
import { simulerDouleurs } from './mocks/douleurs';

// Sur iPhone, la barre d'accueil réserve une marge en bas (env(safe-area-inset-bottom), 34 px sur un iPhone récent). Le navigateur
// des tests n'en a pas : on la fixe à la main pour vérifier que la barre du bas, le bouton flottant et le panneau s'en écartent.
test.describe('Marges de sécurité de l\'iPhone', () => {
  test('la barre du bas s\'agrandit de la marge, le bouton et le panneau restent au-dessus', async ({ douleursPage, serveur, page }) => {
    test.skip(test.info().project.name !== 'mobile', 'barre du bas seulement en largeur de téléphone');
    // Une douleur dans le mois : sans elle, l'état vide affiche un second bouton « Noter une crise ».
    simulerDouleurs(serveur, [{ typeDouleur: 'Douleur pelvienne', intensite: 5, date: '2026-09-14T09:00:00' }]);
    await douleursPage.ouvrir();
    await page.addStyleTag({ content: ':root { --marge-securite-bas: 34px; }' });
    const hauteurEcran = page.viewportSize()!.height;

    const barre = await page.getByRole('navigation', { name: 'Navigation principale' }).boundingBox();
    expect(Math.round(barre!.height)).toBe(68 + 34);
    expect(Math.round(barre!.y + barre!.height)).toBe(hauteurEcran);

    const bouton = await page.getByRole('button', { name: 'Noter une crise' }).and(page.locator(':visible')).first().boundingBox();
    expect(bouton!.y + bouton!.height).toBeLessThanOrEqual(barre!.y - 16 + 1);

    await douleursPage.ouvrirAjout();
    // Le panneau défile : son bas garde 1,5rem de marge en plus de celle de la barre d'accueil.
    await expect(douleursPage.panneau).toHaveCSS('padding-bottom', `${24 + 34}px`);
  });
});
