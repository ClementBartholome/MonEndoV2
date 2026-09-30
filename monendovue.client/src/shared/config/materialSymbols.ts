import type { CardIconConfig } from '@/shared/types/card'

export const materialSymbols = {
  cycle: 'menstrual_health',
  symptoms: 'monitor_heart',
  treatments: 'pill',
  pastTreatments: 'history',
  fallback: 'help',
} as const

// Une rubrique = une teinte (tokens teinte-*) : les types se distinguent par l'icône et le libellé.
export const symptomeIconConfig: Record<string, CardIconConfig> = {
  'Acné': { color: 'text-teinte-symptome', bg: 'bg-teinte-symptome-fond', icon: 'face' },
  'Spotting': { color: 'text-teinte-symptome', bg: 'bg-teinte-symptome-fond', icon: 'water_drop' },
  'Nausée': { color: 'text-teinte-symptome', bg: 'bg-teinte-symptome-fond', icon: 'sick' },
  'Fatigue': { color: 'text-teinte-symptome', bg: 'bg-teinte-symptome-fond', icon: 'hotel' },
  'Autre': { color: 'text-teinte-neutre', bg: 'bg-teinte-neutre-fond', icon: materialSymbols.fallback },
}

export const traitementPriseIconConfig: Record<string, CardIconConfig> = {
  'Antalgique': { color: 'text-teinte-traitement', bg: 'bg-teinte-traitement-fond', icon: materialSymbols.treatments },
  'AINS': { color: 'text-teinte-traitement', bg: 'bg-teinte-traitement-fond', icon: materialSymbols.treatments },
  'Autre': { color: 'text-teinte-neutre', bg: 'bg-teinte-neutre-fond', icon: materialSymbols.treatments },
}

