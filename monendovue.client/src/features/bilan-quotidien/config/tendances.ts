import { scoreHumeur } from '@/features/bilan-quotidien/utils/humeur';
import { stressDuBilan } from '@/features/bilan-quotidien/utils/mesures';
import type { BilanQuotidien } from '@/features/bilan-quotidien/types/bilan-quotidien';
import type { WellbeingGoals } from '@/shared/services/wellbeingGoalsStorage';

export interface ConfigIndicateur {
  cle: string;
  libelle: string;
  icone: string;
  valeur: (bilan: BilanQuotidien) => number | null;
  /** Suffixe collé à la valeur (« /10 », « L »…). */
  unite: string;
  decimales: number;
  /** En dessous de cet écart (en valeur absolue), l'indicateur est « stable ». */
  seuilStable: number;
}

/** Indicateurs suivis dans « Tendances », dans l'ordre d'affichage. */
export const indicateursTendances: ConfigIndicateur[] = [
  { cle: 'douleur', libelle: 'Douleur moyenne', icone: 'sick', valeur: (b) => b.douleurMoyenne, unite: '/10', decimales: 1, seuilStable: 0.5 },
  { cle: 'fatigue', libelle: 'Fatigue moyenne', icone: 'bedtime', valeur: (b) => b.fatigue, unite: '/5', decimales: 1, seuilStable: 0.3 },
  { cle: 'stress', libelle: 'Stress moyen', icone: 'psychology', valeur: stressDuBilan, unite: '/5', decimales: 1, seuilStable: 0.3 },
  {
    // Part moyenne d'émotions agréables par jour ; les anciens bilans comptent par leur humeur.
    cle: 'emotions', libelle: 'Émotions agréables', icone: 'mood',
    valeur: (b) => {
      const score = scoreHumeur(b);
      return score === null ? null : score * 100;
    },
    unite: ' %', decimales: 0, seuilStable: 10,
  },
  { cle: 'pas', libelle: 'Pas par jour', icone: 'footprint', valeur: (b) => b.pas, unite: '', decimales: 0, seuilStable: 500 },
  { cle: 'hydratation', libelle: 'Hydratation', icone: 'water_drop', valeur: (b) => b.hydratation, unite: ' L', decimales: 1, seuilStable: 0.2 },
];

export interface ConfigRepere {
  cle: string;
  libelle: string;
  icone: string;
  valeur: (bilan: BilanQuotidien) => number | null;
  cible: (objectifs: WellbeingGoals) => number;
  /** « min » : repère atteint à partir de la cible ; « max » : jusqu'à la cible. */
  sens: 'min' | 'max';
  unite: string;
  decimales: number;
}

/** Objectifs bien-être (Paramètres) présentés comme des repères personnels. */
export const reperesPersonnels: ConfigRepere[] = [
  { cle: 'hydratation', libelle: 'Hydratation', icone: 'water_drop', valeur: (b) => b.hydratation, cible: (o) => o.hydrationLitersGoal, sens: 'min', unite: ' L', decimales: 1 },
  { cle: 'pas', libelle: 'Pas', icone: 'footprint', valeur: (b) => b.pas, cible: (o) => o.stepsGoal, sens: 'min', unite: ' pas', decimales: 0 },
  { cle: 'stress', libelle: 'Stress', icone: 'psychology', valeur: stressDuBilan, cible: (o) => o.stressMaxGoal, sens: 'max', unite: '/5', decimales: 1 },
  { cle: 'fatigue', libelle: 'Fatigue', icone: 'bedtime', valeur: (b) => b.fatigue, cible: (o) => o.fatigueMaxGoal, sens: 'max', unite: '/5', decimales: 1 },
  { cle: 'douleur', libelle: 'Douleur', icone: 'sick', valeur: (b) => b.douleurMoyenne, cible: (o) => o.painMaxGoal, sens: 'max', unite: '/10', decimales: 0 },
];

/** Douleur à partir de laquelle un jour compte comme « jour de forte douleur » dans les observations. */
export const SEUIL_FORTE_DOULEUR = 6;

/** Nombre minimal de bilans dans chaque groupe (règles / autres jours) pour comparer la douleur. */
export const MIN_BILANS_COMPARAISON = 2;
