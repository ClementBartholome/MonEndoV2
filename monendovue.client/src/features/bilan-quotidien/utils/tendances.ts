import {
  indicateursTendances, MIN_BILANS_COMPARAISON, reperesPersonnels, SEUIL_FORTE_DOULEUR,
} from '@/features/bilan-quotidien/config/tendances';
import { moyenneDesBilans } from '@/features/bilan-quotidien/utils/mesures';
import type { BilanQuotidien } from '@/features/bilan-quotidien/types/bilan-quotidien';
import type { JourHistorique } from '@/features/bilan-quotidien/types/historique';
import type { Evolution, Observation, RepereSuivi, TendanceIndicateur, Tendances } from '@/features/bilan-quotidien/types/tendances';
import type { WellbeingGoals } from '@/shared/services/wellbeingGoalsStorage';

const MOINS = '−';

/** Nombre arrondi et formaté en français (« 4,2 », « 10 000 »). */
export const formaterNombre = (valeur: number, decimales: number): string =>
  valeur.toLocaleString('fr-FR', { maximumFractionDigits: decimales });

/** Écart arrondi avec son signe (« +0,8 », « −1 200 ») ; un écart arrondi à zéro s'écrit « 0 ». */
export const formaterEcart = (ecart: number, decimales: number): string => {
  const facteur = 10 ** decimales;
  const arrondi = Math.round(Math.abs(ecart) * facteur) / facteur;
  if (arrondi === 0) return '0';
  return `${ecart > 0 ? '+' : MOINS}${formaterNombre(arrondi, decimales)}`;
};

export const evolutionDe = (ecart: number, seuilStable: number): Evolution => {
  if (Math.abs(ecart) < seuilStable) return 'stable';
  return ecart > 0 ? 'hausse' : 'baisse';
};

const pluriel = (nombre: number, singulier: string, pluriel = `${singulier}s`) =>
  `${formaterNombre(nombre, 0)} ${nombre > 1 ? pluriel : singulier}`;

export const calculerIndicateurs = (bilans: BilanQuotidien[], bilansPrecedents: BilanQuotidien[]): TendanceIndicateur[] =>
  indicateursTendances.flatMap((config) => {
    const moyenne = moyenneDesBilans(bilans, config.valeur);
    const precedente = moyenneDesBilans(bilansPrecedents, config.valeur);
    // Indicateur jamais renseigné sur les deux périodes : pas de carte.
    if (moyenne === null && precedente === null) return [];

    const ecart = moyenne !== null && precedente !== null ? moyenne - precedente : null;
    return [{
      cle: config.cle,
      libelle: config.libelle,
      icone: config.icone,
      valeur: moyenne === null ? null : `${formaterNombre(moyenne, config.decimales)}${config.unite}`,
      ecart: ecart === null ? null : formaterEcart(ecart, config.decimales),
      evolution: ecart === null ? null : evolutionDe(ecart, config.seuilStable),
      joursRenseignes: bilans.filter((b) => config.valeur(b) !== null).length,
    }];
  });

export const calculerReperes = (bilans: BilanQuotidien[], objectifs: WellbeingGoals): RepereSuivi[] =>
  reperesPersonnels.flatMap((config) => {
    const valeurs = bilans.map(config.valeur).filter((v): v is number => v !== null);
    if (valeurs.length === 0) return [];

    const cible = config.cible(objectifs);
    const atteint = (v: number) => (config.sens === 'min' ? v >= cible : v <= cible);
    const cibleFormatee = `${formaterNombre(cible, config.decimales)}${config.unite}`;
    return [{
      cle: config.cle,
      libelle: config.libelle,
      icone: config.icone,
      repere: config.sens === 'min' ? `${cibleFormatee} par jour ou plus` : `${cibleFormatee} ou moins`,
      atteints: valeurs.filter(atteint).length,
      renseignes: valeurs.length,
    }];
  });

const texteFortesDouleurs = (pendantRegles: number, total: number): string => {
  const jours = `jours de forte douleur (${SEUIL_FORTE_DOULEUR}/10 ou plus)`;
  if (pendantRegles === 0) return `Aucun de tes ${total} ${jours} n'était un jour de règles.`;
  if (pendantRegles === total) return `Tes ${total} ${jours} étaient tous des jours de règles.`;
  return `${pendantRegles} de tes ${total} ${jours} ${pendantRegles > 1 ? 'étaient des jours' : 'était un jour'} de règles.`;
};

/** Observations factuelles sur le lien entre douleur et règles : elles décrivent, elles ne concluent pas. */
export const calculerObservations = (jours: JourHistorique[]): Observation[] => {
  const avecBilan = jours.filter((j): j is JourHistorique & { bilan: BilanQuotidien } => j.bilan !== undefined);
  const pendantRegles = avecBilan.filter((j) => j.regles).map((j) => j.bilan);
  const autresJours = avecBilan.filter((j) => !j.regles).map((j) => j.bilan);
  const observations: Observation[] = [];

  if (pendantRegles.length >= MIN_BILANS_COMPARAISON && autresJours.length >= MIN_BILANS_COMPARAISON) {
    const douleur = (b: BilanQuotidien) => b.douleurMoyenne;
    const pendant = moyenneDesBilans(pendantRegles, douleur)!;
    const autres = moyenneDesBilans(autresJours, douleur)!;
    observations.push({
      cle: 'douleur-regles',
      icone: 'compare_arrows',
      texte: `Douleur moyenne pendant tes règles : ${formaterNombre(pendant, 1)}/10, `
        + `contre ${formaterNombre(autres, 1)}/10 les autres jours.`,
      detail: `D'après ${pluriel(pendantRegles.length, 'bilan')} pendant les règles et ${autresJours.length} les autres jours.`,
    });
  }

  const reglesNotees = jours.some((j) => j.regles);
  const fortes = avecBilan.filter((j) => j.bilan.douleurMoyenne >= SEUIL_FORTE_DOULEUR);
  if (reglesNotees && fortes.length >= 2) {
    observations.push({
      cle: 'forte-douleur-regles',
      icone: 'event',
      texte: texteFortesDouleurs(fortes.filter((j) => j.regles).length, fortes.length),
      detail: 'D\'après les jours de règles notés dans Cycle.',
    });
  }

  return observations;
};

/** Tendances d'une période, comparées à la période précédente de même durée (semaine ou mois). */
export const calculerTendances = (
  jours: JourHistorique[],
  bilansPrecedents: BilanQuotidien[],
  objectifs: WellbeingGoals,
): Tendances => {
  const bilans = jours.flatMap((j) => (j.bilan ? [j.bilan] : []));
  return {
    couverture: {
      bilans: bilans.length,
      jours: jours.filter((j) => !j.aVenir).length,
      bilansPrecedents: bilansPrecedents.length,
    },
    indicateurs: calculerIndicateurs(bilans, bilansPrecedents),
    reperes: calculerReperes(bilans, objectifs),
    observations: calculerObservations(jours),
    reglesNotees: jours.some((j) => j.regles),
  };
};
