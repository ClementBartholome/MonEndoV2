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
  { id: 'notes', titre: 'Notes', icone: 'edit_note', detail: 'Un texte libre', parDefaut: true },
];

/**
 * Ancienne catégorie « Rapports » (1.5.0 à 1.7.0), plus proposée depuis la 1.8.0 : « Où as-tu eu mal ? » note la douleur pendant un
 * rapport, avec son intensité. Les réponses déjà données restent dans le détail d'un jour, seulement si elle était activée sur l'appareil.
 */
export const ancienneCategorieRapports: PresentationCategorie =
  { id: 'rapports', titre: 'Rapports', icone: 'favorite', detail: 'Douleur pendant ou après un rapport', parDefaut: false };

/** Catégories dont le détail d'un jour récapitule les réponses : celles du bilan, puis l'ancienne catégorie « Rapports ». */
export const categoriesRecapitulees: PresentationCategorie[] = [...categoriesDuBilan, ancienneCategorieRapports];

export type ChoixCategories = Record<CategorieBilan, boolean>;

export const choixParDefaut = (): ChoixCategories =>
  Object.fromEntries(categoriesRecapitulees.map((c) => [c.id, c.parDefaut])) as ChoixCategories;
