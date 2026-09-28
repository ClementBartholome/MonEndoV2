<template>
  <main class="mx-auto flex w-full max-w-xl flex-col gap-3.5 px-5 pb-40 pt-24 lg:pb-12 lg:pt-10">
    <header class="flex items-center gap-3">
      <i class="material-symbols-outlined rounded-xl bg-teinte-douleur-fond p-2 text-[26px] text-teinte-douleur" aria-hidden="true">sick</i>
      <h1 class="m-0 grow text-[26px] font-semibold tracking-normal text-texte">Douleurs</h1>
      <button type="button" class="hidden min-h-11 items-center gap-2 rounded-[14px] bg-button px-4 font-semibold text-texte lg:inline-flex"
              @click="ouvrirAjout">
        <i class="material-symbols-outlined" aria-hidden="true">add</i>Noter une douleur
      </button>
    </header>

    <nav aria-label="Mois affiché" class="-mx-3 flex items-center justify-between">
      <button type="button" aria-label="Mois précédent" class="flex h-11 w-11 items-center justify-center rounded-full text-texte hover:bg-surface-2"
              @click="changerDeMois(-1)">
        <i class="material-symbols-outlined" aria-hidden="true">chevron_left</i>
      </button>
      <span class="text-base font-semibold text-texte" aria-live="polite">{{ titreMois }}</span>
      <button type="button" aria-label="Mois suivant" :disabled="!moisSuivantPossible"
              class="flex h-11 w-11 items-center justify-center rounded-full text-texte hover:bg-surface-2 disabled:text-trait"
              @click="changerDeMois(1)">
        <i class="material-symbols-outlined" aria-hidden="true">chevron_right</i>
      </button>
    </nav>

    <div v-if="chargement" class="flex flex-col gap-3.5" aria-busy="true" aria-label="Chargement des douleurs">
      <Skeleton class="h-20 rounded-grand"/>
      <Skeleton class="h-48 rounded-grand"/>
      <Skeleton class="h-32 rounded-moyen"/>
    </div>

    <section v-else-if="erreur" role="alert" class="flex flex-col items-start gap-3 rounded-grand bg-surface p-5 shadow-elevation">
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
            class="fixed bottom-[calc(4.25rem+16px)] right-5 z-30 inline-flex min-h-14 items-center gap-2 rounded-[18px] bg-button pl-4 pr-5 text-[15px] font-semibold text-texte shadow-elevation lg:hidden"
            @click="ouvrirAjout">
      <i class="material-symbols-outlined" aria-hidden="true">add</i>Noter une douleur
    </button>

    <SaisieDouleur v-model:open="saisieOuverte" :entree="entreeModifiee" :actions="actions"/>
  </main>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { format } from 'date-fns';
import { fr } from 'date-fns/locale';
import { Button } from '@/shared/components/ui/button';
import { Skeleton } from '@/shared/components/ui/skeleton';
import { useToast } from '@/shared/components/ui/toast';
import EmptyStateAction from '@/shared/components/EmptyStateAction.vue';
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
const { mois, chargement, erreur, chiffres, graphique, groupes, moisSuivantPossible, charger, changerDeMois, enregistrer, supprimer } =
    useDouleurs({ carnetSanteId: () => auth.user?.carnetSanteId });

const saisieOuverte = ref(false);
const entreeModifiee = ref<DonneesDouleur | null>(null);

const titreMois = computed(() => {
  const texte = format(mois.value, 'MMMM yyyy', { locale: fr });
  return texte.charAt(0).toUpperCase() + texte.slice(1);
});

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
