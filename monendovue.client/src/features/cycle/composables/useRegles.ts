import { computed, ref } from 'vue';
import { format, startOfMonth } from 'date-fns';
import apiService from '@/shared/services/apiService';
import { derniereDemande } from '@/shared/utils/derniereDemande';
import type { CycleDuMois, DetailJourRegles } from '../types/cycle';
import { calendrierDuMois, type CaseCalendrier } from '../utils/cycle';

/** Onglet Règles : mois affiché, cycle en cours, historique, et jours de règles ajoutés ou retirés d'un geste. */
export function useRegles({ maintenant = () => new Date() }: { maintenant?: () => Date } = {}) {
    const mois = ref(startOfMonth(maintenant()));
    const donnees = ref<CycleDuMois | null>(null);
    const chargement = ref(true);
    const erreur = ref(false);
    /** Jours (AAAA-MM-JJ) dont l'ajout ou le retrait est en cours d'envoi. */
    const enCoursDEnvoi = ref(new Set<string>());
    /** Cycles demandés au serveur : 6, puis 6 de plus à chaque « Voir plus » (jamais tout l'historique d'un coup). */
    const nombreCycles = ref(6);

    /** Jour dont on précise le flux et les caillots (choisi ou ajouté en dernier) ; s'il n'est plus noté, le dernier jour noté du mois. */
    const jourChoisi = ref<string | null>(null);
    const enCoursDeDetail = ref(false);

    const calendrier = computed(() => calendrierDuMois(mois.value, donnees.value?.joursDeRegles ?? [], maintenant(), donnees.value?.detailsJours ?? []));
    const joursNotes = computed(() => [...(donnees.value?.joursDeRegles ?? [])].sort((a, b) => a.localeCompare(b)));
    const jourDetail = computed(() => (jourChoisi.value && joursNotes.value.includes(jourChoisi.value) ? jourChoisi.value : (joursNotes.value[joursNotes.value.length - 1] ?? null)));
    const detailDuJour = computed<DetailJourRegles | null>(() => donnees.value?.detailsJours.find((d) => d.jour === jourDetail.value) ?? null);
    const aujourdhui = () => format(maintenant(), 'yyyy-MM-dd');
    const aujourdhuiNote = computed(() => donnees.value?.enCours?.jourDeRegles != null);

    const nouvelleDemande = derniereDemande();

    async function charger() {
        const estCourante = nouvelleDemande();
        chargement.value = donnees.value === null;
        erreur.value = false;
        try {
            const recu = await apiService.getCycle(aujourdhui(), format(mois.value, 'yyyy-MM-dd'), nombreCycles.value);
            if (estCourante()) donnees.value = recu;
        } catch {
            if (estCourante()) erreur.value = true;
        } finally {
            if (estCourante()) chargement.value = false;
        }
    }

    /** Mois choisi (flèches ou choix direct) ; jamais après le mois en cours. */
    async function allerAuMois(choisi: Date) {
        const debut = startOfMonth(choisi);
        if (debut > startOfMonth(maintenant())) return;
        mois.value = debut;
        await charger();
    }

    /**
     * Ajoute ou retire un jour de règles. Le calendrier change tout de suite ; le cycle en cours et l'historique
     * sont recalculés par le serveur ensuite. En cas d'échec, le jour revient à son état et l'erreur remonte.
     */
    async function basculer(jour: Pick<CaseCalendrier, 'cle' | 'regles' | 'futur'>) {
        if (jour.futur || enCoursDEnvoi.value.has(jour.cle) || !donnees.value) return;
        const avant = donnees.value.joursDeRegles;
        const detailsAvant = donnees.value.detailsJours;
        donnees.value.joursDeRegles = jour.regles ? avant.filter((j) => j !== jour.cle) : [...avant, jour.cle];
        if (jour.regles) donnees.value.detailsJours = detailsAvant.filter((d) => d.jour !== jour.cle);
        else jourChoisi.value = jour.cle;
        enCoursDEnvoi.value = new Set([...enCoursDEnvoi.value, jour.cle]);
        try {
            if (jour.regles) await apiService.deleteJourDeRegles(jour.cle);
            else await apiService.putJourDeRegles(jour.cle);
            await charger();
        } catch (e) {
            donnees.value.joursDeRegles = avant;
            donnees.value.detailsJours = detailsAvant;
            throw e;
        } finally {
            enCoursDEnvoi.value = new Set([...enCoursDEnvoi.value].filter((j) => j !== jour.cle));
        }
    }

    function choisirJour(cle: string) {
        jourChoisi.value = cle;
    }

    /**
     * Flux et/ou caillots du jour choisi : enregistrés dès le geste (les deux champs sont renvoyés, le serveur les remplace).
     * Le choix s'affiche tout de suite ; en cas d'échec il revient à son état et l'erreur remonte.
     */
    async function preciser(changement: Partial<Pick<DetailJourRegles, 'flux' | 'caillots'>>) {
        const jour = jourDetail.value;
        if (!jour || !donnees.value || enCoursDeDetail.value) return;
        const avant = donnees.value.detailsJours;
        const actuel = avant.find((d) => d.jour === jour) ?? { jour, flux: null, caillots: null };
        const suivant: DetailJourRegles = { ...actuel, ...changement };
        const sansLeJour = avant.filter((d) => d.jour !== jour);
        donnees.value.detailsJours = suivant.flux === null && suivant.caillots === null ? sansLeJour : [...sansLeJour, suivant];
        enCoursDeDetail.value = true;
        try {
            await apiService.putDetailsJourDeRegles(jour, { flux: suivant.flux, caillots: suivant.caillots });
        } catch (e) {
            donnees.value.detailsJours = avant;
            throw e;
        } finally {
            enCoursDeDetail.value = false;
        }
    }

    async function voirPlusDeCycles() {
        nombreCycles.value += 6;
        await charger();
    }

    /** « Règles aujourd'hui » depuis la carte du cycle en cours, quel que soit le mois affiché. */
    const noterAujourdhui = () => basculer({ cle: aujourdhui(), regles: false, futur: false });

    return {
        mois, donnees, chargement, erreur, enCoursDEnvoi, calendrier, aujourdhuiNote, charger, allerAuMois, basculer, noterAujourdhui, voirPlusDeCycles,
        joursNotes, jourDetail, detailDuJour, enCoursDeDetail, choisirJour, preciser,
    };
}
