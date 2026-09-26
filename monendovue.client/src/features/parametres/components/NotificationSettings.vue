<script setup lang="ts">
import { onMounted } from 'vue';
import { Button } from '@/shared/components/ui/button';
import { Skeleton } from '@/shared/components/ui/skeleton';
import { useToast } from '@/shared/components/ui/toast';
import { usePushNotifications } from '@/features/parametres/composables/usePushNotifications';
import ReglageRappelCard from '@/features/parametres/components/ReglageRappelCard.vue';
import { presentationRappels } from '@/features/parametres/config/rappels';
import type { ReglageRappel, TypeRappel } from '@/features/parametres/types/notifications';

const { toast } = useToast();
const { etat, enCours, rappels, initialiser, activer, desactiver, enregistrerRappel, envoyerTest } =
    usePushNotifications();

onMounted(() => {
  initialiser().catch((error) => {
    console.error('Push initialisation error:', error);
    etat.value = 'non-disponible';
  });
});

const erreur = (description: string) => toast({ title: 'Erreur', description, variant: 'destructive' });

const onActiver = async () => {
  try {
    await activer();
    if (etat.value === 'actif') {
      toast({ title: 'Notifications activées', description: 'Tu recevras le rappel de ton bilan quotidien.', variant: 'custom' });
    }
  } catch (e) {
    erreur((e as Error).message);
  }
};

const onDesactiver = async () => {
  try {
    await desactiver();
    toast({ title: 'Notifications désactivées', description: "Cet appareil ne recevra plus de notifications.", variant: 'custom' });
  } catch {
    erreur('La désactivation a échoué. Réessaie dans un instant.');
  }
};

const onModifierRappel = async (type: TypeRappel, modifications: Partial<ReglageRappel>) => {
  try {
    await enregistrerRappel(type, modifications);
    if (modifications.heure || modifications.jourSemaine !== undefined) {
      toast({ title: 'Rappel enregistré', description: presentationRappels[type].titre, variant: 'custom' });
    }
  } catch {
    erreur("Le réglage du rappel n'a pas pu être enregistré.");
  }
};

const onTest = async () => {
  try {
    await envoyerTest();
    toast({ title: 'Notification envoyée', description: 'Elle devrait arriver dans quelques secondes.', variant: 'custom' });
  } catch {
    erreur("La notification de test n'a pas pu être envoyée. Désactive puis réactive les notifications.");
  }
};
</script>

<template>
  <div class="flex gap-2 mb-4 items-center">
    <i class="material-symbols-outlined">notifications</i>
    <h3 class="text-headline text-2xl">Notifications</h3>
  </div>
  <hr class="mb-4 border-gray-300">

  <Skeleton v-if="etat === 'chargement'" class="h-16 w-full rounded-xl" />

  <p v-else-if="etat === 'non-supporte'" class="text-paragraph">
    Ce navigateur ne permet pas de recevoir des notifications.
  </p>

  <p v-else-if="etat === 'non-disponible'" class="text-paragraph">
    Les notifications ne sont pas disponibles pour le moment.
  </p>

  <div v-else-if="etat === 'ios-a-installer'" class="flex flex-col gap-3">
    <p class="text-paragraph">
      Sur iPhone et iPad (iOS 16.4 ou plus récent), les notifications fonctionnent quand MonEndo est installée sur l'écran d'accueil :
    </p>
    <ol class="flex flex-col gap-2">
      <li class="flex items-start gap-2">
        <span class="shrink-0 w-7 h-7 rounded-full bg-button/20 text-headline font-bold flex items-center justify-center">1</span>
        <span class="text-paragraph">Dans Safari, touche le bouton Partager <i class="material-symbols-outlined align-middle text-base">ios_share</i>.</span>
      </li>
      <li class="flex items-start gap-2">
        <span class="shrink-0 w-7 h-7 rounded-full bg-button/20 text-headline font-bold flex items-center justify-center">2</span>
        <span class="text-paragraph">Choisis « Sur l'écran d'accueil », puis « Ajouter ».</span>
      </li>
      <li class="flex items-start gap-2">
        <span class="shrink-0 w-7 h-7 rounded-full bg-button/20 text-headline font-bold flex items-center justify-center">3</span>
        <span class="text-paragraph">Ouvre MonEndo depuis la nouvelle icône et reviens dans Paramètres pour activer les notifications.</span>
      </li>
    </ol>
  </div>

  <p v-else-if="etat === 'refuse'" class="text-paragraph">
    Les notifications sont bloquées pour MonEndo. Sur iPhone : Réglages › Notifications › MonEndo.
    Sur ordinateur : autorise les notifications dans les paramètres du site de ton navigateur, puis recharge la page.
  </p>

  <div v-else-if="etat === 'inactif'" class="flex flex-col gap-3">
    <p class="text-paragraph">
      Reçois un rappel quand ton bilan quotidien n'est pas encore rempli, à l'heure de ton choix.
    </p>
    <Button variant="custom" class="w-full sm:w-auto sm:self-start min-h-11" :disabled="enCours" @click="onActiver">
      <i class="material-symbols-outlined mr-2">notifications_active</i>
      Activer les notifications sur cet appareil
    </Button>
  </div>

  <div v-else class="flex flex-col gap-4">
    <ReglageRappelCard
        v-for="rappel in rappels.filter((r) => presentationRappels[r.type])"
        :key="rappel.type"
        :rappel="rappel"
        :presentation="presentationRappels[rappel.type]"
        :desactive="enCours"
        @modifier="(modifications) => onModifierRappel(rappel.type, modifications)"
    />

    <div class="flex flex-col sm:flex-row gap-2">
      <Button variant="outline" class="min-h-11" :disabled="enCours" @click="onTest">
        <i class="material-symbols-outlined mr-2">send</i>
        Envoyer une notification de test
      </Button>
      <Button variant="outline" class="min-h-11" :disabled="enCours" @click="onDesactiver">
        <i class="material-symbols-outlined mr-2">notifications_off</i>
        Désactiver sur cet appareil
      </Button>
    </div>
  </div>
</template>
