import type { CategoriesBilan, CodeEmotion, TransitBilan } from '@/features/bilan-quotidien/types/bilan-quotidien';

/** Bloc « Corps » : facultatif, null = non renseigné. */
export interface CorpsBilan extends TransitBilan {
  pas: number | null;
  hydratation: number | null;
  gluten: boolean;
  lactose: boolean;
  grignotage: boolean;
}

/** État du formulaire de saisie en un écran ; la douleur reste null tant qu'elle n'a pas été choisie. */
export interface FormulaireBilan extends CorpsBilan, CategoriesBilan {
  douleurMoyenne: number | null;
  emotions: CodeEmotion[];
  fatigue: number | null;
  stressPro: number | null;
  stressPerso: number | null;
  commentaire: string;
}

export type ChampManquant = 'douleur' | 'emotions';
