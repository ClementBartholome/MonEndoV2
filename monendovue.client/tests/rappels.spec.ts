import { expect, test } from './fixtures';
import { simulerNavigateurPush, simulerNotifications } from './mocks/onboarding';
import { simulerLiaisonAgenda } from './mocks/agenda';

test.describe('Paramètres · rappels', () => {
  test('le rappel de rendez-vous se règle comme les autres et annonce une notification sans titre ni lieu', async ({ parametresPage, page, serveur }) => {
    simulerNotifications(serveur);
    serveur.on('PUT', /^Notifications\/rappels\/RendezVous$/, () => ({ status: 204 }));
    await simulerNavigateurPush(page, 'granted');
    await parametresPage.ouvrir();

    await expect(parametresPage.ligne('Notifications')).toContainText('rendez-vous');
    await parametresPage.ligne('Notifications').click();
    const panneau = parametresPage.panneau('Notifications');
    await panneau.getByRole('button', { name: 'Activer les notifications sur cet appareil' }).click();

    await expect(panneau.getByText('Rappel de rendez-vous')).toBeVisible();
    await expect(panneau).toContainText('seulement si un rendez-vous est prévu le lendemain');
    await expect(panneau).toContainText('ni le titre ni le lieu');
    await panneau.getByRole('switch').nth(2).click();

    expect(serveur.appelsVers('PUT', /^Notifications\/rappels\/RendezVous$/).at(-1)?.corps).toMatchObject({ actif: true });
  });

  test('sans épisode d\'acné en cours, le réglage dit que le rappel attend et mène à la page Acné', async ({ parametresPage, page, serveur }) => {
    simulerNotifications(serveur);
    serveur.on('GET', /^Notifications\/rappels$/, () => ({
      body: [{
        type: 'SuiviAcne', estHebdomadaire: true, actif: true, heure: '20:00', jourSemaine: 0, fuseauHoraire: 'Europe/Paris',
        enAttente: 'Aucun épisode d\'acné en cours : le rappel partira dès que tu en noteras un.',
      }],
    }));
    await simulerNavigateurPush(page, 'granted');
    await parametresPage.ouvrir();
    await parametresPage.ligne('Notifications').click();
    const panneau = parametresPage.panneau('Notifications');
    await panneau.getByRole('button', { name: 'Activer les notifications sur cet appareil' }).click();

    await expect(panneau).toContainText('Aucun épisode d\'acné en cours : le rappel partira dès que tu en noteras un.');
    await expect(panneau.getByRole('link', { name: 'Ouvrir la page Acné' })).toHaveAttribute('href', '/acne');
  });

  test('sans agenda lié, le rappel de rendez-vous le dit et son lien ouvre le réglage de l\'agenda', async ({ parametresPage, page, serveur }) => {
    simulerNotifications(serveur);
    simulerLiaisonAgenda(serveur);
    serveur.on('GET', /^Notifications\/rappels$/, () => ({
      body: [{
        type: 'RendezVous', estHebdomadaire: false, actif: true, heure: '19:00', jourSemaine: null, fuseauHoraire: 'Europe/Paris',
        enAttente: 'Ton agenda Google n\'est pas lié : le rappel partira dès qu\'il le sera.',
      }],
    }));
    await simulerNavigateurPush(page, 'granted');
    await parametresPage.ouvrir();
    await parametresPage.ligne('Notifications').click();
    const panneau = parametresPage.panneau('Notifications');
    await panneau.getByRole('button', { name: 'Activer les notifications sur cet appareil' }).click();

    await expect(panneau).toContainText('Ton agenda Google n\'est pas lié : le rappel partira dès qu\'il le sera.');
    await panneau.getByRole('link', { name: 'Régler mon agenda Google' }).click();

    await expect(parametresPage.panneau('Agenda Google').getByRole('button', { name: 'Lier mon agenda Google' })).toBeVisible();
    await expect(page).toHaveURL(/\/parametres$/);
  });
});
