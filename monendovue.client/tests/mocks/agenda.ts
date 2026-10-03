import type { CalendrierAgenda, EvenementAgenda } from '../../src/features/schedule/types/agenda';
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

export function rendezVous(id: string, titre: string, debut: string, extra: Partial<EvenementAgenda> = {}): EvenementAgenda {
  return { id, titre, debut, fin: null, journeeEntiere: false, lieu: null, lien: `https://calendar.example/${id}`, ...extra };
}

/** Rendez-vous de la semaine du 15 septembre 2026 (jour fixé des tests) : demain, plus tard dans la semaine, plus tard. */
export const KINE = rendezVous('kine', 'Kinésithérapie', '2026-09-16T10:00:00+02:00', { lieu: 'Cabinet Lefèvre, Lyon 3e' });
export const ECHO = rendezVous('echo', 'Échographie pelvienne', '2026-09-18T09:15:00+02:00', { lieu: "Centre d'imagerie du Parc" });
export const GYNECO = rendezVous('gyneco', 'Consultation gynécologie', '2026-10-06T14:30:00+02:00', { lieu: 'Dr Martin, Lyon 6e' });
export const PRECEDENT = rendezVous('precedent', 'Consultation précédente', '2026-06-12T11:00:00+02:00');

/**
 * Agenda lié (calendrier choisi) avec ses rendez-vous : `Agenda/evenements` ne renvoie que ceux de la période demandée,
 * `Agenda/precedent` le dernier commencé avant la date (`precedent`, ou aucun).
 */
export function simulerRendezVous(
  serveur: FauxServeur,
  evenements: EvenementAgenda[],
  { precedent = null }: { precedent?: EvenementAgenda | null } = {},
) {
  simulerLiaisonAgenda(serveur, { liee: true, calendrierId: 'rdv@example.com' });
  serveur
    .on('GET', /^Agenda\/evenements$/, ({ url }) => {
      const debut = new Date(url.searchParams.get('debut') ?? '').getTime();
      const fin = new Date(url.searchParams.get('fin') ?? '').getTime();
      // Comme le serveur : une période de plus de 62 jours (en UTC, donc changement d'heure compris) est refusée.
      if (fin <= debut || fin - debut > 62 * 24 * 3600 * 1000) return { status: 400, body: { message: 'La période demandée est invalide.' } };
      return { body: evenements.filter((e) => new Date(e.debut).getTime() >= debut && new Date(e.debut).getTime() < fin) };
    })
    .on('GET', /^Agenda\/precedent$/, () => ({ body: precedent ? [precedent] : [] }));
}
