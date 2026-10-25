import { computed, ref, type Ref } from 'vue';
import apiService from '@/shared/services/apiService';
import type { CriseDouleur, ZoneDouleur } from '@/features/douleurs/types/douleurs-du-jour';

export interface UseZonesDouleurOptions {
  /** Jour du bilan, AAAA-MM-JJ. */
  jour: string;
  /** Douleur de la journée : intensité proposée pour une zone qu'on touche. */
  douleurDuJour: Ref<number | null>;
}

const INTENSITE_MIN = 1;
const INTENSITE_MAX = 10;
const borner = (n: number) => Math.min(INTENSITE_MAX, Math.max(INTENSITE_MIN, n));

/**
 * « Où as-tu eu mal ? » dans le bilan : zones touchées (enregistrées comme douleurs du jour, marquées « depuis le bilan ») et
 * crises déjà notées sur la page Douleurs ce jour-là (affichées, jamais modifiées ici). Si le chargement échoue, rien n'est
 * enregistré : on n'efface pas des zones qu'on n'a pas pu lire.
 */
export const useZonesDouleur = (options: UseZonesDouleurOptions) => {
  const zones = ref<ZoneDouleur[]>([]);
  const crises = ref<CriseDouleur[]>([]);
  const chargement = ref(true);
  const indisponible = ref(false);
  const instantane = ref('[]');

  const charger = async () => {
    chargement.value = true;
    try {
      const duJour = await apiService.getDouleursDuJour(options.jour);
      zones.value = duJour.zones;
      crises.value = duJour.crises;
      instantane.value = JSON.stringify(duJour.zones);
      indisponible.value = false;
    } catch {
      indisponible.value = true;
    } finally {
      chargement.value = false;
    }
  };

  /** Types déjà notés comme crise ce jour-là : on ne les note pas une seconde fois depuis le bilan. */
  const typesEnCrise = computed(() => new Set(crises.value.map((c) => c.typeDouleur)));
  const estModifie = computed(() => JSON.stringify(zones.value) !== instantane.value);

  const basculer = (typeDouleur: string) => {
    if (typesEnCrise.value.has(typeDouleur)) return;
    zones.value = zones.value.some((z) => z.typeDouleur === typeDouleur)
      ? zones.value.filter((z) => z.typeDouleur !== typeDouleur)
      : [...zones.value, { typeDouleur, intensite: borner(options.douleurDuJour.value ?? INTENSITE_MIN) }];
  };

  const ajuster = (typeDouleur: string, ecart: number) => {
    zones.value = zones.value.map((z) => (z.typeDouleur === typeDouleur ? { ...z, intensite: borner(z.intensite + ecart) } : z));
  };

  /** Envoie les zones si elles ont changé (le bilan du jour vient d'être enregistré). */
  const enregistrer = async () => {
    if (indisponible.value || !estModifie.value) return;
    await apiService.putZonesDouleur(options.jour, zones.value);
    instantane.value = JSON.stringify(zones.value);
  };

  return { zones, crises, chargement, indisponible, typesEnCrise, estModifie, charger, basculer, ajuster, enregistrer, INTENSITE_MIN, INTENSITE_MAX };
};
