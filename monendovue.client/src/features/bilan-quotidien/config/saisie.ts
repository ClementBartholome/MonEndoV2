/** Paramètres de l'écran de saisie du bilan (mêmes bornes que BilanMesuresValidator côté serveur). */

export const DOULEUR_MAX = 10;
export const ECHELLE_MAX = 5;
export const PAS_MAX = 100_000;
export const HYDRATATION_MAX = 10;

export interface Prereglage {
  valeur: number;
  libelle: string;
  /** Précision affichée sous le libellé (plage représentée). */
  detail?: string;
}

/** Un préréglage enregistre une valeur représentative de sa plage ; la saisie libre reste possible. */
export const prereglagesPas: Prereglage[] = [
  { valeur: 2000, libelle: 'Peu', detail: '< 3 000' },
  { valeur: 5000, libelle: 'Moyen', detail: '3 000 – 7 000' },
  { valeur: 8000, libelle: 'Beaucoup', detail: '> 7 000' },
];

export const prereglagesHydratation: Prereglage[] = [
  { valeur: 0.5, libelle: '0,5 L' },
  { valeur: 1, libelle: '1 L' },
  { valeur: 1.5, libelle: '1,5 L' },
  { valeur: 2, libelle: '2 L +' },
];

export type ConsommationAlimentaire = 'gluten' | 'lactose' | 'grignotage';

export const consommationsAlimentaires: { cle: ConsommationAlimentaire; libelle: string; icone: string }[] = [
  { cle: 'gluten', libelle: 'Gluten', icone: 'bakery_dining' },
  { cle: 'lactose', libelle: 'Lactose', icone: 'icecream' },
  { cle: 'grignotage', libelle: 'Grignotage', icone: 'cookie' },
];

/** Repères affichés aux extrémités des échelles de pastilles. */
export const reperesEchelles = {
  douleur: { min: 'Aucune', max: 'Maximale' },
  fatigue: { min: 'En forme', max: 'Épuisée' },
  stress: { min: 'Aucun', max: 'Maximal' },
} as const;
