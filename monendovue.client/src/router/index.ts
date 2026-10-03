import {createRouter, createWebHistory} from 'vue-router'
import AccueilPage from '@/features/accueil/pages/AccueilPage.vue'
import AgendaPage from '@/features/schedule/pages/AgendaPage.vue'
import LoginPage from "@/features/auth/pages/LoginPage.vue";
import {useAuthStore} from "@/features/auth/store/auth";
import PainPage from "@/features/douleurs/pages/DouleursPage.vue";
import ActivitePage from "@/features/activite/pages/ActivitePage.vue";
import TraitementsPage from "@/features/medicament/pages/TraitementsPage.vue";
import HistoriqueTraitementPage from "@/features/medicament/pages/HistoriqueTraitementPage.vue";
import TransitPage from "@/features/transit/pages/TransitPage.vue";
import PreparerRendezVousPage from "@/features/export/pages/PreparerRendezVousPage.vue";
import BilanQuotidienPage from "@/features/bilan-quotidien/pages/BilanQuotidienPage.vue";
import ParametresPage from "@/features/parametres/pages/ParametresPage.vue";
import RegisterPage from "@/features/auth/pages/RegisterPage.vue";
import CyclePage from "@/features/cycle/pages/CyclePage.vue";
import PolitiqueConfidentialitePage from "@/features/legal/pages/PolitiqueConfidentialitePage.vue";
import MentionsLegalesPage from "@/features/legal/pages/MentionsLegalesPage.vue";
import RetoursPage from '@/features/retours/pages/RetoursPage.vue'
import InformerPage from '@/features/informer/pages/InformerPage.vue'
import BienvenuePage from '@/features/auth/pages/BienvenuePage.vue'
import RappelBienvenuePage from '@/features/onboarding/pages/RappelBienvenuePage.vue'
import {dejaVenue} from '@/features/auth/utils/parcours'
import ConsentementPage from "@/features/legal/pages/ConsentementPage.vue";

const router = createRouter({
    history: createWebHistory(import.meta.env.BASE_URL),
    // Une nouvelle page s'ouvre en haut (sans cela, elle garde le défilement de la précédente) ; « retour » retrouve sa position.
    scrollBehavior: (_vers, _depuis, positionEnregistree) => positionEnregistree ?? {top: 0},
    routes: [
        {
            path: '/',
            name: 'home',
            component: AccueilPage,
        },
        {
            path: '/douleurs',
            name: 'douleurs',
            component: PainPage,
        },
        {
            path: '/activite',
            name: 'activite',
            component: ActivitePage,
        },
        {
            path: '/transit',
            name: 'transit',
            component: TransitPage,
        },
        {
            path: '/medicaments',
            name: 'medicaments',
            component: TraitementsPage,
        },
        {
            path: '/medicaments/:id(\\d+)',
            name: 'historique-traitement',
            component: HistoriqueTraitementPage,
        },
        {
            path: '/agenda',
            name: 'agenda',
            component: AgendaPage
        },
        {
            path: '/login',
            name: 'login',
            component: LoginPage,
            meta: {public: true}
        },
        {
            path: '/export',
            name: 'export',
            component: PreparerRendezVousPage
        },
        {
            path: '/bilan-quotidien',
            name: 'bilan-quotidien',
            component: BilanQuotidienPage
        },
        {
            path: '/parametres',
            name: 'parametres',
            component: ParametresPage
        },
        {
            path: '/register',
            name: 'register',
            component: RegisterPage,
            meta: {public: true}
        },
        {
            path: '/cycle',
            name: 'cycle',
            component: CyclePage
        },
        {
            path: '/bienvenue',
            name: 'bienvenue',
            component: BienvenuePage,
            meta: {public: true}
        },
        {
            path: '/bienvenue/rappel',
            name: 'rappel-bienvenue',
            component: RappelBienvenuePage,
            meta: {sansNavigation: true}
        },
        {
            path: '/s-informer',
            name: 's-informer',
            component: InformerPage
        },
        {
            path: '/suggestions',
            name: 'suggestions',
            component: RetoursPage
        },
        {
            path: '/confidentialite',
            name: 'confidentialite',
            component: PolitiqueConfidentialitePage,
            meta: {public: true}
        },
        {
            path: '/mentions-legales',
            name: 'mentions-legales',
            component: MentionsLegalesPage,
            meta: {public: true}
        },
        {
            path: '/consentement',
            name: 'consentement',
            component: ConsentementPage,
            meta: {sansNavigation: true}
        }
    ]
})

router.beforeEach((to, from, next) => {
    const authStore = useAuthStore();
    // Pages publiques (connexion, inscription, documents légaux) : meta.public.
    if (!authStore.user && !to.meta.public) {
        // Une personne qui s'est déjà connectée retrouve la connexion ; une première visite commence par la bienvenue.
        next({name: dejaVenue() ? 'login' : 'bienvenue'});
    } else if (authStore.user?.consentementAJour === false && !to.meta.public && to.name !== 'consentement') {
        // Compte sans consentement aux données de santé : rien d'autre n'est accessible avant l'accord.
        next({name: 'consentement'});
    } else {
        next();
    }
});

export default router