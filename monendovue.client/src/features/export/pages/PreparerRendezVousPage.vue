<template>
  <main class="mx-auto flex w-full max-w-xl flex-col gap-[18px] px-5 pb-32 pt-24 lg:pb-12 lg:pt-10">
    <header class="flex flex-col gap-3">
      <div class="flex items-center gap-3">
        <i class="material-symbols-outlined rounded-controle bg-teinte-neutre-fond p-2 text-titre-page text-teinte-neutre" aria-hidden="true">picture_as_pdf</i>
        <h1 class="m-0 text-titre-page font-semibold leading-tight tracking-normal text-texte">Préparer un rendez-vous</h1>
      </div>
      <p class="m-0 text-left text-sm leading-normal text-texte-2">
        Une synthèse claire à montrer ou à envoyer à ton médecin : ce que tu as noté, jour par jour, sans interprétation.
      </p>
    </header>

    <form class="flex flex-col gap-[18px]" @submit.prevent="creerLePdf">
      <ChoixPeriode v-model:choix="choix" v-model:du="du" v-model:au="au" :aujourdhui="aujourdhui" :libelle="libelle" :erreur="erreurPeriode"/>
      <ChoixRubriques v-model="rubriques" :aucune="aucuneRubrique"/>

      <div class="flex flex-col gap-2">
        <label for="questions-rendez-vous" class="flex items-baseline gap-2 text-corps font-semibold text-texte">
          Mes questions <span class="text-legende font-normal text-texte-3">facultatif</span>
        </label>
        <p id="aide-questions" class="m-0 text-left text-legende text-texte-2">
          Note ce que tu veux demander pendant la consultation : tes questions apparaîtront en première page. Elles restent
          sur cet appareil.
        </p>
        <textarea id="questions-rendez-vous" v-model="questions" rows="4" :maxlength="QUESTIONS_MAX" aria-describedby="aide-questions"
                  placeholder="Ex. : les douleurs pendant les règles augmentent-elles ?"
                  class="resize-none rounded-controle border-[1.5px] border-contour bg-champ px-3 py-2.5 text-corps font-normal text-texte"></textarea>
      </div>

      <p v-if="erreur" role="alert" class="m-0 rounded-carte bg-surface p-4 text-left text-sm text-texte shadow-elevation">
        Le PDF n'a pas pu être créé. Vérifie ta connexion puis réessaie.
      </p>

      <button type="submit" :disabled="!peutCreer"
              class="inline-flex min-h-[52px] items-center justify-center gap-2 rounded-controle bg-button text-base font-semibold text-texte disabled:opacity-60">
        <i class="material-symbols-outlined" aria-hidden="true">download</i>{{ creation ? 'Création du PDF…' : 'Créer le PDF' }}
      </button>
    </form>
  </main>
</template>

<script setup lang="ts">
import { useToast } from '@/shared/components/ui/toast';
import ChoixPeriode from '../components/ChoixPeriode.vue';
import ChoixRubriques from '../components/ChoixRubriques.vue';
import { usePreparationRendezVous } from '../composables/usePreparationRendezVous';
import { QUESTIONS_MAX } from '../services/questionsRendezVous';

const { toast } = useToast();
const { aujourdhui, choix, du, au, libelle, erreurPeriode, rubriques, aucuneRubrique, questions, creation, erreur, peutCreer, creer } = usePreparationRendezVous();

async function creerLePdf() {
  if (await creer()) toast({ title: 'PDF créé', description: 'Il est dans tes téléchargements.', variant: 'custom' });
}
</script>
