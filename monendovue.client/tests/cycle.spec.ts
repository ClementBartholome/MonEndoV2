import { test, expect } from './fixtures';
import { champs, simulerRegles, simulerSymptomes } from './mocks/cycle';

test.describe('Cycle · règles', () => {
  test('affiche le cycle en cours, le mois et l\'historique', async ({ cyclePage, serveur }) => {
    simulerRegles(serveur, ['2026-09-14', '2026-09-15'], {
      enCours: { debut: '2026-09-14', jourDuCycle: 2, jourDeRegles: 2 },
      cycles: [
        { debut: '2026-08-14', joursDeRegles: 5, duree: 31, joursDouleurForte: [1, 2, 16] },
        { debut: '2026-07-14', joursDeRegles: 4, duree: 31, joursDouleurForte: [] },
        { debut: '2025-12-18', joursDeRegles: 5, duree: 27, joursDouleurForte: [2] },
      ],
      dureeMoyenne: 30, reglesMoyenne: 5, dureeMinimale: 27, dureeMaximale: 31,
    });
    await cyclePage.ouvrir();

    await expect(cyclePage.cycleEnCours).toContainText('Règles · jour 2');
    await expect(cyclePage.cycleEnCours).toContainText('Débutées le lundi 14 septembre');
    await expect(cyclePage.cycleEnCours).toContainText('Noté aujourd\'hui');
    await expect(cyclePage.jour('Lundi 14 septembre')).toHaveAttribute('aria-pressed', 'true');
    await expect(cyclePage.jour('Mardi 15 septembre')).toHaveAccessibleName('Mardi 15 septembre, aujourd\'hui, règles');
    await expect(cyclePage.jour('Mercredi 16 septembre')).toBeDisabled();
    await expect(cyclePage.mesCycles).toContainText('Cycle30 j');
    await expect(cyclePage.mesCycles).toContainText('Règles5 j');
    await expect(cyclePage.mesCycles).toContainText('Écart27–31 j');
    await expect(cyclePage.mesCycles.getByRole('heading', { name: '2026' })).toBeVisible();
    await expect(cyclePage.mesCycles.getByRole('heading', { name: '2025' })).toBeVisible();
    await expect(cyclePage.mesCycles).toContainText('14 août – 13 sept.');
    await expect(cyclePage.mesCycles.getByRole('img', { name: 'Cycle de 31 jours, règles 5 jours, douleur forte 3 jours (jours 1, 2, 16)' })).toBeVisible();
    await expect(cyclePage.mesCycles.getByRole('img', { name: 'Cycle de 31 jours, règles 4 jours' })).toBeVisible();
  });

  test('ajoute puis retire un jour de règles d\'un geste', async ({ cyclePage, serveur }) => {
    const jours = simulerRegles(serveur, ['2026-09-14']);
    await cyclePage.ouvrir();

    await cyclePage.jour('Jeudi 10 septembre').click();
    await expect(cyclePage.jour('Jeudi 10 septembre')).toHaveAttribute('aria-pressed', 'true');
    expect(jours.has('2026-09-10')).toBe(true);

    await cyclePage.jour('Lundi 14 septembre').click();
    await expect(cyclePage.jour('Lundi 14 septembre')).toHaveAttribute('aria-pressed', 'false');
    expect(serveur.appelsVers('DELETE', /^Cycle\/regles\/2026-09-14$/)).toHaveLength(1);
  });

  test('sans règles récentes : « Règles aujourd\'hui » note le jour même', async ({ cyclePage, serveur }) => {
    simulerRegles(serveur);
    await cyclePage.ouvrir();

    await expect(cyclePage.cycleEnCours).toContainText('Pas de règles récentes');
    await expect(cyclePage.mesCycles).toContainText('dès que deux débuts de règles seront notés');
    await cyclePage.cycleEnCours.getByRole('button', { name: 'Règles aujourd\'hui' }).click();

    expect(serveur.appelsVers('PUT', /^Cycle\/regles\/2026-09-15$/)).toHaveLength(1);
  });

  test('l\'historique se charge par six cycles, la moyenne reste celle des six derniers', async ({ cyclePage, serveur, page }) => {
    const cycles = Array.from({ length: 14 }, (_, i) => ({
      debut: `2026-${String(8 - (i % 8)).padStart(2, '0')}-0${(i % 9) + 1}`, joursDeRegles: 5, duree: 28 + (i % 3), joursDouleurForte: [],
    }));
    simulerRegles(serveur, [], { cycles, dureeMoyenne: 29, reglesMoyenne: 5, dureeMinimale: 28, dureeMaximale: 30 });
    await cyclePage.ouvrir();

    await expect(cyclePage.mesCycles.getByRole('img', { name: /^Cycle de/ })).toHaveCount(6);
    await page.getByRole('button', { name: 'Voir les cycles précédents (8)' }).click();

    await expect(cyclePage.mesCycles.getByRole('img', { name: /^Cycle de/ })).toHaveCount(12);
    await expect(page.getByRole('button', { name: 'Voir les cycles précédents (2)' })).toBeVisible();
    expect(serveur.appelsVers('GET', /^Cycle$/).at(-1)!.parametres.get('cycles')).toBe('12');
    await expect(cyclePage.mesCycles).toContainText('Cycle29 j');
  });

  test('le titre du mois permet de revenir loin en arrière d\'un coup', async ({ cyclePage, serveur, page }) => {
    simulerRegles(serveur, ['2025-03-10']);
    await cyclePage.ouvrir();

    await page.getByRole('button', { name: /Septembre 2026, choisir un autre mois/ }).click();
    await cyclePage.panneau.getByRole('button', { name: 'Année précédente' }).click();
    await expect(cyclePage.panneau.getByRole('button', { name: 'oct.' })).toBeEnabled();
    await cyclePage.panneau.getByRole('button', { name: 'mars' }).click();

    await expect(page.getByRole('button', { name: /Mars 2025, choisir un autre mois/ })).toBeVisible();
    await expect(cyclePage.jour('Lundi 10 mars')).toHaveAttribute('aria-pressed', 'true');
    expect(serveur.appelsVers('GET', /^Cycle$/).at(-1)!.parametres.get('mois')).toBe('2025-03-01');
  });

  test('précise le flux et les caillots d\'un jour noté, enregistrés dès le geste', async ({ cyclePage, serveur }) => {
    const details = new Map();
    simulerRegles(serveur, ['2026-09-14', '2026-09-15'], {}, details);
    await cyclePage.ouvrir();

    // Le dernier jour noté est proposé d'office ; rien n'est précisé par défaut.
    await expect(cyclePage.detailDuJour).toContainText('Mardi 15 septembre');
    await expect(cyclePage.detailDuJour.getByRole('button', { name: 'Moyen' })).toHaveAttribute('aria-pressed', 'false');

    await cyclePage.detailDuJour.getByRole('button', { name: 'Moyen' }).click();
    await expect(cyclePage.detailDuJour.getByRole('button', { name: 'Moyen' })).toHaveAttribute('aria-pressed', 'true');
    await expect(cyclePage.jour('Mardi 15 septembre')).toHaveAccessibleName('Mardi 15 septembre, aujourd\'hui, règles, flux moyen');
    expect(serveur.appelsVers('PUT', /^Cycle\/regles\/2026-09-15\/details$/).at(-1)!.corps).toEqual({ flux: 'Moyen', caillots: null });

    await cyclePage.detailDuJour.getByRole('button', { name: 'Oui' }).click();
    expect(serveur.appelsVers('PUT', /^Cycle\/regles\/2026-09-15\/details$/).at(-1)!.corps).toEqual({ flux: 'Moyen', caillots: true });

    // Re-toucher un choix le retire : le flux revient à « non précisé », les caillots restent.
    await cyclePage.detailDuJour.getByRole('button', { name: 'Moyen' }).click();
    expect(serveur.appelsVers('PUT', /^Cycle\/regles\/2026-09-15\/details$/).at(-1)!.corps).toEqual({ flux: null, caillots: true });
    await expect(cyclePage.jour('Mardi 15 septembre')).toHaveAccessibleName('Mardi 15 septembre, aujourd\'hui, règles');
  });

  test('plusieurs jours notés : on choisit celui dont on précise le flux', async ({ cyclePage, serveur }) => {
    const details = new Map([['2026-09-14', { flux: 'Abondant' as const, caillots: false }]]);
    simulerRegles(serveur, ['2026-09-14', '2026-09-15'], {}, details);
    await cyclePage.ouvrir();
    await expect(cyclePage.jour('Lundi 14 septembre')).toHaveAccessibleName('Lundi 14 septembre, règles, flux abondant');

    await cyclePage.detailDuJour.getByRole('button', { name: 'lundi 14 septembre' }).click();

    await expect(cyclePage.detailDuJour).toContainText('Lundi 14 septembre');
    await expect(cyclePage.detailDuJour.getByRole('button', { name: 'Abondant' })).toHaveAttribute('aria-pressed', 'true');
    await expect(cyclePage.detailDuJour.getByRole('button', { name: 'Non' })).toHaveAttribute('aria-pressed', 'true');
  });

  test('un nouveau jour noté devient le jour à préciser ; sans jour noté, la carte disparaît', async ({ cyclePage, serveur }) => {
    simulerRegles(serveur);
    await cyclePage.ouvrir();
    await expect(cyclePage.detailDuJour).toHaveCount(0);

    await cyclePage.jour('Jeudi 10 septembre').click();
    await expect(cyclePage.detailDuJour).toContainText('Jeudi 10 septembre');

    await cyclePage.jour('Jeudi 10 septembre').click();
    await expect(cyclePage.detailDuJour).toHaveCount(0);
  });

  test('les séries de choix sont nommées pour un lecteur d\'écran', async ({ cyclePage, serveur, page }) => {
    simulerRegles(serveur, ['2026-09-15']);
    await cyclePage.ouvrir();

    await expect(cyclePage.detailDuJour.getByRole('group', { name: 'Quel flux ?' })).toBeVisible();
    await expect(cyclePage.detailDuJour.getByRole('group', { name: 'Des caillots ?' })).toBeVisible();
    await expect(page.getByText('Repère du carnet de suivi des HUG')).toBeVisible();
  });

  test('un échec d\'enregistrement du flux remet le choix dans son état', async ({ cyclePage, serveur, page }) => {
    simulerRegles(serveur, ['2026-09-15']);
    serveur.on('PUT', /^Cycle\/regles\/[\d-]+\/details$/, () => ({ status: 500 }));
    await cyclePage.ouvrir();

    await cyclePage.detailDuJour.getByRole('button', { name: 'Léger' }).click();

    await expect(page.getByText('Le jour n\'a pas pu être enregistré. Réessaie dans un instant.').first()).toBeVisible();
    await expect(cyclePage.detailDuJour.getByRole('button', { name: 'Léger' })).toHaveAttribute('aria-pressed', 'false');
  });

  test('un échec d\'enregistrement remet le jour dans son état', async ({ cyclePage, serveur, page }) => {
    simulerRegles(serveur);
    serveur.on('PUT', /^Cycle\/regles\//, () => ({ status: 500 }));
    await cyclePage.ouvrir();

    await cyclePage.jour('Jeudi 10 septembre').click();

    await expect(page.getByText('Le jour n\'a pas pu être enregistré. Réessaie dans un instant.').first()).toBeVisible();
    await expect(cyclePage.jour('Jeudi 10 septembre')).toHaveAttribute('aria-pressed', 'false');
  });
});

test.describe('Cycle · symptômes', () => {
  test('liste le mois par jour, sans l\'acné', async ({ cyclePage, serveur, page }) => {
    simulerSymptomes(serveur, [
      { typeSymptome: 'Fatigue', intensite: 6, date: '2026-09-15T14:10:00', commentaire: 'après le déjeuner' },
      { typeSymptome: 'Nausée', intensite: 3, date: '2026-09-15T08:30:00' },
      { typeSymptome: 'Spotting', intensite: 2, date: '2026-09-14T19:00:00' },
      { typeSymptome: 'Acné', intensite: 4, date: '2026-09-14T09:00:00' },
    ]);
    await cyclePage.ouvrir('?onglet=symptomes');

    await expect(cyclePage.onglet('Symptômes')).toHaveAttribute('aria-selected', 'true');
    await expect(page.getByRole('region', { name: 'Ce mois-ci' })).toContainText('3symptômes notés');
    await expect(page.getByRole('region', { name: 'Ce mois-ci' })).toContainText('2jours concernés');
    await expect(page.getByRole('heading', { name: 'Mardi 15 septembre' })).toBeVisible();
    await expect(cyclePage.entree('Fatigue.*14 h 10 · après le déjeuner')).toBeVisible();
    await expect(cyclePage.entree('Acné')).toHaveCount(0);
    // L'acné a sa page : seul un raccourci y mène, et l'onglet n'existe plus.
    await expect(page.getByRole('link', { name: /Suivi de l'acné/ })).toBeVisible();
    await expect(page.getByRole('tab')).toHaveCount(2);
  });

  test('note un symptôme maintenant, à l\'heure locale', async ({ cyclePage, serveur, page }) => {
    simulerSymptomes(serveur);
    await cyclePage.ouvrir('?onglet=symptomes');

    await cyclePage.ouvrirAjout();
    await expect(cyclePage.panneau.getByRole('button', { name: 'Enregistrer' })).toBeDisabled();
    await cyclePage.panneau.getByRole('button', { name: 'Fatigue' }).click();
    await cyclePage.panneau.getByRole('button', { name: 'Intensité 7 sur 10' }).click();
    await cyclePage.panneau.getByRole('button', { name: 'Enregistrer' }).click();

    await expect(cyclePage.panneau).toBeHidden();
    await expect(page.getByText('Symptôme noté', { exact: true })).toBeVisible();
    await expect(cyclePage.entree('Fatigue.*10 h 00')).toBeVisible();
    expect(champs(serveur.appelsVers('POST', /^SymptomesCycle$/)[0].corps)).toMatchObject({
      typeSymptome: 'Fatigue', intensite: '7', date: '2026-09-15T10:00:00', carnetSanteId: '1', commentaire: '',
    });
  });

  test('modifie puis supprime un symptôme depuis sa ligne', async ({ cyclePage, serveur }) => {
    const symptomes = simulerSymptomes(serveur, [{ typeSymptome: 'Nausée', intensite: 3, date: '2026-09-14T08:30:00' }]);
    await cyclePage.ouvrir('?onglet=symptomes');

    await cyclePage.entree('Nausée').click();
    await cyclePage.panneau.getByRole('button', { name: 'Intensité 5 sur 10' }).click();
    await cyclePage.panneau.getByRole('button', { name: 'Enregistrer' }).click();
    await expect(cyclePage.panneau).toBeHidden();
    expect(symptomes[0]).toMatchObject({ intensite: 5, date: '2026-09-14T08:30:00' });

    await cyclePage.entree('Nausée').click();
    await cyclePage.panneau.getByRole('button', { name: 'Supprimer ce symptôme' }).click();
    await cyclePage.panneau.getByRole('button', { name: 'Confirmer la suppression' }).click();
    await expect(cyclePage.entree('Nausée')).toHaveCount(0);
    expect(symptomes).toHaveLength(0);
  });

  test('ouvre la saisie depuis la tuile « Symptôme » de l\'accueil', async ({ cyclePage, serveur }) => {
    simulerSymptomes(serveur);
    await cyclePage.ouvrir('?onglet=symptomes&ajouter');

    await expect(cyclePage.panneau.getByRole('heading', { name: 'Noter un symptôme' })).toBeVisible();
  });
});
