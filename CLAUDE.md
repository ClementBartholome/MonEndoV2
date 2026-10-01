# MonEndo — instructions pour Claude

Référence unique des règles de travail sur ce dépôt. Les conventions détaillées par couche sont dans
[MonEndoVue.Server/CLAUDE.md](MonEndoVue.Server/CLAUDE.md) (API .NET) et
[monendovue.client/CLAUDE.md](monendovue.client/CLAUDE.md) (front Vue), chargés automatiquement quand on y travaille.
La feuille de route produit est dans [docs/modernization-plan.md](docs/modernization-plan.md).

## Le projet
MonEndo aide les personnes atteintes d'endométriose à suivre leur quotidien (douleurs, cycle, symptômes, traitements,
transit, activité, bilan quotidien) et à préparer leurs rendez-vous médicaux (export PDF). Production : https://monendoapp.fr.

- **Données de santé = données sensibles (RGPD art. 9)** : minimisation, cloisonnement strict par utilisatrice, aucune fuite dans les logs.
- **Le dépôt GitHub est public.** Jamais de secret, de donnée personnelle, d'identifiant (même de test) ni de description
  d'une faille non corrigée dans un fichier versionné, un message de commit ou une PR. Les constats de sécurité exploitables
  vont dans `docs/private/` (ignoré par git).
- Développeur unique, francophone : échanges, docs, commits et UI en **français**.

## Stack et carte du dépôt
| Zone | Techno |
|---|---|
| `MonEndoVue.Server/` | ASP.NET Core 8, EF Core + SQL Server, Identity + JWT en cookies HttpOnly, Serilog, Quartz, Web Push (VAPID), Azure Blob (photos) |
| `monendovue.client/` | Vue 3.4 + TypeScript, Vite 5, Pinia, shadcn-vue (radix-vue), Tailwind 3, vee-validate + zod, Playwright |
| `.github/workflows/ci.yml` | CI/CD : vérification → image Docker → déploiement VPS |
| `Dockerfile` | Image unique : le client est buildé par `dotnet publish` (esproj) et servi depuis `wwwroot` |
| `deploy/` | Référence versionnée de la config du VPS (compose de prod, logrotate) et procédure d'application |
| `docs/` | Roadmap (`modernization-plan.md`), vue d'ensemble des tests (`tests.md`) ; `docs/private/` local et non versionné |

## Commandes
```bash
# Client (depuis monendovue.client/)
npm run dev          # serveur Vite https://localhost:5173
npm run type-check   # vue-tsc (code) + tsc (tests E2E), à lancer après chaque changement significatif
npm run test:e2e     # parcours de l'interface avec une API simulée (Playwright, mobile 375px + desktop)
npm run build        # type-check + build (même commande que la CI et le Dockerfile)
npx eslint <fichiers>  # préférer au `npm run lint`, qui fait --fix sur tout le client

# Serveur (depuis la racine)
dotnet build -c Release
dotnet test          # tests xUnit de MonEndoVue.Server.Tests (aussi exécutés par la CI)
dotnet ef migrations add NomEnPascalCase --project MonEndoVue.Server
```
- Lancement complet en dev : profil `https` de `MonEndoVue.Server` (démarre Vite via SpaProxy). API : https://localhost:7206.
- Configuration locale : `MonEndoVue.Server/appsettings.Development.json` et `monendovue.client/.env` (non versionnés, **ne pas les lire ni les afficher**).
- **Tests** : vue d'ensemble (types, emplacements, CI, bloquant ou non, limites) dans [docs/tests.md](docs/tests.md).
- Tests backend : projet `MonEndoVue.Server.Tests` (xUnit). Tests front : parcours E2E Playwright avec une **API simulée**
  (`monendovue.client/tests/`, ni serveur ni compte), lancés en CI sur les PR vers `main` ou avec l'étiquette `e2e` (conventions : `monendovue.client/CLAUDE.md`).

## Domaine fonctionnel
- **`CarnetSante`** : un carnet par utilisatrice (1-1 avec `ApplicationUser`), racine de toutes les données.
  Chaque entité porte un `CarnetSanteId` : c'est la clé du cloisonnement.
- **`DonneesDouleur`** (type, intensité 0-10, date, commentaire) — page `/douleurs`.
- **`SymptomeCycle`** (type, intensité, date, commentaire, photo optionnelle) dont l'**acné** avec suivi photo ;
  **`JourRegle`** (jours de règles) — page `/cycle`, onglets Règles (`?onglet=cycles`), Symptômes et Acné.
  Règles via `CycleController` (carnet de la session) : `GET Cycle?jour=&mois=` (jours du mois, cycle en cours,
  6 derniers cycles et moyenne dès deux cycles) et `PUT`/`DELETE Cycle/regles/{jour}` (un jour à la fois, jamais à venir).
  Règles = jours notés consécutifs, un oubli d'un jour toléré ; un écart de plus de 60 jours n'est ni listé ni compté
  (`HistoriqueCycles`, même seuils que `CycleDuJour`). **Aucune prédiction** de prochaines règles.
  Acné : **`EpisodeAcne`** (début, fin vide = en cours ; un seul en cours, sans chevauchement) via `AcneController`
  (`GET Acne?jour=`, `POST/PUT/DELETE Acne/episodes`, `POST Acne/episodes/{id}/fin`) : rien à noter chaque jour
  tant qu'il dure. Le point de suivi hebdomadaire (photo, intensité) reste un `SymptomeCycle` « Acné » avec photo.
  Les anciens jours d'acné (une entrée par jour) ont été regroupés en épisodes par la migration `AjouteEpisodesAcne`
  et sont conservés tels quels.
