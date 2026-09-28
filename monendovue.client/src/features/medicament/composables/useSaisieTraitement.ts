import { computed, ref } from 'vue';
import { format } from 'date-fns';
import type { FrequencePrise, JourSemaine, Traitement, TraitementSaisie, TypeTraitement } from '../types/traitements';

/** Formulaire d'un traitement : ajout (valeurs par défaut) ou modification. */
export function useSaisieTraitement({ maintenant = () => new Date() }: { maintenant?: () => Date } = {}) {
    const id = ref<number | null>(null);
    const type = ref<TypeTraitement>('Medicamenteux');
    const nom = ref('');
    const dose = ref('');
    const frequence = ref<FrequencePrise>('ChaqueJour');
    const joursSemaine = ref<JourSemaine[]>([]);
    const intervalleJours = ref(2);
    const horaires = ref<string[]>(['08:00']);
    const dateDebut = ref('');
    const dateFin = ref('');

    const enModification = computed(() => id.value !== null);
    const avecHoraires = computed(() => frequence.value !== 'AuBesoin');
    const complete = computed(() => nom.value.trim() !== '' && dateDebut.value !== ''
        && (!avecHoraires.value || (horaires.value.length > 0 && horaires.value.every((h) => h !== '')))
        && (frequence.value !== 'CertainsJours' || joursSemaine.value.length > 0));

    function preparerAjout() {
        id.value = null;
        type.value = 'Medicamenteux';
        nom.value = '';
        dose.value = '';
        frequence.value = 'ChaqueJour';
        joursSemaine.value = [];
        intervalleJours.value = 2;
        horaires.value = ['08:00'];
        dateDebut.value = format(maintenant(), 'yyyy-MM-dd');
        dateFin.value = '';
    }

    function preparerModification(traitement: Traitement) {
        id.value = traitement.id;
        type.value = traitement.type;
        nom.value = traitement.nom;
        dose.value = traitement.dose ?? '';
        frequence.value = traitement.frequence;
        joursSemaine.value = [...traitement.joursSemaine];
        intervalleJours.value = traitement.intervalleJours ?? 2;
        horaires.value = traitement.horaires.length ? [...traitement.horaires] : ['08:00'];
        dateDebut.value = traitement.dateDebut;
        dateFin.value = traitement.dateFin ?? '';
    }

    function basculerJour(jour: JourSemaine) {
        joursSemaine.value = joursSemaine.value.includes(jour)
            ? joursSemaine.value.filter((j) => j !== jour)
            : [...joursSemaine.value, jour];
    }

    function ajouterHoraire() {
        horaires.value = [...horaires.value, '20:00'];
    }

    function retirerHoraire(index: number) {
        horaires.value = horaires.value.filter((_, i) => i !== index);
    }

    function saisie(): TraitementSaisie {
        return {
            nom: nom.value.trim(),
            type: type.value,
            dose: dose.value.trim() || null,
            frequence: frequence.value,
            joursSemaine: frequence.value === 'CertainsJours' ? joursSemaine.value : [],
            intervalleJours: frequence.value === 'TousLesNJours' ? intervalleJours.value : null,
            horaires: avecHoraires.value ? [...new Set(horaires.value)].sort().map((h) => `${h}:00`) : [],
            dateDebut: dateDebut.value,
            dateFin: dateFin.value || null,
        };
    }

    return {
        id, type, nom, dose, frequence, joursSemaine, intervalleJours, horaires, dateDebut, dateFin,
        enModification, avecHoraires, complete,
        preparerAjout, preparerModification, basculerJour, ajouterHoraire, retirerHoraire, saisie,
    };
}
