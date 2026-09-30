import { computed, ref } from 'vue';
import { startOfMonth } from 'date-fns';
import apiService from '@/shared/services/apiService';
import { useAuthStore } from '@/features/auth/store/auth';
import { grouperParJour } from '@/shared/utils/jours';
import { enTableau } from '@/shared/utils/json';
import type { DonneesTransit } from '../types/donnees-transit';

/** Ancien suivi du transit d'un mois : chargement, liste par jour et suppression d'une entrée. */
export function useTransit({ maintenant = () => new Date() }: { maintenant?: () => Date } = {}) {
    const carnetSanteId = useAuthStore().user!.carnetSanteId;
    const mois = ref(startOfMonth(maintenant()));
    const entrees = ref<DonneesTransit[]>([]);
    const chargement = ref(true);
    const erreur = ref(false);

    const groupes = computed(() => grouperParJour(entrees.value));

    async function charger() {
        chargement.value = entrees.value.length === 0;
        erreur.value = false;
        try {
            entrees.value = enTableau(await apiService.getDonneesTransitByMonth(carnetSanteId, mois.value.getMonth() + 1, mois.value.getFullYear()));
        } catch {
            erreur.value = true;
        } finally {
            chargement.value = false;
        }
    }

    /** Mois choisi (flèches ou choix direct) ; jamais après le mois en cours. */
    async function allerAuMois(choisi: Date) {
        const debut = startOfMonth(choisi);
        if (debut > startOfMonth(maintenant())) return;
        mois.value = debut;
        entrees.value = [];
        await charger();
    }

    async function supprimer(id: number) {
        await apiService.deleteDonneesTransit(id);
        entrees.value = entrees.value.filter((e) => e.id !== id);
    }

    return { mois, entrees, chargement, erreur, groupes, charger, allerAuMois, supprimer };
}
