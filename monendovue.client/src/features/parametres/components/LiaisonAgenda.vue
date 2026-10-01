<template>
  <div class="flex flex-col gap-4 pb-2 text-left">
    <template v-if="agenda.statut.value?.liee">
      <p class="m-0 text-sm text-texte-2">
        Ton agenda Google est lié{{ depuis }}. MonEndo ne lit que le calendrier que tu choisis, en lecture seule : il n'y
        écrit rien et n'y envoie aucune donnée de santé.
      </p>

      <fieldset class="m-0 flex min-w-0 flex-col gap-2 border-0 p-0">
        <legend class="mb-2 p-0 text-sm font-medium text-texte">Calendrier à afficher</legend>
        <p v-if="!agenda.statut.value.calendrierId" class="m-0 text-sm text-texte-3">
          Choisis-en un : tant que tu n'en as pas choisi, rien n'est lu. Un calendrier réservé à tes rendez-vous médicaux
          est idéal.
        </p>
        <label v-for="calendrier in agenda.calendriers.value" :key="calendrier.id"
               class="flex min-h-12 cursor-pointer items-center gap-3 rounded-controle border-[1.5px] px-3 text-corps text-texte"
               :class="calendrier.id === agenda.statut.value.calendrierId ? 'border-texte bg-surface-2' : 'border-contour'">
          <input type="radio" name="calendrier-agenda" :value="calendrier.id" class="size-4 shrink-0 accent-[var(--texte)]"
                 :checked="calendrier.id === agenda.statut.value.calendrierId" :disabled="agenda.enCours.value"
                 @change="agenda.choisir(calendrier.id)">
          <span class="min-w-0 grow break-words">{{ calendrier.nom }}</span>
          <span v-if="calendrier.principal" class="shrink-0 text-legende text-texte-3">Principal</span>
        </label>
        <button v-if="!agenda.calendriersCharges.value && agenda.erreur.value" type="button"
                class="min-h-11 rounded-controle border-[1.5px] border-contour px-4 font-medium text-texte"
                @click="agenda.chargerCalendriers">Réessayer</button>
      </fieldset>

      <router-link v-if="agenda.statut.value.calendrierId" to="/agenda"
                   class="flex min-h-12 items-center justify-center rounded-controle bg-button px-4 font-semibold text-texte no-underline">
        Voir mon agenda
      </router-link>
      <p class="m-0 text-sm text-texte-3">
        Délier supprime l'accès de MonEndo à ton agenda et le retire aussi chez Google.
      </p>
      <button type="button" :disabled="agenda.enCours.value"
              class="min-h-12 rounded-controle border-[1.5px] border-contour px-4 font-medium text-texte disabled:opacity-60"
              @click="agenda.delier">
        {{ agenda.enCours.value ? 'Un instant…' : 'Délier mon agenda' }}
      </button>
    </template>

    <template v-else>
      <p class="m-0 text-sm text-texte-2">
        En liant ton agenda Google, MonEndo affiche ton prochain rendez-vous sur l'accueil et ton calendrier. Tu choisiras
        ensuite le calendrier à afficher (par exemple un calendrier réservé à tes rendez-vous médicaux) : MonEndo ne lit que
        celui-là, en lecture seule. Il n'écrit rien dans ton agenda et n'envoie aucune donnée de santé à Google. Tu peux
        délier ton agenda à tout moment.
      </p>
      <p class="m-0 rounded-controle bg-surface-2 p-3 text-sm text-texte-2">
        Google affichera d'abord un écran « Cette application n'a pas été validée ». C'est normal : MonEndo est un petit
        projet personnel. Touche « Paramètres avancés », puis « Accéder à monendoapp.fr ».
        Google parlera aussi de « tous tes agendas » : il ne permet pas d'autoriser un seul calendrier. MonEndo, lui, ne lit que
        celui que tu choisis.
      </p>
      <button type="button" :disabled="agenda.enCours.value"
              class="min-h-12 rounded-controle bg-button px-4 font-semibold text-texte disabled:opacity-60"
              @click="agenda.lier">
        {{ agenda.enCours.value ? 'Redirection vers Google…' : 'Lier mon agenda Google' }}
      </button>
    </template>

    <p v-if="agenda.erreur.value" role="alert" class="m-0 text-sm text-danger">{{ agenda.erreur.value }}</p>
  </div>
</template>

<script setup lang="ts">
import { computed, watch } from 'vue';
import { format } from 'date-fns';
import { fr } from 'date-fns/locale';
import type { LiaisonAgenda } from '../composables/useLiaisonAgenda';

/** Contenu du panneau « Agenda Google » : lier (ou délier), choisir le calendrier lu ; l'état vit dans le composable de la page. */
const props = defineProps<{ agenda: LiaisonAgenda }>();

const depuis = computed(() => {
  const date = props.agenda.statut.value?.lieeLe;
  return date ? ` depuis le ${format(new Date(date), 'd MMMM yyyy', { locale: fr })}` : '';
});

// Le statut peut arriver après l'ouverture (retour de Google) : les calendriers ne sont demandés à Google que pour un agenda lié.
watch(() => props.agenda.statut.value?.liee, (liee) => {
  if (liee && !props.agenda.calendriersCharges.value) props.agenda.chargerCalendriers();
}, { immediate: true });
</script>
