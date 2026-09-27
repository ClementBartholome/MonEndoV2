import { ref } from 'vue';
import { format, isSameDay } from 'date-fns';
import apiService from '@/shared/services/apiService';
import type { BilanQuotidien } from '@/features/bilan-quotidien/types/bilan-quotidien';

// L'API n'est pas triée et les saisies s'ajoutent en fin de liste : toujours garder les bilans par date croissante.
const trierParDate = (liste: BilanQuotidien[]): BilanQuotidien[] =>
  [...liste].sort((a, b) => new Date(a.date).getTime() - new Date(b.date).getTime());

/** Fusionne sans doublon : un bilan déjà connu (même id) est remplacé par sa version la plus récente. */
const fusionner = (existants: BilanQuotidien[], nouveaux: BilanQuotidien[]): BilanQuotidien[] => {
  const parId = new Map(existants.map((b) => [b.id, b]));
  nouveaux.forEach((b) => parId.set(b.id, b));
  return trierParDate([...parId.values()]);
};

/** Bilans chargés par semaine ISO, mis à jour localement après une saisie (sans rechargement). */
export const useBilansQuotidiens = (carnetSanteId: number) => {
  const bilans = ref<BilanQuotidien[]>([]);
  const semainesChargees = new Set<string>();

  const chargerSemaine = async (semaine: string, annee: string) => {
    const cle = `${annee}-W${semaine}`;
    if (semainesChargees.has(cle)) return;
    semainesChargees.add(cle);
    try {
      bilans.value = fusionner(bilans.value, await apiService.getBilanQuotidienByWeek(carnetSanteId, semaine, annee));
    } catch (error) {
      semainesChargees.delete(cle);
      console.error('Erreur de chargement des bilans :', error);
    }
  };

  /** Charge la semaine ISO contenant ce jour. */
  const chargerSemaineDe = (date: Date) => chargerSemaine(format(date, 'II'), format(date, 'RRRR'));

  const bilanDu = (date: Date): BilanQuotidien | undefined =>
    bilans.value.find((b) => isSameDay(new Date(b.date), date));

  const enregistrerLocalement = (bilan: BilanQuotidien) => {
    bilans.value = fusionner(bilans.value, [bilan]);
  };

  return { bilans, chargerSemaine, chargerSemaineDe, bilanDu, enregistrerLocalement };
};
