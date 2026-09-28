import { test, expect } from './fixtures';
import { simulerTraitements } from './mocks/traitements';

test.describe('Traitements', () => {
  test('sans traitement : l\'état vide propose d\'en ajouter un', async ({ traitementsPage, serveur, page }) => {
    simulerTraitements(serveur);
    await traitementsPage.ouvrir();

    await expect(page.getByText('Aucun traitement pour l\'instant')).toBeVisible();
  });

  test('prises du jour par moment : prendre, ignorer, annuler', async ({ traitementsPage, serveur, page }) => {
    const { prises } = simulerTraitements(serveur, [
      { nom: 'Diénogest 2 mg', horaires: ['08:00'] },
      { nom: 'Vitamine D', dose: null, horaires: ['20:30'] },
    ]);
    await traitementsPage.ouvrir();

    await expect(traitementsPage.aujourdhui.getByRole('heading', { name: 'Matin · 8 h 00' })).toBeVisible();
    await expect(traitementsPage.aujourdhui.getByRole('heading', { name: 'Soir · 20 h 30' })).toBeVisible();
    await expect(traitementsPage.aujourdhui).toContainText('0 prise sur 2');
    await expect(traitementsPage.aujourdhui.getByRole('button', { name: 'Noter la prise de Diénogest 2 mg de 8 h 00' })).toHaveText('Je l\'ai pris');

    await traitementsPage.aujourdhui.getByRole('button', { name: 'Noter la prise de Diénogest 2 mg de 8 h 00' }).click();
    await expect(traitementsPage.aujourdhui).toContainText('Pris à 10 h 00');
    await expect(traitementsPage.aujourdhui).toContainText('1 prise sur 2');
    expect(prises[0]).toMatchObject({ statut: 'Pris', heurePrevue: '08:00:00', date: '2026-09-15T10:00:00' });

    await traitementsPage.aujourdhui.getByRole('button', { name: 'Ignorer la prise de Vitamine D de 20 h 30' }).click();
    await expect(traitementsPage.aujourdhui).toContainText('Ignorée');
    await expect(page.getByText('Vitamine D : prise ignorée', { exact: true })).toBeVisible();

    await traitementsPage.aujourdhui.getByRole('button', { name: 'Annuler la réponse pour Vitamine D' }).click();
    await expect(traitementsPage.aujourdhui.getByRole('button', { name: 'Ignorer la prise de Vitamine D de 20 h 30' })).toBeVisible();
    expect(prises).toHaveLength(1);
  });

  test('prise au besoin et séance de soin en un geste', async ({ traitementsPage, serveur }) => {
    const { prises, seances } = simulerTraitements(serveur, [
      { nom: 'Ibuprofène', dose: '400 mg', frequence: 'AuBesoin', horaires: [] },
      { nom: 'Kiné', type: 'NonMedicamenteux', dose: null, frequence: 'AuBesoin', horaires: [] },
    ]);
    await traitementsPage.ouvrir();

    await expect(traitementsPage.auBesoin).toContainText('Aucune prise notée');
    await traitementsPage.auBesoin.getByRole('button', { name: 'Noter une prise de Ibuprofène' }).click();
    await expect(traitementsPage.auBesoin).toContainText('Dernière prise : aujourd\'hui, 10 h 00');
    expect(prises[0]).toMatchObject({ statut: 'Pris', heurePrevue: null });

    await traitementsPage.soins.getByRole('button', { name: 'Noter une séance de Kiné' }).click();
    await expect(traitementsPage.soins).toContainText('Dernière séance le 15 septembre');
    expect(seances).toHaveLength(1);
  });

  test('ajoute un traitement certains jours avec deux horaires', async ({ traitementsPage, serveur }) => {
    const { traitements } = simulerTraitements(serveur);
    await traitementsPage.ouvrir();

    await traitementsPage.ouvrirAjout();
    const panneau = traitementsPage.panneau;
    await expect(panneau.getByRole('button', { name: 'Enregistrer' })).toBeDisabled();
    await panneau.getByRole('textbox', { name: 'Nom' }).fill('Progestatif');
    await panneau.getByRole('button', { name: 'Certains jours' }).click();
    await expect(panneau.getByRole('button', { name: 'Enregistrer' })).toBeDisabled();
    await panneau.getByRole('button', { name: 'Lundi' }).click();
    await panneau.getByRole('button', { name: 'Jeudi' }).click();
    await panneau.getByRole('button', { name: 'Ajouter un horaire' }).click();
    await traitementsPage.enregistrer();

    await expect(panneau).toBeHidden();
    await expect(traitementsPage.mesTraitements).toContainText('Lun, jeu · 8 h 00, 20 h 00');
    expect(traitements[0]).toMatchObject({
      nom: 'Progestatif', type: 'Medicamenteux', frequence: 'CertainsJours', joursSemaine: ['Lundi', 'Jeudi'],
      intervalleJours: null, dateDebut: '2026-09-15', dateFin: null,
    });
    expect(serveur.appelsVers('POST', /^Traitements$/)[0].corps.horaires).toEqual(['08:00:00', '20:00:00']);
  });

  test('modifie puis arrête un traitement ; il passe dans les traitements terminés', async ({ traitementsPage, serveur }) => {
    const { traitements } = simulerTraitements(serveur, [{ nom: 'Diénogest 2 mg' }]);
    await traitementsPage.ouvrir();

    await traitementsPage.mesTraitements.getByRole('button', { name: /Diénogest 2 mg/ }).click();
    await traitementsPage.panneau.getByRole('textbox', { name: /Dose/ }).fill('2 comprimés');
    await traitementsPage.enregistrer();
    await expect(traitementsPage.panneau).toBeHidden();
    expect(traitements[0].dose).toBe('2 comprimés');

    await traitementsPage.mesTraitements.getByRole('button', { name: /Diénogest 2 mg/ }).click();
    await traitementsPage.panneau.getByRole('button', { name: 'Arrêter ce traitement' }).click();
    await traitementsPage.panneau.getByRole('button', { name: 'Confirmer l\'arrêt aujourd\'hui' }).click();

    await expect(traitementsPage.panneau).toBeHidden();
    expect(traitements[0].dateFin).toBe('2026-09-15');
    await traitementsPage.mesTraitements.getByRole('button', { name: 'Traitements terminés (1)' }).click();
    await expect(traitementsPage.mesTraitements).toContainText('Du 10 janv. 2026 au 15 sept. 2026');
  });

  test('ouvre la saisie depuis un lien profond', async ({ traitementsPage, serveur }) => {
    simulerTraitements(serveur);
    await traitementsPage.ouvrir('?ajouter');

    await expect(traitementsPage.panneau.getByRole('heading', { name: 'Ajouter un traitement' })).toBeVisible();
  });
});
