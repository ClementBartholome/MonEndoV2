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
