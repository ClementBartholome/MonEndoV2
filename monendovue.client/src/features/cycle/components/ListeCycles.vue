<template>
  <section aria-labelledby="titre-mes-cycles" class="flex flex-col gap-3.5">
    <div class="flex flex-col gap-3 rounded-carte bg-surface px-[18px] py-4 shadow-elevation">
      <div class="flex items-center gap-2.5">
        <i class="material-symbols-outlined rounded-controle bg-teinte-regles-fond p-1.5 text-[22px] text-teinte-regles" aria-hidden="true">menstrual_health</i>
        <div class="flex flex-col">
          <h2 id="titre-mes-cycles" class="m-0 text-[17px] font-semibold tracking-normal text-texte">Mes cycles</h2>
          <span class="text-xs text-texte-3">{{ resume.dureeMoyenne !== null ? 'Sur les 6 derniers cycles' : 'Dès deux cycles terminés' }}</span>
        </div>
      </div>
      <dl v-if="resume.dureeMoyenne !== null" class="m-0 grid grid-cols-3 gap-2">
        <div v-for="chiffre in chiffres" :key="chiffre.libelle" class="flex flex-col rounded-controle bg-teinte-regles-fond p-2.5">
          <dt class="text-xs font-medium text-teinte-regles">{{ chiffre.libelle }}</dt>
          <dd class="m-0 text-lg font-semibold text-texte">{{ chiffre.valeur }}</dd>
        </div>
      </dl>
      <p v-else class="m-0 text-sm text-texte-2">Tes cycles apparaîtront ici dès que deux débuts de règles seront notés.</p>
    </div>

    <template v-if="cycles.length">
      <ul class="m-0 flex list-none flex-wrap gap-x-4 gap-y-1 p-0 text-xs text-texte-2" aria-label="Légende des barres">
        <li class="flex items-center gap-1.5"><span aria-hidden="true" class="h-2 w-3.5 rounded-full bg-teinte-regles"></span>Règles</li>
        <li class="flex items-center gap-1.5">
          <span aria-hidden="true" class="h-2 w-2 rounded-full border-[1.5px] border-surface bg-intensite-10"></span>Douleur 6/10 ou plus
        </li>
      </ul>

      <section v-for="groupe in groupes" :key="groupe.annee" class="flex flex-col gap-2" :aria-labelledby="`cycles-${groupe.annee}`">
        <h3 :id="`cycles-${groupe.annee}`" class="m-0 mt-1 text-[15px] font-semibold tracking-normal text-texte">{{ groupe.annee }}</h3>
        <ul class="m-0 flex list-none flex-col rounded-carte bg-surface p-0 shadow-elevation">
          <li v-for="(cycle, index) in groupe.cycles" :key="cycle.debut"
              class="flex flex-col gap-2 px-3.5 py-3 text-left" :class="{ 'border-t border-trait': index > 0 }">
            <div class="flex items-baseline justify-between gap-2">
              <span class="text-[15px] font-medium text-texte">{{ periodeDuCycle(cycle.debut, cycle.duree) }}</span>
              <span class="shrink-0 text-sm font-semibold text-texte-2">{{ jours(cycle.duree) }}</span>
            </div>
            <!-- Même échelle pour toutes les barres : les cycles se comparent d'un coup d'œil. -->
            <div role="img" :aria-label="description(cycle)" class="relative h-3 rounded-full bg-surface-2" :style="{ width: `${Math.min(100, cycle.duree / echelle * 100)}%` }">
              <span class="absolute inset-y-0 left-0 rounded-full bg-teinte-regles" :style="{ width: `${Math.min(100, cycle.joursDeRegles / cycle.duree * 100)}%` }"></span>
              <span v-for="jour in cycle.joursDouleurForte" :key="jour"
                    class="absolute top-1/2 -ml-1.5 -mt-1.5 h-3 w-3 rounded-full border-[1.5px] border-surface bg-intensite-10"
                    :style="{ left: `${(jour - 0.5) / cycle.duree * 100}%` }"></span>
            </div>
          </li>
        </ul>
      </section>

      <button v-if="plusAnciens > 0" type="button" class="inline-flex min-h-11 items-center gap-1.5 self-start text-sm font-medium text-lien" @click="emit('voirPlus')">
        <i class="material-symbols-outlined text-lg" aria-hidden="true">expand_more</i>Voir les cycles précédents ({{ plusAnciens }})
      </button>
    </template>
  </section>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import type { CycleDuMois, CycleTermine } from '../types/cycle';
import { grouperParAnnee, jours, periodeDuCycle } from '../utils/cycle';

const props = defineProps<{
  cycles: CycleTermine[];
  resume: Pick<CycleDuMois, 'dureeMoyenne' | 'reglesMoyenne' | 'dureeMinimale' | 'dureeMaximale'>;
  plusAnciens: number;
}>();
const emit = defineEmits<{ voirPlus: [] }>();

const groupes = computed(() => grouperParAnnee(props.cycles));
/** 40 jours = toute la largeur, sauf cycle plus long affiché. */
const echelle = computed(() => Math.max(40, ...props.cycles.map((c) => c.duree)));

const chiffres = computed(() => [
  { libelle: 'Cycle', valeur: `${props.resume.dureeMoyenne} j` },
  { libelle: 'Règles', valeur: `${props.resume.reglesMoyenne} j` },
  { libelle: 'Écart', valeur: `${props.resume.dureeMinimale}–${props.resume.dureeMaximale} j` },
]);

function description(cycle: CycleTermine): string {
  const douleur = cycle.joursDouleurForte.length
    ? `, douleur forte ${jours(cycle.joursDouleurForte.length)} (jour${cycle.joursDouleurForte.length > 1 ? 's' : ''} ${cycle.joursDouleurForte.join(', ')})`
    : '';
  return `Cycle de ${jours(cycle.duree)}, règles ${jours(cycle.joursDeRegles)}${douleur}`;
}
</script>
