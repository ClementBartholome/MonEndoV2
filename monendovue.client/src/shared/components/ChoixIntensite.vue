<template>
  <fieldset class="m-0 border-0 p-0">
    <legend class="mb-2.5 p-0 text-sm font-semibold text-texte">
      Intensité<span v-if="intensite !== null" class="font-medium text-texte-2"> : {{ intensite }}/10</span>
    </legend>
    <div class="grid grid-cols-10 gap-[3px]">
      <!-- Choisie : pleine dans sa couleur. Sinon : neutre, avec un trait de sa couleur pour lire l'échelle. -->
      <button v-for="n in 10" :key="n" type="button"
              class="relative min-h-12 overflow-hidden rounded-controle border-[1.5px] p-0 text-sm font-semibold"
              :class="intensite === n ? '' : 'border-trait bg-surface text-texte-2'"
              :style="intensite === n ? pleine(n) : undefined"
              :aria-label="`Intensité ${n} sur 10`" :aria-pressed="intensite === n"
              @click="intensite = n">
        {{ n }}
        <span v-if="intensite !== n" aria-hidden="true" class="absolute inset-x-0 bottom-0 h-1" :style="{ background: `var(--intensite-${n})` }"></span>
      </button>
    </div>
    <div class="mt-1.5 flex justify-between text-xs text-texte-3"><span>{{ minimum }}</span><span>{{ maximum }}</span></div>
  </fieldset>
</template>

<script setup lang="ts">
/** Intensité de 1 à 10 sur l'échelle de couleur commune (`--intensite-N`) : douleurs, symptômes, acné. */
const intensite = defineModel<number | null>({ required: true });
defineProps<{ minimum: string; maximum: string }>();

function pleine(n: number) {
  const couleur = `var(--intensite-${n})`;
  return { background: couleur, borderColor: couleur, color: n >= 6 ? '#ffffff' : 'var(--couleur-texte)' };
}
</script>
