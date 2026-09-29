import { computed, ref } from 'vue';
import { format, subDays } from 'date-fns';
import type { Activite, ActiviteSaisie, EffetActivite, NiveauActivite } from '../types/activite';
import { DUREES, TYPES_ACTIVITE } from '../utils/activite';

export type Moment = 'aujourdhui' | 'hier' | 'autre';

/** Heure retenue quand seul le jour est choisi (« Aujourd'hui », « Hier ») : la fin de journée est la plus probable. */
const HEURE_PAR_DEFAUT = '18:00';

/** Formulaire d'une activité : ajout (valeurs par défaut) ou modification. */
export function useSaisieActivite({ maintenant = () => new Date() }: { maintenant?: () => Date } = {}) {
    const id = ref<number | null>(null);
    /** Type proposé choisi, ou « Autre » avec un libellé libre. */
    const type = ref<string | null>(null);
    const typeLibre = ref('');
    const duree = ref<number | null>(30);
    const niveau = ref<NiveauActivite | null>(null);
    const effet = ref<EffetActivite>('NonRenseigne');
    const moment = ref<Moment>('aujourdhui');
    const jour = ref('');
    const commentaire = ref('');

    const enModification = computed(() => id.value !== null);
    const typeChoisi = computed(() => (type.value === 'Autre' ? typeLibre.value.trim() : type.value));
    const complete = computed(() => !!typeChoisi.value && !!duree.value && duree.value > 0 && niveau.value !== null
        && (moment.value !== 'autre' || jour.value !== ''));

    function preparerAjout() {
        id.value = null;
        type.value = null;
        typeLibre.value = '';
        duree.value = 30;
        niveau.value = null;
        effet.value = 'NonRenseigne';
        moment.value = 'aujourdhui';
        jour.value = format(maintenant(), 'yyyy-MM-dd');
        commentaire.value = '';
    }

    function preparerModification(activite: Activite) {
        id.value = activite.id;
        const propose = TYPES_ACTIVITE.some((t) => t.valeur === activite.type);
        type.value = propose ? activite.type : 'Autre';
        typeLibre.value = propose ? '' : activite.type;
        duree.value = activite.duree;
        niveau.value = activite.niveau;
        effet.value = activite.effet;
        moment.value = 'autre';
        jour.value = activite.date.slice(0, 10);
        commentaire.value = activite.commentaire ?? '';
    }

    /** Date locale sans fuseau ; en modification, l'heure d'origine est conservée. */
    function dateChoisie(dateOrigine: string | null): string {
        const heure = dateOrigine?.slice(11, 19) ?? `${HEURE_PAR_DEFAUT}:00`;
        if (moment.value === 'aujourdhui') return `${format(maintenant(), 'yyyy-MM-dd')}T${heure}`;
        if (moment.value === 'hier') return `${format(subDays(maintenant(), 1), 'yyyy-MM-dd')}T${heure}`;
        return `${jour.value}T${heure}`;
    }

    function saisie(dateOrigine: string | null): ActiviteSaisie {
        return {
            type: typeChoisi.value!,
            date: dateChoisie(dateOrigine),
            duree: duree.value!,
            niveau: niveau.value!,
            effet: effet.value,
            commentaire: commentaire.value.trim() || null,
        };
    }

    const dureeProposee = computed(() => duree.value !== null && DUREES.includes(duree.value));

    return {
        id, type, typeLibre, duree, niveau, effet, moment, jour, commentaire,
        enModification, complete, dureeProposee, preparerAjout, preparerModification, saisie,
    };
}
