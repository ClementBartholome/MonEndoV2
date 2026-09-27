/** Sens d'évolution par rapport à la période précédente, sans connotation bonne ou mauvaise. */
export type Evolution = 'hausse' | 'baisse' | 'stable';

export interface TendanceIndicateur {
  cle: string;
  libelle: string;
  icone: string;
  /** Valeur formatée en français avec son unité (« 4,2/10 »), ou null si rien n'est renseigné sur la période. */
  valeur: string | null;
  /** Écart formaté avec son signe (« −0,8 »), null sans comparaison possible. */
  ecart: string | null;
  evolution: Evolution | null;
  joursRenseignes: number;
}

export interface RepereSuivi {
  cle: string;
  libelle: string;
  icone: string;
  /** « 1,5 L par jour ou plus », « 3/5 ou moins »… */
  repere: string;
  atteints: number;
  renseignes: number;
}

export interface Observation {
  cle: string;
  icone: string;
  texte: string;
  /** Sur quoi repose l'observation (nombre de bilans…). */
  detail: string;
}

export interface Tendances {
  /** Bilans saisis et jours écoulés de la période, bilans de la période précédente. */
  couverture: { bilans: number; jours: number; bilansPrecedents: number };
  indicateurs: TendanceIndicateur[];
  reperes: RepereSuivi[];
  observations: Observation[];
  /** Vrai si au moins un jour de règles est noté sur la période. */
  reglesNotees: boolean;
}
