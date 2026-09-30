import { addDays, differenceInCalendarDays, format } from 'date-fns';
import type { CodeEmotion } from '../../src/features/bilan-quotidien/types/bilan-quotidien';
import type { BilanDeTest } from './bilan-quotidien';

/**
 * Jeux de données proches du réel pour les captures et les tests d'affichage : un bilan presque chaque jour, toutes les
 * mesures, des règles tous les ~29 jours avec la douleur qui monte autour, quelques jours oubliés. Tirages pseudo-
 * aléatoires à graine fixe : les données sont les mêmes à chaque exécution.
 */
/** mulberry32 : entiers 32 bits (Math.imul), sans la perte de précision d'un produit en nombre flottant. */
function generateur(graine: number) {
  let etat = graine >>> 0;
  return () => {
    etat = (etat + 0x6d2b79f5) >>> 0;
    let t = etat;
    t = Math.imul(t ^ (t >>> 15), t | 1);
    t ^= t + Math.imul(t ^ (t >>> 7), t | 61);
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}

const AGREABLES: CodeEmotion[] = ['Joie', 'Calme', 'Soulagement', 'Motivation', 'Fierte'];
const DIFFICILES: CodeEmotion[] = ['Tristesse', 'Anxiete', 'Irritabilite', 'Frustration', 'Decouragement'];

export interface PeriodeRealiste {
  bilans: BilanDeTest[];
  joursRegles: string[];
}

/** Du `du` au `au` inclus (AAAA-MM-JJ) ; premier jour de règles `debutRegles`, puis tous les 29 jours (5 jours). */
export function periodeRealiste(du: string, au: string, debutRegles: string, graine = 42): PeriodeRealiste {
  const hasard = generateur(graine);
  const debut = new Date(`${du}T12:00:00`);
  const nombre = differenceInCalendarDays(new Date(`${au}T12:00:00`), debut) + 1;
  const bilans: BilanDeTest[] = [];
  const joursRegles: string[] = [];

  for (let i = 0; i < nombre; i++) {
    const date = addDays(debut, i);
    const jour = format(date, 'yyyy-MM-dd');
    const jourDuCycle = ((differenceInCalendarDays(date, new Date(`${debutRegles}T12:00:00`)) % 29) + 29) % 29;
    const regles = jourDuCycle < 5;
    if (regles) joursRegles.push(jour);
    // Un oubli environ un jour sur huit.
    if (hasard() < 0.12) continue;

    const pic = regles ? 4 - jourDuCycle * 0.6 : jourDuCycle > 26 ? 2 : 0;
    const douleur = Math.max(0, Math.min(10, Math.round(2 + pic + hasard() * 3)));
    const difficile = douleur >= 6 || hasard() < 0.3;
    const emotions = [difficile ? DIFFICILES[Math.floor(hasard() * 5)] : AGREABLES[Math.floor(hasard() * 5)]];
    if (hasard() < 0.4) emotions.push(AGREABLES[Math.floor(hasard() * 5)]);
    bilans.push({
      jour,
      douleurMoyenne: douleur,
      emotions: [...new Set(emotions)],
      fatigue: hasard() < 0.1 ? null : Math.min(5, Math.round(1 + douleur / 3 + hasard() * 1.5)),
      stressPro: hasard() < 0.25 ? null : Math.round(hasard() * 4),
      stressPerso: hasard() < 0.25 ? null : Math.round(hasard() * 3),
      pas: hasard() < 0.2 ? null : Math.round(2500 + hasard() * 7500),
      hydratation: hasard() < 0.2 ? null : Math.round((0.8 + hasard() * 1.4) * 10) / 10,
      gluten: hasard() < 0.3,
      commentaire: hasard() < 0.15 ? 'Journée chargée au travail' : null,
    });
  }
  return { bilans, joursRegles };
}
