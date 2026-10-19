import { computed, ref } from 'vue';
import { addDays, endOfMonth, endOfWeek, format, isSameDay, startOfDay, startOfMonth, startOfWeek } from 'date-fns';
import { useRouter } from 'vue-router';
import apiService from '@/shared/services/apiService';
import type { EvenementAgenda, StatutLiaisonAgenda } from '../types/agenda';
import { debutDe, estAVenir, estPasse } from '../utils/rendezVous';

export type VueAgenda = 'liste' | 'passes' | 'mois';

/** Ce que la page montre : chargement, un état particulier (agenda à lier, calendrier à choisir, panne), ou les rendez-vous. */
export type EtatAgenda = 'chargement' | 'non-lie' | 'sans-calendrier' | 'indisponible' | 'pret';

/**
 * Le serveur refuse une période de plus de 62 jours (en UTC) : la liste « à venir » avance par fenêtres de 60 jours,
 * ce qui laisse la marge d'une heure de changement d'heure (62 jours locaux en font 62 et une heure à l'automne).
 */
const JOURS_PAR_FENETRE = 60;
const FENETRES_MAXIMUM = 4;
/** Rendez-vous passés : six fenêtres de 60 jours, soit à peu près l'année que couvre au plus un résumé. */
const FENETRES_PASSEES_MAXIMUM = 6;

interface Options {
    /** Heure locale (injectée pour les tests). */
    maintenant?: () => Date;
}

