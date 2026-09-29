import { format } from 'date-fns';
import { fr } from 'date-fns/locale';

/** Jour calendaire local (AAAA-MM-JJ) d'une date reçue de l'API (sans fuseau) ou d'une Date locale. */
export function cleJour(date: string | Date): string {
    return typeof date === 'string' ? date.slice(0, 10) : format(date, 'yyyy-MM-dd');
}

/** « 8 h 40 » à partir d'une date locale sans fuseau. */
export function heure(date: string | Date): string {
    const texte = typeof date === 'string' ? date.slice(11, 16) : format(date, 'HH:mm');
    const [heures, minutes] = texte.split(':');
    return `${Number(heures)} h ${minutes}`;
}

/** « Mardi 15 septembre » à partir d'une clé AAAA-MM-JJ. */
export function titreDuJour(cle: string): string {
    const titre = format(new Date(`${cle}T12:00:00`), 'EEEE d MMMM', { locale: fr });
    return titre.charAt(0).toUpperCase() + titre.slice(1);
}

export interface GroupeDuJour<T> {
    cle: string;
    /** « Mardi 15 septembre » */
    titre: string;
    entrees: T[];
}

/** Entrées regroupées par jour, du plus récent au plus ancien, et par heure décroissante dans la journée. */
export function grouperParJour<T extends { date: string }>(entrees: T[]): GroupeDuJour<T>[] {
    const groupes = new Map<string, T[]>();
    [...entrees]
        .sort((a, b) => String(b.date).localeCompare(String(a.date)))
        .forEach((e) => {
            const cle = cleJour(e.date);
            groupes.set(cle, [...(groupes.get(cle) ?? []), e]);
        });
    return [...groupes.entries()].map(([cle, duJour]) => ({ cle, titre: titreDuJour(cle), entrees: duJour }));
}
