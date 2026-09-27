import { onBeforeUnmount, onMounted, ref, type Ref } from 'vue';
import { onBeforeRouteLeave, useRouter, type RouteLocationRaw } from 'vue-router';

/**
 * Évite de perdre une saisie non enregistrée : quitter la page (lien, retour) ouvre une demande de confirmation,
 * fermer l'onglet déclenche l'alerte native du navigateur.
 */
export const useConfirmationSortie = (aDesModifications: Ref<boolean>) => {
  const router = useRouter();
  const demandeOuverte = ref(false);
  let destination: RouteLocationRaw | null = null;
  let actionApresConfirmation: (() => void) | null = null;
  let sortieAutorisee = false;

  onBeforeRouteLeave((to) => {
    if (sortieAutorisee || !aDesModifications.value) return true;
    destination = to.fullPath;
    actionApresConfirmation = null;
    demandeOuverte.value = true;
    return false;
  });

  const avantDechargement = (event: BeforeUnloadEvent) => {
    if (!aDesModifications.value) return;
    event.preventDefault();
    event.returnValue = '';
  };
  onMounted(() => window.addEventListener('beforeunload', avantDechargement));
  onBeforeUnmount(() => window.removeEventListener('beforeunload', avantDechargement));

  /** Exécute une action interne (ex. fermer la saisie) après confirmation si des changements seraient perdus. */
  const demanderSiNecessaire = (action: () => void) => {
    if (!aDesModifications.value) {
      action();
      return;
    }
    destination = null;
    actionApresConfirmation = action;
    demandeOuverte.value = true;
  };

  const quitter = () => {
    demandeOuverte.value = false;
    if (actionApresConfirmation) {
      actionApresConfirmation();
    } else if (destination) {
      sortieAutorisee = true;
      void router.push(destination);
    }
  };

  const rester = () => {
    demandeOuverte.value = false;
  };

  return { demandeOuverte, demanderSiNecessaire, quitter, rester };
};
