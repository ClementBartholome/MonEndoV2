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
- Taille : au-delà d'environ 150-200 lignes pour un bloc UI métier, extraire un composant dédié. `CyclePage.vue`
  et `MedicamentPage.vue` sont trop gros : les découper quand on y travaille (skill `fonctionnalite-front`).
- Réutiliser l'existant avant de créer : `GenericCardList` (cartes mobiles, callbacks `onEdit`/`onDelete`/`onPhotoClick`),
  `SectionKpiHeader`, `EmptyStateAction`, `Datatable`, `SelectMonth`, composants `ui/`.

## Page de suivi refondue (1.3.0) : modèle à suivre
`features/douleurs/` est le modèle des pages du lot 3 : `utils/` (calculs purs : chiffres, graphique, regroupement par jour),
composable de page (`useDouleurs` : chargement, mois, enregistrement), composable de formulaire (`useSaisieDouleur`),
composants de présentation (`ChiffresDuMois`, `GraphiqueDouleursMois`, `ListeDouleurs`) et saisie dans `PanneauBas` avec des
**actions en props** (`ActionsSaisieDouleur`) plutôt que des événements. Choix en boutons (`aria-pressed`), dates envoyées en
AAAA-MM-JJTHH:mm:ss, lien profond `?ajouter`, bouton flottant au-dessus de la barre (mobile) et bouton d'en-tête (desktop),
plus de DataTables.

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

## SOLID côté client
- **Responsabilité unique** :
  - la page orchestre ;
  - le composable porte la logique et les appels API ;
  - le composant de présentation ne fait que du rendu ;
  - un service ne fait que de l'I/O.
  
  Un fichier qui mélange ces rôles se découpe.
- **Ouvert/fermé** : les variations par type passent par de la configuration typée (`shared/config/materialSymbols.ts`,
  `features/bilan-quotidien/config/transit.ts`, `extraFields` de `GenericCardList`), pas par des chaînes de `v-if` / `switch`.
- **Substitution** : un composant partagé se comporte de la même façon quel que soit le parent. Pas de prop ajoutée
  « pour un seul écran » qui change son contrat ; préférer un slot ou un nouveau composant.
- **Interfaces ciblées** : props minimales et typées. Un composant de présentation reçoit un `model` et des `actions`
  dédiés, jamais l'objet métier complet « au cas où ».
- **Inversion des dépendances** :
  - un composant ne dépend jamais d'axios ou de `fetch` : il passe par un composable, qui passe par `apiService` ;
  - un composable reçoit ses collaborateurs (callbacks, identifiants) via ses `options`, pour rester testable.
- **Dette connue** (lot C de la roadmap) :
  - `authService` / `tokenService` appellent axios directement ;
  - `apiService` renvoie des `Promise<any>` ;
  - `CyclePage` et `MedicamentPage` mélangent orchestration, logique et rendu.

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
- **Tokens de design** (`src/assets/tokens.css`, palette validée en 1.3.0, contrastes WCAG AA vérifiés) exposés à Tailwind :
  `bg-fond`, `bg-surface`, `bg-surface-2`, `border-trait`, `text-texte` / `-2` / `-3`, `text-lien`, `border-contour` (champs,
  3:1), `text-danger`, `rounded-petit` / `moyen` / `grand`, `shadow-elevation` (la seule ombre). **Aucune couleur en dur ni
  couleur Tailwind brute** (`blue-100`, `gray-300`…) dans le code nouveau ou modifié. Les anciens noms (`--headline`,
  `--button`, `.text-headline`, `Button variant="custom"`) et les variables shadcn pointent vers ces tokens.
  - Une **rubrique = une teinte**, partout (tuile + icône) : `bg-teinte-<r>-fond` + `text-teinte-<r>` avec <r> = `bilan`,
    `douleur`, `regles`, `symptome`, `traitement`, `neutre`. Les types d'une rubrique se distinguent par icône et libellé.
  - Le rose poudré `--couleur-accent` (`bg-button`, `#f2b3c2`) est un **fond** avec texte foncé (8.2:1) ; jamais de texte blanc
    dessus ni de texte rose. L'ancien `#ff8ba7`, trop saturé, tranchait avec le reste.
  - Icônes : `polices.css` force `line-height: 1` (les classes `text-lg`… décalaient le glyphe) ; ne pas le surcharger.
  - Pas de bouton « Revenir en arrière » dans les pages : la navigation est toujours visible. L'en-tête (logo, compte) est
    posé sur la page en `pointer-events-none` : la première carte peut passer dessous sans perdre ses clics.
  - Intensité 0-10 : `var(--intensite-N)` (texte foncé jusqu'à 5, blanc à partir de 6).
  - Les pages pas encore refaites (Cycle, Traitements, accueil…) gardent des couleurs brutes : les migrer quand on les touche.
- Polices servies par l'app (`src/assets/polices.css`, paquets `@fontsource`), **jamais Google Fonts** (IP envoyée à Google, RGPD) :
  Poppins 400/400 italique/500/600/700 en latin ; une graisse ou un axe d'icône en plus s'ajoute dans ce fichier.
- Icônes Material Symbols (`<i class="material-symbols-outlined">nom</i>`, axe FILL seul, graisse 400) ; les correspondances type → icône vont dans
  `shared/config/materialSymbols.ts`.
- Textes 100 % en français, dates via `useDateTimeFormat` (`fr-FR`).
- **Jour calendaire envoyé à l'API** : jamais un `Date` à minuit (sérialisé en UTC, il glisse au jour précédent et le
  serveur le stocke sans fuseau) ; envoyer `format(jour, "yyyy-MM-dd'T'12:00:00")` (modèle : `useSaisieBilan`).
