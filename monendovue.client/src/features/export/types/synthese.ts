import type { Traitement } from '@/features/medicament/types/traitements';

/** Miroir de `SyntheseRendezVousViewModel` (`GET Synthese`). Jours au format « AAAA-MM-JJ ». */
export interface SyntheseRendezVous {
    du: string;
    au: string;
    regles: SyntheseRegles;
    douleurs: SyntheseDouleurs;
    symptomes: SyntheseSymptomes;
    traitements: SyntheseTraitement[];
    bilans: SyntheseBilans;
    activite: SyntheseActivite;
    /** Ancien suivi du transit par événements. */
    transit: { jour: string; type: string }[];
}

export interface SyntheseRegles {
    jours: string[];
    /** Premiers jours des règles commencées dans la période. */
    debuts: string[];
    /** null avant deux cycles terminés dans la période. */
    cycleMoyen: number | null;
    reglesMoyenne: number | null;
    flux: SyntheseFlux;
}

/** Jours de règles de la période par flux noté ; `nonPrecise` = jours sans flux précisé. Caillots : jours « oui » et « non » (le reste n'est pas précisé). */
export interface SyntheseFlux {
    traces: number;
    leger: number;
    moyen: number;
    abondant: number;
    nonPrecise: number;
    joursAvecCaillots: number;
    joursSansCaillots: number;
}

export interface EntreeIntensite {
    jour: string;
    type: string;
    intensite: number;
}

export interface SyntheseDouleurs {
    jours: number;
    /** Jours avec une douleur à 6/10 ou plus. */
    joursDouleurForte: number;
    joursDouleurFortePendantRegles: number;
    parType: { type: string; jours: number; intensiteMoyenne: number; intensiteMax: number; joursPendantRegles: number }[];
    entrees: EntreeIntensite[];
}

export interface SyntheseSymptomes {
    parType: { type: string; jours: number; intensiteMoyenne: number; joursPendantRegles: number }[];
    entrees: EntreeIntensite[];
}

export interface SyntheseTraitement {
    traitement: Traitement;
    enCours: boolean;
    /** Prises prévues dans la période (0 pour « au besoin » et les soins). */
    prevues: number;
    /** Prises faites, ou séances pour un soin. */
    faites: number;
    ignorees: number;
    jours: string[];
}

export interface BilanDuJour {
    jour: string;
    douleur: number;
    fatigue: number | null;
    stress: number | null;
    selles: boolean | null;
    typeBristol: number | null;
    ballonnements: boolean | null;
    crampes: boolean | null;
    notes: string | null;
}

export interface SyntheseBilans {
    nombre: number;
    douleurMoyenne: number | null;
    fatigueMoyenne: number | null;
    stressMoyen: number | null;
    joursBallonnements: number;
    joursCrampes: number;
    emotions: { emotion: string; jours: number }[];
    jours: BilanDuJour[];
}

export interface SyntheseActivite {
    seances: number;
    minutes: number;
    soulagee: number;
    pareille: number;
    plusForte: number;
    parType: { type: string; seances: number; minutes: number }[];
    /** Niveau : 1 douce, 2 modérée, 3 soutenue. */
    entrees: { jour: string; type: string; niveau: number }[];
}

/** Rubriques que l'utilisatrice choisit d'inclure dans le PDF. */
export type Rubrique = 'douleurs' | 'cycle' | 'traitements' | 'bilans' | 'activite' | 'transit';

export interface OptionsPdf {
    rubriques: Record<Rubrique, boolean>;
    /** Texte libre, en première page ; vide = pas de bloc. */
    questions: string;
    /** Jour de création, affiché sur le document. */
    creeLe: Date;
}
