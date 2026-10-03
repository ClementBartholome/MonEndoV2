<template>
  <main class="mx-auto flex w-full max-w-xl flex-col gap-5 px-5 pb-40 pt-24 lg:pb-12 lg:pt-10">
    <header class="flex flex-col gap-2">
      <h1 class="m-0 text-left text-titre-page font-semibold tracking-normal text-texte">S'informer sur l'endométriose</h1>
      <p class="m-0 text-left text-corps text-texte-2">
        Des sources fiables, choisies pour t'aider à mieux comprendre ce que tu vis et à en parler. Prends ce qui te sert, quand tu veux.
      </p>
    </header>

    <section class="flex flex-col gap-2.5 rounded-carte bg-surface p-4 text-left shadow-elevation" aria-labelledby="titre-reperes">
      <h2 id="titre-reperes" class="m-0 text-corps font-semibold text-texte">À garder en tête</h2>
      <ul class="m-0 flex list-disc flex-col gap-1.5 pl-5 text-left text-corps text-texte-2">
        <li>Ta douleur est réelle et mérite d'être écoutée, quels que soient les résultats des examens.</li>
        <li>L'endométriose prend des formes très différentes d'une personne à l'autre&nbsp;: ton vécu n'a pas à ressembler à celui d'une autre.</li>
        <li>Noter tes symptômes au fil des jours aide à en parler en consultation, comme le conseillent les recommandations médicales.</li>
      </ul>
      <router-link to="/export" class="inline-flex min-h-11 items-center self-start text-corps font-semibold underline">Préparer mon prochain rendez-vous</router-link>
      <p class="m-0 text-legende text-texte-3">Si c'est trop lourd en ce moment, tu peux en parler à un soignant.</p>
    </section>

    <section v-for="groupe in GROUPES_SOURCES" :key="groupe.id" class="flex flex-col gap-2" :aria-labelledby="`titre-${groupe.id}`">
      <h2 :id="`titre-${groupe.id}`" class="m-0 text-left text-legende font-semibold uppercase tracking-wider text-texte-3">{{ groupe.titre }}</h2>
      <ul class="m-0 flex list-none flex-col divide-y divide-trait overflow-hidden rounded-carte bg-surface p-0 shadow-elevation">
        <li v-for="source in groupe.sources" :key="source.url">
          <a :href="source.url" target="_blank" rel="noopener noreferrer" :hreflang="source.langue"
             class="flex min-h-16 items-center gap-3 px-3.5 py-2 text-left !text-texte no-underline">
            <span class="flex min-w-0 grow flex-col">
              <span class="text-corps font-medium" :lang="source.langue">{{ source.titre }}</span>
              <span class="text-legende text-texte-3">{{ source.origine }} — {{ source.detail }}<span class="sr-only"> (s'ouvre dans un nouvel onglet)</span></span>
            </span>
            <i class="material-symbols-outlined text-texte-3" aria-hidden="true">open_in_new</i>
          </a>
        </li>
      </ul>
    </section>

    <p class="m-0 text-left text-legende text-texte-3">
      MonEndo ne pose pas de diagnostic et ne remplace pas un soignant. Ces liens sont des sources publiques, consultées le
      {{ SOURCES_CONSULTEES_LE }}, qui s'ouvrent dans un nouvel onglet. Rien de ce que tu notes dans MonEndo n'y est transmis.
      Ces sites ont leurs propres règles de confidentialité.
    </p>
  </main>
</template>

<script setup lang="ts">
import { GROUPES_SOURCES, SOURCES_CONSULTEES_LE } from '../config/sources';
</script>
