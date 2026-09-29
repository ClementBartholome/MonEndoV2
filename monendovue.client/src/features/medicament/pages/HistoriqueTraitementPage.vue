<template>
  <main class="mx-auto flex w-full max-w-xl flex-col gap-3.5 px-5 pb-40 pt-24 lg:pb-12 lg:pt-10">
    <header class="flex items-center gap-2">
      <router-link to="/medicaments" aria-label="Retour aux traitements"
                   class="-ml-3 flex h-11 w-11 shrink-0 items-center justify-center rounded-full !text-texte hover:bg-surface-2">
        <i class="material-symbols-outlined" aria-hidden="true">arrow_back</i>
      </router-link>
      <div class="flex min-w-0 grow flex-col">
        <h1 class="m-0 truncate text-[22px] font-semibold tracking-normal text-texte">{{ donnees?.traitement.nom ?? 'Traitement' }}</h1>
        <span v-if="donnees" class="truncate text-[13px] text-texte-3">{{ detail }}</span>
      </div>
      <button v-if="donnees" type="button"
              class="inline-flex min-h-11 shrink-0 items-center gap-1.5 rounded-controle border-[1.5px] border-contour px-3 text-sm font-medium text-texte"
              @click="saisieOuverte = true">
        <i class="material-symbols-outlined text-lg" aria-hidden="true">edit</i>Modifier
      </button>
    </header>

    <section v-if="erreur" role="alert" class="flex flex-col items-start gap-3 rounded-carte bg-surface p-5 shadow-elevation">
      <p class="m-0 text-texte">{{ introuvable ? 'Ce traitement est introuvable.' : 'L\'historique n\'a pas pu être chargé.' }}</p>
      <router-link v-if="introuvable" to="/medicaments" class="inline-flex min-h-11 items-center text-sm font-medium !text-lien">Revenir aux traitements</router-link>
      <button v-else type="button" class="min-h-11 rounded-controle border-[1.5px] border-contour px-4 text-sm font-medium text-texte"
              @click="charger">Réessayer</button>
    </section>

    <template v-else>
      <nav aria-label="Mois affiché" class="-mx-3">
        <SelecteurMois :mois="mois" @changer="allerAuMois"/>
      </nav>

      <div v-if="chargement" class="flex flex-col gap-3.5" aria-busy="true" aria-label="Chargement de l'historique">
        <Skeleton class="h-20 rounded-carte"/>
        <Skeleton class="h-40 rounded-carte"/>
      </div>
      <template v-else-if="donnees">
        <ResumeHistoriqueTraitement :historique="donnees"/>
        <JoursHistoriqueTraitement v-if="donnees.jours.length" :jours="donnees.jours" :envoi="envoi" @retirer="retirer"/>
        <p v-else class="m-0 rounded-carte bg-surface-2 px-4 py-3 text-sm text-texte-2">
          {{ donnees.traitement.type === 'NonMedicamenteux' ? 'Aucune séance notée ce mois-ci.' : 'Aucune prise notée ce mois-ci.' }}
        </p>
      </template>
    </template>

    <SaisieTraitement v-if="donnees" v-model:open="saisieOuverte" :traitement="donnees.traitement" :actions="actions"/>
  </main>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { useRoute } from 'vue-router';
import { Skeleton } from '@/shared/components/ui/skeleton';
import { useToast } from '@/shared/components/ui/toast';
import SelecteurMois from '@/shared/components/SelecteurMois.vue';
import JoursHistoriqueTraitement from '../components/JoursHistoriqueTraitement.vue';
import ResumeHistoriqueTraitement from '../components/ResumeHistoriqueTraitement.vue';
import SaisieTraitement, { type ActionsSaisieTraitement } from '../components/SaisieTraitement.vue';
import { useHistoriqueTraitement } from '../composables/useHistoriqueTraitement';
import type { EntreeHistoriqueTraitement } from '../types/traitements';
import { resumeFrequence } from '../utils/prises';

const route = useRoute();
const { toast } = useToast();
const { mois, donnees, chargement, erreur, introuvable, charger, allerAuMois, retirer: retirerEntree, modifier, arreter } =
    useHistoriqueTraitement(() => Number(route.params.id));

const saisieOuverte = ref(false);
const envoi = ref(false);

const detail = computed(() => {
  const t = donnees.value!.traitement;
  if (t.type === 'NonMedicamenteux') return 'Soin';
  return [t.dose, resumeFrequence(t)].filter(Boolean).join(' · ');
});

const actions: ActionsSaisieTraitement = {
  enregistrer: async (saisie) => {
    await modifier(saisie);
    toast({ title: 'Traitement modifié', variant: 'custom' });
  },
  arreter: async () => {
    await arreter();
    toast({ title: 'Traitement arrêté', variant: 'custom' });
  },
};

async function retirer(entree: EntreeHistoriqueTraitement) {
  envoi.value = true;
  try {
    await retirerEntree(entree);
    toast({ title: entree.nature === 'Seance' ? 'Séance retirée' : 'Prise retirée', variant: 'custom' });
  } catch {
    toast({ title: 'Le retrait a échoué. Réessaie dans un instant.', variant: 'destructive' });
  } finally {
    envoi.value = false;
  }
}

onMounted(charger);
</script>
