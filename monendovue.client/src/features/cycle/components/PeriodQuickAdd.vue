<script setup lang="ts">
import { ref } from 'vue'
import { Button } from '@/shared/components/ui/button'
import { Dialog, DialogContent, DialogHeader, DialogTitle } from '@/shared/components/ui/dialog'
import { Input } from '@/shared/components/ui/input'
import { useToast } from '@/shared/components/ui/toast'
import { format, subDays, eachDayOfInterval, isAfter, isBefore, isSameDay } from 'date-fns'

const props = defineProps<{
  isPeriodMarkedToday: boolean
  onMarkPeriod: (date: string) => Promise<void>
}>()

const emit = defineEmits<{
  marked: []
}>()

const { toast } = useToast()
const isLoading = ref(false)
const showDatePicker = ref(false)
const showPeriodRangePicker = ref(false)
const selectedDate = ref(format(new Date(), 'yyyy-MM-dd'))
const periodStartDate = ref(format(new Date(), 'yyyy-MM-dd'))
const periodEndDate = ref(format(new Date(), 'yyyy-MM-dd'))

const handleQuickAdd = async (daysAgo: number) => {
  isLoading.value = true
  try {
    const date = format(subDays(new Date(), daysAgo), 'yyyy-MM-dd')
    await props.onMarkPeriod(date)
    emit('marked')
    
    const label = daysAgo === 0 ? 'aujourd\'hui' : daysAgo === 1 ? 'hier' : `il y a ${daysAgo} jours`
    toast({
      title: 'Règles marquées',
      description: `Marqué comme ${label}`,
      variant: 'custom',
    })
  } catch (error) {
    console.error('Error marking period:', error)
    toast({
      title: 'Erreur',
      description: 'Impossible de marquer les règles',
      variant: 'destructive',
    })
  } finally {
    isLoading.value = false
  }
}

const handleCustomDate = async () => {
  isLoading.value = true
  try {
    await props.onMarkPeriod(selectedDate.value)
    emit('marked')
    showDatePicker.value = false
    
    toast({
      title: 'Règles marquées',
      description: `Marqué pour le ${format(new Date(selectedDate.value), 'dd/MM/yyyy')}`,
      variant: 'custom',
    })
  } catch (error) {
    console.error('Error marking period:', error)
    toast({
      title: 'Erreur',
      description: 'Impossible de marquer les règles',
      variant: 'destructive',
    })
  } finally {
    isLoading.value = false
  }
}

const handlePeriodRange = async () => {
  isLoading.value = true
  try {
    const start = new Date(periodStartDate.value)
    const end = new Date(periodEndDate.value)
    
    if (isAfter(start, end)) {
      toast({
        title: 'Erreur',
        description: 'La date de début doit être avant la date de fin',
        variant: 'destructive',
      })
      isLoading.value = false
      return
    }
    
    // Generate all dates in the range
    const daysInRange = eachDayOfInterval({ start, end })
    
    // Mark each day in the range
    for (const day of daysInRange) {
      await props.onMarkPeriod(format(day, 'yyyy-MM-dd'))
    }
    
    emit('marked')
    showPeriodRangePicker.value = false
    
    toast({
      title: 'Période enregistrée',
      description: `${daysInRange.length} jour${daysInRange.length > 1 ? 's' : ''} marqué${daysInRange.length > 1 ? 's' : ''}`,
      variant: 'custom',
    })
  } catch (error) {
    console.error('Error marking period range:', error)
    toast({
      title: 'Erreur',
      description: 'Impossible de marquer la période',
      variant: 'destructive',
    })
  } finally {
    isLoading.value = false
  }
}
</script>

<template>
  <div v-if="!isPeriodMarkedToday" class="bg-white border-2 border-red-200 rounded-xl p-4 shadow-sm">
    <div class="flex items-center justify-between gap-3 mb-3">
      <div class="flex items-center gap-2">
        <span class="text-2xl">🩸</span>
        <span class="font-medium text-headline">Règles aujourd'hui ?</span>
      </div>
    </div>
    
    <div class="flex gap-2 flex-wrap">
      <Button
        size="sm"
        variant="custom"
        @click="handleQuickAdd(0)"
        :disabled="isLoading"
      >
        Aujourd'hui
      </Button>
      <Button
        size="sm"
        variant="outline"
        @click="handleQuickAdd(1)"
        :disabled="isLoading"
      >
        Hier
      </Button>
      
      <!-- Date picker dialog -->
      <Dialog v-model:open="showDatePicker">
        <Button
          size="sm"
          variant="outline"
          @click="showDatePicker = true"
          :disabled="isLoading"
        >
          Date…
        </Button>
        
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Marquer une date</DialogTitle>
          </DialogHeader>
          
          <div class="flex flex-col gap-4 py-4">
            <div>
              <label class="text-sm font-medium text-headline mb-2 block">Sélectionner une date</label>
              <Input
                v-model="selectedDate"
                type="date"
                :max="format(new Date(), 'yyyy-MM-dd')"
              />
            </div>
            
            <div class="flex gap-2 justify-end">
              <Button
                variant="outline"
                size="sm"
                @click="showDatePicker = false"
              >
                Annuler
              </Button>
              <Button
                variant="custom"
                size="sm"
                @click="handleCustomDate"
                :disabled="isLoading"
              >
                Confirmer
              </Button>
            </div>
          </div>
        </DialogContent>
      </Dialog>
      
      <!-- Period range picker dialog -->
      <Dialog v-model:open="showPeriodRangePicker">
        <Button
          size="sm"
          variant="outline"
          @click="showPeriodRangePicker = true"
          :disabled="isLoading"
        >
          Période…
        </Button>
        
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Marquer une période</DialogTitle>
          </DialogHeader>
          
          <div class="flex flex-col gap-4 py-4">
            <div>
              <label class="text-sm font-medium text-headline mb-2 block">Du</label>
              <Input
                v-model="periodStartDate"
                type="date"
                :max="format(new Date(), 'yyyy-MM-dd')"
              />
            </div>
            
            <div>
              <label class="text-sm font-medium text-headline mb-2 block">Au</label>
              <Input
                v-model="periodEndDate"
                type="date"
                :max="format(new Date(), 'yyyy-MM-dd')"
              />
            </div>
            
            <div class="text-xs text-paragraph">
              <p v-if="periodStartDate && periodEndDate">
                {{ Math.ceil((new Date(periodEndDate).getTime() - new Date(periodStartDate).getTime()) / (1000 * 60 * 60 * 24)) + 1 }} jour(s) sélectionné(s)
              </p>
            </div>
            
            <div class="flex gap-2 justify-end">
              <Button
                variant="outline"
                size="sm"
                @click="showPeriodRangePicker = false"
              >
                Annuler
              </Button>
              <Button
                variant="custom"
                size="sm"
                @click="handlePeriodRange"
                :disabled="isLoading"
              >
                Confirmer
              </Button>
            </div>
          </div>
        </DialogContent>
      </Dialog>
    </div>
  </div>
</template>



