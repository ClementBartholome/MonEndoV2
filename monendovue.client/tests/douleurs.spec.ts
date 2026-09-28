import { test, expect } from './fixtures';
import { simulerDouleurs } from './mocks/douleurs';

test.describe('Douleurs', () => {
  test('affiche le mois : chiffres, graphique et entrées par jour', async ({ douleursPage, serveur, page }) => {
    simulerDouleurs(serveur, [
      { typeDouleur: 'Douleur pelvienne', intensite: 7, date: '2026-09-15T08:40:00', commentaire: 'réveil difficile' },
      { typeDouleur: 'Douleur lombaire', intensite: 4, date: '2026-09-15T14:05:00' },
      { typeDouleur: 'Douleur pelvienne', intensite: 6, date: '2026-09-14T21:15:00', commentaire: 'Pas de commentaire' },
      { typeDouleur: 'Douleur abdominale', intensite: 3, date: '2026-08-30T10:00:00' },
    ], ['2026-09-14', '2026-09-15']);
    await douleursPage.ouvrir();

    await expect(douleursPage.moisAffiche).toContainText('Septembre 2026');
    await expect(douleursPage.chiffres).toContainText('2jours avec douleur');
    await expect(douleursPage.chiffres).toContainText('Pelvienne');
    await expect(page.getByRole('img', { name: /Douleur notée 2 jours ce mois-ci, au plus 7 sur 10/ })).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Mardi 15 septembre' })).toBeVisible();
    await expect(douleursPage.entree('Pelvienne.*8 h 40 · réveil difficile')).toBeVisible();
    // L'ancien texte de remplacement n'est jamais affiché.
    await expect(page.getByText('Pas de commentaire')).toHaveCount(0);
  });

  test('note une douleur maintenant, à l\'heure locale', async ({ douleursPage, serveur, page }) => {
    const douleurs = simulerDouleurs(serveur);
    await douleursPage.ouvrir();

    await douleursPage.ouvrirAjout();
    await expect(douleursPage.panneau.getByRole('button', { name: 'Enregistrer' })).toBeDisabled();
    await douleursPage.remplir({ type: 'Lombaire', intensite: 6, commentaire: 'après le sport' });
    await douleursPage.enregistrer();

    await expect(douleursPage.panneau).toBeHidden();
    await expect(douleursPage.entree('Lombaire.*10 h 00 · après le sport')).toBeVisible();
    expect(douleurs[0]).toMatchObject({ typeDouleur: 'Douleur lombaire', intensite: 6, date: '2026-09-15T10:00:00', commentaire: 'après le sport' });
    await expect(page.getByText('Douleur notée', { exact: true })).toBeVisible();
  });

  test('modifie puis supprime une douleur depuis sa ligne', async ({ douleursPage, serveur }) => {
    const douleurs = simulerDouleurs(serveur, [{ typeDouleur: 'Douleur pelvienne', intensite: 5, date: '2026-09-14T09:00:00' }]);
    await douleursPage.ouvrir();

    await douleursPage.entree('Pelvienne').click();
    await douleursPage.remplir({ intensite: 8 });
    await douleursPage.enregistrer();
    await expect(douleursPage.panneau).toBeHidden();
    expect(douleurs[0]).toMatchObject({ intensite: 8, date: '2026-09-14T09:00:00' });

    await douleursPage.entree('Pelvienne').click();
    await douleursPage.panneau.getByRole('button', { name: 'Supprimer cette douleur' }).click();
    await douleursPage.panneau.getByRole('button', { name: 'Confirmer la suppression' }).click();

    await expect(douleursPage.entree('Pelvienne')).toHaveCount(0);
    expect(douleurs).toHaveLength(0);
  });

  test('change de mois ; le mois suivant n\'est pas proposé pour le mois en cours', async ({ douleursPage, serveur }) => {
    simulerDouleurs(serveur, [{ typeDouleur: 'Douleur abdominale', intensite: 3, date: '2026-08-30T10:00:00' }]);
    await douleursPage.ouvrir();

    await expect(douleursPage.moisAffiche.getByRole('button', { name: 'Mois suivant' })).toBeDisabled();
    await douleursPage.moisAffiche.getByRole('button', { name: 'Mois précédent' }).click();

    await expect(douleursPage.moisAffiche).toContainText('Août 2026');
    await expect(douleursPage.entree('Abdominale')).toBeVisible();
  });

  test('ouvre la saisie depuis l\'accueil (lien profond)', async ({ douleursPage, serveur }) => {
    simulerDouleurs(serveur);
    await douleursPage.ouvrir('?ajouter');

    await expect(douleursPage.panneau.getByRole('heading', { name: 'Noter une douleur' })).toBeVisible();
  });
});
