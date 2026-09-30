<template>
  <main class="mx-auto flex w-full max-w-xl flex-col gap-3.5 px-5 pb-40 pt-24 lg:pb-12 lg:pt-10">
    <header class="flex items-center gap-3">
      <i class="material-symbols-outlined rounded-controle bg-teinte-bilan-fond p-2 text-titre-page text-teinte-bilan" aria-hidden="true">directions_run</i>
      <h1 class="m-0 grow text-titre-page font-semibold tracking-normal text-texte">Activité</h1>
      <button type="button" class="hidden min-h-11 items-center gap-2 rounded-controle bg-button px-4 font-semibold text-texte lg:inline-flex"
              @click="ouvrirAjout">
        <i class="material-symbols-outlined" aria-hidden="true">add</i>Noter une activité
      </button>
    </header>

    <nav aria-label="Mois affiché" class="-mx-3">
      <SelecteurMois :mois="mois" @changer="allerAuMois"/>
    </nav>

    <div v-if="chargement" class="flex flex-col gap-3.5" aria-busy="true" aria-label="Chargement des activités">
      <Skeleton class="h-20 rounded-carte"/>
      <Skeleton class="h-32 rounded-carte"/>
    </div>

    <section v-else-if="erreur" role="alert" class="flex flex-col items-start gap-3 rounded-carte bg-surface p-5 shadow-elevation">
      <p class="m-0 text-texte">Les activités n'ont pas pu être chargées.</p>
      <button type="button" class="min-h-11 rounded-controle border-[1.5px] border-contour px-4 text-sm font-medium text-texte"
              @click="charger">Réessayer</button>
    </section>

    <template v-else-if="activites.length">
      <ChiffresActivite :chiffres="chiffres"/>
      <ListeActivites :groupes="groupes" @modifier="ouvrirModification"/>
    </template>
    <EmptyStateAction v-else title="Aucune activité notée ce mois-ci"
                      description="Marche, yoga, étirements… note tes séances et ce qu'elles changent à ta douleur."
                      action-label="Noter une activité" @action="ouvrirAjout"/>

    <button type="button"
            class="fixed bottom-[calc(4.25rem+16px)] right-5 z-30 inline-flex min-h-14 items-center gap-2 rounded-controle bg-button pl-4 pr-5 text-corps font-semibold text-texte shadow-elevation lg:hidden"
            @click="ouvrirAjout">
      <i class="material-symbols-outlined" aria-hidden="true">add</i>Noter une activité
    </button>

    <SaisieActivite v-model:open="saisieOuverte" :activite="activiteModifiee" :actions="actions"/>
  </main>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { Skeleton } from '@/shared/components/ui/skeleton';
import { useToast } from '@/shared/components/ui/toast';
import EmptyStateAction from '@/shared/components/EmptyStateAction.vue';
import SelecteurMois from '@/shared/components/SelecteurMois.vue';
import ChiffresActivite from '../components/ChiffresActivite.vue';
import ListeActivites from '../components/ListeActivites.vue';
import SaisieActivite, { type ActionsSaisieActivite } from '../components/SaisieActivite.vue';
import { useActivites } from '../composables/useActivites';
import type { Activite } from '../types/activite';

const route = useRoute();
const router = useRouter();
const { toast } = useToast();
const { mois, activites, chargement, erreur, chiffres, groupes, charger, allerAuMois, enregistrer, supprimer } = useActivites();

const saisieOuverte = ref(false);
const activiteModifiee = ref<Activite | null>(null);

const actions: ActionsSaisieActivite = {
  enregistrer: async (saisie, id) => {
    await enregistrer(saisie, id);
    toast({ title: id === null ? 'Activité notée' : 'Activité modifiée', variant: 'custom' });
  },
  supprimer: async (id) => {
    await supprimer(id);
    toast({ title: 'Activité supprimée', variant: 'custom' });
  },
};

function ouvrirAjout() {
  activiteModifiee.value = null;
  saisieOuverte.value = true;
}

function ouvrirModification(activite: Activite) {
  activiteModifiee.value = activite;
  saisieOuverte.value = true;
}

onMounted(async () => {
  if (route.query.ajouter !== undefined) {
    ouvrirAjout();
    await router.replace({ query: {} });
  }
  await charger();
});
</script>
