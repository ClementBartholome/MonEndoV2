import {createRouter, createWebHistory} from 'vue-router'
import HomePage from '@/pages/HomePage.vue'
import Schedule from '@/pages/Schedule.vue'
import LoginPage from "@/pages/LoginPage.vue";
import {useAuthStore} from "@/store/auth";
import PainPage from "@/pages/DouleursPage.vue";
import ActivitePage from "@/pages/ActivitePage.vue";
import MedicamentPage from "@/pages/MedicamentPage.vue";
import TransitPage from "@/pages/TransitPage.vue";
import ExportPdfPage from "@/pages/ExportPdfPage.vue";
import BilanQuotidienPage from "@/pages/BilanQuotidienPage.vue";
import ParametresPage from "@/pages/ParametresPage.vue";
import RegisterPage from "@/pages/RegisterPage.vue";
import CyclePage from "@/pages/CyclePage.vue";

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
            component: LoginPage
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
            component: RegisterPage
        },
        {
            path: '/cycle',
            name: 'cycle',
            component: CyclePage
        }
    ]
})

router.beforeEach((to, from, next) => {
    const authStore = useAuthStore();
    if (!authStore.user && to.name !== 'login' && to.name !== 'register') {
        next({name: 'login'});
    } else {
        next();
    }
});

export default router