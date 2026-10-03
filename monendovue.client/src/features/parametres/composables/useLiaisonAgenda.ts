import { ref } from 'vue';
import type { AxiosError } from 'axios';
import apiService from '@/shared/services/apiService';
import type { CalendrierAgenda, StatutLiaisonAgenda } from '@/features/schedule/types/agenda';

/** Seule adresse où l'on envoie le navigateur : celle de Google, jamais une adresse reçue telle quelle d'ailleurs. */
const DEBUT_ADRESSE_GOOGLE = 'https://accounts.google.com/';

interface Options {
    /** Redirige le navigateur vers Google (remplaçable dans un test). */
    aller?: (url: string) => void;
}

/** Statut de la liaison de l'agenda Google, départ du flux OAuth et déliaison. */
export function useLiaisonAgenda({ aller = (url) => window.location.assign(url) }: Options = {}) {
    const statut = ref<StatutLiaisonAgenda | null>(null);
    const enCours = ref(false);
    const erreur = ref<string | null>(null);
    const calendriers = ref<CalendrierAgenda[]>([]);
    const calendriersCharges = ref(false);

    async function charger() {
        try {
            statut.value = await apiService.getStatutLiaisonAgenda();
        } catch {
            // Statut inconnu : la ligne « Agenda Google » n'est simplement pas proposée.
            statut.value = null;
        }
    }

    /** Calendriers à proposer : demandés à Google à l'ouverture du panneau d'un agenda lié. */
    async function chargerCalendriers() {
        erreur.value = null;
        try {
            calendriers.value = await apiService.getCalendriersAgenda();
            calendriersCharges.value = true;
        } catch {
            calendriersCharges.value = false;
            erreur.value = 'Tes calendriers n\'ont pas pu être chargés. Réessaie dans quelques minutes.';
        }
    }

    /** Un seul calendrier est lu : celui-ci. Le choix est enregistré dès qu'on le touche. */
    async function choisir(id: string) {
        if (enCours.value || statut.value?.calendrierId === id) return;
        enCours.value = true;
        erreur.value = null;
        try {
            await apiService.choisirCalendrierAgenda(id);
            await charger();
        } catch {
            erreur.value = 'Le choix n\'a pas pu être enregistré. Réessaie dans quelques minutes.';
        } finally {
            enCours.value = false;
        }
    }

    /** Demande l'adresse d'autorisation au serveur puis y envoie le navigateur ; Google renvoie ensuite vers Paramètres. */
    async function lier() {
        if (enCours.value) return;
        enCours.value = true;
        erreur.value = null;
        try {
            const { url } = await apiService.demarrerLiaisonAgenda();
            if (!url.startsWith(DEBUT_ADRESSE_GOOGLE)) throw new Error('Adresse de liaison inattendue');
            aller(url);
        } catch (error_) {
            const message = (error_ as AxiosError<{ message?: string }>).response?.data?.message;
            erreur.value = message ?? 'La liaison n\'a pas pu démarrer. Réessaie dans quelques minutes.';
            enCours.value = false;
        }
    }

    async function delier() {
        if (enCours.value) return;
        enCours.value = true;
        erreur.value = null;
        try {
            await apiService.delierAgenda();
            calendriers.value = [];
            calendriersCharges.value = false;
            await charger();
        } catch {
            erreur.value = 'La déliaison n\'a pas pu aboutir. Réessaie dans quelques minutes.';
        } finally {
            enCours.value = false;
        }
    }

    return { statut, enCours, erreur, calendriers, calendriersCharges, charger, chargerCalendriers, choisir, lier, delier };
}

export type LiaisonAgenda = ReturnType<typeof useLiaisonAgenda>;
