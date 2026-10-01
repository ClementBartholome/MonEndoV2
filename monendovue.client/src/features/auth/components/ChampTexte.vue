<template>
  <div class="flex flex-col gap-1.5 text-left">
    <label :for="idChamp" class="text-sm font-medium text-texte">{{ label }}</label>
    <div class="relative">
      <input :id="idChamp" v-model="valeur" :type="typeAffiche" :name="name" :autocomplete="autocomplete" :inputmode="inputmode"
             :aria-invalid="erreur ? 'true' : undefined" :aria-describedby="erreur ? idErreur : undefined"
             class="min-h-[52px] w-full rounded-controle border-[1.5px] bg-champ px-3.5 text-base text-texte"
             :class="[erreur ? 'border-danger' : 'border-contour', motDePasse ? 'pr-14' : '']">
      <button v-if="motDePasse" type="button" :aria-label="visible ? 'Masquer le mot de passe' : 'Afficher le mot de passe'" :aria-pressed="visible"
              class="absolute right-0.5 top-0.5 flex h-11 w-11 items-center justify-center text-texte-2" @click="visible = !visible">
        <i class="material-symbols-outlined" aria-hidden="true">{{ visible ? 'visibility_off' : 'visibility' }}</i>
      </button>
    </div>
    <slot/>
    <p v-if="erreur" :id="idErreur" role="alert" class="m-0 flex items-start gap-2 text-sm leading-normal text-danger">
      <i class="material-symbols-outlined text-xl" aria-hidden="true">error</i>
      <span><slot name="erreur">{{ erreur }}</slot></span>
    </p>
  </div>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue';
import { useId } from 'radix-vue';

/** Champ de formulaire de l'arrivée : libellé, saisie, message d'erreur lié au champ ; l'œil n'existe que pour un mot de passe. */
const props = defineProps<{
  label: string;
  name: string;
  type?: 'text' | 'email' | 'password';
  autocomplete?: string;
  inputmode?: 'text' | 'email';
  erreur?: string | null;
}>();
const valeur = defineModel<string>({ required: true });

const idChamp = useId(undefined, 'champ');
const idErreur = `${idChamp}-erreur`;
const visible = ref(false);
const motDePasse = computed(() => props.type === 'password');
const typeAffiche = computed(() => (motDePasse.value && visible.value ? 'text' : (props.type ?? 'text')));
</script>
