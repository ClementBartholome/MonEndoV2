/** Catégories facultatives du bilan quotidien (douleur, émotions, fatigue et stress sont toujours là). */
export type CategorieBilan = 'corps' | 'notes';

export interface PresentationCategorie {
  id: CategorieBilan;
  titre: string;
  icone: string;
  detail: string;
  /** Affichée tant que la personne n'a rien choisi. */
  parDefaut: boolean;
}

/**
 * Ordre d'affichage dans le bilan et dans Paramètres. Ajouter une catégorie = une entrée ici, son bloc dans `SaisieBilan.vue`
 * (derrière `estActive`) et son résumé : rien d'autre à toucher.
 */
export const categoriesDuBilan: PresentationCategorie[] = [
  { id: 'corps', titre: 'Corps', icone: 'accessibility_new', detail: 'Transit, alimentation, pas, hydratation', parDefaut: true },
  { id: 'notes', titre: 'Notes', icone: 'edit_note', detail: 'Un texte libre', parDefaut: true },
];

export type ChoixCategories = Record<CategorieBilan, boolean>;

export const choixParDefaut = (): ChoixCategories =>
  Object.fromEntries(categoriesDuBilan.map((c) => [c.id, c.parDefaut])) as ChoixCategories;
