import { computed, ref } from 'vue';
import { format } from 'date-fns';
import apiService from '@/shared/services/apiService';
import type { PrisePrevue, Soin, StatutPrise, TraitementAuBesoin, TraitementsDuJour, TraitementSaisie } from '../types/traitements';
import { grouperParMoment, reponse } from '../utils/prises';

/** Page Traitements : planning du jour, réponses aux prises, prises au besoin, séances, saisie et arrêt. */
export function useTraitements({ maintenant = () => new Date() }: { maintenant?: () => Date } = {}) {
    const donnees = ref<TraitementsDuJour | null>(null);
    const chargement = ref(true);
    const erreur = ref(false);
    const envoi = ref(false);

    const jour = () => format(maintenant(), 'yyyy-MM-dd');
    const groupes = computed(() => grouperParMoment(donnees.value?.prisesPrevues ?? []));
    const faites = computed(() => (donnees.value?.prisesPrevues ?? []).filter((p) => p.reponse?.statut === 'Pris').length);

    async function charger() {
        chargement.value = donnees.value === null;
        erreur.value = false;
        try {
            donnees.value = await apiService.getTraitementsDuJour(jour());
        } catch {
            erreur.value = true;
        } finally {
            chargement.value = false;
        }
    }

    /** Lance une écriture unique à la fois ; recharge le planning ensuite pour rester juste. */
    async function ecrire(action: () => Promise<unknown>) {
        if (envoi.value) return;
        envoi.value = true;
        try {
            await action();
            await charger();
        } finally {
            envoi.value = false;
        }
    }

    const repondre = (prise: PrisePrevue, statut: StatutPrise) =>
        ecrire(() => apiService.postPrise(prise.traitementId, reponse(statut, prise.heurePrevue, maintenant())));

    const annuler = (prise: PrisePrevue) => ecrire(() => apiService.deletePrise(prise.reponse!.priseId));

    const prendreAuBesoin = (traitement: TraitementAuBesoin) =>
        ecrire(() => apiService.postPrise(traitement.id, reponse('Pris', null, maintenant())));

    const noterSeance = (soin: Soin) =>
        ecrire(() => apiService.postSeance(soin.id, { date: format(maintenant(), "yyyy-MM-dd'T'HH:mm:ss"), duree: null, commentaire: null }));

    const enregistrer = (saisie: TraitementSaisie, id: number | null) =>
        ecrire(() => (id === null ? apiService.postTraitement(saisie) : apiService.putTraitement(id, saisie)));

    const arreter = (id: number) => ecrire(() => apiService.postArretTraitement(id, jour()));

    return { donnees, chargement, erreur, envoi, groupes, faites, charger, repondre, annuler, prendreAuBesoin, noterSeance, enregistrer, arreter };
}
