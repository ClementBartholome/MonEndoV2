import { expect, test } from './fixtures';
import { simulerActivite } from './mocks/activite';

test.describe('Activité physique', () => {
  test('affiche les séances du mois', async ({ activitePage, serveur }) => {
    simulerActivite(serveur, [{ typeActivite: 'Natation', date: '2026-09-10T08:00:00', duree: 45 }]);

    await activitePage.ouvrir();

    await expect(activitePage.seance('Natation')).toBeVisible();
  });

  test('affiche un commentaire comme du texte, sans interpréter le HTML saisi', async ({ activitePage, page, serveur }) => {
    const commentaire = '<b id="injection">gras</b>';
    simulerActivite(serveur, [{ typeActivite: 'Marche', commentaire }]);

    await activitePage.ouvrir();

    await expect(activitePage.seance(commentaire)).toBeVisible();
    await expect(page.locator('#injection')).toHaveCount(0);
  });

  test('ajoute une séance', async ({ activitePage, serveur }) => {
    const seances = simulerActivite(serveur);
    await activitePage.ouvrir();

    await activitePage.ouvrirAjout();
    await activitePage.remplir({ type: 'Yoga', date: '2026-09-14', heure: '18:30', duree: 40 });
    await activitePage.valider('Enregistrer');

    await expect(activitePage.seance('Yoga')).toBeVisible();
    expect(seances).toHaveLength(1);
    expect(seances[0]).toMatchObject({ typeActivite: 'Yoga', duree: 40 });
  });

  test('modifie une séance', async ({ activitePage, serveur }) => {
    const seances = simulerActivite(serveur, [{ typeActivite: 'Marche', duree: 30 }]);
    await activitePage.ouvrir();

    await activitePage.action('Marche', 'Modifier').click();
    await activitePage.remplir({ duree: 50 });
    await activitePage.valider('Mettre à jour');

    await expect(activitePage.formulaire).toBeHidden();
    expect(seances[0]).toMatchObject({ typeActivite: 'Marche', duree: 50 });
  });

  test('supprime une séance', async ({ activitePage, page, serveur }) => {
    const seances = simulerActivite(serveur, [{ typeActivite: 'Vélo' }]);
    await activitePage.ouvrir();

    await activitePage.action('Vélo', 'Supprimer').click();

    await expect(page.getByText('La session a été supprimée avec succès', { exact: true })).toBeVisible();
    await expect(activitePage.seance('Vélo')).toHaveCount(0);
    expect(seances).toHaveLength(0);
  });
});
