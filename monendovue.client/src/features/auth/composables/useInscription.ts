import { computed, ref } from 'vue';
import { useAuthStore } from '@/features/auth/store/auth';
import { lancerPremiersPas } from '../utils/parcours';
import { CRITERES_MOT_DE_PASSE, emailPlausible, motDePasseValide } from '../utils/motDePasse';

/** Message du serveur (traduit par `AccountController`) reconnu comme concernant l'adresse ou le mot de passe. */
const MESSAGE_EMAIL_PRIS = /adresse e-mail est déjà utilisée|nom d'utilisateur est déjà pris/i;
const MESSAGE_EMAIL_INVALIDE = /adresse e-mail est invalide/i;
const MESSAGE_MOT_DE_PASSE = /mot de passe/i;

/**
 * Création d'un compte : champs, règles montrées pendant la saisie, erreurs au bon endroit (adresse, mot de passe, case de
 * consentement, réseau) et lancement des « premiers pas ». Les erreurs ne s'affichent qu'après une première tentative.
 */
export function useInscription() {
    const auth = useAuthStore();

    const email = ref('');
    const motDePasse = ref('');
    /** Case non cochée par défaut : l'accord doit être un geste explicite (RGPD, art. 9.2.a). */
    const consentement = ref(false);

    const tentative = ref(false);
    const envoiEnCours = ref(false);
    const erreurServeurEmail = ref<string | null>(null);
    const erreurServeurMotDePasse = ref<string | null>(null);
    const erreurGenerale = ref<string | null>(null);
    /** Vrai quand l'adresse est déjà prise : on propose alors de se connecter. */
    const emailDejaPris = ref(false);

    const criteres = computed(() => CRITERES_MOT_DE_PASSE.map((critere) => ({
        cle: critere.cle,
        libelle: critere.libelle,
        respecte: critere.respecte(motDePasse.value),
    })));

    const erreurEmail = computed(() => {
        if (erreurServeurEmail.value) return erreurServeurEmail.value;
        return tentative.value && !emailPlausible(email.value) ? 'Saisis une adresse e-mail valide.' : null;
    });

    const erreurMotDePasse = computed(() => {
        if (erreurServeurMotDePasse.value) return erreurServeurMotDePasse.value;
        return tentative.value && !motDePasseValide(motDePasse.value) ? 'Ton mot de passe ne respecte pas encore toutes les règles.' : null;
    });

    const erreurConsentement = computed(() => (tentative.value && !consentement.value ? 'Coche cette case pour créer ton compte.' : null));

    function reinitialiserErreursServeur() {
        erreurServeurEmail.value = null;
        erreurServeurMotDePasse.value = null;
        erreurGenerale.value = null;
        emailDejaPris.value = false;
    }

    function classerErreur(erreur: unknown) {
        const reponse = (erreur as { response?: { status?: number } })?.response;
        const estAxios = (erreur as { isAxiosError?: boolean })?.isAxiosError === true;
        const message = erreur instanceof Error ? erreur.message : '';

        if (!estAxios && MESSAGE_EMAIL_PRIS.test(message)) {
            erreurServeurEmail.value = 'Impossible de créer un compte avec cette adresse. Tu en as peut-être déjà un.';
            emailDejaPris.value = true;
        } else if (!estAxios && MESSAGE_EMAIL_INVALIDE.test(message)) {
            erreurServeurEmail.value = 'Cette adresse e-mail ne semble pas valide.';
        } else if (!estAxios && MESSAGE_MOT_DE_PASSE.test(message)) {
            erreurServeurMotDePasse.value = message;
        } else if (estAxios && !reponse) {
            erreurGenerale.value = 'Pas de connexion pour le moment. Ce que tu as saisi est conservé : réessaie dans un instant.';
        } else if (reponse?.status === 429) {
            erreurGenerale.value = 'Trop de tentatives. Patiente une minute avant de réessayer.';
        } else {
            erreurGenerale.value = 'Le service est momentanément indisponible. Réessaie dans un instant.';
        }
    }

    /** Crée le compte ; vrai si c'est fait (la session est alors ouverte). */
    async function creer(): Promise<boolean> {
        if (envoiEnCours.value) return false;
        tentative.value = true;
        reinitialiserErreursServeur();
        if (!emailPlausible(email.value) || !motDePasseValide(motDePasse.value) || !consentement.value) return false;

        envoiEnCours.value = true;
        try {
            await auth.register(email.value.trim(), motDePasse.value, consentement.value);
            lancerPremiersPas();
            return true;
        } catch (erreur) {
            console.error(erreur);
            classerErreur(erreur);
            return false;
        } finally {
            envoiEnCours.value = false;
        }
    }

    return {
        email, motDePasse, consentement, criteres, envoiEnCours, emailDejaPris,
        erreurEmail, erreurMotDePasse, erreurConsentement, erreurGenerale, creer,
    };
}
