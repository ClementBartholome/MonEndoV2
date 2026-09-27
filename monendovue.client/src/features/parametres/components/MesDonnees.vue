<template>
  <div class="flex flex-col gap-4">
    <p class="text-sm text-muted-foreground">
      Tout ce que tu as noté dans MonEndo t'appartient. Tu peux le récupérer à tout moment, dans un format lisible,
      avec tes photos de suivi.
    </p>
    <div class="flex flex-col gap-2 md:flex-row md:items-center">
      <Button type="button" variant="custom" class="min-h-11 md:w-fit" :disabled="exportEnCours" @click="telechargerMesDonnees">
        <i class="material-symbols-outlined mr-2" aria-hidden="true">download</i>
        {{ exportEnCours ? 'Préparation de l\'export…' : 'Télécharger toutes mes données' }}
      </Button>
      <p class="text-xs text-muted-foreground">Archive ZIP : un fichier de données (JSON) et tes photos.</p>
    </div>
    <p v-if="erreurExport" role="alert" class="text-sm text-destructive">{{ erreurExport }}</p>
  </div>
</template>

<script setup lang="ts">
import {Button} from '@/shared/components/ui/button';
import {useExportDonnees} from '../composables/useExportDonnees';

const {exportEnCours, erreurExport, telechargerMesDonnees} = useExportDonnees();
</script>
