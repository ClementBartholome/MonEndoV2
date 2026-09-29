import { computed, ref } from 'vue';
import { format } from 'date-fns';
import apiService from '@/shared/services/apiService';
import type { EvenementAgenda } from '@/features/schedule/types/agenda';
import type { PrisePrevue } from '@/features/medicament/types/traitements';
import { reponse } from '@/features/medicament/utils/prises';
import type { Aujourdhui } from '../types/aujourdhui';
import { phrasesSemaine } from '../utils/semaine';

interface Options {
    /** Heure locale (injectée pour les tests). */
    maintenant?: () => Date;
}

/** Données de l'accueil « Aujourd'hui » et prise prévue notée en un geste. */
export function useAujourdhui({ maintenant = () => new Date() }: Options = {}) {
    const aujourdhui = ref<Aujourdhui | null>(null);
    const prochainRendezVous = ref<EvenementAgenda | null>(null);
    const chargement = ref(true);
    const erreur = ref(false);
    /** Clé « traitementId-heure » de la prise en cours d'envoi. */
    const priseEnCours = ref<string | null>(null);

    const phrases = computed(() => (aujourdhui.value ? phrasesSemaine(aujourdhui.value.semaine) : []));

    async function charger() {
        chargement.value = true;
        erreur.value = false;
        try {
            const [donnees, rendezVous] = await Promise.all([
                apiService.getAujourdhui(format(maintenant(), 'yyyy-MM-dd')),
                // Sans agenda associé (ou agenda indisponible), le bloc n'est simplement pas affiché.
                apiService.getProchainsRendezVous().catch(() => null),
            ]);
            aujourdhui.value = donnees;
            prochainRendezVous.value = rendezVous?.[0] ?? null;
        } catch {
            erreur.value = true;
        } finally {
            chargement.value = false;
        }
    }

    /** Note la prise prévue comme faite maintenant et met la carte à jour sans recharger. */
    async function prendre(prise: PrisePrevue) {
        if (priseEnCours.value !== null) return false;
        priseEnCours.value = `${prise.traitementId}-${prise.heurePrevue}`;
        const saisie = reponse('Pris', prise.heurePrevue, maintenant());
        try {
            const { id } = await apiService.postPrise(prise.traitementId, saisie);
            prise.reponse = { priseId: id, statut: 'Pris', date: saisie.date };
            return true;
        } catch {
            return false;
        } finally {
            priseEnCours.value = null;
        }
    }

    return { aujourdhui, prochainRendezVous, chargement, erreur, priseEnCours, phrases, charger, prendre };
}
