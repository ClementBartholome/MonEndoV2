<template>
  <section class="flex flex-col gap-2.5 rounded-carte bg-surface p-4 text-left shadow-elevation" aria-labelledby="engagements-donnees">
    <h2 id="engagements-donnees" class="m-0 text-sm font-semibold tracking-normal text-texte">Ce que MonEndo s'engage à faire de tes données</h2>
    <ul class="m-0 flex list-none flex-col gap-2.5 p-0 text-sm leading-snug text-texte">
      <li v-for="engagement in ENGAGEMENTS" :key="engagement.icone" class="flex items-start gap-2.5 text-left">
        <i class="material-symbols-outlined text-xl text-texte-2" aria-hidden="true">{{ engagement.icone }}</i>{{ engagement.texte }}
      </li>
    </ul>
    <p class="m-0 text-legende leading-normal text-texte-3">MonEndo ne pose aucun diagnostic et ne remplace pas un professionnel de santé.</p>
  </section>
</template>

<script setup lang="ts">
/** Les engagements montrés avant de demander l'accord aux données de santé : ils décrivent ce que la politique de confidentialité garantit. */
const ENGAGEMENTS = [
  { icone: 'lock', texte: 'Ton suivi n\'est visible que depuis ton compte.' },
  { icone: 'block', texte: 'Ni publicité, ni traceur, ni revente.' },
  { icone: 'dns', texte: 'Hébergé en Europe : serveur en France, photos dans l\'Union européenne.' },
  { icone: 'download', texte: 'Tu peux tout exporter ou tout supprimer, à tout moment.' },
];
</script>
