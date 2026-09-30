import type { CodeEmotion } from '@/features/bilan-quotidien/types/bilan-quotidien';
import type { PrisePrevue, TraitementAuBesoin } from '@/features/medicament/types/traitements';

/** Miroir de `AujourdhuiViewModel` (GET Accueil/aujourdhui) ; les listes arrivent sous `$values`, voir `enTableau`. */
export interface Aujourdhui {
    cycle: CycleAujourdhui;
    /** Null tant que le bilan du jour n'est pas rempli. */
    bilan: BilanAujourdhui | null;
    /** Prises prévues aujourd'hui, dans l'ordre des horaires. */
    prisesPrevues: PrisePrevue[];
    /** Traitements « au besoin » en cours. */
    auBesoin: TraitementAuBesoin[];
    semaine: Semaine;
}

export interface CycleAujourdhui {
    enRegles: boolean;
    jourDeRegles: number | null;
    jourDuCycle: number | null;
}

export interface BilanAujourdhui {
    douleurMoyenne: number;
    emotions: CodeEmotion[];
    fatigue: number | null;
}

export interface Semaine {
    joursAvecDouleur: number;
    joursAvecDouleurPendantRegles: number;
    fatigueMoyenne: number | null;
    fatigueMoyennePrecedente: number | null;
}
