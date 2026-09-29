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
  **`JourRegle`** (jours de règles) — page `/cycle` (onglets symptômes, acné, cycles).
- **`Medicament`** (traitement, `TypeTraitement` médicamenteux ou non, en cours ou passé, avec une **fréquence** à la
  manière de l'app Santé d'Apple : `AuBesoin`, `ChaqueJour`, `CertainsJours` + jours de la semaine, `TousLesNJours`
  depuis la date de début, et jusqu'à 6 **`HorairePrise`**), **`DonneesMedicament`** (réponses aux prises : `Statut`
  `Pris` ou `Ignore`, `HeurePrevue` pour une prise planifiée, absente pour une prise au besoin),
  **`DonneesTraitementNonMedicamenteux`** (séances) — page `/medicaments` (`TraitementsController`, `GET Traitements/jour`).
  Le planning d'un jour est calculé par `PlanningTraitement` ; une prise **ignorée** est stockée dans la même table :
  toute lecture qui compte des prises (PDF, historique, accueil) filtre `Statut == Pris`.
- **`DonneesTransit`** — `/transit` (ancien suivi par événements ; le suivi quotidien passe désormais par le bilan) ; **`DonneesActivitePhysique`** — `/activite`.
- **`BilanQuotidien`** (émotions, stress, fatigue, pas, douleur moyenne, hydratation, alimentation, notes, et une catégorie
  **transit** facultative : selles avec type de Bristol 1-7, crampes d'estomac et ballonnements avec intensité) — `/bilan-quotidien`,
  avec des repères personnels réglables dans `/parametres` (stockés en `localStorage`). L'onglet « Analyse & Tendances »
  ne donne ni score ni note : moyennes, évolution vs période précédente, repères atteints, observations factuelles
  douleur / règles (`utils/tendances.ts`). Historique par semaine ou par mois (`GET BilanQuotidien/periode`,
  bilans + jours de règles, carnet déduit de la session) : une seule période pilote calendrier, détail, courbes et analyse. Émotions : 1 à 3 **`EmotionBilan`** par bilan (table
  `EmotionsBilan`, type possédé chargé avec le bilan ; enum `Emotion`, libellés et tonalité dans `config/emotions.ts`).
  Saisie en un écran (`components/saisie/`) : douleur et émotions obligatoires, stress, fatigue, pas et hydratation
  **nullables** (null = non renseigné, jamais compté pour 0) ; **un seul bilan par jour** et aucun jour futur (409 / 400).
  Les bilans antérieurs gardent leur ancienne humeur `Mood` (`Heureuse`/`Neutre`/`Triste`) : tout calcul d'humeur passe
  par `features/bilan-quotidien/utils/humeur.ts`, qui prend en compte les deux.
- Accueil `/` « Aujourd'hui » (`GET Accueil/aujourdhui?jour=AAAA-MM-JJ`, jour local envoyé par le client, carnet de la
  session : cycle déduit des jours de règles sans prédiction, bilan du jour, prises prévues ce jour-là (heure et réponse) et
  traitements au besoin, faits descriptifs des 7 derniers jours ; prise notée en un geste par `POST Traitements/{id}/prises`), agenda `/agenda` (Google Calendar en lecture via le serveur, seulement pour une utilisatrice associée à un calendrier dans la configuration `Agenda`), export PDF `/export`.
- Pages publiques (sans compte, `meta: { public: true }` dans le routeur) : connexion, inscription, politique de
  confidentialité `/confidentialite` et mentions légales `/mentions-legales` (`features/legal/`). **Consentement explicite**
  aux données de santé (`ApplicationUser.ConsentementDonneesSanteLe`, version de la politique acceptée) : case à l'inscription,
  page `/consentement` pour les comptes sans accord à jour, exigé par l'API (voir `MonEndoVue.Server/CLAUDE.md`). **Tout changement de
  donnée collectée, de sous-traitant ou de durée de conservation met à jour la politique** (et sa date, `config/editeur.ts`)
  dans la même PR.
