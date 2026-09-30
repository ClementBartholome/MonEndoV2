<template>
  <main class="mx-auto flex w-full max-w-xl flex-col gap-3.5 px-5 pb-40 pt-24 lg:pb-12 lg:pt-10">
    <header class="flex items-center gap-3">
      <i class="material-symbols-outlined rounded-controle bg-teinte-neutre-fond p-2 text-titre-page text-teinte-neutre" aria-hidden="true">gastroenterology</i>
      <h1 class="m-0 grow text-titre-page font-semibold tracking-normal text-texte">Transit</h1>
    </header>

    <p class="m-0 rounded-carte bg-surface-2 p-4 text-left text-sm text-texte-2">
      Ancien suivi, consultable ici. Depuis les bilans quotidiens, le transit (selles, crampes, ballonnements) se note dans le
      bilan du jour.
      <router-link to="/bilan-quotidien" class="font-medium !text-lien">Aller au bilan</router-link>
    </p>

    <nav aria-label="Mois affiché" class="-mx-3">
      <SelecteurMois :mois="mois" @changer="allerAuMois"/>
    </nav>

    <div v-if="chargement" class="flex flex-col gap-3.5" aria-busy="true" aria-label="Chargement du suivi du transit">
      <Skeleton class="h-32 rounded-carte"/>
    </div>

    <section v-else-if="erreur" role="alert" class="flex flex-col items-start gap-3 rounded-carte bg-surface p-5 shadow-elevation">
      <p class="m-0 text-texte">Le suivi du transit n'a pas pu être chargé.</p>
      <button type="button" class="min-h-11 rounded-controle border-[1.5px] border-contour px-4 text-sm font-medium text-texte"
              @click="charger">Réessayer</button>
    </section>

    <ListeTransit v-else-if="entrees.length" :groupes="groupes" :envoi="envoi" @supprimer="retirer"/>
    <p v-else class="m-0 rounded-carte bg-surface p-5 text-center text-texte-2 shadow-elevation">Aucune entrée ce mois-ci.</p>
  </main>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { Skeleton } from '@/shared/components/ui/skeleton';
import { useToast } from '@/shared/components/ui/toast';
import SelecteurMois from '@/shared/components/SelecteurMois.vue';
import ListeTransit from '../components/ListeTransit.vue';
import { useTransit } from '../composables/useTransit';
import type { DonneesTransit } from '../types/donnees-transit';

const { toast } = useToast();
const { mois, entrees, chargement, erreur, groupes, charger, allerAuMois, supprimer } = useTransit();
const envoi = ref(false);

async function retirer(entree: DonneesTransit) {
  envoi.value = true;
  try {
    await supprimer(entree.id);
    toast({ title: 'Entrée supprimée', variant: 'custom' });
  } catch {
    toast({ title: 'Erreur', description: "L'entrée n'a pas pu être supprimée. Réessaie dans un instant.", variant: 'destructive' });
  } finally {
    envoi.value = false;
  }
}

onMounted(charger);
</script>
