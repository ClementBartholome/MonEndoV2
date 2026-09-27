import {ref} from 'vue';
import type {AxiosError} from 'axios';
import apiService from '@/shared/services/apiService';
import {useAuthStore} from '@/features/auth/store/auth';

interface Options {
    /** Appelé une fois le compte supprimé et la session fermée (retour à la connexion). */
    surSuppression: () => void;
}

/** Suppression définitive du compte, confirmée par le mot de passe. */
export function useSuppressionCompte({surSuppression}: Options) {
    const auth = useAuthStore();
    const motDePasse = ref('');
    const suppressionEnCours = ref(false);
    const erreurSuppression = ref<string | null>(null);

    function reinitialiser() {
        motDePasse.value = '';
        erreurSuppression.value = null;
    }

    async function supprimerMonCompte() {
        if (!motDePasse.value || suppressionEnCours.value) return;
        suppressionEnCours.value = true;
        erreurSuppression.value = null;
        try {
            await apiService.postSuppressionCompte(motDePasse.value);
            auth.clearAuth();
            surSuppression();
        } catch (error_) {
            const message = (error_ as AxiosError<{ message?: string }>).response?.data?.message;
            erreurSuppression.value = message ?? 'La suppression n\'a pas pu aboutir. Réessaie dans quelques minutes.';
        } finally {
            suppressionEnCours.value = false;
        }
    }

    return {motDePasse, suppressionEnCours, erreurSuppression, reinitialiser, supprimerMonCompte};
}
