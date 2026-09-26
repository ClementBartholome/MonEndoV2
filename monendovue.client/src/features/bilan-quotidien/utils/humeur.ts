import { anciennesHumeurs, emotions, presentationEmotion, type Tonalite } from '@/features/bilan-quotidien/config/emotions';
import type { BilanQuotidien, CodeEmotion } from '@/features/bilan-quotidien/types/bilan-quotidien';

type BilanHumeur = Pick<BilanQuotidien, 'mood' | 'emotions'>;

const VALEUR_TONALITE: Record<Tonalite, number> = { agreable: 1, neutre: 0.5, difficile: 0 };

export const emotionsDuBilan = (bilan: BilanHumeur): CodeEmotion[] =>
  (bilan.emotions ?? []).map((e) => e.emotion).filter((code) => code in presentationEmotion);

/** Tonalités déclarées dans un bilan : une par émotion, ou celle de l'ancienne humeur. */
const tonalitesDuBilan = (bilan: BilanHumeur): Tonalite[] => {
  const codes = emotionsDuBilan(bilan);
  if (codes.length > 0) return codes.map((code) => presentationEmotion[code].tonalite);
  const ancienne = bilan.mood ? anciennesHumeurs[bilan.mood] : undefined;
  return ancienne ? [ancienne.tonalite] : [];
};

/** Humeur d'un bilan entre 0 (difficile) et 1 (agréable), ou null si rien n'est renseigné. */
export const scoreHumeur = (bilan: BilanHumeur): number | null => {
  const tonalites = tonalitesDuBilan(bilan);
  if (tonalites.length === 0) return null;
  return tonalites.reduce((somme, t) => somme + VALEUR_TONALITE[t], 0) / tonalites.length;
};

export type TendanceSemaine = 'agreable' | 'difficile' | 'mitigee';

export interface EmotionSemaine {
  tendance: TendanceSemaine;
  emoji: string;
  libelle: string;
  /** Émotions les plus citées (3 au plus), des plus fréquentes aux moins fréquentes. */
  principales: CodeEmotion[];
  nombreBilans: number;
}

const SEUIL_MAJORITE = 0.6;

const PRESENTATION_TENDANCE: Record<TendanceSemaine, { emoji: string; libelle: string }> = {
  agreable: { emoji: '😊', libelle: 'Semaine plutôt agréable' },
  difficile: { emoji: '😔', libelle: 'Semaine plus difficile' },
  mitigee: { emoji: '😕', libelle: 'Semaine en demi-teinte' },
};

/**
 * Émotion représentative d'une période : majorité (60 % ou plus) d'émotions agréables ou difficiles,
 * sinon « mitigée ». Les anciens bilans comptent par leur humeur (Positive, Neutre, Négative).
 */
export const emotionDeLaSemaine = (bilans: BilanHumeur[]): EmotionSemaine | null => {
  const tonalites = bilans.flatMap(tonalitesDuBilan);
  if (tonalites.length === 0) return null;

  const part = (t: Tonalite) => tonalites.filter((x) => x === t).length / tonalites.length;
  let tendance: TendanceSemaine = 'mitigee';
  if (part('agreable') >= SEUIL_MAJORITE) tendance = 'agreable';
  else if (part('difficile') >= SEUIL_MAJORITE) tendance = 'difficile';

  const frequences = new Map<CodeEmotion, number>();
  bilans.flatMap(emotionsDuBilan).forEach((code) => frequences.set(code, (frequences.get(code) ?? 0) + 1));
  const ordre = emotions.map((e) => e.code);
  const principales = [...frequences.entries()]
    .sort(([a, na], [b, nb]) => nb - na || ordre.indexOf(a) - ordre.indexOf(b))
    .slice(0, 3)
    .map(([code]) => code);

  return {
    tendance,
    ...PRESENTATION_TENDANCE[tendance],
    principales,
    nombreBilans: bilans.filter((b) => tonalitesDuBilan(b).length > 0).length,
  };
};
