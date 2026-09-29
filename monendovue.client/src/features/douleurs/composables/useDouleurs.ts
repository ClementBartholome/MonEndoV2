import { computed, ref } from 'vue';
import { startOfMonth } from 'date-fns';
import apiService from '@/shared/services/apiService';
import { enTableau } from '@/shared/utils/json';
import type { DonneesDouleur, DonneesDouleurModification } from '../types/donnees-douleur';
import { cleJour, grouperParJour } from '@/shared/utils/jours';
import { chiffresDuMois, joursDuMois } from '../utils/douleurs';

interface Options {
    carnetSanteId: () => number | undefined;
    maintenant?: () => Date;
}

/** Douleurs d'un mois : chargement, chiffres, graphique, liste par jour, ajout, modification, suppression. */
export function useDouleurs({ carnetSanteId, maintenant = () => new Date() }: Options) {
    const mois = ref(startOfMonth(maintenant()));
    const entrees = ref<DonneesDouleur[]>([]);
    const joursDeRegles = ref<string[]>([]);
    const chargement = ref(true);
    const erreur = ref(false);

    const chiffres = computed(() => chiffresDuMois(entrees.value));
    const graphique = computed(() => joursDuMois(mois.value, entrees.value, joursDeRegles.value));
    const groupes = computed(() => grouperParJour(entrees.value));

    async function charger() {
        const carnet = carnetSanteId();
        if (!carnet) return;
        chargement.value = true;
        erreur.value = false;
        const numero = mois.value.getMonth() + 1;
        const annee = mois.value.getFullYear();
        try {
            const [douleurs, regles] = await Promise.all([
                apiService.getDonneesDouleursByMonth(carnet, numero, annee),
                // Les jours de règles ne servent qu'au graphique : leur absence ne bloque pas la page.
                apiService.getJoursReglesByMonth(carnet, numero, annee).catch(() => []),
            ]);
            entrees.value = enTableau<DonneesDouleur>(douleurs);
            joursDeRegles.value = enTableau<{ date: string }>(regles).map((j) => cleJour(j.date));
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
        await charger();
    }

    /** Ajout ou modification ; la liste est rechargée si la date quitte ou rejoint le mois affiché. */
    async function enregistrer(saisie: DonneesDouleurModification, id: number | null) {
        const carnet = carnetSanteId();
        if (!carnet) return;
        if (id === null) {
            await apiService.postDonneesDouleurs({ ...saisie, carnetSanteId: carnet });
        } else {
            await apiService.editDonneesDouleurs(id, saisie);
        }
        await charger();
    }

    async function supprimer(id: number) {
        await apiService.deleteDonneesDouleurs(id);
        entrees.value = entrees.value.filter((e) => e.id !== id);
    }

    return {
        mois, entrees, chargement, erreur, chiffres, graphique, groupes,
        charger, allerAuMois, enregistrer, supprimer,
    };
}
