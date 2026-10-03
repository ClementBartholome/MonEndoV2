<template>
  <main class="mx-auto flex min-h-dvh w-full max-w-md flex-col gap-6 px-6 pb-6 pt-14">
    <header class="flex flex-col gap-3 text-left">
      <h1 class="m-0 text-titre-page font-semibold leading-tight tracking-normal text-texte">Ton compte est créé.<br>Un rappel, si tu veux ?</h1>
      <p v-if="etat === 'ios-a-installer'" class="m-0 text-corps leading-relaxed text-texte-2">
        Sur iPhone, les rappels n'arrivent que si MonEndo est installé sur ton écran d'accueil. Cela se fait une fois.
      </p>
      <p v-else class="m-0 text-corps leading-relaxed text-texte-2">
        Un message chaque soir pour penser à ton bilan du jour. Tu pourras le changer ou le couper à tout moment dans les Paramètres.
      </p>
    </header>

    <div v-if="etat === 'chargement'" class="flex flex-col gap-3" aria-busy="true" aria-label="Chargement">
      <Skeleton class="h-28 w-full rounded-carte"/>
      <Skeleton class="h-20 w-full rounded-carte"/>
    </div>

    <template v-else-if="etat === 'ios-a-installer'">
      <ol class="m-0 flex list-none flex-col gap-3 p-0 text-left">
        <li v-for="(etape, indice) in ETAPES_IOS" :key="etape.titre" class="flex items-center gap-3.5 rounded-carte bg-surface px-4 py-3.5 shadow-elevation">
          <span class="flex h-8 w-8 shrink-0 items-center justify-center rounded-full bg-texte text-sm font-semibold text-fond">{{ indice + 1 }}</span>
          <span class="flex min-w-0 grow flex-col gap-0.5">
            <span class="text-corps font-semibold text-texte">{{ etape.titre }}</span>
            <span class="text-sm leading-snug text-texte-2">{{ etape.detail }}</span>
          </span>
          <i class="material-symbols-outlined shrink-0 rounded-controle bg-teinte-bilan-fond p-2 text-teinte-bilan" aria-hidden="true">{{ etape.icone }}</i>
        </li>
      </ol>
      <p class="m-0 text-left text-sm leading-normal text-texte-3">Tu pourras le faire plus tard : MonEndo fonctionne très bien sans rappel.</p>
    </template>

    <template v-else>
      <div v-if="etat !== 'refuse'" class="flex flex-col gap-3 rounded-carte bg-surface p-4 text-left shadow-elevation">
        <div class="flex flex-col gap-0.5">
          <span class="text-corps font-semibold text-texte">Rappel du bilan quotidien</span>
          <span class="text-sm text-texte-2">Pas de rappel si ton bilan est déjà fait.</span>
        </div>
        <div role="group" aria-label="Heure du rappel" class="grid grid-cols-4 gap-2">
          <button v-for="proposee in HEURES_PROPOSEES" :key="proposee" type="button" :aria-pressed="heureProposee && heure === proposee"
                  class="min-h-11 rounded-controle border-[1.5px] text-sm"
                  :class="heureProposee && heure === proposee ? 'border-texte bg-texte font-semibold text-fond' : 'border-contour bg-surface font-medium text-texte'"
                  @click="choisirHeure(proposee)">{{ heureLisible(proposee) }}</button>
          <button type="button" :aria-pressed="autreHeure"
                  class="min-h-11 rounded-controle border-[1.5px] text-sm"
                  :class="autreHeure ? 'border-texte bg-texte font-semibold text-fond' : 'border-contour bg-surface font-medium text-texte'"
                  @click="autreHeure = true">Autre</button>
        </div>
        <label v-if="autreHeure" class="flex flex-col gap-1 text-sm font-medium text-texte">Heure du rappel
          <input v-model="heure" type="time" class="min-h-11 rounded-controle border-[1.5px] border-contour bg-champ px-3 text-corps font-normal text-texte">
        </label>
      </div>

      <section v-if="etat !== 'refuse'" class="flex flex-col gap-2 text-left" aria-labelledby="apercu-notification">
        <h2 id="apercu-notification" class="m-0 text-legende font-semibold uppercase tracking-wider text-texte-3">Aperçu</h2>
        <div class="flex items-center gap-3 rounded-carte bg-surface px-3.5 py-3 shadow-elevation">
          <img src="@/images/MonEndoIconMobile.jpg" alt="" class="h-10 w-10 rounded-xl object-cover">
          <span class="flex min-w-0 flex-col">
            <span class="text-sm font-semibold text-texte">MonEndo</span>
            <span class="text-sm text-texte-2">Ton bilan du jour t'attend.</span>
          </span>
        </div>
        <p class="m-0 text-sm text-texte-3">La notification ne contient jamais de donnée de santé.</p>
      </section>

      <p v-if="etat === 'refuse'" role="status" class="m-0 flex items-start gap-3 rounded-carte bg-surface p-3.5 text-left text-sm leading-normal text-texte shadow-elevation">
        <i class="material-symbols-outlined rounded-controle bg-teinte-neutre-fond p-1.5 text-teinte-neutre" aria-hidden="true">notifications_off</i>
        <span>Les notifications sont bloquées par ton navigateur. Tu pourras les autoriser plus tard depuis les Paramètres.</span>
      </p>
      <p v-if="erreur" role="alert" class="m-0 text-left text-sm text-danger">{{ erreur }}</p>
    </template>

    <div class="mt-auto flex flex-col gap-1 pt-4">
      <template v-if="etat === 'ios-a-installer' || etat === 'refuse'">
        <button type="button" class="inline-flex min-h-[54px] items-center justify-center rounded-[16px] bg-button text-base font-semibold text-texte" @click="passer">
          {{ etat === 'refuse' ? 'Continuer' : 'J\'ai compris' }}
        </button>
      </template>
      <template v-else-if="etat !== 'chargement'">
        <button type="button" :disabled="enCours"
                class="inline-flex min-h-[54px] items-center justify-center rounded-[16px] bg-button text-base font-semibold text-texte disabled:opacity-70" @click="activer">
          {{ enCours ? 'Activation…' : 'Activer le rappel' }}
        </button>
        <p class="m-0 mt-1 text-center text-sm text-texte-3">Ton téléphone va te demander l'autorisation.</p>
        <button type="button" class="min-h-12 text-corps font-medium text-texte underline" @click="passer">Pas maintenant</button>
      </template>
    </div>
  </main>
</template>

<script setup lang="ts">
import { onMounted } from 'vue';
import { Skeleton } from '@/shared/components/ui/skeleton';
import { HEURES_PROPOSEES, heureLisible, useRappelBienvenue } from '../composables/useRappelBienvenue';

/** Étape facultative après l'inscription : le rappel du bilan du soir (voir `useRappelBienvenue`). */
const ETAPES_IOS = [
  { titre: 'Touche Partager', detail: 'Dans Safari. Si tu ne le vois pas, touche d\'abord le menu « … ».', icone: 'ios_share' },
  { titre: '« Sur l\'écran d\'accueil »', detail: 'Fais défiler le menu si besoin, puis touche Ajouter.', icone: 'add_box' },
  { titre: 'Ouvre MonEndo depuis l\'icône', detail: 'Reconnecte-toi, puis active le rappel dans les Paramètres.', icone: 'favorite' },
];

const { etat, enCours, heure, autreHeure, heureProposee, erreur, demarrer, choisirHeure, activer, passer } = useRappelBienvenue();

onMounted(demarrer);
</script>
