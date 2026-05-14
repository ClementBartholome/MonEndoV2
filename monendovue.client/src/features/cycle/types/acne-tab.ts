export interface AcneWindowMonth {
  key: string
  label: string
  entries: any[]
}

export interface AcneTabModel {
  isQuickAddingAcne: boolean
  acneMarkedToday: boolean
  ongoingAcnePeriods: any[]
  acneWindowMonths: AcneWindowMonth[]
  acnePhotoEntries: any[]
  orderedComparePhotos: any[]
  displayedAcneHistoryEntries: any[]
  processedAcneEntriesCount: number
  acneHistoryEntriesCount: number
  acneHistoryHiddenByPhotoCount: number
  acneHistoryInitialCount: number
  showFullAcneHistory: boolean
  isAcneHistoryExpanded: boolean
  showAcneHistoryWithPhotos: boolean
  acneMonthYear: string
  acneWindowLabel: string
}

export interface AcneTabActions {
  openAddAcneDialog: () => void
  quickAddAcneToday: () => Promise<void>
  extendAcnePeriod: (period: any) => Promise<void>
  openEndPeriodDialog: (period: any) => void
  openPhotoModal: (url: string) => void
  slideAcneWindow: (direction: -1 | 1) => void
  selectLatestComparePair: () => void
  togglePhotoCompare: (id: number) => void
  resetPhotoCompare: () => void
  editSymptome: (id: string | number) => void
  deleteSymptome: (id: string | number) => Promise<void>
  toggleShowFullHistory: () => void
  toggleAcneHistoryExpanded: () => void
  toggleAcneHistoryWithPhotos: () => void
  onAcneMonthYearChange: (value: string) => void
  isPhotoSelectedForCompare: (id: number) => boolean
}

