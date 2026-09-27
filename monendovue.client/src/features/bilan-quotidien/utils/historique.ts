import {
  addDays, differenceInCalendarDays, endOfMonth, endOfWeek, format, isAfter, isSameMonth, isWithinInterval,
  startOfDay, startOfMonth, startOfWeek,
} from 'date-fns';
import { fr } from 'date-fns/locale';
import type { ModePeriode } from '@/features/bilan-quotidien/types/historique';

const LUNDI = { weekStartsOn: 1 } as const;

/** Clé d'un jour calendaire (yyyy-MM-dd), en heure locale. */
export const cleJour = (date: Date): string => format(date, 'yyyy-MM-dd');

/** Premier et dernier jour de la semaine (lundi-dimanche) ou du mois contenant `ancre`. */
export const intervalleDe = (mode: ModePeriode, ancre: Date): { debut: Date; fin: Date } =>
  mode === 'mois'
    ? { debut: startOfMonth(ancre), fin: startOfDay(endOfMonth(ancre)) }
    : { debut: startOfWeek(ancre, LUNDI), fin: startOfDay(endOfWeek(ancre, LUNDI)) };

export const joursEntre = (debut: Date, fin: Date): Date[] =>
  Array.from({ length: differenceInCalendarDays(fin, debut) + 1 }, (_, i) => addDays(debut, i));

export const contient = (debut: Date, fin: Date, date: Date): boolean =>
  isWithinInterval(startOfDay(date), { start: debut, end: fin });

export const libellePeriode = (mode: ModePeriode, debut: Date, fin: Date): string => {
  if (mode === 'mois') return format(debut, 'MMMM yyyy', { locale: fr });
  if (isSameMonth(debut, fin)) return `${format(debut, 'd')} - ${format(fin, 'd MMM yyyy', { locale: fr })}`;
  return `${format(debut, 'd MMM', { locale: fr })} - ${format(fin, 'd MMM yyyy', { locale: fr })}`;
};

/** Jour à sélectionner après un changement de période : aujourd'hui s'il en fait partie, sinon son dernier jour passé. */
export const jourParDefaut = (debut: Date, fin: Date, aujourdhui: Date): Date => {
  const jour = startOfDay(aujourdhui);
  if (contient(debut, fin, jour)) return jour;
  return isAfter(debut, jour) ? debut : fin;
};

/** Cases vides avant le premier jour d'un mois dans une grille commençant le lundi. */
export const decalageLundi = (date: Date): number => (date.getDay() + 6) % 7;

/**
 * Niveau (0 à 4) d'une douleur de 0 à 10, pour une échelle de couleur à une seule teinte :
 * 0-1, 2-3, 4-5, 6-7, 8-10.
 */
export const niveauDouleur = (douleur: number): number => Math.min(Math.floor(Math.max(douleur, 0) / 2), 4);

/** Niveau (0 à 4) d'une humeur de 0 (difficile) à 1 (agréable). */
export const niveauHumeur = (score: number): number => Math.min(Math.round(Math.max(score, 0) * 4), 4);
