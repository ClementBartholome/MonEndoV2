import type { CodeEmotion } from '@/features/bilan-quotidien/types/bilan-quotidien';

export type Tonalite = 'agreable' | 'neutre' | 'difficile';

export interface PresentationEmotion {
  code: CodeEmotion;
  libelle: string;
  tonalite: Exclude<Tonalite, 'neutre'>;
}

/**
 * Pastille de tonalité à côté du libellé d'une émotion (décorative, aria-hidden) : claire pour une émotion agréable,
 * foncée pour une émotion difficile (elles diffèrent par la clarté, pas seulement par la teinte). Pas d'emoji : leur
 * dessin change d'un téléphone à l'autre et leur ton est plus enfantin que le reste de l'app.
 */
export const pastilleTonalite: Record<Exclude<Tonalite, 'neutre'>, string> = {
  agreable: 'bg-emotion-1 ring-1 ring-inset ring-emotion-3',
  difficile: 'bg-emotion-3',
};

/** Nombre maximal d'émotions par bilan (même règle côté serveur, BilanHumeurValidator). */
export const EMOTIONS_MAX = 3;

/** Longueur maximale des notes personnelles (même règle côté serveur). */
export const COMMENTAIRE_MAX = 1000;

/** Émotions proposées, dans l'ordre d'affichage ; les codes sont ceux de l'enum Emotion côté serveur. */
export const emotions: PresentationEmotion[] = [
  { code: 'Joie', libelle: 'Joie', tonalite: 'agreable' },
  { code: 'Calme', libelle: 'Calme', tonalite: 'agreable' },
  { code: 'Soulagement', libelle: 'Soulagement', tonalite: 'agreable' },
  { code: 'Motivation', libelle: 'Motivation', tonalite: 'agreable' },
  { code: 'Fierte', libelle: 'Fierté', tonalite: 'agreable' },
  { code: 'Tristesse', libelle: 'Tristesse', tonalite: 'difficile' },
  { code: 'Anxiete', libelle: 'Anxiété', tonalite: 'difficile' },
  { code: 'Irritabilite', libelle: 'Irritabilité', tonalite: 'difficile' },
  { code: 'Frustration', libelle: 'Frustration', tonalite: 'difficile' },
  { code: 'Decouragement', libelle: 'Découragement', tonalite: 'difficile' },
];

export const presentationEmotion = Object.fromEntries(
  emotions.map((e) => [e.code, e]),
) as Record<CodeEmotion, PresentationEmotion>;

/** Humeur des bilans saisis avant les émotions (valeurs stockées : Heureuse, Neutre, Triste). */
export const anciennesHumeurs: Record<string, { libelle: string; icone: string; tonalite: Tonalite }> = {
  Heureuse: { libelle: 'Positive', icone: 'sentiment_satisfied', tonalite: 'agreable' },
  Neutre: { libelle: 'Neutre', icone: 'sentiment_neutral', tonalite: 'neutre' },
  Triste: { libelle: 'Négative', icone: 'sentiment_dissatisfied', tonalite: 'difficile' },
};
