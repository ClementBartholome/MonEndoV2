<script setup lang="ts">
// Proposition discrète, en bas de l'accueil, de lier son agenda Google (la fonction est sinon cachée dans Plus › Paramètres).
defineProps<{ /** Agenda déjà lié, mais aucun calendrier choisi. */ sansCalendrier: boolean }>();
const emit = defineEmits<{ 'plus-tard': [] }>();
</script>

<template>
  <section aria-labelledby="titre-proposition-agenda" class="flex flex-col gap-2.5 border-y border-trait py-4 text-left">
    <h2 id="titre-proposition-agenda" class="m-0 text-legende font-semibold uppercase tracking-wider text-texte-2">Tes rendez-vous médicaux</h2>
    <p v-if="sansCalendrier" class="m-0 text-sm leading-normal text-texte-2">
      Ton agenda Google est lié : choisis le calendrier de tes rendez-vous médicaux pour voir le prochain ici.
    </p>
    <p v-else class="m-0 text-sm leading-normal text-texte-2">
      Lie ton agenda Google, en lecture seule : ton prochain rendez-vous s'affiche ici, un rappel peut partir la veille, et le résumé
      pour le médecin se prépare depuis le rendez-vous précédent.
    </p>
    <div class="flex flex-wrap items-center gap-2">
      <router-link to="/parametres?agenda=lier"
                   class="inline-flex min-h-11 items-center rounded-controle border-[1.5px] border-contour bg-surface px-4 text-sm font-semibold !text-texte no-underline">
        {{ sansCalendrier ? 'Choisir le calendrier' : 'Lier mon agenda Google' }}
      </router-link>
      <button type="button" class="min-h-11 rounded-controle px-3 text-sm font-medium text-texte-2" @click="emit('plus-tard')">Plus tard</button>
    </div>
  </section>
</template>
