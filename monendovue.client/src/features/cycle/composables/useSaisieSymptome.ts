import { computed, ref } from 'vue';
import { format } from 'date-fns';
import type { SymptomeCycle } from '../types/symptome-cycle';
import { urlDeApi } from '@/shared/services/apiBase';

export type Moment = 'maintenant' | 'matin' | 'autre';

/** Heure retenue pour « Ce matin ». */
const HEURE_DU_MATIN = '08:00';

/** Formulaire d'un symptôme (sans la photo, gérée par `usePhotoSymptome`) : ajout ou modification. */
export function useSaisieSymptome({ maintenant = () => new Date() }: { maintenant?: () => Date } = {}) {
    const id = ref<number | null>(null);
    const type = ref<string | null>(null);
    const intensite = ref<number | null>(null);
    const moment = ref<Moment>('maintenant');
    const jour = ref('');
    const heure = ref('');
    const commentaire = ref('');
    /** Photo déjà enregistrée (modification) : conservée si aucune nouvelle n'est choisie. */
    const photoExistante = ref<string | null>(null);

    const enModification = computed(() => id.value !== null);
    const complete = computed(() => type.value !== null && intensite.value !== null
        && (moment.value !== 'autre' || (jour.value !== '' && heure.value !== '')));

    function preparerAjout(typeImpose: string | null = null) {
        id.value = null;
        type.value = typeImpose;
        intensite.value = null;
        moment.value = 'maintenant';
        jour.value = format(maintenant(), 'yyyy-MM-dd');
        heure.value = format(maintenant(), 'HH:mm');
        commentaire.value = '';
        photoExistante.value = null;
    }

    function preparerModification(entree: SymptomeCycle) {
        id.value = entree.id;
        type.value = entree.typeSymptome;
        intensite.value = entree.intensite;
        moment.value = 'autre';
        jour.value = entree.date.slice(0, 10);
        heure.value = entree.date.slice(11, 16);
        commentaire.value = entree.commentaire?.trim() ?? '';
        // La photo est servie par l'API (réservée à sa propriétaire), jamais lue directement dans le stockage.
        photoExistante.value = entree.photoUrl ? urlDeApi(`Acne/photos/${entree.id}`) : null;
    }

    /** Date locale sans fuseau selon le moment choisi (jamais une Date, qui glisserait en UTC). */
    function dateChoisie(): string {
        if (moment.value === 'maintenant') return format(maintenant(), "yyyy-MM-dd'T'HH:mm:ss");
        if (moment.value === 'matin') return `${format(maintenant(), 'yyyy-MM-dd')}T${HEURE_DU_MATIN}:00`;
        return `${jour.value}T${heure.value}:00`;
    }

    return { id, type, intensite, moment, jour, heure, commentaire, photoExistante, enModification, complete, preparerAjout, preparerModification, dateChoisie };
}
