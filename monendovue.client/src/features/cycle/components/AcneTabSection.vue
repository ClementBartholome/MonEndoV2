<script setup lang="ts">
import { computed, toRef } from 'vue'
import { format } from 'date-fns'
import { useAcneTracking } from '@/features/cycle/composables/useAcneTracking'
import AcneTabContent from '@/features/cycle/components/AcneTabContent.vue'
import { symptomeIconConfig } from '@/shared/config/materialSymbols'
import type { AcneTabActions } from '@/features/cycle/types/acne-tab'
import { Dialog, DialogContent, DialogFooter, DialogHeader, DialogTitle } from '@/shared/components/ui/dialog'
import { Button } from '@/shared/components/ui/button'
import { Input } from '@/shared/components/ui/input'
import { FormControl, FormField, FormItem, FormLabel, FormMessage } from '@/shared/components/ui/form'

const props = defineProps<{
  carnetSanteId: number
  refreshKey: number
}>()

const emit = defineEmits<{
  (e: 'open-add'): void
  (e: 'edit-entry', entry: any): void
  (e: 'photo-click', url: string): void
  (e: 'changed'): void
}>()

const tracking = useAcneTracking({
  carnetSanteId: props.carnetSanteId,
  refreshKey: toRef(props, 'refreshKey'),
  onChanged: () => emit('changed'),
  onRequestEditEntry: (entry) => emit('edit-entry', entry),
  onRequestPhotoModal: (url) => emit('photo-click', url),
})

const viewModel = computed(() => ({
  ...tracking.model.value,
  iconConfig: symptomeIconConfig,
  historyExtraFields: [{ key: 'commentaire', label: 'Note' }],
}))

const viewActions = computed<AcneTabActions>(() => ({
  ...tracking.actions,
  openAddAcneDialog: () => emit('open-add'),
}))
</script>

<template>
  <AcneTabContent :model="viewModel" :actions="viewActions" />

  <Dialog v-model:open="tracking.endPeriodDialog.showEndPeriodDialog.value">
    <DialogContent>
      <DialogHeader>
        <DialogTitle class="text-2xl">Terminer la période d'acné</DialogTitle>
      </DialogHeader>
      <div class="flex flex-col gap-4 py-4">
        <p>Cette période a commencé le {{ tracking.endPeriodDialog.selectedPeriodToEnd.value?.startDate }} et se termine actuellement le {{ tracking.endPeriodDialog.selectedPeriodToEnd.value?.endDate }}.</p>
        <FormField name="endDate">
          <FormItem>
            <FormLabel>Date de fin réelle</FormLabel>
            <FormControl>
              <Input type="date" v-model="tracking.endPeriodDialog.endPeriodDate.value" :max="format(new Date(), 'yyyy-MM-dd')" />
            </FormControl>
            <FormMessage />
          </FormItem>
        </FormField>
      </div>
      <DialogFooter>
        <Button variant="outline" @click="tracking.endPeriodDialog.showEndPeriodDialog.value = false">Annuler</Button>
        <Button variant="custom" @click="tracking.endPeriodDialog.confirmEndPeriod">Confirmer</Button>
      </DialogFooter>
    </DialogContent>
  </Dialog>
</template>
