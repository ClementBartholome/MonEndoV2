<template>
  <main class="mx-auto flex w-full max-w-xl flex-col gap-3.5 px-5 pb-28 pt-24 lg:pt-10">
    <header class="flex flex-col gap-2">
      <h1 class="m-0 text-[22px] font-semibold tracking-normal text-texte">{{ date }}</h1>
      <div v-if="aujourdhui" class="flex flex-wrap gap-2">
        <span v-if="aujourdhui.cycle.enRegles"
              class="inline-flex items-center gap-1.5 text-sm font-medium text-teinte-regles">
          <i class="material-symbols-outlined icone-pleine text-lg" aria-hidden="true">{{ materialSymbols.cycle }}</i>Règles · jour {{ aujourdhui.cycle.jourDeRegles }}
        </span>
        <span v-else-if="aujourdhui.cycle.jourDuCycle"
              class="inline-flex items-center gap-1.5 text-sm text-texte-2">
          <i class="material-symbols-outlined text-lg" aria-hidden="true">{{ materialSymbols.cycle }}</i>Jour {{ aujourdhui.cycle.jourDuCycle }} du cycle
        </span>
      </div>
    </header>

    <div v-if="chargement" class="flex flex-col gap-3.5" aria-busy="true" aria-label="Chargement de l'accueil">
      <Skeleton class="h-40 rounded-carte"/>
      <Skeleton class="h-28 rounded-carte"/>
      <Skeleton class="h-36 rounded-carte"/>
    </div>

    <section v-else-if="erreur" class="flex flex-col items-start gap-3 rounded-carte bg-surface p-5 shadow-elevation" role="alert">
      <p class="m-0 text-texte">L'accueil n'a pas pu être chargé.</p>
      <Button variant="custom" class="min-h-11" @click="charger">Réessayer</Button>
    </section>

    <template v-else-if="aujourdhui">
      <CarteBilanDuJour :bilan="aujourdhui.bilan"/>
      <TuilesAjout/>
      <CarteTraitementsDuJour v-if="aujourdhui.prisesPrevues.length || aujourdhui.auBesoin.length"
                              :prises="aujourdhui.prisesPrevues" :au-besoin="aujourdhui.auBesoin"
                              :prise-en-cours="priseEnCours" @prendre="noterPrise"/>
      <CarteRendezVous v-if="prochainRendezVous" :evenement="prochainRendezVous"/>
      <CarteSemaine v-if="phrases.length" :phrases="phrases"/>
    </template>
  </main>
</template>

<script setup lang="ts">
import { onMounted } from 'vue';
import { format } from 'date-fns';
import { fr } from 'date-fns/locale';
import { Button } from '@/shared/components/ui/button';
import { Skeleton } from '@/shared/components/ui/skeleton';
import { useToast } from '@/shared/components/ui/toast';
import { materialSymbols } from '@/shared/config/materialSymbols';
import type { PrisePrevue } from '@/features/medicament/types/traitements';
import { heureAffichee } from '@/features/medicament/utils/prises';
import CarteBilanDuJour from '../components/CarteBilanDuJour.vue';
import CarteTraitementsDuJour from '../components/CarteTraitementsDuJour.vue';
import TuilesAjout from '../components/TuilesAjout.vue';
import CarteRendezVous from '../components/CarteRendezVous.vue';
import CarteSemaine from '../components/CarteSemaine.vue';
import { useAujourdhui } from '../composables/useAujourdhui';
const { toast } = useToast();
const { aujourdhui, prochainRendezVous, chargement, erreur, priseEnCours, phrases, charger, prendre } = useAujourdhui();

const jour = format(new Date(), 'EEEE d MMMM', { locale: fr });
const date = jour.charAt(0).toUpperCase() + jour.slice(1);

/** Retour immédiat : message avec le traitement et l'heure notée (la carte se met à jour en même temps). */
async function noterPrise(prise: PrisePrevue) {
  if (await prendre(prise)) {
    toast({ title: 'Prise notée', description: `${prise.nom} à ${heureAffichee(prise.reponse!.date)}`, variant: 'custom' });
  } else {
    toast({ title: 'Prise non enregistrée', description: 'Réessaie dans un instant.', variant: 'destructive' });
  }
}

onMounted(charger);
</script>
