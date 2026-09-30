import { differenceInCalendarDays, subMonths } from 'date-fns';
import type { SymptomeCycle } from '../types/symptome-cycle';
import type { SuiviAcne } from '../types/acne';
import { ACNE } from './symptomes';

export type EcartComparaison = 1 | 3 | 6;

/**
 * Avant / après : la photo la plus récente, et celle la plus proche de « N mois plus tôt ». Aucune comparaison avant
 * deux photos ; si toutes sont récentes, la plus ancienne sert de point de départ.
 */
export function photosAComparer(suivis: SuiviAcne[], ecart: EcartComparaison): { avant: SuiviAcne; apres: SuiviAcne } | null {
    if (suivis.length < 2) return null;
    const [apres, ...anciennes] = suivis;
    const cible = subMonths(new Date(apres.date), ecart).getTime();
    const avant = anciennes.reduce((meilleure, s) =>
        Math.abs(new Date(s.date).getTime() - cible) < Math.abs(new Date(meilleure.date).getTime() - cible) ? s : meilleure);
    return { avant, apres };
}

/** « aujourd'hui », « hier », « il y a 5 jours » */
export function ilYA(date: string, maintenant: Date): string {
    const jours = differenceInCalendarDays(maintenant, new Date(date));
    if (jours <= 0) return 'aujourd\'hui';
    return jours === 1 ? 'hier' : `il y a ${jours} jours`;
}

/** Un suivi photo ouvert dans la saisie commune des symptômes (modification ou suppression). */
export function enSymptome(suivi: SuiviAcne): SymptomeCycle {
    return { ...suivi, carnetSanteId: 0, typeSymptome: ACNE };
}
