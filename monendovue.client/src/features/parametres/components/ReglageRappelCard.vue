<script setup lang="ts">
import { ref, watch } from 'vue';
import { Switch } from '@/shared/components/ui/switch';
import { joursSemaine, type PresentationRappel } from '@/features/parametres/config/rappels';
import type { Rappel, ReglageRappel } from '@/features/parametres/types/notifications';

// Présentation d'un rappel : aucun appel API, les modifications sont remontées au parent.
const props = defineProps<{
  rappel: Rappel;
  presentation: PresentationRappel;
  desactive: boolean;
}>();

const emit = defineEmits<{
  modifier: [modifications: Partial<ReglageRappel>];
}>();

const heure = ref(props.rappel.heure);
watch(() => props.rappel.heure, (valeur) => { heure.value = valeur; });

const onHeure = () => {
  if (heure.value && heure.value !== props.rappel.heure) {
    emit('modifier', { heure: heure.value });
  }
};

const onJour = (event: Event) => {
  emit('modifier', { jourSemaine: Number((event.target as HTMLSelectElement).value) });
};
</script>

<template>
  <div class="flex flex-col gap-3 rounded-xl border border-gray-100 bg-white p-3">
    <label class="flex items-center justify-between gap-4 min-h-11">
      <span class="flex items-center gap-2 text-headline font-medium">
        <i class="material-symbols-outlined text-button">{{ presentation.icone }}</i>
        {{ presentation.titre }}
      </span>
      <Switch
          :checked="rappel.actif"
          :disabled="desactive"
          @update:checked="(actif: boolean) => emit('modifier', { actif })"
      />
    </label>

    <template v-if="rappel.actif">
      <label v-if="rappel.estHebdomadaire" class="flex items-center justify-between gap-4">
        <span class="text-paragraph">Jour</span>
        <select
            :value="rappel.jourSemaine ?? 0"
            :disabled="desactive"
            class="rounded-md border border-input bg-white px-3 py-2 min-h-11"
            @change="onJour"
        >
          <option v-for="jour in joursSemaine" :key="jour.valeur" :value="jour.valeur">{{ jour.libelle }}</option>
        </select>
      </label>

      <label class="flex items-center justify-between gap-4">
        <span class="text-paragraph">Heure</span>
        <input
            v-model="heure"
            type="time"
            step="900"
            :disabled="desactive"
            class="rounded-md border border-input px-3 py-2 min-h-11"
            @change="onHeure"
        >
      </label>
    </template>

    <p class="text-sm text-muted-foreground">{{ presentation.description }}</p>
  </div>
</template>
