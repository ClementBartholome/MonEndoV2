import { cleJour } from '@/shared/utils/jours';
import type { SymptomeCycle } from '../types/symptome-cycle';

/** Type enregistré par le suivi de l'acné (onglet dédié, avec photo). */
export const ACNE = 'Acné';

/** Types proposés dans l'onglet Symptômes (valeurs enregistrées) ; l'acné a son propre onglet. */
export const TYPES_SYMPTOME: { valeur: string; icone: string }[] = [
    { valeur: 'Spotting', icone: 'water_drop' },
    { valeur: 'Nausée', icone: 'sick' },
    { valeur: 'Fatigue', icone: 'bedtime' },
    { valeur: 'Autre', icone: 'more_horiz' },
];

export function iconeSymptome(type: string): string {
    if (type === ACNE) return 'face';
    return TYPES_SYMPTOME.find((t) => t.valeur === type)?.icone ?? 'more_horiz';
}

export interface ChiffresSymptomes {
    nombre: number;
    jours: number;
}

export function chiffresDuMois(entrees: SymptomeCycle[]): ChiffresSymptomes {
    return { nombre: entrees.length, jours: new Set(entrees.map((e) => cleJour(e.date))).size };
}

/**
 * Entrée envoyée par l'onglet Acné (`useAcneTracking`) : date « 15/09/2026 » et heure « 08h30 » d'affichage, remises
 * au format de l'API pour le formulaire commun.
 */
export function depuisEntreeAcne(entree: { id: number; typeSymptome: string; date: string; time?: string; intensite: number; commentaire?: string; photoUrl?: string }): SymptomeCycle {
    const [jour, mois, annee] = entree.date.split('/');
    const heure = (entree.time ?? '12:00').replace('h', ':').padStart(5, '0');
    return {
        id: entree.id,
        carnetSanteId: 0,
        typeSymptome: entree.typeSymptome,
        date: `${annee}-${mois}-${jour}T${heure}:00`,
        intensite: entree.intensite,
        commentaire: entree.commentaire || null,
        photoUrl: entree.photoUrl || null,
    };
}
