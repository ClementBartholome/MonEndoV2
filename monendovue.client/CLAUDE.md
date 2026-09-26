# Front MonEndo (Vue 3 + TypeScript) — conventions

Complète le [CLAUDE.md racine](../CLAUDE.md). S'applique à tout le code de `monendovue.client/`.

## Arborescence
```
src/
  features/<domaine>/        # un dossier par domaine, nommé en kebab-case français
    pages/                   # composants routés (XxxPage.vue) : orchestration uniquement
    components/              # composants propres au domaine (conteneurs + présentation)
    composables/             # logique métier réutilisable (useXxx.ts)
    types/                   # contrats TS (kebab-case : donnees-douleur.ts), miroir des DTO C#
    services/ store/         # seulement si propre au domaine (ex. auth)
  shared/
    components/              # composants transverses (GenericCardList, SectionKpiHeader, EmptyStateAction, Datatable…)
    components/ui/           # composants shadcn-vue générés : les modifier le moins possible
    composables/             # useMonthData, useCrudOperations, useDialogForm, useDateTimeFormat
    services/apiService.ts   # unique point d'accès à l'API
    config/materialSymbols.ts# icônes et configurations d'icônes par type
    types/card.ts            # contrats des cartes génériques
  lib/utils.ts               # cn() utilisé par shadcn : ne pas déplacer
```
Nouveau composant shadcn : `npx shadcn-vue add <nom>` (alias configurés vers `@/shared/components`).

## Composants
- `<script setup lang="ts">` uniquement (pas d'Options API), bloc `script` avant `template` dans les nouveaux fichiers.
- Props typées : `defineProps<{ … }>()` (+ `withDefaults` si besoin) ; emits : `defineEmits<{ 'edit-entry': [id: number] }>()`, événements en kebab-case.
- `import type` obligatoire pour les imports de types (`verbatimModuleSyntax`).
- Taille : au-delà d'environ 150-200 lignes pour un bloc UI métier, extraire un composant dédié. `CyclePage.vue`,
  `MedicamentPage.vue` et `BilanQuotidienPage.vue` sont trop gros : les découper quand on y travaille (skill `fonctionnalite-front`).
- Réutiliser l'existant avant de créer : `GenericCardList` (cartes mobiles, callbacks `onEdit`/`onDelete`/`onPhotoClick`),
  `SectionKpiHeader`, `EmptyStateAction`, `Datatable`, `SelectMonth`, composants `ui/`.

## Deux patterns de référence
1. **Page CRUD par mois** — modèle : `features/douleurs/pages/DouleursPage.vue`.
   `useMonthData({ fetchFunction, transformData })` + `useCrudOperations(entries)` + `useDialogForm`, formulaire
   vee-validate avec `toTypedSchema(z.object(...))` et messages d'erreur en français, `GenericCardList` en mobile
   (`md:hidden`) et `Datatable` en desktop (`hidden md:block`), `SectionKpiHeader`, `EmptyStateAction`, `Skeleton` pendant le chargement.
2. **Fonctionnalité riche** — modèle : la chaîne acné de `features/cycle/`.
   - `types/acne-tab.ts` : contrat `XxxModel` (état en lecture seule) + `XxxActions` (signatures des actions).
   - `composables/useAcneTracking.ts` : `useXxx(options)` qui gère état, appels API, toasts et renvoie
     `{ model: computed<XxxModel>, actions: XxxActions }` ; options par callbacks (`onChanged`, `onRequest…`).
   - `components/AcneTabSection.vue` : conteneur qui instancie le composable et relie callbacks ↔ emits.
   - `components/AcneTabContent.vue` : présentation pure, deux props (`model`, `actions`), aucun état ni appel API.

## Typage
- **Aucun nouveau `any`** : typer avec les interfaces de `features/*/types`, sinon `unknown` + rétrécissement.
  L'existant en contient beaucoup (`apiService`, `useAcneTracking`) : les réduire quand on y touche.
- Les types de `features/*/types` sont écrits à la main et doivent rester alignés sur les DTO/ViewModels C#.
- `interface` pour les objets, `type` pour les unions et intersections.

## Appels API
- Toujours via `shared/services/apiService.ts` (instance axios `withCredentials`, rafraîchissement du token, redirection login).
  Pas d'`axios`/`fetch` direct dans un composant.
- Nouvelle méthode : typée (`Promise<T>`, payload typé), nommée `get|post|put|delete` + entité (`getDonneesDouleursByMonth`).
- Les listes renvoyées par l'API sont sous `$values` (sérialisation .NET `ReferenceHandler.Preserve`).
- Retours utilisateur par `useToast()` : variante `custom` (succès/erreur) ou `destructive` ; messages en français.
- Aucun secret dans `import.meta.env` : toute variable `VITE_*` est publique.

## Style et UX
- **Mobile d'abord** : styles de base = mobile, `md:` = desktop ; ajustements ≤ 425px via `@media (max-width: 425px)`
  dans le `<style scoped>` ou la classe `.hide-xsm`. Tester à 375px.
- Tokens de marque (`src/assets/index.css`) : `var(--button)`, `var(--headline)`, classes `.bg-clearer`, `.text-headline`,
  `.text-paragraph`, `Button variant="custom"` / `"selected"`. Pas de couleurs en dur.
- Icônes Material Symbols (`<i class="material-symbols-outlined">nom</i>`) ; les correspondances type → icône vont dans
  `shared/config/materialSymbols.ts`.
- Textes 100 % en français, dates via `useDateTimeFormat` (`fr-FR`).
- Accessibilité : labels associés aux champs, navigation clavier, cibles tactiles d'au moins 44px, contraste suffisant.
- Jamais de `v-html` ; pour `Datatable`, ne pas rendre de texte saisi comme HTML.

## État et stockage
- Pinia uniquement pour l'authentification (`features/auth/store/auth.ts`) ; le reste en état local ou composable.
- `localStorage` réservé aux préférences non sensibles (`user` sans token, `notification-permission`,
  `monendo.wellbeing-goals`). **Jamais** de token ni de donnée de santé dans `localStorage`/IndexedDB.
- Pas de cache hors ligne ni de service worker applicatif (voir les décisions d'architecture du CLAUDE.md racine).

## Vérifications
- `npm run type-check` après chaque changement significatif, `npm run build` avant de commiter.
- `npx eslint <fichiers modifiés>` (le script `npm run lint` corrige tout le client et mélangerait les commits).
- Test visuel dans le navigateur à 375px et en desktop ; console sans erreur. Pas de `console.log` laissé dans le code.
- E2E Playwright (`npm run test:e2e`) : uniquement contre un environnement local, avec `E2E_EMAIL`/`E2E_PASSWORD`.
