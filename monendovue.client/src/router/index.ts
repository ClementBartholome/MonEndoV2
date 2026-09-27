import {createRouter, createWebHistory} from 'vue-router'
import HomePage from '@/features/home/pages/HomePage.vue'
import Schedule from '@/features/schedule/pages/Schedule.vue'
import LoginPage from "@/features/auth/pages/LoginPage.vue";
import {useAuthStore} from "@/features/auth/store/auth";
import PainPage from "@/features/douleurs/pages/DouleursPage.vue";
import ActivitePage from "@/features/activite/pages/ActivitePage.vue";
import MedicamentPage from "@/features/medicament/pages/MedicamentPage.vue";
import TransitPage from "@/features/transit/pages/TransitPage.vue";
import ExportPdfPage from "@/features/export/pages/ExportPdfPage.vue";
import BilanQuotidienPage from "@/features/bilan-quotidien/pages/BilanQuotidienPage.vue";
import ParametresPage from "@/features/parametres/pages/ParametresPage.vue";
import RegisterPage from "@/features/auth/pages/RegisterPage.vue";
import CyclePage from "@/features/cycle/pages/CyclePage.vue";
import PolitiqueConfidentialitePage from "@/features/legal/pages/PolitiqueConfidentialitePage.vue";
import MentionsLegalesPage from "@/features/legal/pages/MentionsLegalesPage.vue";

const router = createRouter({
    history: createWebHistory(import.meta.env.BASE_URL),
    routes: [
        {
            path: '/',
            name: 'home',
            component: HomePage,
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
            component: MedicamentPage,
        },
        {
            path: '/agenda',
            name: 'agenda',
            component: Schedule
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
            component: ExportPdfPage
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
        }
    ]
})

router.beforeEach((to, from, next) => {
    const authStore = useAuthStore();
    // Pages publiques (connexion, inscription, documents légaux) : meta.public.
    if (!authStore.user && !to.meta.public) {
        next({name: 'login'});
    } else {
        next();
    }
});

export default router