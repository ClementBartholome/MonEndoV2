<template>
  <section class="flex flex-col gap-2.5" aria-labelledby="titre-aujourdhui">
    <div class="flex items-baseline justify-between">
      <h2 id="titre-aujourdhui" class="m-0 text-[17px] font-semibold tracking-normal text-texte">Aujourd'hui</h2>
      <span v-if="total" class="text-[13px] text-texte-3">{{ faites }} {{ faites > 1 ? 'prises' : 'prise' }} sur {{ total }}</span>
    </div>
    <p v-if="!total" class="m-0 rounded-carte bg-surface-2 px-4 py-3 text-sm text-texte-2">
      Aucune prise prévue aujourd'hui. Les traitements à heures fixes apparaissent ici.
    </p>

    <div v-for="groupe in groupes" :key="groupe.moment" class="flex flex-col gap-1.5">
      <h3 class="m-0 flex items-center gap-1.5 text-[13px] font-medium tracking-normal text-texte-3">
        <i class="material-symbols-outlined text-base" aria-hidden="true">{{ icones[groupe.moment] }}</i>{{ groupe.moment }} · {{ groupe.heure }}
      </h3>
      <div v-for="prise in groupe.prises" :key="`${prise.traitementId}-${prise.heurePrevue}`"
           class="flex flex-col gap-2.5 rounded-carte bg-surface px-3.5 py-3 shadow-elevation">
        <div class="flex items-center gap-3">
          <div class="flex min-w-0 grow flex-col text-left">
            <span class="text-[15px] font-medium text-texte">{{ prise.nom }}</span>
            <span class="text-[13px] text-texte-3">{{ [prise.dose, heureAffichee(prise.heurePrevue)].filter(Boolean).join(' · ') }}</span>
          </div>
          <template v-if="prise.reponse">
            <EtatPrise :reponse="prise.reponse"/>
            <button type="button" class="flex h-11 w-11 shrink-0 items-center justify-center rounded-full text-texte-3 hover:bg-surface-2"
                    :aria-label="`Annuler la réponse pour ${prise.nom}`" :disabled="envoi" @click="emit('annuler', prise)">
              <i class="material-symbols-outlined" aria-hidden="true">undo</i>
            </button>
          </template>
        </div>
        <div v-if="!prise.reponse" class="grid grid-cols-2 gap-2">
          <button type="button" class="min-h-11 rounded-controle border-[1.5px] border-contour text-sm font-medium text-texte disabled:opacity-60"
                  :disabled="envoi" :aria-label="`Ignorer la prise de ${prise.nom} de ${heureAffichee(prise.heurePrevue)}`"
                  @click="emit('repondre', prise, 'Ignore')">Ignorer</button>
          <button type="button" class="inline-flex min-h-11 items-center justify-center gap-1 rounded-controle bg-button text-sm font-semibold text-texte disabled:opacity-60"
                  :disabled="envoi" :aria-label="`Noter la prise de ${prise.nom} de ${heureAffichee(prise.heurePrevue)}`"
                  @click="emit('repondre', prise, 'Pris')">Je l'ai pris</button>
        </div>
      </div>
    </div>
  </section>
</template>

<script setup lang="ts">
import type { PrisePrevue, StatutPrise } from '../types/traitements';
import { heureAffichee, type GroupePrises } from '../utils/prises';
import EtatPrise from './EtatPrise.vue';

defineProps<{ groupes: GroupePrises[]; faites: number; total: number; envoi: boolean }>();
const emit = defineEmits<{ repondre: [prise: PrisePrevue, statut: StatutPrise]; annuler: [prise: PrisePrevue] }>();

const icones = { 'Matin': 'wb_sunny', 'Après-midi': 'partly_cloudy_day', 'Soir': 'bedtime' } as const;
</script>
