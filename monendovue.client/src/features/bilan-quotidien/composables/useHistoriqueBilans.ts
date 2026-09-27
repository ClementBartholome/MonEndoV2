import { computed, ref } from 'vue';
import { addMonths, addWeeks, isAfter, startOfDay } from 'date-fns';
import apiService from '@/shared/services/apiService';
import { contient, cleJour, intervalleDe, joursEntre, jourParDefaut, libellePeriode } from '@/features/bilan-quotidien/utils/historique';
import type { BilanQuotidien } from '@/features/bilan-quotidien/types/bilan-quotidien';
import type {
  HistoriqueActions, HistoriqueModel, JourHistorique, ModePeriode, PeriodeHistorique,
} from '@/features/bilan-quotidien/types/historique';

/**
 * Historique des bilans : une seule période (semaine ou mois) pilote toute la page.
 * Les bilans et jours de règles déjà chargés sont gardés par jour, pour la saisie (« Comme hier ») et le détail.
 */
export const useHistoriqueBilans = (options: { mode?: ModePeriode } = {}) => {
  const mode = ref<ModePeriode>(options.mode ?? 'mois');
  const ancre = ref(startOfDay(new Date()));
  const jourSelectionne = ref(startOfDay(new Date()));
  const chargement = ref(false);
  const erreur = ref(false);

  const bilansParJour = ref(new Map<string, BilanQuotidien>());
  const joursRegles = ref(new Set<string>());

  const intervalle = computed(() => intervalleDe(mode.value, ancre.value));
  const decalage = (date: Date, sens: number) => (mode.value === 'mois' ? addMonths(date, sens) : addWeeks(date, sens));
  // Période de même nature juste avant (semaine ou mois précédent), pour les comparaisons de l'analyse.
  const intervallePrecedent = computed(() => intervalleDe(mode.value, decalage(intervalle.value.debut, -1)));

  const periode = computed<PeriodeHistorique>(() => {
    const { debut, fin } = intervalle.value;
    const aujourdhui = startOfDay(new Date());
    return {
      mode: mode.value,
      debut,
      fin,
      libelle: libellePeriode(mode.value, debut, fin),
      contientAujourdhui: contient(debut, fin, aujourdhui),
      suivantePossible: isAfter(aujourdhui, fin),
    };
  });

  const jours = computed<JourHistorique[]>(() => {
    const aujourdhui = startOfDay(new Date());
    return joursEntre(intervalle.value.debut, intervalle.value.fin).map((date) => {
      const cle = cleJour(date);
      return {
        date,
        cle,
        bilan: bilansParJour.value.get(cle),
        regles: joursRegles.value.has(cle),
        aVenir: isAfter(date, aujourdhui),
      };
    });
  });

  const bilans = computed(() => jours.value.flatMap((jour) => (jour.bilan ? [jour.bilan] : [])));

  const bilansPrecedents = computed(() => joursEntre(intervallePrecedent.value.debut, intervallePrecedent.value.fin)
    .flatMap((date) => {
      const bilan = bilansParJour.value.get(cleJour(date));
      return bilan ? [bilan] : [];
    }));

  /** Charge les jours du `debut` au `fin` inclus ; remplace ce qui était connu pour ces jours. */
  const chargerJours = async (debut: Date, fin: Date) => {
    const resultat = await apiService.getHistoriqueBilans(cleJour(debut), cleJour(fin));
    const cles = new Set(joursEntre(debut, fin).map(cleJour));

    const parJour = new Map([...bilansParJour.value].filter(([cle]) => !cles.has(cle)));
    resultat.bilans.forEach((bilan) => parJour.set(cleJour(new Date(bilan.date)), bilan));
    bilansParJour.value = parJour;

    const regles = new Set([...joursRegles.value].filter((cle) => !cles.has(cle)));
    resultat.joursRegles.forEach((cle) => regles.add(cle));
    joursRegles.value = regles;
  };

  // Seule la dernière demande met à jour l'état de chargement (navigation rapide).
  let derniereDemande = 0;
  const recharger = async () => {
    const demande = ++derniereDemande;
    chargement.value = true;
    erreur.value = false;
    try {
      await Promise.all([
        chargerJours(intervalle.value.debut, intervalle.value.fin),
        chargerJours(intervallePrecedent.value.debut, intervallePrecedent.value.fin),
      ]);
    } catch {
      if (demande === derniereDemande) erreur.value = true;
    } finally {
      if (demande === derniereDemande) chargement.value = false;
    }
  };

  const allerA = (nouvelleAncre: Date) => {
    ancre.value = startOfDay(nouvelleAncre);
    const { debut, fin } = intervalle.value;
    if (!contient(debut, fin, jourSelectionne.value)) jourSelectionne.value = jourParDefaut(debut, fin, new Date());
    void recharger();
  };

  const decaler = (sens: 1 | -1) => allerA(decalage(intervalle.value.debut, sens));

  const actions: HistoriqueActions = {
    changerMode: (nouveau) => {
      if (nouveau === mode.value) return;
      mode.value = nouveau;
      // La nouvelle période contient le jour sélectionné : on garde le contexte.
      allerA(jourSelectionne.value);
    },
    precedente: () => decaler(-1),
    suivante: () => decaler(1),
    revenirAujourdhui: () => {
      jourSelectionne.value = startOfDay(new Date());
      allerA(new Date());
    },
    selectionnerJour: (date) => {
      jourSelectionne.value = startOfDay(date);
    },
    recharger,
  };

  const model = computed<HistoriqueModel>(() => ({
    periode: periode.value,
    jours: jours.value,
    bilans: bilans.value,
    bilansPrecedents: bilansPrecedents.value,
    jourSelectionne: jourSelectionne.value,
    chargement: chargement.value,
    erreur: erreur.value,
  }));

  const bilanDu = (date: Date): BilanQuotidien | undefined => bilansParJour.value.get(cleJour(date));

  /** Après une saisie : met à jour le jour concerné sans recharger la période. */
  const enregistrerLocalement = (bilan: BilanQuotidien) => {
    const parJour = new Map(bilansParJour.value);
    parJour.set(cleJour(new Date(bilan.date)), bilan);
    bilansParJour.value = parJour;
  };

  return { model, actions, bilanDu, chargerJours, enregistrerLocalement };
};
