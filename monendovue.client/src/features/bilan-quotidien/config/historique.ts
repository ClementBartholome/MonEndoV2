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
  /** Classes de fond et de texte par niveau (0 à 4), une seule teinte : pas de code couleur « bon / mauvais ». */
  classes: string[];
  /** Libellés des deux extrémités de l'échelle, pour la légende. */
  bornes: [string, string];
}

const LIBELLES_HUMEUR = ['difficile', 'plutôt difficile', 'mitigée', 'plutôt agréable', 'agréable'];

/** Indicateurs proposés pour colorer les pastilles du calendrier. */
export const echellesCalendrier: Record<IndicateurCalendrier, EchelleCalendrier> = {
  douleur: {
    libelle: 'Douleur',
    valeur: (bilan) => bilan.douleurMoyenne,
    niveau: niveauDouleur,
    description: (valeur) => `douleur ${valeur} sur 10`,
    classes: [
      'bg-violet-100 text-violet-950',
      'bg-violet-200 text-violet-950',
      'bg-violet-300 text-violet-950',
      'bg-violet-500 text-white',
      'bg-violet-700 text-white',
    ],
    bornes: ['0', '10'],
  },
  humeur: {
    libelle: 'Humeur',
    valeur: scoreHumeur,
    // Comme pour la douleur, plus c'est foncé, plus la journée a été lourde : foncé = humeur difficile.
    niveau: (valeur) => 4 - niveauHumeur(valeur),
    description: (valeur) => `humeur ${LIBELLES_HUMEUR[niveauHumeur(valeur)]}`,
    classes: [
      'bg-sky-100 text-sky-950',
      'bg-sky-200 text-sky-950',
      'bg-sky-300 text-sky-950',
      'bg-sky-500 text-white',
      'bg-sky-700 text-white',
    ],
    bornes: ['agréable', 'difficile'],
  },
};

export interface SerieCourbe {
  cle: string;
  libelle: string;
  valeur: (bilan: BilanQuotidien) => number | null;
  /** Classes du trait et des points (SVG) et de la pastille de légende. */
  trait: string;
  point: string;
  pastille: string;
}

export interface GraphiqueCourbes {
  titre: string;
  max: number;
  graduations: number[];
  series: SerieCourbe[];
}

/** Deux graphiques, chacun sur sa propre échelle : douleur de 0 à 10, fatigue, stress et humeur de 0 à 5. */
export const graphiquesCourbes: GraphiqueCourbes[] = [
  {
    titre: 'Douleur (0 à 10)',
    max: 10,
    graduations: [0, 5, 10],
    series: [{
      cle: 'douleur', libelle: 'Douleur', valeur: (b) => b.douleurMoyenne,
      trait: 'stroke-violet-600', point: 'fill-violet-600', pastille: 'bg-violet-600',
    }],
  },
  {
    titre: 'Fatigue, stress et humeur (0 à 5)',
    max: 5,
    graduations: [0, 2.5, 5],
    series: [
      {
        cle: 'fatigue', libelle: 'Fatigue', valeur: (b) => b.fatigue,
        trait: 'stroke-emerald-500', point: 'fill-emerald-500', pastille: 'bg-emerald-500',
      },
      {
        cle: 'stress', libelle: 'Stress', valeur: stressDuBilan,
        trait: 'stroke-amber-600', point: 'fill-amber-600', pastille: 'bg-amber-600',
      },
      {
        // Même échelle que le stress et la fatigue ; les anciens bilans comptent par leur humeur.
        cle: 'humeur', libelle: 'Humeur', valeur: (b) => {
          const score = scoreHumeur(b);
          return score === null ? null : score * 5;
        },
        trait: 'stroke-blue-700', point: 'fill-blue-700', pastille: 'bg-blue-700',
      },
    ],
  },
];
