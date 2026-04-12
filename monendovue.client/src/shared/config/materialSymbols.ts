import type { CardIconConfig } from '@/shared/types/card'

export const materialSymbols = {
  cycle: 'female',
  symptoms: 'monitor_heart',
  treatments: 'medication',
  pastTreatments: 'history',
  fallback: 'help',
} as const

export const symptomeIconConfig: Record<string, CardIconConfig> = {
  'Acné': { color: 'text-purple-600', bg: 'bg-purple-100', icon: 'face' },
  'Spotting': { color: 'text-red-500', bg: 'bg-red-100', icon: 'water_drop' },
  'Nausée': { color: 'text-yellow-600', bg: 'bg-yellow-100', icon: 'sick' },
  'Fatigue': { color: 'text-blue-500', bg: 'bg-blue-100', icon: 'hotel' },
  'Autre': { color: 'text-gray-500', bg: 'bg-gray-100', icon: materialSymbols.fallback },
}

export const douleurIconConfig: Record<string, CardIconConfig> = {
  'Douleur pelvienne': { color: 'text-red-600', bg: 'bg-red-100', icon: 'person' },
  'Douleur abdominale': { color: 'text-orange-600', bg: 'bg-orange-100', icon: 'sick' },
  'Douleur lombaire': { color: 'text-amber-600', bg: 'bg-amber-100', icon: 'chair' },
  'Douleur thoracique': { color: 'text-blue-600', bg: 'bg-blue-100', icon: 'favorite' },
  'Douleur projetée': { color: 'text-purple-600', bg: 'bg-purple-100', icon: 'neurology' },
  'Douleur neuropathique': { color: 'text-indigo-600', bg: 'bg-indigo-100', icon: 'bolt' },
  'Dyspareunie': { color: 'text-pink-600', bg: 'bg-pink-100', icon: 'favorite' },
  'Autre': { color: 'text-gray-500', bg: 'bg-gray-100', icon: materialSymbols.fallback },
}

export const traitementPriseIconConfig: Record<string, CardIconConfig> = {
  'Antalgique': { color: 'text-blue-600', bg: 'bg-blue-100', icon: materialSymbols.treatments },
  'AINS': { color: 'text-indigo-600', bg: 'bg-indigo-100', icon: materialSymbols.treatments },
  'Autre': { color: 'text-gray-500', bg: 'bg-gray-100', icon: materialSymbols.treatments },
}

