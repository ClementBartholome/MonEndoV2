import { computed, onMounted, ref, watch, type Ref } from 'vue'
import { format } from 'date-fns'
import { useDateTimeFormat } from '@/shared/composables/useDateTimeFormat'
import { useToast } from '@/shared/components/ui/toast'
import apiService from '@/shared/services/apiService'
import type { SymptomeCycle } from '@/features/cycle/types/symptome-cycle'
import type { AcneTabModel } from '@/features/cycle/types/acne-tab'

interface UseAcneTrackingOptions {
  carnetSanteId: number
  refreshKey: Ref<number>
  onChanged?: () => void
  onRequestEditEntry?: (entry: any) => void
  onRequestPhotoModal?: (url: string) => void
}

const ACNE_WINDOW_RADIUS = 1
const ACNE_HISTORY_INITIAL_COUNT = 8

const parseDisplayDate = (displayDate: string): Date => {
  const [day, month, year] = displayDate.split('/').map(Number)
  return new Date(year, month - 1, day)
}

const parseMonthYear = (monthYear: string): Date => {
  const [yearValue, monthValue] = monthYear.split('-').map(Number)
  return new Date(yearValue, monthValue - 1, 1)
}

const monthKeyFromDate = (date: Date): string => {
  return `${date.getFullYear()}-${String(date.getMonth() + 1).padStart(2, '0')}`
}

const monthLabelFromKey = (monthKey: string): string => {
  const [yearValue, monthValue] = monthKey.split('-').map(Number)
  const date = new Date(yearValue, monthValue - 1, 1)
  return date.toLocaleDateString('fr-FR', { month: 'long', year: 'numeric' })
}

