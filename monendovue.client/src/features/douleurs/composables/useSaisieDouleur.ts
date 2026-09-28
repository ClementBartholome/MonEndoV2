import { computed, ref } from 'vue';
import { format } from 'date-fns';
import type { DonneesDouleur, DonneesDouleurModification } from '../types/donnees-douleur';
import { commentaireAffiche } from '../utils/douleurs';

export type Moment = 'maintenant' | 'matin' | 'autre';

/** Heure retenue pour « Ce matin ». */
const HEURE_DU_MATIN = '08:00';

/** Formulaire de saisie d'une douleur : ajout (valeurs par défaut) ou modification d'une entrée existante. */
export function useSaisieDouleur({ maintenant = () => new Date() }: { maintenant?: () => Date } = {}) {
    const id = ref<number | null>(null);
    const type = ref<string | null>(null);
    const intensite = ref<number | null>(null);
    const moment = ref<Moment>('maintenant');
    const jour = ref('');
    const heure = ref('');
    const commentaire = ref('');

    const enModification = computed(() => id.value !== null);
    const complete = computed(() => type.value !== null && intensite.value !== null
        && (moment.value !== 'autre' || (jour.value !== '' && heure.value !== '')));

    function preparerAjout() {
        id.value = null;
        type.value = null;
        intensite.value = null;
        moment.value = 'maintenant';
        jour.value = format(maintenant(), 'yyyy-MM-dd');
        heure.value = format(maintenant(), 'HH:mm');
        commentaire.value = '';
    }

    function preparerModification(entree: DonneesDouleur) {
        id.value = entree.id;
        type.value = entree.typeDouleur;
        intensite.value = entree.intensite;
        moment.value = 'autre';
        jour.value = entree.date.slice(0, 10);
        heure.value = entree.date.slice(11, 16);
        commentaire.value = commentaireAffiche(entree.commentaire) ?? '';
    }

    /** Date locale sans fuseau selon le moment choisi (jamais une Date, qui glisserait en UTC). */
    function dateChoisie(): string {
        if (moment.value === 'maintenant') return format(maintenant(), "yyyy-MM-dd'T'HH:mm:ss");
        if (moment.value === 'matin') return `${format(maintenant(), 'yyyy-MM-dd')}T${HEURE_DU_MATIN}:00`;
        return `${jour.value}T${heure.value}:00`;
    }

    function saisie(): DonneesDouleurModification {
        return {
            typeDouleur: type.value!,
            intensite: intensite.value!,
            date: dateChoisie(),
            commentaire: commentaire.value.trim() || null,
        };
    }

    return { id, type, intensite, moment, jour, heure, commentaire, enModification, complete, preparerAjout, preparerModification, saisie };
}
