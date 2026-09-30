import {ref} from 'vue';
import apiService from '@/shared/services/apiService';
import {useAuthStore} from '@/features/auth/store/auth';

interface Options {
    /** Appelé une fois l'accord enregistré (navigation vers l'accueil). */
    surAccord: () => void;
}

/** Recueil du consentement d'un compte existant : envoi de l'accord, puis mise à jour de la session. */
export function useConsentement({surAccord}: Options) {
    const auth = useAuthStore();
    const accepte = ref(false);
    const envoiEnCours = ref(false);
    const erreur = ref<string | null>(null);

    async function donnerAccord() {
        if (!accepte.value || envoiEnCours.value) return;
        envoiEnCours.value = true;
        erreur.value = null;
        try {
            const reponse = await apiService.postConsentement();
            auth.setConsentement(reponse.consentementAJour, reponse.tokenExpiry);
            surAccord();
        } catch {
            erreur.value = 'Ton accord n\'a pas pu être enregistré. Réessaie dans un instant.';
        } finally {
            envoiEnCours.value = false;
        }
    }

    return {accepte, envoiEnCours, erreur, donnerAccord};
}
