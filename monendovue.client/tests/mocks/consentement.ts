import type { Page } from '@playwright/test';
import { CARNET_ID, UTILISATRICE } from './session';
import type { FauxServeur } from './faux-serveur';

const JETON_LOINTAIN = new Date(2099, 0, 1).toISOString();

/**
 * Routes simulées des comptes (`AccountController`) : inscription (202, sans session : l'adresse doit être confirmée), confirmation
 * de l'adresse, renvoi du lien, connexion, et accord d'un compte existant.
 */
export function simulerConsentement(serveur: FauxServeur, { surInvitation = false }: { surInvitation?: boolean } = {}) {
  serveur
    .on('GET', /^Account\/inscription$/, () => ({ body: { surInvitation } }))
    .on('POST', /^Account\/register$/, ({ corps }) => (corps?.consentementDonneesSante
      ? { status: 202, body: { confirmationRequise: true } }
      : { status: 400, body: { $values: ['Ton accord est nécessaire pour créer un compte.'] } }))
    .on('POST', /^Account\/confirmer-adresse$/, () => ({ status: 204 }))
    .on('POST', /^Account\/renvoyer-confirmation$/, () => ({ status: 202 }))
    .on('POST', /^Account\/login$/, ({ corps }) => ({
      body: { userName: corps.email, carnetSanteId: CARNET_ID, tokenExpiry: JETON_LOINTAIN, consentementAJour: true },
    }))
    .on('POST', /^Account\/consentement$/, () => ({ body: { tokenExpiry: JETON_LOINTAIN, consentementAJour: true } }))
    // Notifications non configurées : l'étape « rappel » qui suit l'inscription se passe d'elle-même.
    .on('GET', /^Notifications\/cle-publique$/, () => ({ status: 404 }));
}

/** Refus de l'API pour un compte sans consentement à jour (`ExigeConsentementFilter`). */
export const REFUS_CONSENTEMENT = { status: 403, body: { code: 'consentement-requis', message: 'Accord nécessaire.' } };

/** Session ouverte par un compte existant : sans consentement enregistré, ou d'avant la 1.3.0 (champ absent). */
export async function ouvrirSessionSansConsentement(page: Page, champ: 'refuse' | 'absent') {
  const utilisatrice = champ === 'refuse' ? { ...UTILISATRICE, consentementAJour: false } : UTILISATRICE;
  await page.addInitScript((u) => localStorage.setItem('user', JSON.stringify(u)), utilisatrice);
}
