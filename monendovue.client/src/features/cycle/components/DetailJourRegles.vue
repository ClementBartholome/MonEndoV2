<template>
  <section aria-labelledby="titre-detail-regles" class="flex flex-col gap-3.5 rounded-carte bg-surface p-4 shadow-elevation">
    <div class="flex flex-col gap-0.5">
      <h2 id="titre-detail-regles" class="m-0 text-titre-carte font-semibold tracking-normal text-texte">{{ titre }}</h2>
      <p class="m-0 text-sm text-texte-2">Jour de règles noté. Le reste est facultatif.</p>
    </div>

    <!-- Plusieurs jours notés ce mois-ci : on choisit celui dont on précise les détails. -->
    <div v-if="jours.length > 1" role="group" aria-label="Jour à préciser" class="flex flex-wrap gap-2">
      <Button v-for="jour in jours" :key="jour" type="button" size="sm" class="min-h-11 min-w-11"
              :variant="jour === selection ? 'selected' : 'outline'" :aria-pressed="jour === selection"
              :aria-label="jourEnToutesLettres(jour)" @click="emit('selectionner', jour)">
        {{ Number(jour.slice(8)) }}
      </Button>
    </div>

    <div role="group" aria-labelledby="question-flux" class="flex flex-col gap-2">
      <p id="question-flux" class="m-0 text-sm font-medium text-texte">Quel flux ?</p>
      <div class="grid grid-cols-2 gap-2">
        <Button v-for="niveau in niveauxDeFlux" :key="niveau.valeur" type="button" class="h-11"
                :variant="detail?.flux === niveau.valeur ? 'selected' : 'outline'" :aria-pressed="detail?.flux === niveau.valeur"
                :disabled="envoi" @click="emit('changer', { flux: detail?.flux === niveau.valeur ? null : niveau.valeur })">
          {{ niveau.libelle }}
        </Button>
      </div>
      <p class="m-0 text-xs text-texte-2">{{ reperesDeProtections }}</p>
    </div>

    <div role="group" aria-labelledby="question-caillots" class="flex flex-col gap-2">
      <p id="question-caillots" class="m-0 text-sm font-medium text-texte">Des caillots ?</p>
      <div class="grid grid-cols-2 gap-2">
        <Button v-for="reponse in reponses" :key="reponse.libelle" type="button" class="h-11"
                :variant="detail?.caillots === reponse.valeur ? 'selected' : 'outline'" :aria-pressed="detail?.caillots === reponse.valeur"
                :disabled="envoi" @click="emit('changer', { caillots: detail?.caillots === reponse.valeur ? null : reponse.valeur })">
          {{ reponse.libelle }}
        </Button>
      </div>
    </div>

    <p class="m-0 text-xs text-texte-2">Enregistré dès que tu touches un choix. Touche-le à nouveau pour le retirer.</p>
  </section>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { Button } from '@/shared/components/ui/button';
import { niveauxDeFlux, reperesDeProtections } from '../config/flux';
import type { DetailJourRegles } from '../types/cycle';
import { jourEnToutesLettres } from '../utils/cycle';

/** Flux et caillots d'un jour de règles noté ; tout est facultatif (re-toucher un choix le retire). */
const props = defineProps<{
  /** Jours de règles du mois affiché (AAAA-MM-JJ), dans l'ordre. */
  jours: string[];
  /** Jour dont on précise les détails. */
  selection: string;
  detail: DetailJourRegles | null;
  envoi: boolean;
}>();

const emit = defineEmits<{
  selectionner: [jour: string];
  changer: [changement: { flux?: DetailJourRegles['flux']; caillots?: DetailJourRegles['caillots'] }];
}>();

const reponses = [
  { valeur: true, libelle: 'Oui' },
  { valeur: false, libelle: 'Non' },
];

const titre = computed(() => {
  const texte = jourEnToutesLettres(props.selection);
  return `${texte.charAt(0).toUpperCase()}${texte.slice(1)}`;
});
</script>
