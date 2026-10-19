import { ref } from 'vue';
import { categoriesDuBilan, choixParDefaut, type CategorieBilan, type ChoixCategories } from '../config/categories';

/** Clé du `localStorage` : un réglage d'affichage propre à l'appareil, rien n'est envoyé au serveur. */
export const CLE_CATEGORIES_BILAN = 'monendo-categories-bilan';

function lire(): ChoixCategories {
  const choix = choixParDefaut();
  try {
    const brut = localStorage.getItem(CLE_CATEGORIES_BILAN);
    if (!brut) return choix;
    const lu = JSON.parse(brut) as Partial<Record<string, unknown>>;
    // Seules les catégories connues et les vrais booléens comptent : une valeur abîmée revient au réglage par défaut.
    for (const { id } of categoriesDuBilan) {
      const valeur = lu[id];
      if (typeof valeur === 'boolean') choix[id] = valeur;
    }
  } catch {
    // Stockage indisponible ou illisible (navigation privée, données effacées) : réglages par défaut.
  }
  return choix;
}

function ecrire(choix: ChoixCategories): void {
  try {
    localStorage.setItem(CLE_CATEGORIES_BILAN, JSON.stringify(choix));
  } catch {
    // Le réglage reste valable jusqu'au rechargement de la page.
  }
}

const choix = ref<ChoixCategories>(lire());

/**
 * Catégories du bilan que la personne a choisi d'afficher. Un état partagé : le réglage de Paramètres et la saisie du bilan
 * lisent le même. Masquer une catégorie ne supprime rien : les réponses déjà enregistrées restent dans l'historique et l'export.
 */
export function useCategoriesBilan() {
  const estActive = (id: CategorieBilan): boolean => choix.value[id];

  function definir(id: CategorieBilan, actif: boolean): void {
    choix.value = { ...choix.value, [id]: actif };
    ecrire(choix.value);
  }

  function reinitialiser(): void {
    choix.value = choixParDefaut();
    ecrire(choix.value);
  }

  /** Relit le stockage (nouvel onglet ou test) : à appeler quand la page s'ouvre. */
  function recharger(): void {
    choix.value = lire();
  }

  return { choix, estActive, definir, reinitialiser, recharger };
}
