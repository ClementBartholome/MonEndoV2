<template>
  <!-- Un état n'a jamais l'air d'un bouton : texte coloré et icône pleine, sans fond ni bordure. -->
  <span class="inline-flex shrink-0 items-center gap-1 text-sm"
        :class="reponse.statut === 'Pris' ? 'font-medium text-etat-fait' : 'text-texte-3'">
    <i class="material-symbols-outlined icone-pleine text-xl" aria-hidden="true">
      {{ reponse.statut === 'Pris' ? 'check_circle' : 'do_not_disturb_on' }}
    </i>{{ reponse.statut === 'Pris' ? `Pris à ${heureAffichee(reponse.date)}` : 'Ignorée' }}
  </span>
</template>

<script setup lang="ts">
import type { ReponsePrise } from '../types/traitements';
import { heureAffichee } from '../utils/prises';

defineProps<{ reponse: ReponsePrise }>();
</script>
