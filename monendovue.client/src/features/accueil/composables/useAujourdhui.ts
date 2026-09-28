import { computed, ref } from 'vue';
import { format } from 'date-fns';
import apiService from '@/shared/services/apiService';
import type { EvenementAgenda } from '@/features/schedule/types/agenda';
import type { Aujourdhui, TraitementAujourdhui } from '../types/aujourdhui';
import { phrasesSemaine } from '../utils/semaine';

interface Options {
    /** Carnet de la session, pour enregistrer une prise. */
    carnetSanteId: () => number | undefined;
    /** Heure locale (injectée pour les tests). */
    maintenant?: () => Date;
}

/** Données de l'accueil « Aujourd'hui » et prise d'un traitement en un geste. */
export function useAujourdhui({ carnetSanteId, maintenant = () => new Date() }: Options) {
    const aujourdhui = ref<Aujourdhui | null>(null);
    const prochainRendezVous = ref<EvenementAgenda | null>(null);
    const chargement = ref(true);
    const erreur = ref(false);
    const priseEnCours = ref<number | null>(null);

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

    /** Note une prise maintenant (heure locale, comme les autres saisies) et met la carte à jour sans recharger. */
    async function prendre(traitement: TraitementAujourdhui) {
        const carnet = carnetSanteId();
        if (!carnet || priseEnCours.value !== null) return false;
        priseEnCours.value = traitement.id;
        const date = format(maintenant(), "yyyy-MM-dd'T'HH:mm:ss");
        try {
            await apiService.postDonneesPriseMedicament({
                carnetSanteId: carnet,
                medicamentId: traitement.id,
                nombreComprimes: 1,
                date,
                commentaire: null,
            });
            traitement.prisesDuJour += 1;
            traitement.dernierePrise = date;
            return true;
        } catch {
            return false;
        } finally {
            priseEnCours.value = null;
        }
    }

    return { aujourdhui, prochainRendezVous, chargement, erreur, priseEnCours, phrases, charger, prendre };
}
