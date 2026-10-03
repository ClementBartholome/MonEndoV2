import { test, expect } from './fixtures';
import { ECHO, GYNECO, KINE, PRECEDENT, simulerLiaisonAgenda, simulerRendezVous } from './mocks/agenda';

test.describe('Agenda', () => {
  test('la rubrique se trouve dans la navigation', async ({ navigationPage, serveur, page }) => {
    simulerRendezVous(serveur, [KINE]);
    await page.goto('/');

    await navigationPage.ouvrirRubrique('Agenda');

    await expect(page).toHaveURL(/\/agenda$/);
    await expect(page.getByRole('heading', { level: 1, name: 'Agenda' })).toBeVisible();
  });

  test('les rendez-vous à venir sont regroupés par jour, la liste est la vue par défaut', async ({ serveur, page }) => {
    simulerRendezVous(serveur, [GYNECO, KINE, ECHO]);

    await page.goto('/agenda');

    await expect(page.getByRole('button', { name: 'Liste' })).toHaveAttribute('aria-pressed', 'true');
    // textContent : le style met ces titres en capitales, que innerText restituerait.
    const titres = await page.getByRole('heading', { level: 2 }).evaluateAll((titres) => titres.map((titre) => titre.textContent));
    expect(titres).toEqual(['Demain · mercredi 16 septembre', 'Cette semaine', 'Plus tard']);
    await expect(page.getByRole('region', { name: /^Demain/ })).toContainText('Kinésithérapie');
    await expect(page.getByRole('region', { name: /^Demain/ })).toContainText('10 h 00');
    await expect(page.getByRole('region', { name: 'Cette semaine' })).toContainText('Échographie pelvienne');
    await expect(page.getByRole('region', { name: 'Cette semaine' })).toContainText('ven. 18');
    await expect(page.getByRole('region', { name: 'Plus tard' })).toContainText('Consultation gynécologie');
    await expect(page.getByRole('region', { name: 'Plus tard' })).toContainText('Dr Martin, Lyon 6e');
  });

  test('seul le prochain rendez-vous propose « Préparer » en ligne, avec le nombre de jours', async ({ serveur, page }) => {
    simulerRendezVous(serveur, [GYNECO, KINE]);

    await page.goto('/agenda');

    await expect(page.getByRole('button', { name: 'Préparer ce rendez-vous' })).toHaveCount(1);
    await expect(page.getByRole('region', { name: /^Demain/ }).getByRole('button', { name: 'Préparer ce rendez-vous' })).toBeVisible();
  });

  test('un rendez-vous s\'ouvre dans un panneau : lieu, itinéraire, lien vers Google Agenda', async ({ serveur, page }) => {
    simulerRendezVous(serveur, [GYNECO]);
    await page.goto('/agenda');

    await page.getByRole('button', { name: /Consultation gynécologie/ }).click();

    const panneau = page.getByRole('dialog', { name: 'Consultation gynécologie' });
    await expect(panneau).toContainText('Mardi 6 octobre 2026 · 14 h 30');
    await expect(panneau).toContainText('Dr Martin, Lyon 6e');
    await expect(panneau.getByRole('link', { name: 'Itinéraire' })).toHaveAttribute('href', /destination=Dr%20Martin/);
    await expect(panneau.getByRole('link', { name: 'Ouvrir dans Google Agenda' })).toHaveAttribute('href', 'https://calendar.example/gyneco');
    await expect(panneau.getByRole('link', { name: 'Ouvrir dans Google Agenda' })).toHaveAttribute('rel', /noopener/);
    await panneau.getByRole('button', { name: 'Fermer' }).click();
    await expect(panneau).toBeHidden();
  });

  test('« Préparer ce rendez-vous » ouvre l\'export depuis le rendez-vous précédent, sans rien mettre dans l\'adresse', async ({ serveur, page }) => {
    simulerRendezVous(serveur, [GYNECO], { precedent: PRECEDENT });
    await page.goto('/agenda');

    await page.getByRole('button', { name: /Consultation gynécologie/ }).click();
    await page.getByRole('dialog', { name: 'Consultation gynécologie' }).getByRole('button', { name: 'Préparer ce rendez-vous' }).click();

    await expect(page).toHaveURL(/\/export$/);
    expect(page.url()).not.toContain('gyn');
    expect(serveur.appelsVers('GET', /^Agenda\/precedent$/)[0].parametres.get('avant')).toBe('2026-10-06T14:30:00+02:00');
    await expect(page.getByText('Pour ton rendez-vous du 6 octobre à 14 h 30')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Depuis le dernier RDV' })).toHaveAttribute('aria-pressed', 'true');
    await expect(page.getByText(/^Du 12 juin au /)).toBeVisible();
  });

  test('sans rendez-vous précédent, l\'export garde sa période habituelle', async ({ serveur, page }) => {
    simulerRendezVous(serveur, [GYNECO]);
    await page.goto('/agenda');

    await page.getByRole('button', { name: 'Préparer ce rendez-vous' }).click();

    await expect(page).toHaveURL(/\/export$/);
    await expect(page.getByText('Pour ton rendez-vous du 6 octobre à 14 h 30')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Depuis le dernier RDV' })).toHaveCount(0);
    await expect(page.getByRole('button', { name: '3 mois' })).toHaveAttribute('aria-pressed', 'true');
  });

  test('la vue Mois montre les jours avec rendez-vous et la liste du jour choisi', async ({ serveur, page }) => {
    simulerRendezVous(serveur, [KINE, ECHO, GYNECO]);
    await page.goto('/agenda');

    await page.getByRole('button', { name: 'Mois' }).click();

    await expect(page.getByRole('heading', { name: 'Septembre 2026' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'mercredi 16 septembre, un rendez-vous' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'jeudi 17 septembre', exact: true })).toBeVisible();
    await page.getByRole('button', { name: 'vendredi 18 septembre, un rendez-vous' }).click();
    await expect(page.getByRole('region', { name: 'Vendredi 18 septembre' })).toContainText('Échographie pelvienne');

    await page.getByRole('button', { name: 'Mois suivant' }).click();
    await expect(page.getByRole('heading', { name: 'Octobre 2026' })).toBeVisible();
    await page.getByRole('button', { name: 'mardi 6 octobre, un rendez-vous' }).click();
    await expect(page.getByRole('region', { name: 'Mardi 6 octobre' })).toContainText('Consultation gynécologie');
  });

  test('un rendez-vous passé s\'ouvre sans « Préparer »', async ({ serveur, page }) => {
    simulerRendezVous(serveur, [PRECEDENT]);
    await page.goto('/agenda');
    await page.getByRole('button', { name: 'Mois' }).click();
    for (let i = 0; i < 3; i++) await page.getByRole('button', { name: 'Mois précédent' }).click();

    await page.getByRole('button', { name: 'vendredi 12 juin, un rendez-vous' }).click();
    await page.getByRole('button', { name: /Consultation précédente/ }).click();

    const panneau = page.getByRole('dialog', { name: 'Consultation précédente' });
    await expect(panneau).toBeVisible();
    await expect(panneau.getByRole('button', { name: 'Préparer ce rendez-vous' })).toHaveCount(0);
  });

  test.describe('états', () => {
    test('agenda non lié : invite à le lier depuis les Paramètres', async ({ serveur, page }) => {
      simulerLiaisonAgenda(serveur);

      await page.goto('/agenda');

      await expect(page.getByRole('heading', { name: "Ton agenda n'est pas lié" })).toBeVisible();
      await page.getByRole('link', { name: 'Lier mon agenda Google' }).click();
      await expect(page).toHaveURL(/\/parametres$/);
    });

    test('liaison pas encore ouverte sur MonEndo', async ({ serveur, page }) => {
      simulerLiaisonAgenda(serveur, { disponible: false });

      await page.goto('/agenda');

      await expect(page.getByRole('heading', { name: "La liaison avec Google n'est pas disponible" })).toBeVisible();
      await expect(page.getByRole('link', { name: 'Lier mon agenda Google' })).toHaveCount(0);
    });

    test('agenda lié sans calendrier choisi : invite à en choisir un', async ({ serveur, page }) => {
      simulerLiaisonAgenda(serveur, { liee: true, calendrierId: null });

      await page.goto('/agenda');

      await expect(page.getByRole('heading', { name: 'Choisis le calendrier à afficher' })).toBeVisible();
      await expect(page.getByRole('link', { name: 'Choisir un calendrier' })).toHaveAttribute('href', '/parametres');
    });

    test('aucun rendez-vous à venir', async ({ serveur, page }) => {
      simulerRendezVous(serveur, []);

      await page.goto('/agenda');

      await expect(page.getByRole('heading', { name: 'Aucun rendez-vous à venir' })).toBeVisible();
    });

    test('agenda indisponible : message rassurant, puis « Réessayer » rétablit la liste', async ({ serveur, page }) => {
      simulerRendezVous(serveur, [KINE]);
      let panne = true;
      serveur.on('GET', /^Agenda\/evenements$/, ({ url }) => {
        if (panne) return { status: 503 };
        const debut = new Date(url.searchParams.get('debut') ?? '').getTime();
        return { body: new Date(KINE.debut).getTime() >= debut ? [KINE] : [] };
      });

      await page.goto('/agenda');

      await expect(page.getByRole('alert')).toContainText("L'agenda est momentanément indisponible");
      panne = false;
      await page.getByRole('button', { name: 'Réessayer' }).click();
      await expect(page.getByText('Kinésithérapie')).toBeVisible();
    });
  });
});
