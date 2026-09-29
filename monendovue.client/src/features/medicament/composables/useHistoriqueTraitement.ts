import { ref } from 'vue';
import { format, startOfMonth } from 'date-fns';
import apiService from '@/shared/services/apiService';
import type { EntreeHistoriqueTraitement, HistoriqueTraitement, TraitementSaisie } from '../types/traitements';

/** Page d'un traitement : son historique un mois à la fois, retrait d'une prise ou d'une séance, modification, arrêt. */
export function useHistoriqueTraitement(id: () => number, { maintenant = () => new Date() }: { maintenant?: () => Date } = {}) {
    const mois = ref(startOfMonth(maintenant()));
    const donnees = ref<HistoriqueTraitement | null>(null);
    const chargement = ref(true);
    const erreur = ref(false);
    const introuvable = ref(false);

    const jour = () => format(maintenant(), 'yyyy-MM-dd');

    async function charger() {
        chargement.value = donnees.value === null;
        erreur.value = false;
        try {
            donnees.value = await apiService.getHistoriqueTraitement(id(), format(mois.value, 'yyyy-MM-dd'), jour());
        } catch (e) {
            const statut = (e as { response?: { status?: number } })?.response?.status;
            introuvable.value = statut === 404 || statut === 403;
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

    /** Retire une prise (faite ou ignorée) ou une séance notée par erreur. */
    async function retirer(entree: EntreeHistoriqueTraitement) {
        if (entree.nature === 'Seance') await apiService.deleteSeance(entree.id);
        else await apiService.deletePrise(entree.id);
        await charger();
    }

    async function modifier(saisie: TraitementSaisie) {
        await apiService.putTraitement(id(), saisie);
        await charger();
    }

    async function arreter() {
        await apiService.postArretTraitement(id(), jour());
        await charger();
    }

    return { mois, donnees, chargement, erreur, introuvable, charger, allerAuMois, retirer, modifier, arreter };
}
