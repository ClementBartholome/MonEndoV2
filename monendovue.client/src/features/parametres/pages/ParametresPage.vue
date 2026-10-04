<template>
  <main class="mx-auto flex w-full max-w-xl flex-col gap-5 px-5 pb-40 pt-24 lg:pb-12 lg:pt-10">
    <h1 class="m-0 text-left text-titre-page font-semibold tracking-normal text-texte">Paramètres</h1>

    <GroupeParametres titre="Rappels">
      <LigneParametre titre="Notifications" detail="Bilan du jour · photo de l'acné" icone="notifications"
                      teinte="bilan" @ouvrir="panneau = 'notifications'"/>
    </GroupeParametres>

    <GroupeParametres v-if="agenda.statut.value?.disponible" titre="Agenda">
      <LigneParametre titre="Agenda Google" :detail="detailAgenda"
                      icone="calendar_month" teinte="bilan" @ouvrir="panneau = 'agenda'"/>
    </GroupeParametres>

    <GroupeParametres titre="Suivi">
      <LigneParametre titre="Catégories du bilan" detail="Choisir ce qui apparaît" icone="tune"
                      teinte="bilan" @ouvrir="panneau = 'categories'"/>
      <LigneParametre titre="Repères personnels" detail="Hydratation, pas, stress…" icone="flag"
                      teinte="traitement" @ouvrir="panneau = 'reperes'"/>
    </GroupeParametres>

    <GroupeParametres titre="Compte">
      <LigneParametre titre="Mot de passe" detail="Modifier mon mot de passe" icone="lock" @ouvrir="panneau = 'mot-de-passe'"/>
      <LigneParametre titre="Se déconnecter" detail="De cet appareil" icone="logout" :chevron="false" @ouvrir="deconnecter"/>
    </GroupeParametres>

    <GroupeParametres titre="Mes données">
      <LigneParametre :titre="exportEnCours ? 'Préparation de l\'export…' : 'Télécharger mes données'"
                      detail="Tes données et tes photos (ZIP)" icone="download" :chevron="false"
                      @ouvrir="telechargerMesDonnees"/>
      <LigneParametre titre="Supprimer mon compte" detail="Définitivement, avec tes données" icone="delete_forever"
                      teinte="regles" @ouvrir="panneau = 'suppression'"/>
    </GroupeParametres>
    <p v-if="erreurExport" role="alert" class="m-0 text-left text-sm text-danger">{{ erreurExport }}</p>

    <div class="flex flex-col items-center">
      <span class="text-xs text-texte-3">MonEndo v{{ version }}</span>
      <LiensLegaux/>
    </div>

    <PanneauBas v-model:open="notificationsOuvertes" titre="Notifications">
      <NotificationSettings/>
    </PanneauBas>
    <PanneauBas v-model:open="categoriesOuvertes" titre="Catégories du bilan">
      <CategoriesBilan/>
    </PanneauBas>
    <PanneauBas v-model:open="reperesOuverts" titre="Repères personnels">
      <ReperesPersonnels @enregistre="panneau = null"/>
    </PanneauBas>
    <PanneauBas v-model:open="motDePasseOuvert" titre="Mot de passe">
      <MotDePasse @change="panneau = null"/>
    </PanneauBas>
    <PanneauBas v-model:open="agendaOuvert" titre="Agenda Google">
      <LiaisonAgenda :agenda="agenda"/>
    </PanneauBas>
    <SuppressionCompte v-model:open="suppressionOuverte"/>
  </main>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { useToast } from '@/shared/components/ui/toast';
import LiensLegaux from '@/features/legal/components/LiensLegaux.vue';
import { useAuthStore } from '@/features/auth/store/auth';
import PanneauBas from '@/shared/components/PanneauBas.vue';
import CategoriesBilan from '../components/CategoriesBilan.vue';
import GroupeParametres from '../components/GroupeParametres.vue';
import LigneParametre from '../components/LigneParametre.vue';
import LiaisonAgenda from '../components/LiaisonAgenda.vue';
import MotDePasse from '../components/MotDePasse.vue';
import NotificationSettings from '../components/NotificationSettings.vue';
import ReperesPersonnels from '../components/ReperesPersonnels.vue';
import SuppressionCompte from '../components/SuppressionCompte.vue';
import { useExportDonnees } from '../composables/useExportDonnees';
import { useLiaisonAgenda } from '../composables/useLiaisonAgenda';

type Panneau = 'notifications' | 'categories' | 'reperes' | 'mot-de-passe' | 'suppression' | 'agenda';

const version = __APP_VERSION__;
const router = useRouter();
const route = useRoute();
const { toast } = useToast();
const auth = useAuthStore();
const agenda = useLiaisonAgenda();
const { exportEnCours, erreurExport, telechargerMesDonnees } = useExportDonnees();

/** Un seul panneau ouvert à la fois ; chaque contenu n'est monté (et ne charge ses données) qu'à l'ouverture. */
const panneau = ref<Panneau | null>(null);
const ouvertSi = (nom: Panneau) => computed({
  get: () => panneau.value === nom,
  set: (ouvert: boolean) => { panneau.value = ouvert ? nom : null; },
});
const notificationsOuvertes = ouvertSi('notifications');
const categoriesOuvertes = ouvertSi('categories');
const reperesOuverts = ouvertSi('reperes');
const motDePasseOuvert = ouvertSi('mot-de-passe');
const suppressionOuverte = ouvertSi('suppression');
const agendaOuvert = ouvertSi('agenda');

const detailAgenda = computed(() => {
  const statut = agenda.statut.value;
  if (!statut?.liee) return 'Non lié';
  return statut.calendrierId ? 'Lié · prochains rendez-vous sur l\'accueil' : 'Lié · choisis un calendrier';
});

/** Retour de Google : le serveur redirige ici avec `?agenda=lie` ou `?agenda=echec` ; l'indication est lue puis retirée de l'adresse. */
onMounted(async () => {
  const retour = route.query.agenda;
  if (retour === 'lie') {
    toast({ title: 'Agenda lié', description: 'Choisis maintenant le calendrier à afficher.', variant: 'custom' });
    panneau.value = 'agenda';
  } else if (retour === 'echec') {
    toast({ title: 'La liaison n\'a pas abouti', description: 'Tu peux réessayer depuis « Agenda Google ».', variant: 'custom' });
  }
  if (retour !== undefined) await router.replace({ query: {} });
  await agenda.charger();
});

async function deconnecter() {
  await auth.logout();
  router.push('/login');
}
</script>
