import { type ClassValue, clsx } from 'clsx'
import { extendTailwindMerge } from 'tailwind-merge'

// Les tailles de texte nommées (tailwind.config.js) doivent être connues de tailwind-merge : sinon `text-corps` est pris
// pour une couleur et disparaît à côté de `text-texte`.
const twMerge = extendTailwindMerge({
  extend: {
    classGroups: {
      'font-size': [{ text: ['legende', 'corps', 'titre-carte', 'titre-2', 'titre-page'] }],
    },
  },
})

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs))
}
