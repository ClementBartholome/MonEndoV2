import { computed, ref } from 'vue';
import { addMonths, startOfMonth } from 'date-fns';
import apiService from '@/shared/services/apiService';
import { estMoisCourant, grouperParJour } from '@/shared/utils/jours';
import type { SymptomeCycle, SymptomeSaisie } from '../types/symptome-cycle';
import { ACNE, chiffresDuMois } from '../utils/symptomes';

interface Options {
    carnetSanteId: () => number | undefined;
    maintenant?: () => Date;
}

/** Onglet Symptômes : symptômes d'un mois (hors acné, qui a son onglet), liste par jour, ajout, modification, suppression. */
export function useSymptomes({ carnetSanteId, maintenant = () => new Date() }: Options) {
    const mois = ref(startOfMonth(maintenant()));
    const entrees = ref<SymptomeCycle[]>([]);
    const chargement = ref(true);
    const erreur = ref(false);

    const chiffres = computed(() => chiffresDuMois(entrees.value));
    const groupes = computed(() => grouperParJour(entrees.value));
    const moisSuivantPossible = computed(() => !estMoisCourant(mois.value, maintenant()));

    async function charger() {
        const carnet = carnetSanteId();
        if (!carnet) return;
        chargement.value = entrees.value.length === 0;
        erreur.value = false;
        try {
            const duMois = await apiService.getSymptomesDuMois(carnet, mois.value.getMonth() + 1, mois.value.getFullYear());
            entrees.value = duMois.filter((s) => s.typeSymptome !== ACNE);
        } catch {
            erreur.value = true;
        } finally {
            chargement.value = false;
        }
    }

    async function changerDeMois(decalage: number) {
        if (decalage > 0 && !moisSuivantPossible.value) return;
        mois.value = addMonths(mois.value, decalage);
        entrees.value = [];
        await charger();
    }

    /** Ajout ou modification (acné comprise, depuis son onglet) ; la liste du mois est rechargée ensuite. */
    async function enregistrer(saisie: SymptomeSaisie, id: number | null) {
        const carnet = carnetSanteId();
        if (!carnet) return;
        if (id === null) await apiService.postSymptome(carnet, saisie);
        else await apiService.putSymptome(id, carnet, saisie);
        await charger();
    }

    async function supprimer(id: number) {
        await apiService.deleteSymptomeCycle(id);
        entrees.value = entrees.value.filter((e) => e.id !== id);
    }

    return { mois, entrees, chargement, erreur, chiffres, groupes, moisSuivantPossible, charger, changerDeMois, enregistrer, supprimer };
}