export const useAcneTracking = (options: UseAcneTrackingOptions) => {
  const { formatDateDisplay, combineDateTime, getCurrentMonthYear } = useDateTimeFormat()
  const { toast } = useToast()

  const acneMonthYear = ref(getCurrentMonthYear())
  const selectedComparePhotoIds = ref<number[]>([])
  const acneWindowEntriesByMonth = ref<Record<string, any[]>>({})

  const showFullAcneHistory = ref(false)
  const isAcneHistoryExpanded = ref(false)
  const showAcneHistoryWithPhotos = ref(false)
  const isQuickAddingAcne = ref(false)
  const acneMarkedToday = ref(false)

  const showEndPeriodDialog = ref(false)
  const selectedPeriodToEnd = ref<any>(null)
  const endPeriodDate = ref('')

  const mapSymptomeToViewModel = (symptomeCycle: SymptomeCycle) => ({
    id: symptomeCycle.id,
    typeSymptome: symptomeCycle.typeSymptome,
    date: formatDateDisplay(symptomeCycle.date),
    time: format(new Date(symptomeCycle.date), 'HH:mm').replace(':', 'h'),
    intensite: symptomeCycle.intensite,
    commentaire: symptomeCycle.commentaire || '',
    photoUrl: symptomeCycle.photoUrl || '',
  })

  const notifyChanged = () => {
    options.onChanged?.()
  }

  const acneWindowMonthKeys = computed(() => {
    const center = parseMonthYear(acneMonthYear.value)
    const keys: string[] = []

    for (let offset = -ACNE_WINDOW_RADIUS; offset <= ACNE_WINDOW_RADIUS; offset++) {
      const date = new Date(center)
      date.setMonth(center.getMonth() + offset)
      keys.push(monthKeyFromDate(date))
    }

    return keys
  })

  const acneWindowDisplayMonthKeys = computed(() => {
    if (acneWindowMonthKeys.value.length === 0) {
      return []
    }

    const selectedIndex = acneWindowMonthKeys.value.indexOf(acneMonthYear.value)
    if (selectedIndex < 0) {
      return acneWindowMonthKeys.value
    }

    const selectedKey = acneWindowMonthKeys.value[selectedIndex]
    const before = acneWindowMonthKeys.value.slice(0, selectedIndex).reverse()
    const after = acneWindowMonthKeys.value.slice(selectedIndex + 1)
    return [selectedKey, ...before, ...after]
  })

  const acneWindowLabel = computed(() => {
    if (acneWindowMonthKeys.value.length === 0) {
      return ''
    }

    const firstLabel = monthLabelFromKey(acneWindowMonthKeys.value[0])
    const lastLabel = monthLabelFromKey(acneWindowMonthKeys.value[acneWindowMonthKeys.value.length - 1])
    return `${firstLabel} -> ${lastLabel}`
  })

  const acneWindowMonths = computed(() => {
    return acneWindowDisplayMonthKeys.value.map((monthKey) => {
      const monthEntries = acneWindowEntriesByMonth.value[monthKey] ?? []
      const photoEntries = monthEntries
        .filter((entry: any) => entry.typeSymptome === 'Acné' && !!entry.photoUrl)
        .sort((a: any, b: any) => parseDisplayDate(b.date).getTime() - parseDisplayDate(a.date).getTime())

      return {
        key: monthKey,
        label: monthLabelFromKey(monthKey),
        entries: photoEntries,
      }
    })
  })

  const acnePhotoEntries = computed(() => acneWindowMonths.value.flatMap((month) => month.entries))

  const currentMonthEntries = computed(() => acneWindowEntriesByMonth.value[acneMonthYear.value] ?? [])

  const processedAcneEntries = computed(() => {
    if (!currentMonthEntries.value || currentMonthEntries.value.length === 0) return []

    const sortedEntries = [...currentMonthEntries.value]
      .filter((entry: any) => entry.typeSymptome === 'Acné')
      .sort((a: any, b: any) => parseDisplayDate(a.date).getTime() - parseDisplayDate(b.date).getTime())

    const result: any[] = []
    let i = 0

    while (i < sortedEntries.length) {
      const acneGroup = [sortedEntries[i]]
      let j = i + 1

      while (j < sortedEntries.length) {
        const prevDate = parseDisplayDate(sortedEntries[j - 1].date)
        const currDate = parseDisplayDate(sortedEntries[j].date)
        const diffInDays = Math.round((currDate.getTime() - prevDate.getTime()) / (1000 * 60 * 60 * 24))
        if (diffInDays !== 1) break
        acneGroup.push(sortedEntries[j])
        j++
      }

      if (acneGroup.length > 1) {
        const avg = acneGroup.reduce((sum, e) => sum + Number(e.intensite || 0), 0) / acneGroup.length
        result.push({
          id: `acne-group-${acneGroup[0].id}`,
          typeSymptome: 'Acné',
          date: `${acneGroup[0].date} - ${acneGroup[acneGroup.length - 1].date}`,
          time: `${acneGroup.length} jour${acneGroup.length > 1 ? 's' : ''}`,
          intensite: avg % 1 === 0 ? avg.toString() : avg.toFixed(1),
          commentaire: acneGroup[0].commentaire,
          photoUrl: acneGroup[0].photoUrl || '',
          isGroup: true,
          groupedEntries: acneGroup,
          entryIds: acneGroup.map((entry) => entry.id),
        })
      } else {
        result.push(acneGroup[0])
      }

      i = j
    }

    return result
  })

  const acneHistoryEntriesWithoutPhoto = computed(() => {
    return processedAcneEntries.value.filter((entry: any) => !entry.photoUrl)
  })

  const acneHistoryHiddenByPhotoCount = computed(() => {
    return Math.max(0, processedAcneEntries.value.length - acneHistoryEntriesWithoutPhoto.value.length)
  })

  const acneHistorySourceEntries = computed(() => {
    if (showAcneHistoryWithPhotos.value) {
      return processedAcneEntries.value
    }

    return acneHistoryEntriesWithoutPhoto.value
  })

  const displayedAcneHistoryEntries = computed(() => {
    if (showFullAcneHistory.value) {
      return acneHistorySourceEntries.value
    }

    return acneHistorySourceEntries.value.slice(0, ACNE_HISTORY_INITIAL_COUNT)
  })

  const ongoingAcnePeriods = computed(() => {
    if (!currentMonthEntries.value || currentMonthEntries.value.length === 0) return []

    const acneEntries = currentMonthEntries.value
      .filter((entry: any) => entry.typeSymptome === 'Acné')
      .sort((a: any, b: any) => parseDisplayDate(a.date).getTime() - parseDisplayDate(b.date).getTime())

    if (acneEntries.length === 0) return []

    const groups: any[] = []
    let currentGroup = [acneEntries[0]]

    for (let i = 1; i < acneEntries.length; i++) {
      const prevDate = parseDisplayDate(acneEntries[i - 1].date)
      const currentDate = parseDisplayDate(acneEntries[i].date)
      const diffInDays = Math.round((currentDate.getTime() - prevDate.getTime()) / (1000 * 60 * 60 * 24))

      if (diffInDays === 1) {
        currentGroup.push(acneEntries[i])
      } else {
        if (currentGroup.length > 1) {
          groups.push([...currentGroup])
        }
        currentGroup = [acneEntries[i]]
      }
    }

    if (currentGroup.length > 1) {
      groups.push(currentGroup)
    }

    const today = new Date()
    today.setHours(0, 0, 0, 0)

    return groups
      .filter((group) => {
        const lastDate = parseDisplayDate(group[group.length - 1].date)
        lastDate.setHours(0, 0, 0, 0)
        return Math.round((today.getTime() - lastDate.getTime()) / (1000 * 60 * 60 * 24)) === 0
      })
      .map((group) => ({
        startDate: group[0].date,
        endDate: group[group.length - 1].date,
        duration: group.length,
        entries: group,
        avgIntensity: (() => {
          const avg = group.reduce((sum: number, e: any) => sum + Number(e.intensite || 0), 0) / group.length
          return avg % 1 === 0 ? avg.toString() : avg.toFixed(1)
        })(),
      }))
  })

  const orderedComparePhotos = computed(() => {
    return selectedComparePhotoIds.value
      .map((id) => acnePhotoEntries.value.find((entry: any) => entry.id === id))
      .filter((entry): entry is any => !!entry)
      .sort((a: any, b: any) => parseDisplayDate(a.date).getTime() - parseDisplayDate(b.date).getTime())
  })

  const fetchMonthEntries = async (monthKey: string, force = false) => {
    if (!force && acneWindowEntriesByMonth.value[monthKey]) {
      return
    }

    const [yearValue, monthValue] = monthKey.split('-').map(Number)
    const response = await apiService.getSymptomesByMonth(options.carnetSanteId, monthValue, yearValue)
    acneWindowEntriesByMonth.value[monthKey] = response.map((symptomeCycle: SymptomeCycle) => mapSymptomeToViewModel(symptomeCycle))
  }

  const hydrateAcneWindow = async () => {
    const missingMonthKeys = acneWindowMonthKeys.value.filter((monthKey) => !acneWindowEntriesByMonth.value[monthKey])
    if (missingMonthKeys.length === 0) {
      return
    }

    try {
      await Promise.all(missingMonthKeys.map((monthKey) => fetchMonthEntries(monthKey, false)))
    } catch (error) {
      console.error('Erreur lors du chargement de la fenetre acné:', error)
    }
  }

  const refreshAcneMarkedToday = async () => {
    const now = new Date()
    const currentMonth = now.getMonth() + 1
    const currentYear = now.getFullYear()
    const todayDisplay = format(now, 'dd/MM/yyyy')

    try {
      const currentMonthEntries = await apiService.getSymptomesByMonth(options.carnetSanteId, currentMonth, currentYear)
      acneMarkedToday.value = currentMonthEntries
        .some((entry: SymptomeCycle) => entry.typeSymptome === 'Acné' && formatDateDisplay(entry.date) === todayDisplay)
    } catch {
      acneMarkedToday.value = false
    }
  }

  const refresh = async () => {
    await fetchMonthEntries(acneMonthYear.value, true)
    await hydrateAcneWindow()
    await refreshAcneMarkedToday()
  }

  const onAcneMonthYearChange = (value: string) => {
    acneMonthYear.value = value
  }

  const slideAcneWindow = (direction: -1 | 1) => {
    const center = parseMonthYear(acneMonthYear.value)
    center.setMonth(center.getMonth() + direction)
    acneMonthYear.value = monthKeyFromDate(center)
  }

  const togglePhotoCompare = (id: number) => {
    if (selectedComparePhotoIds.value.includes(id)) {
      selectedComparePhotoIds.value = selectedComparePhotoIds.value.filter((photoId) => photoId !== id)
      return
    }

    if (selectedComparePhotoIds.value.length === 2) {
      selectedComparePhotoIds.value = [selectedComparePhotoIds.value[1], id]
      return
    }

    selectedComparePhotoIds.value = [...selectedComparePhotoIds.value, id]
  }

  const isPhotoSelectedForCompare = (id: number) => selectedComparePhotoIds.value.includes(id)

  const resetPhotoCompare = () => {
    selectedComparePhotoIds.value = []
  }

  const selectLatestComparePair = () => {
    const latestEntries = [...acnePhotoEntries.value]
      .sort((a: any, b: any) => parseDisplayDate(b.date).getTime() - parseDisplayDate(a.date).getTime())
      .slice(0, 2)

    if (latestEntries.length < 2) {
      return
    }

    selectedComparePhotoIds.value = latestEntries.map((entry: any) => entry.id)
  }

  const toggleShowFullHistory = () => {
    showFullAcneHistory.value = !showFullAcneHistory.value
  }

  const toggleAcneHistoryExpanded = () => {
    isAcneHistoryExpanded.value = !isAcneHistoryExpanded.value
  }

  const toggleAcneHistoryWithPhotos = () => {
    showAcneHistoryWithPhotos.value = !showAcneHistoryWithPhotos.value
    showFullAcneHistory.value = false
  }

  const quickAddAcneToday = async () => {
    isQuickAddingAcne.value = true
    try {
      const formData = new FormData()
      formData.append('typeSymptome', 'Acné')
      formData.append('carnetSanteId', options.carnetSanteId.toString())
      formData.append('date', combineDateTime(format(new Date(), 'yyyy-MM-dd'), format(new Date(), 'HH:mm')).toISOString())
      formData.append('intensite', '5')
      formData.append('commentaire', '')

      await apiService.postDonneesSymptomesCycle(formData)
      await refresh()
      notifyChanged()

      toast({ title: 'Acné enregistrée', description: 'Marquée pour aujourd\'hui', variant: 'custom' })
    } catch (error: any) {
      toast({ title: 'Erreur', description: error?.message || 'Impossible d\'enregistrer', variant: 'destructive' })
    } finally {
      isQuickAddingAcne.value = false
    }
  }

  const deleteSymptome = async (id: string | number) => {
    const entry = processedAcneEntries.value.find((item: any) => item.id === id)
    if (!entry) return

    try {
      if (entry?.isGroup && entry.entryIds) {
        await Promise.all(entry.entryIds.map((entryId: number) => apiService.deleteSymptomeCycle(entryId)))
        toast({ title: 'Succès', description: 'Période d\'acné supprimée', variant: 'custom' })
      } else if (typeof id === 'number') {
        await apiService.deleteSymptomeCycle(id)
        toast({ title: 'Succès', description: 'Symptôme supprimé avec succès', variant: 'custom' })
      }

      await refresh()
      notifyChanged()
    } catch {
      toast({ title: 'Erreur', description: 'Erreur lors de la suppression', variant: 'destructive' })
    }
  }

  const editSymptome = (id: string | number) => {
    const found = processedAcneEntries.value.find((entry: any) => entry.id === id)
    if (!found || (found as any).isGroup) {
      return
    }

    options.onRequestEditEntry?.(found)
  }

  const extendAcnePeriod = async (period: any) => {
    const today = format(new Date(), 'yyyy-MM-dd')
    const lastEntry = period.entries[period.entries.length - 1]

    const data = new FormData()
    data.append('typeSymptome', 'Acné')
    data.append('carnetSanteId', options.carnetSanteId.toString())
    data.append('date', combineDateTime(today, '12:00').toISOString())
    data.append('intensite', Number(lastEntry.intensite).toString())
    data.append('commentaire', lastEntry.commentaire || '')

    try {
      await apiService.postDonneesSymptomesCycle(data)
      await refresh()
      notifyChanged()

      toast({ title: 'Succès', description: 'Jour ajouté à la période d\'acné', variant: 'custom' })
    } catch {
      toast({
        title: 'Erreur',
        description: 'Une erreur est survenue lors de l\'extension de la période',
        variant: 'destructive',
      })
    }
  }

  const openEndPeriodDialog = (period: any) => {
    selectedPeriodToEnd.value = period
    const [day, month, year] = period.endDate.split('/').map(Number)
    endPeriodDate.value = `${year}-${String(month).padStart(2, '0')}-${String(day).padStart(2, '0')}`
    showEndPeriodDialog.value = true
  }

  const confirmEndPeriod = async () => {
    if (!selectedPeriodToEnd.value || !endPeriodDate.value) return

    const period = selectedPeriodToEnd.value
    const endDate = new Date(endPeriodDate.value)

    const entriesToDelete = period.entries.filter((entry: any) => {
      const [day, month, year] = entry.date.split('/').map(Number)
      const entryDate = new Date(year, month - 1, day)
      return entryDate > endDate
    })

    if (entriesToDelete.length === 0) {
      showEndPeriodDialog.value = false
      toast({ title: 'Période terminée', description: 'Aucune modification nécessaire', variant: 'custom' })
      return
    }

    try {
      await Promise.all(entriesToDelete.map((entry: any) => apiService.deleteSymptomeCycle(entry.id)))
      await refresh()
      notifyChanged()

      showEndPeriodDialog.value = false
      toast({
        title: 'Succès',
        description: `Période terminée le ${format(endDate, 'dd/MM/yyyy')}`,
        variant: 'custom',
      })
    } catch {
      toast({ title: 'Erreur', description: 'Impossible de terminer la période', variant: 'destructive' })
    }
  }

  watch(acneMonthYear, async () => {
    await fetchMonthEntries(acneMonthYear.value, false)
    await hydrateAcneWindow()
    showFullAcneHistory.value = false
    isAcneHistoryExpanded.value = false
    showAcneHistoryWithPhotos.value = false
  })

  watch(acnePhotoEntries, (photos) => {
    const availableIds = new Set(photos.map((photo: any) => photo.id))
    selectedComparePhotoIds.value = selectedComparePhotoIds.value.filter((id) => availableIds.has(id))
  })

  watch(options.refreshKey, () => {
    refresh()
  })

  onMounted(async () => {
    await fetchMonthEntries(acneMonthYear.value, false)
    await hydrateAcneWindow()
    await refreshAcneMarkedToday()
  })

  const model = computed<AcneTabModel>(() => ({
    isQuickAddingAcne: isQuickAddingAcne.value,
    acneMarkedToday: acneMarkedToday.value,
    ongoingAcnePeriods: ongoingAcnePeriods.value,
    acneWindowMonths: acneWindowMonths.value,
    acnePhotoEntries: acnePhotoEntries.value,
    orderedComparePhotos: orderedComparePhotos.value,
    displayedAcneHistoryEntries: displayedAcneHistoryEntries.value,
    processedAcneEntriesCount: processedAcneEntries.value.length,
    acneHistoryEntriesCount: acneHistorySourceEntries.value.length,
    acneHistoryHiddenByPhotoCount: acneHistoryHiddenByPhotoCount.value,
    acneHistoryInitialCount: ACNE_HISTORY_INITIAL_COUNT,
    showFullAcneHistory: showFullAcneHistory.value,
    isAcneHistoryExpanded: isAcneHistoryExpanded.value,
    showAcneHistoryWithPhotos: showAcneHistoryWithPhotos.value,
    acneMonthYear: acneMonthYear.value,
    acneWindowLabel: acneWindowLabel.value,
  }))

  return {
    model,
    actions: {
      quickAddAcneToday,
      extendAcnePeriod,
      openEndPeriodDialog,
      openPhotoModal: (url: string) => options.onRequestPhotoModal?.(url),
      slideAcneWindow,
      selectLatestComparePair,
      togglePhotoCompare,
      resetPhotoCompare,
      editSymptome,
      deleteSymptome,
      toggleShowFullHistory,
      toggleAcneHistoryExpanded,
      toggleAcneHistoryWithPhotos,
      onAcneMonthYearChange,
      isPhotoSelectedForCompare,
    },
    endPeriodDialog: {
      showEndPeriodDialog,
      selectedPeriodToEnd,
      endPeriodDate,
      confirmEndPeriod,
    },
  }
}

