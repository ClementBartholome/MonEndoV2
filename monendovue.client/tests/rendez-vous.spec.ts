import { expect, test } from './fixtures';
import { simulerSynthese, syntheseRealiste } from './mocks/synthese';

// Aujourd'hui (MAINTENANT) : mardi 15 septembre 2026.
test.describe('Préparer un rendez-vous', () => {
  test('crée le PDF des trois derniers mois, avec les rubriques proposées', async ({ rendezVousPage, serveur }, testInfo) => {
    simulerSynthese(serveur);
    await rendezVousPage.ouvrir();

    await expect(rendezVousPage.choixPeriode('3 mois')).toHaveAttribute('aria-pressed', 'true');
    await expect(rendezVousPage.periode).toContainText('Du 16 juin au 15 septembre 2026');
    await expect(rendezVousPage.rubrique('Douleurs')).toBeChecked();
    await expect(rendezVousPage.rubrique('Activité physique')).not.toBeChecked();

    await rendezVousPage.rubrique('Activité physique').check();
    await rendezVousPage.questions.fill('Les douleurs pendant les règles augmentent-elles ?');
    const telechargement = await rendezVousPage.creer();

    expect(telechargement.suggestedFilename()).toBe('monendo-synthese-2026-06-16-2026-09-15.pdf');
    // Gardé avec les résultats du test : le document se relit à la main (mise en page, lisibilité).
    await telechargement.saveAs(process.env.PDF_SORTIE ?? testInfo.outputPath('synthese.pdf'));
    const demandes = serveur.appelsVers('GET', /^Synthese$/).map((a) => a.parametres.toString());
    expect(demandes).toEqual(['du=2026-06-16&au=2026-09-15']);
    await expect(rendezVousPage.page.getByText('PDF créé', { exact: true })).toBeVisible();
  });

  test('un mois, ou une période libre d\'un an au plus', async ({ rendezVousPage, serveur }) => {
    simulerSynthese(serveur);
    await rendezVousPage.ouvrir();

    await rendezVousPage.choixPeriode('1 mois').click();
    await expect(rendezVousPage.periode).toContainText('Du 16 août au 15 septembre 2026');

    await rendezVousPage.choixPeriode('Autre').click();
    await rendezVousPage.periode.getByLabel('Du').fill('2025-06-01');
    await expect(rendezVousPage.periode.getByRole('alert')).toHaveText('La période ne peut pas dépasser un an.');
    await expect(rendezVousPage.boutonCreer).toBeDisabled();

    await rendezVousPage.periode.getByLabel('Du').fill('2026-01-01');
    await rendezVousPage.periode.getByLabel('Au').fill('2026-03-31');
    await expect(rendezVousPage.periode).toContainText('Du 1 janvier au 31 mars 2026');
    await rendezVousPage.creer();
    // L'aperçu suit la période choisie ; le PDF reprend la synthèse de la dernière, sans la redemander.
    const demandes = serveur.appelsVers('GET', /^Synthese$/).map((a) => a.parametres.toString());
    expect(demandes.at(-1)).toBe('du=2026-01-01&au=2026-03-31');
  });

  test('demande au moins une rubrique', async ({ rendezVousPage, serveur }) => {
    simulerSynthese(serveur);
    await rendezVousPage.ouvrir();

    for (const titre of ['Douleurs', 'Cycle', 'Traitements', 'Bilans quotidiens']) await rendezVousPage.rubrique(titre).uncheck();

    await expect(rendezVousPage.rubriques.getByRole('alert')).toHaveText('Choisis au moins une rubrique.');
    await expect(rendezVousPage.boutonCreer).toBeDisabled();
  });

  test('garde les questions sur l\'appareil d\'une visite à l\'autre', async ({ rendezVousPage, serveur }) => {
    simulerSynthese(serveur);
    await rendezVousPage.ouvrir();
    await rendezVousPage.questions.fill('Faut-il refaire une IRM ?');

    await rendezVousPage.page.reload();

    await expect(rendezVousPage.questions).toHaveValue('Faut-il refaire une IRM ?');
  });

  test('« En un coup d\'œil » : frise et chiffres de la période, avant de créer le PDF', async ({ rendezVousPage, serveur }) => {
    simulerSynthese(serveur);
    await rendezVousPage.ouvrir();

    const apercu = rendezVousPage.page.getByRole('region', { name: 'En un coup d\'œil' });
    await expect(apercu).toContainText('jours avec une douleur notée');
    await expect(apercu).toContainText('/ 92');
    // Frise de toute la période, lisible jour par jour.
    await expect(apercu.getByRole('listitem')).toHaveCount(92);

    await rendezVousPage.choixPeriode('1 mois').click();
    await expect(apercu.getByRole('listitem')).toHaveCount(31);
    await expect(apercu).toContainText('/ 31');
  });

  test('les chiffres de douleur comptent aussi la douleur des bilans', async ({ rendezVousPage, serveur }) => {
    // Aucune entrée de douleur : seulement des bilans (7/10 pendant les règles, 6/10, 2/10, et 0/10 qui ne compte pas).
    simulerSynthese(serveur, (du, au) => {
      const synthese = syntheseRealiste(du, au);
      const modele = synthese.bilans.jours[0];
      synthese.douleurs = { ...synthese.douleurs, jours: 0, joursDouleurForte: 0, joursDouleurFortePendantRegles: 0, parType: [], entrees: [] };
      synthese.regles = { ...synthese.regles, jours: ['2026-09-01'] };
      synthese.bilans.jours = [
        { ...modele, jour: '2026-09-01', douleur: 7 },
        { ...modele, jour: '2026-09-05', douleur: 6 },
        { ...modele, jour: '2026-09-08', douleur: 2 },
        { ...modele, jour: '2026-09-10', douleur: 0 },
      ];
      return synthese;
    });
    await rendezVousPage.ouvrir();

    const apercu = rendezVousPage.page.getByRole('region', { name: 'En un coup d\'œil' });
    await expect(apercu.getByRole('definition').nth(0)).toHaveText('3 / 92');
    await expect(apercu.getByRole('definition').nth(1)).toHaveText('2');
    await expect(apercu).toContainText('jours à 6/10 ou plus, dont 1 pendant les règles');
  });

  test('une période invalide retire l\'aperçu', async ({ rendezVousPage, serveur }) => {
    simulerSynthese(serveur);
    await rendezVousPage.ouvrir();
    const apercu = rendezVousPage.page.getByRole('region', { name: 'En un coup d\'œil' });
    await expect(apercu.getByRole('listitem')).toHaveCount(92);

    await rendezVousPage.choixPeriode('Autre').click();
    await rendezVousPage.periode.getByLabel('Du').fill('2025-06-01');

    await expect(apercu.getByRole('listitem')).toHaveCount(0);
    await expect(apercu.getByRole('definition')).toHaveCount(0);
  });

  test('l\'aperçu indisponible n\'empêche pas de créer le PDF', async ({ rendezVousPage, serveur }) => {
    serveur.on('GET', /^Synthese$/, () => ({ status: 503 }));
    await rendezVousPage.ouvrir();

    await expect(rendezVousPage.page.getByText('L\'aperçu n\'a pas pu être chargé')).toBeVisible();
    await expect(rendezVousPage.boutonCreer).toBeEnabled();
  });

  test('signale un PDF qui n\'a pas pu être créé', async ({ rendezVousPage, serveur }) => {
    serveur.on('GET', /^Synthese$/, () => ({ status: 500 }));
    await rendezVousPage.ouvrir();

    await rendezVousPage.boutonCreer.click();

    await expect(rendezVousPage.page.getByRole('alert').filter({ hasText: 'Le PDF n\'a pas pu être créé' })).toBeVisible();
    await expect(rendezVousPage.boutonCreer).toBeEnabled();
  });
});
