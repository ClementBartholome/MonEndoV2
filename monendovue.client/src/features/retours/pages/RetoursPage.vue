<template>
  <main class="mx-auto flex w-full max-w-xl flex-col gap-5 px-5 pb-40 pt-24 lg:pb-12 lg:pt-10">
    <header class="flex flex-col gap-2">
      <h1 class="m-0 text-left text-titre-page font-semibold tracking-normal text-texte">Une suggestion&nbsp;? Un&nbsp;bug&nbsp;?</h1>
      <p class="m-0 text-left text-corps text-texte-2">
        MonEndo est fait par une seule personne, et ton avis compte. Une idée pour l'améliorer, ou quelque chose qui ne
        marche pas comme tu l'attends ? Écris-moi, je lis tout.
      </p>
    </header>

    <nav aria-label="Écrire à l'éditeur" class="flex flex-col divide-y divide-trait overflow-hidden rounded-carte bg-surface shadow-elevation">
      <a v-for="(type, cle) in TYPES_DE_RETOUR" :key="cle" :href="lien(type)"
         class="flex min-h-16 items-center gap-3 px-3.5 py-2 text-left text-texte no-underline">
        <i class="material-symbols-outlined rounded-controle p-1.5 text-titre-2" :class="teintesRubrique[cle === 'bug' ? 'regles' : 'traitement']" aria-hidden="true">{{ type.icone }}</i>
        <span class="flex min-w-0 grow flex-col">
          <span class="text-corps font-medium">{{ type.titre }}</span>
          <span class="text-legende text-texte-3">{{ type.detail }}</span>
        </span>
        <i class="material-symbols-outlined text-texte-3" aria-hidden="true">mail</i>
      </a>
    </nav>

    <section class="flex flex-col gap-2 rounded-carte bg-surface p-4 text-left shadow-elevation" aria-labelledby="titre-aide">
      <h2 id="titre-aide" class="m-0 text-corps font-semibold text-texte">Pour m'aider à te répondre</h2>
      <ul class="m-0 flex list-disc flex-col gap-1 pl-5 text-left text-corps text-texte-2">
        <li>ce que tu faisais quand c'est arrivé&nbsp;;</li>
        <li>ce que tu espérais voir, et ce que tu as vu&nbsp;;</li>
        <li>une capture d'écran, si tu peux&nbsp;: joins-la à ton message.</li>
      </ul>
      <p class="m-0 text-legende text-texte-3">
        Une capture peut montrer ce que tu as noté dans MonEndo. Tu choisis ce que tu m'envoies&nbsp;: masque ce que tu préfères
        garder pour toi, ou décris simplement ce que tu vois.
      </p>
    </section>

    <section class="flex flex-col gap-2 text-left" aria-labelledby="titre-adresse">
      <h2 id="titre-adresse" class="m-0 text-legende font-semibold uppercase tracking-wider text-texte-3">Pas de messagerie sur cet appareil&nbsp;?</h2>
      <div class="flex items-center justify-between gap-3 rounded-carte bg-surface p-4 shadow-elevation">
        <span class="min-w-0 select-all break-all text-corps text-texte">{{ EDITEUR.email }}</span>
        <Button type="button" variant="outline" size="sm" class="shrink-0" @click="copierAdresse">Copier</Button>
      </div>
      <p class="m-0 text-legende text-texte-3">
        Ton message m'arrive par e-mail, avec ton adresse : je ne m'en sers que pour te répondre.
        <router-link to="/confidentialite" class="underline">En savoir plus</router-link>
      </p>
    </section>
  </main>
</template>

<script setup lang="ts">
import { useRouter } from 'vue-router';
import { Button } from '@/shared/components/ui/button';
import { useToast } from '@/shared/components/ui/toast';
import { teintesRubrique } from '@/shared/config/navigation';
import { EDITEUR } from '@/features/legal/config/editeur';
import { TYPES_DE_RETOUR, type TypeDeRetour } from '../config/types-de-retour';
import { lienMailto } from '../utils/courriel';

const router = useRouter();
const { toast } = useToast();

/** Page d'où l'on vient, gardée par vue-router dans l'historique : le chemin seulement, aucune donnée. */
const pagePrecedente = router.options.history.state.back;

const lien = (type: TypeDeRetour) => lienMailto(EDITEUR.email, type, {
  page: typeof pagePrecedente === 'string' ? pagePrecedente : null,
  version: __APP_VERSION__,
  appareil: navigator.userAgent,
});

async function copierAdresse() {
  try {
    await navigator.clipboard.writeText(EDITEUR.email);
    toast({ title: 'Adresse copiée', variant: 'custom' });
  } catch {
    // Presse-papiers refusé : l'adresse reste affichée et sélectionnable.
    toast({ title: "Copie impossible", description: "Sélectionne l'adresse pour la copier.", variant: 'custom' });
  }
}
</script>
