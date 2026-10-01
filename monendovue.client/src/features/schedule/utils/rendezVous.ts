import { differenceInCalendarDays, endOfWeek, format, parseISO } from 'date-fns';
import { fr } from 'date-fns/locale';
import type { EvenementAgenda } from '../types/agenda';

export type CleGroupe = 'aujourdhui' | 'demain' | 'semaine' | 'plus-tard';

export interface GroupeJour {
    cle: CleGroupe;
    titre: string;
    evenements: EvenementAgenda[];
}

/** Début de l'événement en heure locale ; un événement sur la journée entière n'a qu'une date (AAAA-MM-JJ). */
export const debutDe = (evenement: EvenementAgenda): Date =>
    evenement.journeeEntiere ? parseISO(evenement.debut) : new Date(evenement.debut);

const premierDuMois = (date: Date) => (date.getDate() === 1 ? '1er' : String(date.getDate()));

/** « jeudi 1er octobre ». */
export const jourComplet = (date: Date): string => `${format(date, 'EEEE', { locale: fr })} ${premierDuMois(date)} ${format(date, 'MMMM', { locale: fr })}`;

/** « 10 h 00 », ou « Journée » pour un événement sur la journée entière. */
export const heureDe = (evenement: EvenementAgenda): string =>
    (evenement.journeeEntiere ? 'Journée' : format(debutDe(evenement), "H'\u00a0h\u00a0'mm"));

/** « lun. 5 » (« mar. 14 oct. » au-delà de la semaine) : le jour d'un événement dont le groupe ne le dit pas déjà. */
export const jourCourt = (evenement: EvenementAgenda, avecMois: boolean): string =>
    format(debutDe(evenement), avecMois ? 'EEE d MMM' : 'EEE d', { locale: fr });

/** Nombre de jours calendaires entre `maintenant` et l'événement (0 : aujourd'hui). */
export const joursAvant = (evenement: EvenementAgenda, maintenant: Date): number =>
    differenceInCalendarDays(debutDe(evenement), maintenant);

/** « Dans 13 jours » à partir de deux jours, sinon rien (« aujourd'hui » et « demain » sont déjà dits par le groupe). */
export const dansNJours = (evenement: EvenementAgenda, maintenant: Date): string | null => {
    const jours = joursAvant(evenement, maintenant);
    return jours >= 2 ? `Dans ${jours} jours` : null;
};

/**
 * Événements à venir regroupés comme on les lit : aujourd'hui, demain, le reste de la semaine (jusqu'au dimanche), plus tard.
 * Un groupe vide n'est pas listé ; chaque groupe est trié par heure.
 */
export function grouperParJour(evenements: EvenementAgenda[], maintenant: Date): GroupeJour[] {
    const finDeSemaine = endOfWeek(maintenant, { weekStartsOn: 1 });
    const tries = [...evenements].sort((a, b) => debutDe(a).getTime() - debutDe(b).getTime());
    const groupes: GroupeJour[] = [
        { cle: 'aujourdhui', titre: `Aujourd'hui · ${jourComplet(maintenant)}`, evenements: [] },
        { cle: 'demain', titre: '', evenements: [] },
        { cle: 'semaine', titre: 'Cette semaine', evenements: [] },
        { cle: 'plus-tard', titre: 'Plus tard', evenements: [] },
    ];
    for (const evenement of tries) {
        const jours = joursAvant(evenement, maintenant);
        let cle: CleGroupe = 'plus-tard';
        if (jours <= 0) cle = 'aujourdhui';
        else if (jours === 1) cle = 'demain';
        else if (debutDe(evenement) <= finDeSemaine) cle = 'semaine';
        groupes.find((groupe) => groupe.cle === cle)!.evenements.push(evenement);
    }
    const demain = groupes[1];
    if (demain.evenements.length > 0) demain.titre = `Demain · ${jourComplet(debutDe(demain.evenements[0]))}`;
    return groupes.filter((groupe) => groupe.evenements.length > 0);
}

/** Vrai pour un événement à heure fixe qui n'a pas encore commencé : seul cas où « Préparer ce rendez-vous » a un sens. */
export const estAVenir = (evenement: EvenementAgenda, maintenant: Date): boolean =>
    !evenement.journeeEntiere && debutDe(evenement) > maintenant;