- Notifications **Web Push standard** envoyées par le serveur (clés VAPID, sans service tiers) : chaque appareil s'abonne
  depuis `/parametres` ; rappels réglables (job Quartz toutes les 15 min), chacun omis si le suivi est déjà fait :
  bilan quotidien (bilan du jour pas encore rempli, ouvre `/bilan-quotidien`) et photo de suivi de l'acné hebdomadaire
  (aucune photo d'acné depuis 7 jours, ouvre `/cycle?onglet=acne`). Sur iOS (16.4+), uniquement dans l'app ajoutée à l'écran d'accueil.
  Entités : **`AbonnementPush`** (un par appareil, endpoint unique, rattaché au carnet) et **`Rappel`** (un par carnet et
  par type : actif, heure locale, jour de la semaine si hebdomadaire, fuseau IANA, date du dernier envoi). L'ancienne table
  `PreferencesRappel` est supprimée depuis la 1.2.0.

## Principes produit (non négociables)
- **Mobile-first** : écrans pensés d'abord pour ≤ 425px, aucune information clé tronquée ; le desktop enrichit ensuite.
- Saisie rapide (1-2 taps, presets, états vides avec action), lisibilité avant densité.
- Ton **non anxiogène**, aucune sur-promesse médicale : on montre des tendances, on ne pose pas de diagnostic.
- Privacy by design : l'utilisatrice contrôle ses données, rien n'est stocké sans nécessité.

## Décisions d'architecture
- **Pas de PWA, pas de cache service worker, pas de mode hors ligne** (abandonnés en 2026-09). Le seul service worker
  légitime est `public/push-sw.js` (affichage des notifications, aucun cache). OneSignal, Zapier et SignalR sont abandonnés.
  `public/sw.js` est un worker d'autodestruction temporaire (à retirer après le 2026-12-31).
- **Pas de génération de types** (TypeGen retiré) : les types TS de `features/*/types` sont maintenus à la main et
  modifiés **dans le même commit** que le DTO/modèle C# correspondant.
- Front : logique métier dans des composables, pages qui orchestrent, composants de présentation sans effet de bord.
- Back : contrôleurs minces → services ; entités EF jamais exposées directement dans le nouveau code (DTO/ViewModel).

## Principes SOLID, avec KISS (décision du 2026-09-26)
Tout code nouveau ou modifié respecte SOLID ; la déclinaison concrète est dans le CLAUDE.md de chaque couche.
**Keep it simple prime sur l'abstraction préventive** : une interface ou une couche n'existe que pour un besoin concret
actuel (plusieurs implémentations réelles, dépendance externe à remplacer dans les tests, point d'extension utilisé).
Sinon, une classe concrète simple injectée telle quelle. Pas d'interface « au cas où », pas de repository générique.
- **S** : une classe / un composable / un composant = une responsabilité (HTTP, métier, accès aux données, rendu).
- **O** : étendre par ajout (nouvelle implémentation, nouvelle entrée de configuration) plutôt qu'en modifiant des `switch`/`if` existants.
- **L** : toute implémentation (y compris les faux de test) respecte le contrat de son abstraction, sans cas particulier.
- **I** : interfaces et props petites et ciblées ; pas d'interface « fourre-tout ».
- **D** : tout passe par l'injection de dépendances ; une abstraction (interface, `TimeProvider`) seulement pour ce qui sort
  du process et doit être remplacé en test (push, stockage externe, heure).
- **Code existant non conforme** : ne pas le recopier. Quand on modifie une zone, la remettre d'aplomb dans un commit
  `refactor(…)` séparé, **sans refonte massive non demandée**. La dette connue est listée dans le lot C de `docs/modernization-plan.md`.

## Sécurité — règles à appliquer sur tout changement
1. **Cloisonnement** : toute lecture/écriture est limitée au carnet de l'utilisatrice connectée. Pour une modification ou
   une suppression, charger l'entité **en base** et vérifier **son** `CarnetSanteId` — jamais celui envoyé par le client.
   Vérifier aussi que les clés étrangères référencées (ex. `MedicamentId`) appartiennent au même carnet.
2. `[Authorize]` par défaut. Tout endpoint anonyme ou « admin » doit être justifié et protégé (rôle, propriété).
3. **Rien de secret côté client** : toute variable `VITE_*` finit dans le bundle public. Un appel tiers nécessitant une clé secrète passe par le serveur.
4. Jamais de mot de passe, token ou code OAuth dans une URL (query string, redirection) : body JSON ou cookie HttpOnly.
5. Logs structurés **sans** email, token, donnée de santé ; jamais `ex.Message` renvoyé au client.
6. Pas de `v-html` ni de rendu HTML de texte saisi (attention aux colonnes DataTables).
7. Uploads : valider taille et type côté serveur, ne jamais réutiliser une URL fournie par le client.
8. Ne pas lire, afficher ou copier les fichiers de secrets (`appsettings*.json`, `.env*`, `serviceAccountKey.json`, `keys/`, logs).

Le code existant n'est pas encore partout conforme aux conventions : **ne pas recopier un pattern existant sans vérifier
qu'il respecte ces règles** (ex. `Promise<any>` dans `apiService`, pages de plus de 1000 lignes).
Utiliser le skill `revue-securite` avant de commiter un changement touchant auth, endpoints ou uploads.

## Déploiement (tout push sur `main` = production)
- `ci.yml` : **verifier** (npm ci, type-check + build client, ESLint non bloquant, build et tests .NET, SonarCloud non bloquant) → **image**
  (build Docker ; poussée sur GHCR avec les tags `latest`, `main`, `main-<sha>` et `sha-<court>` uniquement sur `main`,
  et `vX.Y.Z` sur un push de tag de version ; simple build sur les PR et les branches `release/**`) → **deployer** (`main` seulement)
  (SSH vers le VPS, `docker compose -f docker-compose.prod.yml pull app && up -d`, puis contrôle de `https://monendoapp.fr/health`).
- Le VPS (`~/app`) utilise `docker-compose.prod.yml`, dont la référence versionnée est
  [deploy/docker-compose.prod.yml](deploy/docker-compose.prod.yml) (procédure et logs : [deploy/README.md](deploy/README.md)).
  Services : `db` (SQL Server, port non exposé, mot de passe via `DB_PASSWORD` du `.env` écrit par la CI), `app` (image GHCR
  `:latest`, HTTP sur le port 80 interne), `nginx` (seul service exposé, 80/443, TLS) et `dozzle` (lecture des logs,
  `127.0.0.1:8888`, tunnel SSH). Montés dans `app` :
  `config/appsettings.Production.json` → `/app/appsettings.Production.json` (**obligatoire**, `optional: false`),
  `keys/` → `/app/keys` (Data Protection), `logs/` → `/app/Logs`. Les secrets de production vivent uniquement sur le VPS
  (`config/`, dont `config/app.env`, et `.env`, réécrit par la CI à chaque déploiement) : ne jamais les demander ni les recopier.
- **Modifier la configuration de prod** (`~/app/config/appsettings.Production.json`, et non `~/app/appsettings.Production.json`) :
  - les options sont lues au démarrage : recréer le conteneur ensuite (`docker compose -f docker-compose.prod.yml up -d --force-recreate app`) ;
    un 502 pendant quelques secondes est normal ;
  - ne pas faire éditer le JSON à la main (nano) : fournir un script Python qui modifie le fichier et n'affiche que des
    longueurs, écrit dans un fichier (`/tmp/x.py`) puis exécuté — jamais `input()` dans `python3 - <<EOF` (stdin = le script, EOFError) ;
  - **générer les secrets directement sur le VPS** (openssl + script) : un collage dans une saisie masquée a déjà tronqué une clé ;
  - une configuration Web Push invalide est signalée au démarrage par l'avertissement `Web Push notifications disabled: <raison>`
    (`docker compose -f docker-compose.prod.yml logs app | grep -i "web push"`).
- Une modification du compose se fait **à la main sur le VPS** (la CI ne le copie pas), puis dans `deploy/` via une PR.
  VPS de 2 Go partagé avec d'autres projets : pas d'outil de logs lourd (Seq, Loki…), garder les plafonds mémoire.
- Le healthcheck Docker du compose de prod appelle `curl`, absent de l'image aspnet : il est toujours en échec, se fier
  au contrôle `/health` de la CI.
- **Les migrations EF sont appliquées automatiquement au démarrage en production** : elles doivent être rétro-compatibles ;
  toute migration destructive (DROP, colonne supprimée) doit être signalée explicitement avant merge.
- **Versions (SemVer, depuis la 1.0.0)** : les sujets sont regroupés sur une branche `release/X.Y.Z` (PR dont la base est
  cette branche), puis livrés par une PR vers `main`, suivie du tag `vX.Y.Z` et d'une GitHub Release. Numéro de version dans
  `monendovue.client/package.json` **et** `MonEndoVue.Server.csproj`, historique dans `CHANGELOG.md`. Procédure : skill `release`.
- **Sauvegardes de la base** : `deploy/sauvegarde-base.sh` (cron du VPS, 2 h 30) → sauvegarde SQL Server chiffrée par
  `openssl` sur l'hôte → conteneur Azure `sauvegardes` (SAS « Créer » seul, suppression à 30 jours par une règle de cycle de
  vie : durée annoncée par la politique de confidentialité). Restauration et test : `deploy/README.md`.
- Rollback : repointer l'image du service `app` sur une version précédente (`vX.Y.Z`, ou `sha-…` / `main-<sha>` avant la 1.0.0), puis `docker compose up -d`.
  Les images antérieures à 2026-09 chargent encore `serviceAccountKey.json` au démarrage : garder ce montage tant qu'un tel retour est envisageable.
- Ne jamais pousser sur `main`. Travailler sur une branche. **Ouvrir une PR** vers `release/X.Y.Z` (ou `main` pour un hotfix)
  est permis sans demande (décision du 2026-09-27) ; **le merge reste toujours à l'utilisateur**.

## Convention de commit
- Format : `type(perimetre): message court` — **en français**, impératif ou présent, sans point final.
- Types : `feat`, `fix`, `refactor`, `style`, `docs`, `test`, `chore`, `ci`, `build`, `perf`.
- Périmètre en kebab-case : dossier de feature (`cycle`, `douleurs`, `medicament`, `bilan-quotidien`…) ou domaine transverse
  (`auth`, `securite`, `front`, `back`, `serveur`, `ci`, `docker`, `deps`, `docs`).
- Un commit = un sujet ; ne pas mélanger refacto, fonctionnalité et nettoyage.
- **Pas de trailer `Co-Authored-By`** ni d'autre attribution dans les messages.
- Exemples : `feat(cycle): ajoute la fenêtre glissante pour la comparaison photo acné`,
  `fix(auth): corrige le path des cookies JWT`. Contre-exemples : `fix: petits correctifs`, `feat(cycle) ajout` (pas de `:`), message en anglais.

## Definition of Done
- `npm run build` et `npm run test:e2e` (client), `dotnet build -c Release` et `dotnet test` (serveur) verts ; toute règle
  métier nouvelle a ses tests (serveur : xUnit) et tout parcours d'écran nouveau ou modifié a son test E2E.
- Test manuel à 375px **et** sur desktop des écrans touchés, clavier compris.
- Revue sécurité (skill `revue-securite`) si auth, endpoint, upload ou données partagées sont touchés.
- Types TS alignés sur les contrats C# modifiés ; migration relue si le modèle change.
- **Documentation à jour dans la même PR** que l'évolution : `README.md` (fonctionnalités, technologies),
  `docs/modernization-plan.md` (case du backlog cochée, section « déjà implémenté »), `CLAUDE.md` racine et de couche
  (glossaire, décisions, conventions, pièges), skills concernés. Faire le point avec le skill `capitaliser`.

## Skills du projet (`.claude/skills/`)
- `fonctionnalite-front` — créer ou refactoriser une page/un composant Vue selon les patterns du projet.
- `endpoint-api` — ajouter ou modifier un endpoint ASP.NET Core de façon sûre (cloisonnement, DTO, migration, type TS).
- `revue-securite` — revue ciblée MonEndo d'un diff ou d'une zone de code.
- `commit` — préparer un commit conforme (vérifications, découpage, message).
- `release` — préparer, livrer ou annuler une version numérotée (branche de release, CHANGELOG, tag, rollback).
- `revue-pr` — évaluer une PR existante avant merge (conflits, fichiers parasites, migrations, cohérence front/back, impact déploiement).
- `capitaliser` — en fin de tâche, reporter ce qui a été appris dans CLAUDE.md, les skills ou la mémoire.
- `endometriose` — connaissance métier (maladie, parcours de soins, attentes des utilisatrices, apps existantes, ton) à consulter avant toute décision produit.

## Amélioration continue
Objectif : que l'utilisateur n'ait jamais à répéter une consigne ou une information.
- À la fin de chaque tâche significative, appliquer le skill `capitaliser` : décision produit, convention, piège d'outillage
  ou correction de l'utilisateur → le bon fichier (CLAUDE.md racine ou de couche, skill, mémoire personnelle).
- Si une instruction de ce fichier ou d'un skill s'avère fausse ou incomplète, la corriger dans la même branche que le travail
  concerné (commit `docs(claude): …` séparé) plutôt que de la contourner.
- Ces fichiers suivent le circuit normal : branche + PR, jamais de push direct sur `main`, rien de secret (dépôt public).

## Pièges connus de l'environnement (Windows, Git Bash)
- Les serveurs lancés en arrière-plan (Vite, API) **survivent à la fin de la session** et verrouillent les fichiers
  (build « fichier utilisé par un autre processus », `git worktree remove` en échec) : les arrêter explicitement à la fin
  d'un test, en filtrant **sur le nom du processus** (`dotnet.exe`, `node.exe`, `esbuild.exe`) en plus du chemin du worktree :
  un filtre sur le seul chemin tue aussi les shells bash en cours, y compris celui qui exécute la commande.
- Web Push : le package NuGet `WebPush` ne gère que l'ancien encodage `aesgcm`, refusé par Apple ; utiliser
  `Lib.Net.Http.WebPush` (`aes128gcm`, schéma `vapid`).
- `python` lance le stub du Microsoft Store et bloque : utiliser **node** pour les scripts ponctuels (JSON, remplacements).
- Git Bash convertit les arguments `/xxx` en chemins : `dotnet publish … -p:UseAppHost=false` (et non `/p:`).
- Ne jamais mettre de backticks dans une chaîne bash entre guillemets doubles (substitution de commande silencieuse) :
  Markdown, mais aussi template literals JS/TS dans un `node -e "…"` (le code est tronqué sans erreur). Écrire ces
  contenus avec les outils d'édition de fichiers.
- `dotnet build` lance `npm install` dans `monendovue.client/` (projet esproj) : il peut créer `node_modules` dans un
  worktree et modifier `package-lock.json` (ex. version resynchronisée). Relire ce fichier avant de commiter.
- Beaucoup de fichiers sont en CRLF : un script de remplacement doit normaliser (`\r\n` → `\n`) puis restaurer les fins de ligne.
- Chemins trop longs lors d'un checkout d'anciens commits (dossier `packages/` historique) : `git -c core.longpaths=true …`.
- **Plusieurs sessions Claude peuvent travailler en parallèle dans le même dossier** : ne jamais changer de branche,
  rebaser ou réécrire l'historique dans la copie principale sans vérifier `git status` / `git worktree list` ; travailler
  dans un worktree dédié (`git worktree add ../MonEndoVue-<sujet> -b <branche> origin/main`) puis le supprimer après merge.
  GitHub supprime seul la branche distante d'une PR mergée ; la branche locale et le worktree restent à supprimer à la main.
- Pas de Docker sur le poste : le build d'image n'est validé que par la CI d'une PR (job `image`, sans push).
- `dotnet build` du serveur lance un `npm install` du client (esproj) : ne jamais le faire tourner en même temps qu'un
  `npm ci` dans le même worktree (`node_modules` corrompu) ; un seul build à la fois par worktree.
- `sqlcmd` : ajouter `-I` (QUOTED_IDENTIFIER) pour écrire dans une table qui a un index filtré (ex. `AspNetUsers`).
- `gh` est authentifié (jeton dans le trousseau Windows, scopes `repo` et `workflow`) : l'utiliser pour lire PR, checks et runs.
  Si `gh auth status` signale un jeton invalide, demander à l'utilisateur de lancer lui-même
  `gh auth login -h github.com -p https -w` (connexion par navigateur) ; ne jamais demander ni manipuler de jeton.
  Merger une PR reste réservé à l'utilisateur.
- Contrôle visuel sans backend : lancer `npx vite --port <port>` puis, dans le navigateur intégré, poser un faux `user` dans
  `localStorage` (le garde de routes ne vérifie que sa présence) ; les appels API échouent, l'UI reste testable.

## Tester une branche de bout en bout en local
Procédure validée (sans jamais lire les fichiers de secrets) :
1. **Base** : base de dev `MonEndo` sur l'instance `localhost\MSSQLSERVER01` (authentification Windows, `sqlcmd -E -C`).
   Appliquer les migrations de la branche depuis son worktree avec la configuration factice `appsettings.DesignTime.json`
   (voir `MonEndoVue.Server/CLAUDE.md`) et
   `ASPNETCORE_ENVIRONMENT=DesignTime dotnet ef database update --connection "Server=localhost\MSSQLSERVER01;Database=MonEndo;Trusted_Connection=True;TrustServerCertificate=True"`.
2. **API** : `dotnet build -c Release` dans le worktree, puis lancer la DLL en se plaçant dans `MonEndoVue.Server/` de la
   copie principale (qui contient `appsettings.Development.json`, lu par l'application et non par Claude) :
   `ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=https://localhost:7206 dotnet <worktree>/MonEndoVue.Server/bin/Release/net8.0/MonEndoVue.Server.dll`.
3. **Front** : dans le worktree, `monendovue.client/.env.local` avec `VITE_API_URL=https://localhost:7206/` (ignoré par git),
   puis `npx vite --port 5173 --strictPort` (5173 est l'origine autorisée par CORS).
4. Attendre `https://localhost:7206/health` = `Healthy`, ouvrir `https://localhost:5173/login` : **l'utilisateur se connecte
   lui-même** avec son compte local, puis on teste à 375px et en desktop.