- **`Medicament`** (traitement, `TypeTraitement` médicamenteux ou non, en cours ou passé, avec une **fréquence** à la
  manière de l'app Santé d'Apple : `AuBesoin`, `ChaqueJour`, `CertainsJours` + jours de la semaine, `TousLesNJours`
  depuis la date de début, et jusqu'à 6 **`HorairePrise`**), **`DonneesMedicament`** (réponses aux prises : `Statut`
  `Pris` ou `Ignore`, `HeurePrevue` pour une prise planifiée, absente pour une prise au besoin),
  **`DonneesTraitementNonMedicamenteux`** (séances) — page `/medicaments` (`TraitementsController`, `GET Traitements/jour`).
  Le planning d'un jour est calculé par `PlanningTraitement` ; une prise **ignorée** est stockée dans la même table :
  toute lecture qui compte des prises (PDF, historique, accueil) filtre `Statut == Pris`.
  Page `/medicaments/:id` : historique d'un traitement un mois à la fois (`GET Traitements/{id}/historique?mois=&jour=`,
  `HistoriqueTraitementsService`) ; les prises prévues d'un traitement arrêté se comptent jusqu'à sa fin
  (`PlanningTraitement.EstPrevuDansSesDates`), avec la fréquence et les horaires actuels (leurs versions passées ne sont
  pas gardées). Une prise ou une séance se retire depuis cette page (`DELETE Traitements/prises/{id}`, `…/seances/{id}`).
- **`DonneesTransit`** — `/transit` (ancien suivi par événements ; le suivi quotidien passe désormais par le bilan) .
- **`DonneesActivitePhysique`** — `/activite` via `ActiviteController` (carnet de la session) : intensité ressentie sur 3 niveaux
  (`NiveauIntensite` : douce, modérée, soutenue ; l'ancienne `Intensite` 1-10 reste écrite, 2 / 5 / 8, pour un retour
  arrière et le PDF) et effet sur la douleur (`EffetDouleur` : 0 non renseigné, soulagée, inchangée, plus forte).
- **`BilanQuotidien`** (émotions, stress, fatigue, pas, douleur moyenne, hydratation, alimentation, notes, et une catégorie
  **transit** facultative : selles avec type de Bristol 1-7, crampes d'estomac et ballonnements avec intensité) — `/bilan-quotidien`,
  avec des repères personnels réglables dans `/parametres` (stockés en `localStorage`). L'onglet « Tendances »
  ne donne ni score ni note : moyennes, évolution vs période précédente, repères atteints, observations factuelles
  douleur / règles (`utils/tendances.ts`). Historique par semaine ou par mois (`GET BilanQuotidien/periode`,
  bilans + jours de règles, carnet déduit de la session) : une seule période pilote calendrier, détail, courbes et analyse. Émotions : 1 à 3 **`EmotionBilan`** par bilan (table
  `EmotionsBilan`, type possédé chargé avec le bilan ; enum `Emotion`, libellés et tonalité dans `config/emotions.ts`).
  La page s'ouvre sur l'historique ; la saisie du jour ne s'ouvre d'office qu'avec `?ajouter` (accueil, rappel).
  Saisie en un écran (`components/saisie/`) : douleur et émotions obligatoires, stress, fatigue, pas et hydratation
  **nullables** (null = non renseigné, jamais compté pour 0) ; **un seul bilan par jour** et aucun jour futur (409 / 400).
  Les bilans antérieurs gardent leur ancienne humeur `Mood` (`Heureuse`/`Neutre`/`Triste`) : tout calcul d'humeur passe
  par `features/bilan-quotidien/utils/humeur.ts`, qui prend en compte les deux.
- Accueil `/` « Aujourd'hui » (`GET Accueil/aujourdhui?jour=AAAA-MM-JJ`, jour local envoyé par le client, carnet de la
  session : cycle déduit des jours de règles sans prédiction, bilan du jour, prises prévues ce jour-là (heure et réponse) et
  traitements au besoin, faits descriptifs des 7 derniers jours ; prise notée en un geste par `POST Traitements/{id}/prises`), agenda `/agenda` (Google Calendar en lecture via le serveur : l'utilisatrice lie son compte Google depuis `/parametres` (OAuth, `Agenda/liaison`), puis choisit **le calendrier lu**, le seul (idéalement un calendrier de rendez-vous médicaux) ; sans liaison, repli temporaire sur l'entrée de configuration `Agenda` ; sans calendrier choisi, rien n'est lu)
