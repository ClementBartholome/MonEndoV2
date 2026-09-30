<template>
  <main class="mx-auto flex w-full max-w-xl flex-col gap-3.5 px-5 pb-40 pt-24 lg:pb-12 lg:pt-10">
    <header class="flex items-center gap-3">
      <i class="material-symbols-outlined rounded-controle bg-teinte-douleur-fond p-2 text-titre-page text-teinte-douleur" aria-hidden="true">sick</i>
      <h1 class="m-0 grow text-titre-page font-semibold tracking-normal text-texte">Douleurs</h1>
      <button type="button" class="hidden min-h-11 items-center gap-2 rounded-controle bg-button px-4 font-semibold text-texte lg:inline-flex"
              @click="ouvrirAjout">
        <i class="material-symbols-outlined" aria-hidden="true">add</i>Noter une douleur
      </button>
    </header>

    <nav aria-label="Mois affiché" class="-mx-3">
      <SelecteurMois :mois="mois" @changer="allerAuMois"/>
    </nav>

    <div v-if="chargement" class="flex flex-col gap-3.5" aria-busy="true" aria-label="Chargement des douleurs">
      <Skeleton class="h-20 rounded-carte"/>
      <Skeleton class="h-48 rounded-carte"/>
      <Skeleton class="h-32 rounded-carte"/>
    </div>

    <section v-else-if="erreur" role="alert" class="flex flex-col items-start gap-3 rounded-carte bg-surface p-5 shadow-elevation">
      <p class="m-0 text-texte">Les douleurs n'ont pas pu être chargées.</p>
      <Button variant="custom" class="min-h-11" @click="charger">Réessayer</Button>
    </section>

    <template v-else>
      <ChiffresDuMois :chiffres="chiffres"/>
      <GraphiqueDouleursMois :jours="graphique"/>
      <ListeDouleurs v-if="groupes.length" :groupes="groupes" @modifier="ouvrirModification"/>
      <EmptyStateAction v-else title="Aucune douleur notée ce mois-ci"
                        description="Note une douleur quand elle survient : le mois se remplira au fil des jours."
                        action-label="Noter une douleur" @action="ouvrirAjout"/>
    </template>

    <button type="button"
            class="fixed bottom-[calc(4.25rem+16px)] right-5 z-30 inline-flex min-h-14 items-center gap-2 rounded-controle bg-button pl-4 pr-5 text-corps font-semibold text-texte shadow-elevation lg:hidden"
            @click="ouvrirAjout">
      <i class="material-symbols-outlined" aria-hidden="true">add</i>Noter une douleur
    </button>

    <SaisieDouleur v-model:open="saisieOuverte" :entree="entreeModifiee" :actions="actions"/>
  </main>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { Button } from '@/shared/components/ui/button';
import { Skeleton } from '@/shared/components/ui/skeleton';
import { useToast } from '@/shared/components/ui/toast';
import EmptyStateAction from '@/shared/components/EmptyStateAction.vue';
import SelecteurMois from '@/shared/components/SelecteurMois.vue';
import { useAuthStore } from '@/features/auth/store/auth';
import ChiffresDuMois from '../components/ChiffresDuMois.vue';
import GraphiqueDouleursMois from '../components/GraphiqueDouleursMois.vue';
import ListeDouleurs from '../components/ListeDouleurs.vue';
import SaisieDouleur, { type ActionsSaisieDouleur } from '../components/SaisieDouleur.vue';
import { useDouleurs } from '../composables/useDouleurs';
import type { DonneesDouleur } from '../types/donnees-douleur';

const auth = useAuthStore();
const route = useRoute();
const router = useRouter();
const { toast } = useToast();
const { mois, chargement, erreur, chiffres, graphique, groupes, charger, allerAuMois, enregistrer, supprimer } =
    useDouleurs({ carnetSanteId: () => auth.user?.carnetSanteId });

const saisieOuverte = ref(false);
const entreeModifiee = ref<DonneesDouleur | null>(null);

const actions: ActionsSaisieDouleur = {
  enregistrer: async (saisie, id) => {
    await enregistrer(saisie, id);
    toast({ title: id === null ? 'Douleur notée' : 'Douleur modifiée', variant: 'custom' });
  },
  supprimer: async (id) => {
    await supprimer(id);
    toast({ title: 'Douleur supprimée', variant: 'custom' });
  },
};

function ouvrirAjout() {
  entreeModifiee.value = null;
  saisieOuverte.value = true;
}

function ouvrirModification(entree: DonneesDouleur) {
  entreeModifiee.value = entree;
  saisieOuverte.value = true;
}

onMounted(async () => {
  // Lien profond depuis l'accueil (tuile « Douleur ») : ouvre directement la saisie.
  if (route.query.ajouter !== undefined) {
    ouvrirAjout();
    await router.replace({ query: {} });
  }
  await charger();
});
</script>
