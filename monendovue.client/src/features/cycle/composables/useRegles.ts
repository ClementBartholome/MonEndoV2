import { computed, ref } from 'vue';
import { addMonths, format, startOfMonth } from 'date-fns';
import apiService from '@/shared/services/apiService';
import { estMoisCourant } from '@/shared/utils/jours';
import type { CycleDuMois } from '../types/cycle';
import { calendrierDuMois, type CaseCalendrier } from '../utils/cycle';

/** Onglet Règles : mois affiché, cycle en cours, historique, et jours de règles ajoutés ou retirés d'un geste. */
export function useRegles({ maintenant = () => new Date() }: { maintenant?: () => Date } = {}) {
    const mois = ref(startOfMonth(maintenant()));
    const donnees = ref<CycleDuMois | null>(null);
    const chargement = ref(true);
    const erreur = ref(false);
    /** Jours (AAAA-MM-JJ) dont l'ajout ou le retrait est en cours d'envoi. */
    const enCoursDEnvoi = ref(new Set<string>());

    const calendrier = computed(() => calendrierDuMois(mois.value, donnees.value?.joursDeRegles ?? [], maintenant()));
    const moisSuivantPossible = computed(() => !estMoisCourant(mois.value, maintenant()));
    const aujourdhui = () => format(maintenant(), 'yyyy-MM-dd');
    const aujourdhuiNote = computed(() => donnees.value?.enCours?.jourDeRegles != null);

    async function charger() {
        chargement.value = donnees.value === null;
        erreur.value = false;
        try {
            donnees.value = await apiService.getCycle(aujourdhui(), format(mois.value, 'yyyy-MM-dd'));
        } catch {
            erreur.value = true;
        } finally {
            chargement.value = false;
        }
    }

    async function changerDeMois(decalage: number) {
        if (decalage > 0 && !moisSuivantPossible.value) return;
        mois.value = addMonths(mois.value, decalage);
        await charger();
    }

    /**
     * Ajoute ou retire un jour de règles. Le calendrier change tout de suite ; le cycle en cours et l'historique
     * sont recalculés par le serveur ensuite. En cas d'échec, le jour revient à son état et l'erreur remonte.
     */
    async function basculer(jour: Pick<CaseCalendrier, 'cle' | 'regles' | 'futur'>) {
        if (jour.futur || enCoursDEnvoi.value.has(jour.cle) || !donnees.value) return;
        const avant = donnees.value.joursDeRegles;
        donnees.value.joursDeRegles = jour.regles ? avant.filter((j) => j !== jour.cle) : [...avant, jour.cle];
        enCoursDEnvoi.value = new Set([...enCoursDEnvoi.value, jour.cle]);
        try {
            if (jour.regles) await apiService.deleteJourDeRegles(jour.cle);
            else await apiService.putJourDeRegles(jour.cle);
            await charger();
        } catch (e) {
            donnees.value.joursDeRegles = avant;
            throw e;
        } finally {
            enCoursDEnvoi.value = new Set([...enCoursDEnvoi.value].filter((j) => j !== jour.cle));
        }
    }

    /** « Règles aujourd'hui » depuis la carte du cycle en cours, quel que soit le mois affiché. */
    const noterAujourdhui = () => basculer({ cle: aujourdhui(), regles: false, futur: false });

    return { mois, donnees, chargement, erreur, enCoursDEnvoi, calendrier, moisSuivantPossible, aujourdhuiNote, charger, changerDeMois, basculer, noterAujourdhui };
}
