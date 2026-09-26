import type { CodeEmotion } from '@/features/bilan-quotidien/types/bilan-quotidien';

export type Tonalite = 'agreable' | 'neutre' | 'difficile';

export interface PresentationEmotion {
  code: CodeEmotion;
  libelle: string;
  emoji: string;
  tonalite: Exclude<Tonalite, 'neutre'>;
}

/** Nombre maximal d'émotions par bilan (même règle côté serveur, BilanHumeurValidator). */
export const EMOTIONS_MAX = 3;

/** Longueur maximale des notes personnelles (même règle côté serveur). */
export const COMMENTAIRE_MAX = 1000;

/** Émotions proposées, dans l'ordre d'affichage ; les codes sont ceux de l'enum Emotion côté serveur. */
export const emotions: PresentationEmotion[] = [
  { code: 'Joie', libelle: 'Joie', emoji: '😊', tonalite: 'agreable' },
  { code: 'Calme', libelle: 'Calme', emoji: '😌', tonalite: 'agreable' },
  { code: 'Soulagement', libelle: 'Soulagement', emoji: '😮‍💨', tonalite: 'agreable' },
  { code: 'Motivation', libelle: 'Motivation', emoji: '💪', tonalite: 'agreable' },
  { code: 'Fierte', libelle: 'Fierté', emoji: '🌟', tonalite: 'agreable' },
  { code: 'Tristesse', libelle: 'Tristesse', emoji: '😢', tonalite: 'difficile' },
  { code: 'Anxiete', libelle: 'Anxiété', emoji: '😰', tonalite: 'difficile' },
  { code: 'Irritabilite', libelle: 'Irritabilité', emoji: '😠', tonalite: 'difficile' },
  { code: 'Frustration', libelle: 'Frustration', emoji: '😤', tonalite: 'difficile' },
  { code: 'Decouragement', libelle: 'Découragement', emoji: '😞', tonalite: 'difficile' },
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
