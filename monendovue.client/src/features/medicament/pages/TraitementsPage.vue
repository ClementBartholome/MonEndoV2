<template>
  <main class="mx-auto flex w-full max-w-xl flex-col gap-5 px-5 pb-40 pt-24 lg:pb-12 lg:pt-10">
    <header class="flex items-center gap-3">
      <i class="material-symbols-outlined rounded-controle bg-teinte-traitement-fond p-2 text-titre-page text-teinte-traitement" aria-hidden="true">pill</i>
      <h1 class="m-0 grow text-titre-page font-semibold tracking-normal text-texte">Traitements</h1>
      <button type="button" class="hidden min-h-11 items-center gap-2 rounded-controle bg-button px-4 font-semibold text-texte lg:inline-flex"
              @click="ouvrirAjout">
        <i class="material-symbols-outlined" aria-hidden="true">add</i>Ajouter un traitement
      </button>
    </header>

    <div v-if="chargement" class="flex flex-col gap-3.5" aria-busy="true" aria-label="Chargement des traitements">
      <Skeleton class="h-32 rounded-carte"/>
      <Skeleton class="h-24 rounded-carte"/>
      <Skeleton class="h-40 rounded-carte"/>
    </div>

    <section v-else-if="erreur" role="alert" class="flex flex-col items-start gap-3 rounded-carte bg-surface p-5 shadow-elevation">
      <p class="m-0 text-texte">Les traitements n'ont pas pu être chargés.</p>
      <Button variant="custom" class="min-h-11" @click="charger">Réessayer</Button>
    </section>

    <EmptyStateAction v-else-if="!donnees?.enCours.length && !donnees?.termines.length" title="Aucun traitement pour l'instant"
                      description="Ajoute un médicament avec ses horaires : les prises du jour apparaîtront ici et sur l'accueil."
                      action-label="Ajouter un traitement" @action="ouvrirAjout"/>

    <template v-else-if="donnees">
      <PrisesDuJour v-if="donnees.prisesPrevues.length || !donnees.auBesoin.length"
                    :groupes="groupes" :faites="faites" :total="donnees.prisesPrevues.length" :envoi="envoi"
                    @repondre="repondrePrise" @annuler="annulerPrise"/>
      <ListeAuBesoinEtSoins :au-besoin="donnees.auBesoin" :soins="donnees.soins" :envoi="envoi"
                            @prendre="prendre" @seance="seance"/>
      <MesTraitements :en-cours="donnees.enCours" :termines="donnees.termines" @ouvrir="(traitement) => router.push(`/medicaments/${traitement.id}`)"/>
    </template>

    <button type="button"
            class="fixed bottom-[calc(4.25rem+16px)] right-5 z-30 inline-flex min-h-14 items-center gap-2 rounded-controle bg-button pl-4 pr-5 text-corps font-semibold text-texte shadow-elevation lg:hidden"
            @click="ouvrirAjout">
      <i class="material-symbols-outlined" aria-hidden="true">add</i>Ajouter un traitement
    </button>

    <SaisieTraitement v-model:open="saisieOuverte" :traitement="traitementModifie" :actions="actions"/>
  </main>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { Button } from '@/shared/components/ui/button';
import { Skeleton } from '@/shared/components/ui/skeleton';
import { useToast } from '@/shared/components/ui/toast';
import EmptyStateAction from '@/shared/components/EmptyStateAction.vue';
import PrisesDuJour from '../components/PrisesDuJour.vue';
import ListeAuBesoinEtSoins from '../components/ListeAuBesoinEtSoins.vue';
import MesTraitements from '../components/MesTraitements.vue';
import SaisieTraitement, { type ActionsSaisieTraitement } from '../components/SaisieTraitement.vue';
import { useTraitements } from '../composables/useTraitements';
import type { PrisePrevue, Soin, StatutPrise, Traitement, TraitementAuBesoin } from '../types/traitements';

const route = useRoute();
const router = useRouter();
const { toast } = useToast();
const { donnees, chargement, erreur, envoi, groupes, faites, charger, repondre, annuler, prendreAuBesoin, noterSeance, enregistrer, arreter } =
    useTraitements();

const saisieOuverte = ref(false);
const traitementModifie = ref<Traitement | null>(null);

const actions: ActionsSaisieTraitement = {
  enregistrer: async (saisie, id) => {
    await enregistrer(saisie, id);
    toast({ title: id === null ? 'Traitement ajouté' : 'Traitement modifié', variant: 'custom' });
  },
  arreter: async (id) => {
    await arreter(id);
    toast({ title: 'Traitement arrêté', variant: 'custom' });
  },
};

/** Écriture rapide depuis une liste : confirme par un toast, signale l'échec sans quitter la page. */
async function noter(action: () => Promise<void>, succes: string) {
  try {
    await action();
    toast({ title: succes, variant: 'custom' });
  } catch {
    toast({ title: 'L\'enregistrement a échoué. Réessaie dans un instant.', variant: 'destructive' });
  }
}

const repondrePrise = (prise: PrisePrevue, statut: StatutPrise) =>
  noter(() => repondre(prise, statut), statut === 'Pris' ? `${prise.nom} : prise notée` : `${prise.nom} : prise ignorée`);
const annulerPrise = (prise: PrisePrevue) => noter(() => annuler(prise), 'Réponse annulée');
const prendre = (traitement: TraitementAuBesoin) => noter(() => prendreAuBesoin(traitement), `${traitement.nom} : prise notée`);
const seance = (soin: Soin) => noter(() => noterSeance(soin), `${soin.nom} : séance notée`);

function ouvrirAjout() {
  traitementModifie.value = null;
  saisieOuverte.value = true;
}

onMounted(async () => {
  if (route.query.ajouter !== undefined) {
    ouvrirAjout();
    await router.replace({ query: {} });
  }
  await charger();
});
</script>