- **Jour calendaire reçu de l'API** : le comparer par sa clé `yyyy-MM-dd` en heure locale (`cleJour` de
  `features/bilan-quotidien/utils/historique.ts`), jamais par timestamp.
- Couleurs de données : une seule teinte par indicateur, sans code « bon / mauvais », et **foncé = journée plus lourde**
  pour tous les indicateurs (douleur forte, émotions difficiles). Séries d'un même graphique de teintes et de clartés
  bien distinctes (fatigue verte, stress orange, émotions bleu foncé).
- Vocabulaire : parler d'**émotions agréables ou difficiles**, pas d'« humeur agréable / difficile » (« humeur difficile »
  signifie irritable). « Humeur » ne désigne que l'ancienne saisie (Positive, Neutre, Négative) des anciens bilans.
- Courbes à plusieurs échelles ou avec des bandes de fond (règles) : `GraphiqueLignes.vue` (SVG simple, jours non
  renseignés non tracés) plutôt que `LineChart` (unovis), qui n'a qu'un axe et relie les trous.
- Élément collé en bas d'écran (`sticky`/`fixed`) : sous 1024px la navigation est fixée en bas (`BarreNavigation.vue`,
  4.25rem), le décaler d'autant (modèle : barre d'enregistrement de `SaisieBilan.vue`).
- La taille de police racine grandit en desktop (20px à 1280px) : les largeurs en `rem` (`max-w-3xl`…) y sont plus
  larges que prévu, vérifier en situation.
- Pastilles, préréglages et chips : grilles à colonnes égales (`grid-cols-n`) plutôt que `flex-wrap`, pour des rangées
  alignées à 375px ; libellés courts, `whitespace-nowrap` si besoin.
- Accessibilité : labels associés aux champs, navigation clavier, cibles tactiles d'au moins 44px, contraste suffisant.
- Jamais de `v-html` ; pour `Datatable`, ne pas rendre de texte saisi comme HTML.

## Navigation
- Une seule liste des rubriques : `shared/config/navigation.ts` (principales = barre du bas mobile, secondaires = menu
  « Plus », compte). Ajouter une page = une entrée ici, rien à toucher dans les composants.
- Sous 1024px : `BarreNavigation.vue` (Accueil · Bilan · Douleurs · Cycle · Plus) et `MenuPlus.vue` ; au-delà :
  `NavigationLaterale.vue` (toutes les entrées). Orchestration dans `shared/components/Layout.vue`.
- Panneau qui monte du bas (menus, saisies rapides) : `shared/components/PanneauBas.vue` (Dialog radix accessible).

## Routage
- Toute route exige une session, sauf celles marquées `meta: { public: true }` (connexion, inscription, documents légaux) :
  le garde de `router/index.ts` ne teste que ce drapeau.
- Session sans consentement (`user.consentementAJour === false`, ou 403 `consentement-requis` de l'API traité dans
  `apiService`) : seule `/consentement` est accessible (avec les pages publiques). `meta: { sansNavigation: true }` masque
  la navigation.
- Documents légaux (`features/legal/`) : gabarit `DocumentLegal.vue`, éditeur et date de mise à jour dans
  `config/editeur.ts`, liens de pied `LiensLegaux.vue`. Texte en tutoiement, comme le reste de l'app.

## État et stockage
- Pinia uniquement pour l'authentification (`features/auth/store/auth.ts`) ; le reste en état local ou composable.
- `localStorage` réservé aux préférences non sensibles (`user` sans token, `monendo.wellbeing-goals`).
  **Jamais** de token ni de donnée de santé dans `localStorage`/IndexedDB.
