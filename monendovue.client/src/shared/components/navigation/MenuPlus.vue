<template>
  <PanneauBas v-model:open="ouvert" titre="Plus">
    <nav aria-label="Autres rubriques" class="flex flex-col rounded-moyen bg-surface shadow-elevation">
      <router-link
          v-for="(entree, index) in navigationSecondaire"
          :key="entree.vers"
          :to="entree.vers"
          class="flex min-h-16 items-center gap-3 px-4 text-texte"
          :class="{ 'border-b border-trait': index < navigationSecondaire.length - 1 }"
          @click="ouvert = false">
        <i class="material-symbols-outlined rounded-petit p-1.5 text-[22px]" :class="teintesRubrique[entree.teinte ?? 'neutre']" aria-hidden="true">{{ entree.icone }}</i>
        <span class="flex grow flex-col">
          <span class="text-[15px] font-medium">{{ entree.libelle }}</span>
          <span class="text-[13px] text-texte-3">{{ entree.detail }}</span>
        </span>
        <i class="material-symbols-outlined text-texte-3" aria-hidden="true">chevron_right</i>
      </router-link>
    </nav>

    <nav aria-label="Compte" class="flex flex-col rounded-moyen bg-surface shadow-elevation">
      <router-link :to="navigationCompte.vers" class="flex min-h-14 items-center gap-3 border-b border-trait px-4 text-texte" @click="ouvert = false">
        <i class="material-symbols-outlined p-1.5 text-[22px]" aria-hidden="true">{{ navigationCompte.icone }}</i>
        <span class="grow text-[15px]">{{ navigationCompte.libelle }}</span>
        <i class="material-symbols-outlined text-texte-3" aria-hidden="true">chevron_right</i>
      </router-link>
      <button type="button" class="flex min-h-14 items-center gap-3 px-4 text-left text-texte" @click="emit('deconnexion')">
        <i class="material-symbols-outlined p-1.5 text-[22px]" aria-hidden="true">logout</i>
        <span class="grow text-[15px]">Se déconnecter</span>
      </button>
    </nav>

    <div class="flex flex-col items-center">
      <span class="text-xs text-texte-3">MonEndo v{{ version }}</span>
      <LiensLegaux @click="ouvert = false"/>
    </div>
  </PanneauBas>
</template>

<script setup lang="ts">
import PanneauBas from '@/shared/components/PanneauBas.vue';
import LiensLegaux from '@/features/legal/components/LiensLegaux.vue';
import {navigationCompte, navigationSecondaire, teintesRubrique} from '@/shared/config/navigation';

const ouvert = defineModel<boolean>('open', {required: true});
const emit = defineEmits<{ deconnexion: [] }>();

const version = __APP_VERSION__;
</script>
