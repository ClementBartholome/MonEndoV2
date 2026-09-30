import { test, expect } from './fixtures';
import { simulerActivites } from './mocks/activite';

test.describe('Activité', () => {
  test('affiche le mois : chiffres et séances par jour', async ({ activitePage, serveur, page }) => {
    simulerActivites(serveur, [
      { type: 'Yoga', date: '2026-09-15T08:00:00', duree: 20, niveau: 'Douce', effet: 'Soulagee' },
      { type: 'Marche', date: '2026-09-14T18:00:00', duree: 45, niveau: 'Moderee', effet: 'Pareille', commentaire: 'au parc' },
      { type: 'Danse', date: '2026-09-12T19:00:00', duree: 60, niveau: 'Soutenue' },
      { type: 'Vélo', date: '2026-08-30T10:00:00', duree: 30 },
    ]);
    await activitePage.ouvrir();

    await expect(activitePage.chiffres).toContainText('3séances');
    await expect(activitePage.chiffres).toContainText('2 h 05au total');
    await expect(activitePage.chiffres).toContainText('1/2douleur soulagée après');
    await expect(page.getByRole('heading', { name: 'Mardi 15 septembre' })).toBeVisible();
    await expect(activitePage.seance('Yoga.*20 min · douce · douleur soulagée')).toBeVisible();
    await expect(activitePage.seance('Marche.*45 min · modérée · douleur inchangée · au parc')).toBeVisible();
    // Un ancien type libre reste affiché tel quel.
    await expect(activitePage.seance('Danse.*1 h · soutenue')).toBeVisible();
    await expect(activitePage.seance('Vélo')).toHaveCount(0);
  });

  test('note une activité en quelques touches', async ({ activitePage, serveur, page }) => {
    const activites = simulerActivites(serveur);
    await activitePage.ouvrir();

    await activitePage.ouvrirAjout();
    await expect(activitePage.panneau.getByRole('button', { name: 'Enregistrer' })).toBeDisabled();
    await activitePage.choisir('Natation', '45 min', 'Modérée', 'Soulagée');
    await activitePage.enregistrer();

    await expect(activitePage.panneau).toBeHidden();
    await expect(page.getByText('Activité notée', { exact: true }).first()).toBeVisible();
    expect(activites[0]).toMatchObject({
      type: 'Natation', duree: 45, niveau: 'Moderee', effet: 'Soulagee', date: '2026-09-15T18:00:00', commentaire: null,
    });
  });

  test('« Autre » : type libre, durée libre, hier', async ({ activitePage, serveur }) => {
    const activites = simulerActivites(serveur);
    await activitePage.ouvrir();

    await activitePage.ouvrirAjout();
    await activitePage.choisir('Autre activité');
    await activitePage.panneau.getByLabel('Laquelle ?').fill('Pilates');
    await activitePage.panneau.getByLabel('Autre durée').fill('50');
    await activitePage.choisir('Douce', 'Hier');
    await activitePage.enregistrer();

    await expect(activitePage.panneau).toBeHidden();
    expect(activites[0]).toMatchObject({ type: 'Pilates', duree: 50, niveau: 'Douce', effet: 'NonRenseigne', date: '2026-09-14T18:00:00' });
  });

  test('modifie puis supprime une séance, l\'heure d\'origine est conservée', async ({ activitePage, serveur }) => {
    const activites = simulerActivites(serveur, [{ type: 'Marche', date: '2026-09-14T07:30:00', niveau: 'Douce' }]);
    await activitePage.ouvrir();

    await activitePage.seance('Marche').click();
    await activitePage.choisir('Soutenue');
    await activitePage.enregistrer();
    await expect(activitePage.panneau).toBeHidden();
    expect(activites[0]).toMatchObject({ niveau: 'Soutenue', date: '2026-09-14T07:30:00' });

    await activitePage.seance('Marche').click();
    await activitePage.panneau.getByRole('button', { name: 'Supprimer cette activité' }).click();
    await activitePage.panneau.getByRole('button', { name: 'Confirmer la suppression' }).click();
    await expect(activitePage.seance('Marche')).toHaveCount(0);
    expect(activites).toHaveLength(0);
  });
});
