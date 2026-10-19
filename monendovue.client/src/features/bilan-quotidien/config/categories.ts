/** Catégories facultatives du bilan quotidien (douleur, émotions, fatigue et stress sont toujours là). */
export type CategorieBilan = 'corps' | 'urinaire' | 'saignements' | 'nuit-journee' | 'rapports' | 'notes';

export interface PresentationCategorie {
  id: CategorieBilan;
  titre: string;
  icone: string;
  detail: string;
  /** Affichée tant que la personne n'a rien choisi. */
  parDefaut: boolean;
}

/**
 * Ordre d'affichage dans le bilan et dans Paramètres. Ajouter une catégorie à questions = une entrée ici et son groupe dans
 * `config/questions.ts` (`SaisieBilan.vue` les affiche, résume et récapitule seules, derrière `estActive`).
 */
export const categoriesDuBilan: PresentationCategorie[] = [
  { id: 'corps', titre: 'Corps', icone: 'accessibility_new', detail: 'Transit, alimentation, pas, hydratation', parDefaut: true },
  { id: 'urinaire', titre: 'Urinaire', icone: 'urology', detail: 'Douleur en urinant, envies, sang visible', parDefaut: true },
  { id: 'saignements', titre: 'Saignements hors règles', icone: 'bloodtype', detail: 'Traces ou saignements entre les règles', parDefaut: true },
  { id: 'nuit-journee', titre: 'Nuit et journée', icone: 'bed', detail: 'Sommeil, journée limitée, absences', parDefaut: true },
  { id: 'rapports', titre: 'Rapports', icone: 'favorite', detail: 'Douleur pendant ou après un rapport. Discrète, désactivée par défaut.', parDefaut: false },
  { id: 'notes', titre: 'Notes', icone: 'edit_note', detail: 'Un texte libre', parDefaut: true },
];

export type ChoixCategories = Record<CategorieBilan, boolean>;

export const choixParDefaut = (): ChoixCategories =>
  Object.fromEntries(categoriesDuBilan.map((c) => [c.id, c.parDefaut])) as ChoixCategories;
