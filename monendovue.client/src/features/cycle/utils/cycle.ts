import { addDays, format, getDaysInMonth } from 'date-fns';
import { fr } from 'date-fns/locale';

export interface CaseCalendrier {
    /** AAAA-MM-JJ */
    cle: string;
    numero: number;
    regles: boolean;
    aujourdhui: boolean;
    /** Un jour à venir ne peut pas être noté. */
    futur: boolean;
}

/** Cases du mois, semaines commençant le lundi : `decalage` cases vides avant le 1er. */
export function calendrierDuMois(mois: Date, joursDeRegles: string[], maintenant: Date): { decalage: number; cases: CaseCalendrier[] } {
    const regles = new Set(joursDeRegles);
    const aujourdhui = format(maintenant, 'yyyy-MM-dd');
    const cases = Array.from({ length: getDaysInMonth(mois) }, (_, index) => {
        const cle = format(new Date(mois.getFullYear(), mois.getMonth(), index + 1), 'yyyy-MM-dd');
        return { cle, numero: index + 1, regles: regles.has(cle), aujourdhui: cle === aujourdhui, futur: cle > aujourdhui };
    });
    // getDay : 0 = dimanche ; lundi en premier.
    const decalage = (new Date(mois.getFullYear(), mois.getMonth(), 1).getDay() + 6) % 7;
    return { decalage, cases };
}

/** « lundi 14 septembre » */
export function jourEnToutesLettres(cle: string): string {
    return format(new Date(`${cle}T12:00:00`), 'EEEE d MMMM', { locale: fr });
}

/** « 14 août » */
export function jourCourt(cle: string): string {
    return format(new Date(`${cle}T12:00:00`), 'd MMMM', { locale: fr });
}

/** « 14 août – 13 sept. » : du premier jour des règles à la veille des suivantes. */
export function periodeDuCycle(debut: string, duree: number): string {
    const premier = new Date(`${debut}T12:00:00`);
    const dernier = addDays(premier, duree - 1);
    const court = (date: Date) => format(date, 'd MMM', { locale: fr });
    return `${court(premier)} – ${court(dernier)}`;
}

/** Cycles regroupés par année de leur début, dans l'ordre reçu (du plus récent au plus ancien). */
export function grouperParAnnee<T extends { debut: string }>(cycles: T[]): { annee: string; cycles: T[] }[] {
    const groupes: { annee: string; cycles: T[] }[] = [];
    for (const cycle of cycles) {
        const annee = cycle.debut.slice(0, 4);
        const dernier = groupes[groupes.length - 1];
        if (dernier?.annee === annee) dernier.cycles.push(cycle);
        else groupes.push({ annee, cycles: [cycle] });
    }
    return groupes;
}

export function jours(nombre: number): string {
    return `${nombre} ${nombre > 1 ? 'jours' : 'jour'}`;
}
