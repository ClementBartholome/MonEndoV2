import { computed, ref } from 'vue';
import { startOfMonth } from 'date-fns';
import apiService from '@/shared/services/apiService';
import { derniereDemande } from '@/shared/utils/derniereDemande';
import { grouperParJour } from '@/shared/utils/jours';
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

    const nouvelleDemande = derniereDemande();

    async function charger() {
        const carnet = carnetSanteId();
        if (!carnet) return;
        const estCourante = nouvelleDemande();
        chargement.value = entrees.value.length === 0;
        erreur.value = false;
        try {
            const duMois = await apiService.getSymptomesDuMois(carnet, mois.value.getMonth() + 1, mois.value.getFullYear());
            if (estCourante()) entrees.value = duMois.filter((s) => s.typeSymptome !== ACNE);
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

    return { mois, entrees, chargement, erreur, chiffres, groupes, charger, allerAuMois, enregistrer, supprimer };
}
