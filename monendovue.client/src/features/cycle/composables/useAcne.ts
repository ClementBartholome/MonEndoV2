import { computed, ref } from 'vue';
import { format } from 'date-fns';
import apiService from '@/shared/services/apiService';
import type { Acne, EpisodeAcneSaisie } from '../types/acne';
import { photosAComparer, type EcartComparaison } from '../utils/acne';

/** Onglet Acné : épisode en cours, épisodes passés, suivis photo et comparaison avant / après. */
export function useAcne({ maintenant = () => new Date() }: { maintenant?: () => Date } = {}) {
    const donnees = ref<Acne | null>(null);
    const chargement = ref(true);
    const erreur = ref(false);
    const ecart = ref<EcartComparaison>(3);
    /** Photos des N derniers mois (de quoi comparer à 6 mois), puis 12 mois de plus à chaque demande. */
    const moisDePhotos = ref(7);

    const enCours = computed(() => {
        const dernier = donnees.value?.episodes[0];
        return dernier && dernier.fin === null ? dernier : null;
    });
    const comparaison = computed(() => photosAComparer(donnees.value?.suivis ?? [], ecart.value));
    const aujourdhui = () => format(maintenant(), 'yyyy-MM-dd');

    async function charger() {
        chargement.value = donnees.value === null;
        erreur.value = false;
        try {
            donnees.value = await apiService.getAcne(aujourdhui(), moisDePhotos.value);
        } catch {
            erreur.value = true;
        } finally {
            chargement.value = false;
        }
    }

    /** Toute écriture recharge l'onglet : durées et épisode en cours sont recalculés par le serveur. */
    async function ecrire(action: () => Promise<unknown>) {
        await action();
        await charger();
    }

    async function voirPlusDePhotos() {
        moisDePhotos.value += 12;
        await charger();
    }

    const commencer = (debut: string) => ecrire(() => apiService.postEpisodeAcne({ debut, fin: null }));
    const terminer = (id: number, fin: string) => ecrire(() => apiService.postFinEpisodeAcne(id, fin));
    const modifier = (id: number, saisie: EpisodeAcneSaisie) => ecrire(() => apiService.putEpisodeAcne(id, saisie));
    const supprimer = (id: number) => ecrire(() => apiService.deleteEpisodeAcne(id));

    return { donnees, chargement, erreur, ecart, enCours, comparaison, charger, voirPlusDePhotos, commencer, terminer, modifier, supprimer };
}
