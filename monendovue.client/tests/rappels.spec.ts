import { expect, test } from './fixtures';
import { simulerNavigateurPush, simulerNotifications } from './mocks/onboarding';

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
});
