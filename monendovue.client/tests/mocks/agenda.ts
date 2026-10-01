import type { CalendrierAgenda } from '../../src/features/schedule/types/agenda';
import type { FauxServeur } from './faux-serveur';

/** Adresse d'autorisation telle que la renvoie le serveur (le vrai flux OAuth n'est pas joué : Google est hors du test). */
export const URL_GOOGLE = 'https://accounts.google.com/o/oauth2/v2/auth?client_id=test&code_challenge=abc&code_challenge_method=S256';

export const CALENDRIERS: CalendrierAgenda[] = [
  { id: 'moi@example.com', nom: 'moi@example.com', principal: true },
  { id: 'rdv@example.com', nom: 'Rendez-vous médicaux', principal: false },
];

interface Options {
  disponible?: boolean;
  liee?: boolean;
  /** Calendrier déjà choisi (agenda lié). */
  calendrierId?: string | null;
  urlDemarrage?: string;
}

/**
 * Routes simulées de la liaison de l'agenda : le statut est l'état du test (un choix de calendrier y est enregistré, la
 * déliaison le fait retomber à « non liée »). `urlDemarrage` permet de simuler une adresse inattendue.
 */
export function simulerLiaisonAgenda(
  serveur: FauxServeur,
  { disponible = true, liee = false, calendrierId = null, urlDemarrage = URL_GOOGLE }: Options = {},
) {
  let etat = { disponible, liee, lieeLe: liee ? '2026-09-20T09:00:00Z' : null as string | null, calendrierId };
  serveur
    .on('GET', /^Agenda\/liaison$/, () => ({ body: etat }))
    .on('POST', /^Agenda\/liaison\/demarrer$/, () => ({ body: { url: urlDemarrage } }))
    .on('GET', /^Agenda\/calendriers$/, () => ({ body: CALENDRIERS }))
    .on('PUT', /^Agenda\/calendrier$/, ({ corps }) => {
      etat = { ...etat, calendrierId: corps.id };
      return { status: 204 };
    })
    .on('DELETE', /^Agenda\/liaison$/, () => {
      etat = { ...etat, liee: false, lieeLe: null, calendrierId: null };
      return { status: 204 };
    });
}
