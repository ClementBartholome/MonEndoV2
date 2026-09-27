import type { BilanQuotidien } from '@/features/bilan-quotidien/types/bilan-quotidien';

/** Réponse de GET BilanQuotidien/periode (HistoriqueBilansViewModel côté serveur). */
export interface HistoriqueBilans {
  bilans: BilanQuotidien[];
  /** Jours de règles de la période, au format yyyy-MM-dd. */
  joursRegles: string[];
}

export type ModePeriode = 'semaine' | 'mois';

/** Indicateur qui colore les pastilles du calendrier. */
export type IndicateurCalendrier = 'douleur' | 'emotions';

export interface PeriodeHistorique {
  mode: ModePeriode;
  debut: Date;
  fin: Date;
  /** « Septembre 2026 », « 21 - 27 septembre 2026 »… */
  libelle: string;
  contientAujourdhui: boolean;
  /** Faux quand la période suivante est entièrement à venir. */
  suivantePossible: boolean;
}

export interface JourHistorique {
  date: Date;
  /** yyyy-MM-dd */
  cle: string;
  bilan?: BilanQuotidien;
  regles: boolean;
  aVenir: boolean;
}

/** État de l'historique : une seule période pilote le calendrier, le détail, les courbes et l'analyse. */
export interface HistoriqueModel {
  periode: PeriodeHistorique;
  jours: JourHistorique[];
  /** Bilans de la période, triés par date. */
  bilans: BilanQuotidien[];
  jourSelectionne: Date;
  chargement: boolean;
  erreur: boolean;
}

export interface HistoriqueActions {
  changerMode: (mode: ModePeriode) => void;
  precedente: () => void;
  suivante: () => void;
  revenirAujourdhui: () => void;
  selectionnerJour: (date: Date) => void;
  recharger: () => Promise<void>;
}
