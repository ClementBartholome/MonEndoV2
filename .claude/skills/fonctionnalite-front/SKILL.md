---
name: fonctionnalite-front
description: Créer ou refactoriser une page, un composant ou un composable Vue de MonEndo selon les patterns du projet (page CRUD mensuelle, pattern model/actions, mobile-first ≤ 425px, zod, composants partagés). À utiliser pour toute nouvelle fonctionnalité front, tout ajout d'écran ou de formulaire, ou pour découper une page trop grosse (CyclePage, MedicamentPage ; modèle de découpage : BilanQuotidienPage).
---

# Fonctionnalité front MonEndo

Lire d'abord `monendovue.client/CLAUDE.md` (conventions) et le CLAUDE.md racine (principes produit, sécurité).

## 1. Cadrer
- Identifier le domaine (`src/features/<domaine>/`) et l'écran concerné. Nouveau domaine : créer `pages/`, `components/`, `types/` (et `composables/` si besoin).
- Lister les données : existent-elles côté API ? Sinon, faire d'abord l'endpoint (skill `endpoint-api`).
- Vérifier ce qui existe déjà dans `src/shared/` (composants, composables, `materialSymbols.ts`) avant d'écrire quoi que ce soit.

## 2. Choisir le pattern
| Situation | Pattern | Modèle à lire |
|---|---|---|
| Liste d'entrées par mois + ajout/édition/suppression | Page CRUD (`useMonthData` + `useCrudOperations` + `useDialogForm`) | `features/douleurs/pages/DouleursPage.vue` |
| Logique riche, plusieurs vues, état dérivé important | Composable `model`/`actions` + conteneur + présentation | `features/cycle/composables/useAcneTracking.ts`, `components/AcneTabSection.vue`, `components/AcneTabContent.vue`, `types/acne-tab.ts` |
| Petit bloc d'UI réutilisable | Composant de présentation avec props typées | `shared/components/EmptyStateAction.vue` |

Gabarits prêts à adapter : [templates.md](templates.md).

## 3. Implémenter
1. **Types** dans `features/<domaine>/types/` : miroir exact du DTO/ViewModel C#, aucun `any`.
2. **API** : ajouter une méthode typée dans `shared/services/apiService.ts` (`Promise<T>`, gérer `$values` pour les listes).
3. **Logique** dans le composable ; la page ne fait qu'orchestrer ; les composants de présentation n'appellent jamais l'API.
   **SOLID** (voir `monendovue.client/CLAUDE.md`) :
   - une responsabilité par fichier ;
   - les variations par type en configuration, pas en `v-if` en chaîne ;
   - des props minimales (`model` / `actions`) ;
   - dépendances reçues via les `options` du composable ;
   - jamais d'axios dans un composant.
4. **Formulaire** : vee-validate + `toTypedSchema(z.object(...))`, messages en français, bouton de validation désactivé si incomplet, presets pour la saisie rapide.
5. **Affichage** : une seule liste pour mobile et desktop (modèle : `features/activite/`), `SelecteurMois` pour le mois, `EmptyStateAction` pour l'état vide, `Skeleton` pendant le chargement.
6. **Retours** : `useToast()` variante `custom` (ou `destructive` en erreur), textes en français, ton non anxiogène.
7. **Route** : déclarer la page dans `src/router/index.ts` si elle est nouvelle.

## 4. Découper une page géante
1. Repérer les blocs cohérents (un onglet, un dialog, une section) et leur état.
2. Extraire d'abord la **logique** dans un composable `useXxx` (fonctions pures au niveau module, état + actions dans la fonction).
3. Définir le contrat `XxxModel`/`XxxActions` dans `types/`.
4. Créer le conteneur (instancie le composable, relie les callbacks aux emits) puis le composant de présentation (props `model` + `actions`).
5. Remplacer le bloc dans la page, lancer `npm run type-check`, vérifier le comportement à l'écran.
6. Un commit par extraction (`refactor(<domaine>): extrait …`).

## 5. Vérifier (obligatoire)
- `npm run type-check` puis `npm run build` dans `monendovue.client/`.
- `npx eslint <fichiers modifiés>`.
- Test navigateur à **375px** et en desktop : ajout, édition, suppression, état vide, changement de mois, erreurs API ; console propre.
- Aucun `console.log`, `any` ou `v-html` ajouté ; aucune donnée sensible dans `localStorage`.