/** Page Agenda : statut de la liaison, rendez-vous à venir (liste), passés, mois affiché, rendez-vous ouvert et préparation du PDF. */
export function useAgenda({ maintenant = () => new Date() }: Options = {}) {
    const router = useRouter();

    const etat = ref<EtatAgenda>('chargement');
    const statut = ref<StatutLiaisonAgenda | null>(null);
    const vue = ref<VueAgenda>('liste');

    const evenements = ref<EvenementAgenda[]>([]);
    const fenetres = ref(0);
    const chargementSuite = ref(false);
    const erreurSuite = ref(false);

    const passes = ref<EvenementAgenda[]>([]);
    const fenetresPassees = ref(0);
    const chargementPasses = ref(false);
    const erreurPasses = ref(false);

    const moisAffiche = ref(startOfMonth(maintenant()));
    const evenementsMois = ref<EvenementAgenda[]>([]);
    const chargementMois = ref(false);
    const erreurMois = ref(false);
    let derniereDemandeMois = 0;
    const jourChoisi = ref(startOfDay(maintenant()));

    const selection = ref<EvenementAgenda | null>(null);
    const preparationEnCours = ref(false);

    const evenementsDuJour = computed(() =>
        evenementsMois.value
            .filter((evenement) => isSameDay(debutDe(evenement), jourChoisi.value))
            .sort((a, b) => debutDe(a).getTime() - debutDe(b).getTime()));

    /** Prochain rendez-vous à heure fixe : celui qui porte « Préparer ce rendez-vous » dans la liste. */
    const prochain = computed(() =>
        [...evenements.value]
            .filter((evenement) => estAVenir(evenement, maintenant()))
            .sort((a, b) => debutDe(a).getTime() - debutDe(b).getTime())[0] ?? null);

    async function lireFenetre(indice: number): Promise<EvenementAgenda[] | null> {
        const debut = addDays(startOfDay(maintenant()), indice * JOURS_PAR_FENETRE);
        return apiService.getEvenementsAgenda(debut, addDays(debut, JOURS_PAR_FENETRE));
    }

    /** Fenêtre de 60 jours qui finit à la fin d'aujourd'hui, puis les précédentes ; seuls les rendez-vous commencés sont gardés. */
    async function lireFenetrePassee(indice: number): Promise<EvenementAgenda[]> {
        const fin = addDays(startOfDay(maintenant()), 1 - indice * JOURS_PAR_FENETRE);
        const lus = (await apiService.getEvenementsAgenda(addDays(fin, -JOURS_PAR_FENETRE), fin)) ?? [];
        // Une journée entière est passée la veille au plus tard ; un rendez-vous à heure fixe, dès qu'il a commencé.
        return lus.filter((evenement) => (evenement.journeeEntiere
            ? debutDe(evenement) < startOfDay(maintenant())
            : estPasse(evenement, maintenant())));
    }

    /** Rendez-vous passés : la première fenêtre à l'ouverture de la vue, puis une de plus à chaque « plus anciens ». */
    async function chargerPasses(depuisLeDebut: boolean) {
        if (chargementPasses.value) return;
        if (!depuisLeDebut && fenetresPassees.value >= FENETRES_PASSEES_MAXIMUM) return;
        chargementPasses.value = true;
        erreurPasses.value = false;
        const indice = depuisLeDebut ? 0 : fenetresPassees.value;
        try {
            const lus = await lireFenetrePassee(indice);
            const deja = depuisLeDebut ? [] : passes.value;
            const dejaLus = new Set(deja.map((evenement) => evenement.id));
            passes.value = [...deja, ...lus.filter((evenement) => !dejaLus.has(evenement.id))];
            fenetresPassees.value = indice + 1;
        } catch {
            erreurPasses.value = true;
        } finally {
            chargementPasses.value = false;
        }
    }

    const voirPlusAnciens = () => chargerPasses(false);

    async function charger() {
        etat.value = 'chargement';
        try {
            statut.value = await apiService.getStatutLiaisonAgenda();
        } catch {
            etat.value = 'indisponible';
            return;
        }
        if (!statut.value.liee) {
            etat.value = 'non-lie';
            return;
        }
        if (!statut.value.calendrierId) {
            etat.value = 'sans-calendrier';
            return;
        }
        try {
            const premiers = await lireFenetre(0);
            if (premiers === null) {
                etat.value = 'sans-calendrier';
                return;
            }
            evenements.value = premiers;
            fenetres.value = 1;
            etat.value = 'pret';
        } catch {
            etat.value = 'indisponible';
        }
    }

    /** Les deux mois suivants, à la demande (« Voir plus loin »). */
    async function voirPlusLoin() {
        if (chargementSuite.value || fenetres.value >= FENETRES_MAXIMUM) return;
        chargementSuite.value = true;
        erreurSuite.value = false;
        try {
            const suite = await lireFenetre(fenetres.value);
            // Google renvoie tout événement qui chevauche le début de la fenêtre : un événement à cheval sur deux fenêtres revient deux fois.
            const dejaLus = new Set(evenements.value.map((evenement) => evenement.id));
            evenements.value = [...evenements.value, ...(suite ?? []).filter((evenement) => !dejaLus.has(evenement.id))];
            fenetres.value += 1;
        } catch {
            erreurSuite.value = true;
        } finally {
            chargementSuite.value = false;
        }
    }

    /** Six semaines au plus autour du mois affiché (lundi en premier), soit moins de 62 jours. */
    async function chargerMois() {
        // Seule la dernière demande compte : deux clics rapides sur « mois suivant » ne doivent pas laisser la réponse la plus lente l'emporter.
        const numero = ++derniereDemandeMois;
        chargementMois.value = true;
        erreurMois.value = false;
        const debut = startOfWeek(moisAffiche.value, { weekStartsOn: 1 });
        const fin = addDays(endOfWeek(endOfMonth(moisAffiche.value), { weekStartsOn: 1 }), 1);
        try {
            const lus = (await apiService.getEvenementsAgenda(debut, fin)) ?? [];
            if (numero === derniereDemandeMois) evenementsMois.value = lus;
        } catch {
            if (numero === derniereDemandeMois) {
                erreurMois.value = true;
                evenementsMois.value = [];
            }
        } finally {
            if (numero === derniereDemandeMois) chargementMois.value = false;
        }
    }

    async function changerVue(nouvelle: VueAgenda) {
        vue.value = nouvelle;
        if (nouvelle === 'mois') await chargerMois();
        if (nouvelle === 'passes' && fenetresPassees.value === 0) await chargerPasses(true);
    }

    async function changerMois(decalage: number) {
        moisAffiche.value = startOfMonth(new Date(moisAffiche.value.getFullYear(), moisAffiche.value.getMonth() + decalage, 1));
        jourChoisi.value = moisAffiche.value;
        await chargerMois();
    }

    function choisirJour(jour: Date) {
        jourChoisi.value = startOfDay(jour);
    }

    async function actualiser() {
        if (vue.value === 'mois') await chargerMois();
        else if (vue.value === 'passes') await chargerPasses(true);
        else await charger();
    }

    /**
     * Ouvre l'export réglé pour ce rendez-vous. Le rendez-vous précédent donne le début de la période ; s'il est introuvable
     * (ou si l'agenda ne répond pas), l'export s'ouvre sur sa période habituelle. Pour un rendez-vous passé, la période finit
     * le jour du rendez-vous (`jusquau`) : le résumé est refait avec les notes actuelles. Rien de ce rendez-vous ne passe par
     * l'adresse : les dates voyagent dans l'état de la navigation.
     */
    async function preparer(evenement: EvenementAgenda) {
        if (preparationEnCours.value) return;
        preparationEnCours.value = true;
        let depuis: string | null = null;
        try {
            const precedent = await apiService.getRendezVousPrecedent(evenement.debut);
            if (precedent) depuis = format(debutDe(precedent), 'yyyy-MM-dd');
        } catch {
            depuis = null;
        }
        try {
            selection.value = null;
            const jusquau = estPasse(evenement, maintenant()) ? format(debutDe(evenement), 'yyyy-MM-dd') : null;
            await router.push({ path: '/export', state: { rdv: evenement.debut, depuis, jusquau } });
        } finally {
            preparationEnCours.value = false;
        }
    }

    return {
        etat, statut, vue, evenements, fenetres, chargementSuite, erreurSuite, prochain,
        passes, chargementPasses, erreurPasses, voirPlusAnciens,
        peutVoirPlusAnciens: computed(() => fenetresPassees.value < FENETRES_PASSEES_MAXIMUM),
        moisAffiche, evenementsMois, chargementMois, erreurMois, jourChoisi, evenementsDuJour,
        selection, preparationEnCours,
        charger, voirPlusLoin, changerVue, changerMois, choisirJour, actualiser, preparer,
        peutVoirPlusLoin: computed(() => fenetres.value < FENETRES_MAXIMUM),
    };
}
