import { computed, ref } from 'vue';
import { format, startOfMonth } from 'date-fns';
import apiService from '@/shared/services/apiService';
import { derniereDemande } from '@/shared/utils/derniereDemande';
import { grouperParJour } from '@/shared/utils/jours';
import type { Activite, ActiviteSaisie } from '../types/activite';
import { chiffresDuMois } from '../utils/activite';

/** Activités d'un mois : chargement, chiffres, liste par jour, ajout, modification, suppression. */
export function useActivites({ maintenant = () => new Date() }: { maintenant?: () => Date } = {}) {
    const mois = ref(startOfMonth(maintenant()));
    const activites = ref<Activite[]>([]);
    const chargement = ref(true);
    const erreur = ref(false);

    const chiffres = computed(() => chiffresDuMois(activites.value));
    const groupes = computed(() => grouperParJour(activites.value));

    const nouvelleDemande = derniereDemande();

    async function charger() {
        const estCourante = nouvelleDemande();
        chargement.value = activites.value.length === 0;
        erreur.value = false;
        try {
            const recues = await apiService.getActivites(format(mois.value, 'yyyy-MM-dd'));
            if (estCourante()) activites.value = recues;
        } catch {
            if (estCourante()) erreur.value = true;
        } finally {
            if (estCourante()) chargement.value = false;
        }
    }

    /** Mois choisi (flèches ou choix direct) ; jamais après le mois en cours. */
    async function allerAuMois(choisi: Date) {
        const debut = startOfMonth(choisi);
        if (debut > startOfMonth(maintenant())) return;
        mois.value = debut;
        activites.value = [];
        await charger();
    }

    async function enregistrer(saisie: ActiviteSaisie, id: number | null) {
        if (id === null) await apiService.postActivite(saisie);
        else await apiService.putActivite(id, saisie);
        await charger();
    }

    async function supprimer(id: number) {
        await apiService.deleteActivite(id);
        activites.value = activites.value.filter((a) => a.id !== id);
    }

    return { mois, activites, chargement, erreur, chiffres, groupes, charger, allerAuMois, enregistrer, supprimer };
}
