import { computed, ref } from 'vue';
import { useRouter } from 'vue-router';
import { useToast } from '@/shared/components/ui/toast';
import { usePushNotifications } from '@/features/parametres/composables/usePushNotifications';

/** Heures proposées d'un geste ; « Autre » ouvre une saisie libre. */
export const HEURES_PROPOSEES = ['19:00', '20:00', '21:00'];

/** « 20:00 » devient « 20 h » (« 20 h 30 » si besoin). */
export const heureLisible = (heure: string): string => {
    const [h, m] = heure.split(':');
    return m === '00' ? `${Number(h)} h` : `${Number(h)} h ${m}`;
};

/**
 * Étape facultative qui suit l'inscription : proposer le rappel du bilan du soir. Elle s'efface seule (retour à l'accueil)
 * quand l'appareil ne peut pas recevoir de notification ou qu'elles sont déjà actives ; sur iPhone hors écran d'accueil,
 * elle explique l'installation au lieu de demander une permission qui serait refusée.
 */
export function useRappelBienvenue() {
    const router = useRouter();
    const { toast } = useToast();
    const push = usePushNotifications();

    const heure = ref('20:00');
    const autreHeure = ref(false);
    const erreur = ref<string | null>(null);

    const etat = push.etat;
    const enCours = push.enCours;
    const heureProposee = computed(() => HEURES_PROPOSEES.includes(heure.value) && !autreHeure.value);

    const versAccueil = () => router.replace('/');

    async function demarrer() {
        try {
            await push.initialiser();
        } catch {
            push.etat.value = 'non-disponible';
        }
        // Rien à proposer sur cet appareil, ou déjà fait : on ne montre pas d'écran inutile.
        if (['non-supporte', 'non-disponible', 'actif'].includes(push.etat.value)) await versAccueil();
    }

    function choisirHeure(nouvelle: string) {
        heure.value = nouvelle;
        autreHeure.value = false;
    }

    /** Appelé directement depuis le clic : iOS exige un geste pour demander la permission. */
    async function activer() {
        erreur.value = null;
        if (!heure.value) {
            erreur.value = 'Choisis une heure pour le rappel.';
            return;
        }
        try {
            await push.activer();
            if (push.etat.value === 'actif') {
                await push.enregistrerRappel('BilanQuotidien', { actif: true, heure: heure.value });
                toast({ title: 'Rappel activé', description: `Tu recevras un rappel chaque soir à ${heureLisible(heure.value)}.`, variant: 'custom' });
                await versAccueil();
            }
            // Permission refusée : l'état passe à « refuse », la page l'explique et propose de continuer.
        } catch (e) {
            erreur.value = (e as Error).message;
        }
    }

    return { etat, enCours, heure, autreHeure, heureProposee, erreur, demarrer, choisirHeure, activer, passer: versAccueil };
}