- Pas de cache hors ligne ni de service worker applicatif (voir les décisions d'architecture du CLAUDE.md racine).
- Le bloc daté de `src/main.ts` nettoie les restes des anciens systèmes (mode hors ligne, OneSignal) : à retirer après le 2026-12-31,
  en même temps que `public/sw.js`.

## Notifications Web Push
- Tout passe par `features/parametres/composables/usePushNotifications.ts` (états, activation, réglage des rappels) et
  `components/NotificationSettings.vue` ; le worker `public/push-sw.js` ne fait qu'afficher les notifications (aucun cache).
- Rappels : une carte `ReglageRappelCard.vue` par type renvoyé par `GET Notifications/rappels` ; libellés dans
  `config/rappels.ts` (un type sans entrée n'est pas affiché). Jours : 0 = dimanche, comme `DayOfWeek` côté serveur.
- Une notification ouvre `data.url` : une page à onglets doit accepter un lien profond (`/cycle?onglet=acne|symptomes|cycles`,
  lu dans `CyclePage.vue` au montage).
- La clé publique VAPID vient de l'API (`GET Notifications/cle-publique`), jamais d'une variable `VITE_*`.
- **iOS** : push disponible seulement dans l'app ouverte depuis l'écran d'accueil (iOS 16.4+), sinon afficher le guide
  d'installation. `Notification.requestPermission()` doit être le **premier `await`** d'un gestionnaire de clic (geste utilisateur),
  donc toute donnée nécessaire (clé publique) est chargée avant.
- Un ancien abonnement signé avec une autre clé (ex. OneSignal) bloque `pushManager.subscribe` : le désabonner d'abord.
- Le navigateur intégré de l'app Claude bloque les notifications : tester la réception dans Chrome sur le poste.

## Vérifications
- `npm run type-check` après chaque changement significatif, `npm run build` avant de commiter.
- `npx eslint <fichiers modifiés>` (le script `npm run lint` corrige tout le client et mélangerait les commits).
- SonarCloud analyse aussi le client (TS, Vue, **CSS**) : la note de fiabilité du nouveau code doit rester A, sinon le
  check de la PR échoue. Piège déjà rencontré : `font-family` sans famille générique (S4649), y compris pour une police
  d'icônes. Constats d'une PR : `https://sonarcloud.io/api/issues/search?componentKeys=ClementBartholome_MonEndoV2&pullRequest=<n>&resolved=false`.
- Test visuel dans le navigateur à 375px et en desktop ; console sans erreur. Pas de `console.log` laissé dans le code.
- **Tests E2E** (`npm run test:e2e`) : Playwright pilote l'interface comme une utilisatrice, avec une **API simulée**
  (pas de Vitest ni de tests unitaires client : le serveur est couvert par xUnit). Vue d'ensemble : `docs/tests.md`.
  Organisation (bonnes pratiques Playwright : Page Object Model, fixtures, localisateurs accessibles) :
  ```
  tests/
    fixtures.ts                  test/expect du projet : session, horloge, faux serveur, objets de page
    mocks/faux-serveur.ts        interception des appels API (page.route), état propre au test
    mocks/session.ts             MAINTENANT (mardi 15/09/2026 10 h), utilisatrice connectée
    mocks/<fonctionnalite>.ts    routes simulées d'un contrôleur (simulerBilans, simulerActivite)
    pages/<page>.page.ts         objet de page : localisateurs et actions d'un écran
    <fonctionnalite>.spec.ts     ou <fonctionnalite>/<parcours>.spec.ts quand la page a plusieurs parcours
  ```
  - Importer `test`/`expect` de `tests/fixtures.ts` ; les objets de page sont des fixtures (`{ bilanQuotidienPage: bilanPage }`).
    `test.use({ connectee: false })` pour un parcours sans session.
  - **Localisateurs** : rôle et nom accessible (`getByRole('button', { name: 'Douleur 4 sur 10' })`), libellé, texte ;
    CSS seulement en dernier recours, commenté. Un élément difficile à cibler signale souvent un manque d'accessibilité.
  - Les localisateurs et actions vivent dans l'objet de page, les assertions dans le test. Assertions qui attendent
    (`await expect(locator).toBeVisible()`), jamais de `waitForTimeout`.
  - Données : `mocks/<fonctionnalite>.ts` avec un état propre au test (ajout, modification, suppression), typées avec
    les types de `features/*/types` ; le test ne précise que les champs qui comptent (`{ jour: '2026-09-14', pas: 7500 }`).
  - **Reproduire le format réel de l'endpoint** : un tableau C# (`ToArrayAsync`) arrive en tableau JSON, une `List` en
    `{ $values }` (`liste()`). Tout appel API sans route simulée fait échouer le test (appel oublié ou contrat modifié).
  - Chaque test tourne en **mobile 375px et en desktop** : quand une page a deux présentations (cartes / tableau), l'objet
    de page choisit la bonne (`ActivitePage.action`) et cible le texte visible.
  - Pas de propriétés de paramètre (`constructor(readonly page: Page)`) ni d'`enum` dans `tests/` : le chargeur TypeScript
    de Playwright retire seulement les types et refuse cette syntaxe.
