import { computed, ref, watch } from 'vue';
import { addDays, addMonths, differenceInCalendarDays, format, parseISO } from 'date-fns';
import apiService from '@/shared/services/apiService';
import { enregistrerQuestions, lireQuestions } from '../services/questionsRendezVous';
import type { Rubrique } from '../types/synthese';
import { creerPdf, libellePeriode, nomDuFichier } from '../utils/pdfRendezVous';

export type ChoixPeriode = '1mois' | '3mois' | 'autre';

/** Période la plus longue acceptée par le serveur (`SyntheseRendezVousService.JoursMaximum`). */
const JOURS_MAXIMUM = 366;

const cle = (date: Date) => format(date, 'yyyy-MM-dd');

/** Préparation du PDF d'un rendez-vous : période, rubriques à inclure, questions, puis création du document. */
export function usePreparationRendezVous() {
    const aujourdhui = cle(new Date());
    const ilYA = (mois: number) => cle(addDays(addMonths(parseISO(aujourdhui), -mois), 1));

    const choix = ref<ChoixPeriode>('3mois');
    /** Bornes de la période « Autre » (AAAA-MM-JJ). */
    const du = ref(ilYA(6));
    const au = ref(aujourdhui);

    const periode = computed(() => {
        if (choix.value === '1mois') return { du: ilYA(1), au: aujourdhui };
        if (choix.value === '3mois') return { du: ilYA(3), au: aujourdhui };
        return { du: du.value, au: au.value };
    });

    /** Message à afficher si la période choisie ne peut pas être demandée ; null si elle est valide. */
    const erreurPeriode = computed(() => {
        const { du: debut, au: fin } = periode.value;
        if (!debut || !fin) return 'Choisis un début et une fin.';
        if (fin > aujourdhui) return 'La période ne peut pas aller au-delà d\'aujourd\'hui.';
        if (fin < debut) return 'La fin de la période précède son début.';
        if (differenceInCalendarDays(parseISO(fin), parseISO(debut)) >= JOURS_MAXIMUM) return 'La période ne peut pas dépasser un an.';
        return null;
    });

    const libelle = computed(() => (erreurPeriode.value ? '' : libellePeriode(periode.value.du, periode.value.au)));

    const rubriques = ref<Record<Rubrique, boolean>>({
        douleurs: true, cycle: true, traitements: true, bilans: true, activite: false, transit: false,
    });
    const aucuneRubrique = computed(() => !Object.values(rubriques.value).some(Boolean));

    const questions = ref(lireQuestions());
    watch(questions, enregistrerQuestions);

    const creation = ref(false);
    const erreur = ref(false);

    const peutCreer = computed(() => !erreurPeriode.value && !aucuneRubrique.value && !creation.value);

    /** Demande la synthèse de la période puis enregistre le PDF ; renvoie vrai si le document a été créé. */
    async function creer(): Promise<boolean> {
        if (!peutCreer.value) return false;
        creation.value = true;
        erreur.value = false;
        try {
            const synthese = await apiService.getSyntheseRendezVous(periode.value.du, periode.value.au);
            const doc = await creerPdf(synthese, { rubriques: rubriques.value, questions: questions.value, creeLe: new Date() });
            doc.save(nomDuFichier(synthese));
            return true;
        } catch {
            erreur.value = true;
            return false;
        } finally {
            creation.value = false;
        }
    }

    return { aujourdhui, choix, du, au, libelle, erreurPeriode, rubriques, aucuneRubrique, questions, creation, erreur, peutCreer, creer };
}
