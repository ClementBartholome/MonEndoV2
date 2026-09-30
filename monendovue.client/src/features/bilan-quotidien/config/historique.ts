import { scoreHumeur } from '@/features/bilan-quotidien/utils/humeur';
import { stressDuBilan } from '@/features/bilan-quotidien/utils/mesures';
import type { BilanQuotidien } from '@/features/bilan-quotidien/types/bilan-quotidien';
import type { IndicateurCalendrier } from '@/features/bilan-quotidien/types/historique';
import { niveauDouleur, niveauHumeur } from '@/features/bilan-quotidien/utils/historique';

export interface EchelleCalendrier {
  libelle: string;
  /** Valeur de l'indicateur pour un bilan, ou null s'il n'est pas renseigné. */
  valeur: (bilan: BilanQuotidien) => number | null;
  niveau: (valeur: number) => number;
  /** Description lue par les lecteurs d'écran (« douleur 4 sur 10 »). */
  description: (valeur: number) => string;
  /** Lu quand le bilan existe sans cette valeur. */
  nonRenseigne: string;
  /** Classes de fond et de texte par niveau (0 à 4), une seule teinte : pas de code couleur « bon / mauvais ». */
  classes: string[];
  /** Libellés des deux extrémités de l'échelle, pour la légende. */
  bornes: [string, string];
}

const LIBELLES_EMOTIONS = ['difficiles', 'plutôt difficiles', 'partagées', 'plutôt agréables', 'agréables'];

/** Indicateurs proposés pour colorer les pastilles du calendrier. */
export const echellesCalendrier: Record<IndicateurCalendrier, EchelleCalendrier> = {
  douleur: {
    libelle: 'Douleur',
    valeur: (bilan) => bilan.douleurMoyenne,
    niveau: niveauDouleur,
    description: (valeur) => `douleur ${valeur} sur 10`,
    nonRenseigne: 'douleur non renseignée',
    // Même échelle que la page Douleurs (tokens --intensite-N) : texte foncé jusqu'à 5, blanc au-delà.
    classes: [
      'bg-intensite-1 text-texte',
      'bg-intensite-3 text-texte',
      'bg-intensite-5 text-texte',
      'bg-intensite-7 text-sur-fonce',
      'bg-intensite-9 text-sur-fonce',
    ],
    bornes: ['0', '10'],
  },
  emotions: {
    libelle: 'Émotions',
    valeur: scoreHumeur,
    // Comme pour la douleur, plus c'est foncé, plus la journée a été lourde : foncé = émotions difficiles.
    // Les anciens bilans comptent par leur humeur (Positive, Neutre, Négative).
    niveau: (valeur) => 4 - niveauHumeur(valeur),
    description: (valeur) => `émotions ${LIBELLES_EMOTIONS[niveauHumeur(valeur)]}`,
    nonRenseigne: 'émotions non renseignées',
    classes: [
      'bg-emotion-0 text-texte',
      'bg-emotion-1 text-texte',
      'bg-emotion-2 text-texte',
      'bg-emotion-3 text-sur-fonce',
      'bg-emotion-4 text-sur-fonce',
    ],
    bornes: ['agréables', 'difficiles'],
  },
};

/**
 * Un graphique par indicateur (petits multiples), chacun sur sa propre échelle et dans la teinte de l'indicateur
 * (`config/teintes.ts`) : plus la barre est haute, plus la journée a été lourde. Un jour non renseigné n'a pas de barre.
 */
export interface IndicateurGraphique {
  cle: string;
  titre: string;
  max: number;
  /** « /10 », « /5 » : pour la moyenne affichée à côté du titre. */
  unite: string;
  valeur: (bilan: BilanQuotidien) => number | null;
}

export const indicateursGraphiques: IndicateurGraphique[] = [
  { cle: 'douleur', titre: 'Douleur', max: 10, unite: '/10', valeur: (b) => b.douleurMoyenne },
  { cle: 'fatigue', titre: 'Fatigue', max: 5, unite: '/5', valeur: (b) => b.fatigue },
  { cle: 'stress', titre: 'Stress', max: 5, unite: '/5', valeur: stressDuBilan },
  {
    // Tonalité des émotions retournée (0 agréables, 5 difficiles) pour que « plus haut = plus lourd » vaille partout.
    cle: 'emotions', titre: 'Émotions difficiles', max: 5, unite: '/5', valeur: (b) => {
      const score = scoreHumeur(b);
      return score === null ? null : Math.round((1 - score) * 50) / 10;
    },
  },
];
