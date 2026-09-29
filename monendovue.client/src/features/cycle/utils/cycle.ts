import { format, getDaysInMonth } from 'date-fns';
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

export function jours(nombre: number): string {
    return `${nombre} ${nombre > 1 ? 'jours' : 'jour'}`;
}
