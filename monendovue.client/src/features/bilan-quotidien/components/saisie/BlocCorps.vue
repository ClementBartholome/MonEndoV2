<script setup lang="ts">
import { computed } from 'vue';
import { Button } from '@/shared/components/ui/button';
import TransitSaisie from '@/features/bilan-quotidien/components/saisie/TransitSaisie.vue';
import SaisieMesure from '@/features/bilan-quotidien/components/saisie/SaisieMesure.vue';
import {
  consommationsAlimentaires,
  HYDRATATION_MAX,
  PAS_MAX,
  prereglagesHydratation,
  prereglagesPas,
  type ConsommationAlimentaire,
} from '@/features/bilan-quotidien/config/saisie';
import type { TransitBilan } from '@/features/bilan-quotidien/types/bilan-quotidien';
import type { CorpsBilan } from '@/features/bilan-quotidien/types/saisie-bilan';

// Contenu du bloc « Corps » : transit, alimentation, pas et hydratation, tous facultatifs.
const corps = defineModel<CorpsBilan>({ required: true });

const modifier = (changement: Partial<CorpsBilan>) => {
  corps.value = { ...corps.value, ...changement };
};

const transit = computed<TransitBilan>({
  get: () => corps.value,
  set: (valeur) => modifier(valeur),
});

const basculer = (cle: ConsommationAlimentaire) => modifier({ [cle]: !corps.value[cle] });
</script>

<template>
  <TransitSaisie v-model="transit"/>

  <div class="flex flex-col gap-2">
    <p id="libelle-alimentation" class="font-semibold text-headline flex items-center gap-2">
      <i class="material-symbols-outlined text-button" aria-hidden="true">restaurant</i>Alimentation
    </p>
    <div class="grid grid-cols-3 gap-2" role="group" aria-labelledby="libelle-alimentation">
      <Button
          v-for="item in consommationsAlimentaires"
          :key="item.cle"
          type="button"
          class="h-auto min-h-[56px] flex-col gap-0.5 px-1 py-1.5 text-xs"
          :variant="corps[item.cle] ? 'selected' : 'outline'"
          :aria-pressed="corps[item.cle]"
          @click="basculer(item.cle)"
      >
        <i class="material-symbols-outlined text-xl leading-none" aria-hidden="true">{{ item.icone }}</i>{{ item.libelle }}
      </Button>
    </div>
  </div>

  <SaisieMesure
      :model-value="corps.pas"
      libelle="Pas"
      icone="directions_walk"
      unite="pas"
      :prereglages="prereglagesPas"
      :max="PAS_MAX"
      @update:model-value="(pas) => modifier({ pas })"
  />

  <SaisieMesure
      :model-value="corps.hydratation"
      libelle="Hydratation"
      icone="water_drop"
      unite="litres"
      :prereglages="prereglagesHydratation"
      :max="HYDRATATION_MAX"
      :pas="0.1"
      @update:model-value="(hydratation) => modifier({ hydratation })"
  />
</template>
