import type { BilanQuotidien } from '@/features/bilan-quotidien/types/bilan-quotidien';

/** Moyenne des seules valeurs renseignées (null si aucune) : une mesure non saisie ne compte jamais pour 0. */
export const moyenne = (valeurs: (number | null | undefined)[]): number | null => {
  const renseignees = valeurs.filter((v): v is number => typeof v === 'number');
  return renseignees.length === 0 ? null : renseignees.reduce((somme, v) => somme + v, 0) / renseignees.length;
};

/** Moyenne d'une mesure sur plusieurs bilans, en ignorant les jours où elle n'a pas été renseignée. */
export const moyenneDesBilans = (
  bilans: BilanQuotidien[],
  mesure: (bilan: BilanQuotidien) => number | null | undefined,
): number | null => moyenne(bilans.map(mesure));

/** Stress du jour : moyenne du stress pro et perso renseignés, ou null. */
export const stressDuBilan = (bilan: Pick<BilanQuotidien, 'stressPro' | 'stressPerso'>): number | null =>
  moyenne([bilan.stressPro, bilan.stressPerso]);
