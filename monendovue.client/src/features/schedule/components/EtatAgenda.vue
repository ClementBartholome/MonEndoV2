<template>
  <section class="flex flex-col gap-3 rounded-carte bg-surface p-5 text-left shadow-elevation" :aria-labelledby="idTitre" :role="contenu.alerte ? 'alert' : undefined">
    <i class="material-symbols-outlined self-start rounded-controle p-2.5 text-titre-page" :class="contenu.teinte" aria-hidden="true">{{ contenu.icone }}</i>
    <h2 :id="idTitre" class="m-0 text-titre-2 font-semibold leading-snug text-texte">{{ contenu.titre }}</h2>
    <p class="m-0 text-sm leading-normal text-texte-2">{{ contenu.texte }}</p>
    <router-link v-if="contenu.lien" :to="contenu.lien.vers"
                 class="inline-flex min-h-12 items-center justify-center rounded-controle bg-button px-4 text-sm font-semibold text-texte no-underline">
      {{ contenu.lien.libelle }}
    </router-link>
    <button v-if="contenu.action" type="button"
            class="min-h-12 rounded-controle border-[1.5px] border-contour bg-surface px-4 text-sm font-medium text-texte"
            @click="emit('reessayer')">{{ contenu.action }}</button>
  </section>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import { useId } from 'radix-vue';
import { teintesRubrique } from '@/shared/config/navigation';

/** Ce que la page Agenda dit quand il n'y a pas de rendez-vous à montrer : à lier, calendrier à choisir, rien à venir, panne. */
export type TypeEtat = 'non-lie' | 'non-disponible' | 'sans-calendrier' | 'aucun' | 'indisponible';

const props = defineProps<{ type: TypeEtat }>();
const emit = defineEmits<{ reessayer: [] }>();
const idTitre = useId(undefined, 'etat-agenda');

interface Contenu {
  icone: string;
  teinte: string;
  titre: string;
  texte: string;
  alerte?: boolean;
  lien?: { vers: string; libelle: string };
  action?: string;
}

const CONTENUS: Record<TypeEtat, Contenu> = {
  'non-lie': {
    icone: 'calendar_month', teinte: teintesRubrique.bilan, titre: 'Ton agenda n\'est pas lié',
    texte: 'Lie ton agenda Google pour retrouver ici tes rendez-vous médicaux, et les préparer en un geste. MonEndo ne lit que le calendrier que tu choisis.',
    lien: { vers: '/parametres', libelle: 'Lier mon agenda Google' },
  },
  'non-disponible': {
    icone: 'calendar_month', teinte: teintesRubrique.neutre, titre: 'La liaison avec Google n\'est pas disponible',
    texte: 'Elle n\'est pas encore ouverte sur MonEndo. Réessaie plus tard.',
  },
  'sans-calendrier': {
    icone: 'calendar_month', teinte: teintesRubrique.bilan, titre: 'Choisis le calendrier à afficher',
    texte: 'Ton agenda est lié, mais MonEndo ne lit rien tant que tu n\'as pas choisi de calendrier. Un calendrier réservé à tes rendez-vous médicaux est idéal.',
    lien: { vers: '/parametres', libelle: 'Choisir un calendrier' },
  },
  aucun: {
    icone: 'event_available', teinte: teintesRubrique.traitement, titre: 'Aucun rendez-vous à venir',
    texte: 'Quand tu en ajoutes un dans ton agenda, il apparaît ici.', action: 'Actualiser',
  },
  indisponible: {
    icone: 'cloud_off', teinte: teintesRubrique.douleur, titre: 'L\'agenda est momentanément indisponible',
    texte: 'Ce que tu as noté dans MonEndo n\'est pas concerné. Réessaie dans un instant.', alerte: true, action: 'Réessayer',
  },
};

const contenu = computed(() => CONTENUS[props.type]);
</script>
