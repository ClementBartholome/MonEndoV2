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
| `docs/` | Roadmap (`modernization-plan.md`) ; `docs/private/` local et non versionné |

## Commandes
```bash
# Client (depuis monendovue.client/)
npm run dev          # serveur Vite https://localhost:5173
npm run type-check   # vue-tsc, à lancer après chaque changement significatif
npm run build        # type-check + build (même commande que la CI et le Dockerfile)
npx eslint <fichiers>  # préférer au `npm run lint`, qui fait --fix sur tout le client

# Serveur (depuis la racine)
dotnet build -c Release
dotnet test          # tests xUnit de MonEndoVue.Server.Tests (aussi exécutés par la CI)
dotnet ef migrations add NomEnPascalCase --project MonEndoVue.Server
```
- Lancement complet en dev : profil `https` de `MonEndoVue.Server` (démarre Vite via SpaProxy). API : https://localhost:7206.
- Configuration locale : `MonEndoVue.Server/appsettings.Development.json` et `monendovue.client/.env` (non versionnés, **ne pas les lire ni les afficher**).
- Tests E2E Playwright : `E2E_EMAIL`/`E2E_PASSWORD` d'un compte **local**, jamais contre la production. Tests backend : projet `MonEndoVue.Server.Tests` (xUnit) ; pas encore de tests unitaires front.

## Domaine fonctionnel
- **`CarnetSante`** : un carnet par utilisatrice (1-1 avec `ApplicationUser`), racine de toutes les données.
  Chaque entité porte un `CarnetSanteId` : c'est la clé du cloisonnement.
- **`DonneesDouleur`** (type, intensité 0-10, date, commentaire) — page `/douleurs`.
- **`SymptomeCycle`** (type, intensité, date, commentaire, photo optionnelle) dont l'**acné** avec suivi photo ;
  **`JourRegle`** (jours de règles) — page `/cycle` (onglets symptômes, acné, cycles).
- **`Medicament`** (traitement, `TypeTraitement` médicamenteux ou non, en cours ou passé), **`DonneesMedicament`** (prises),
  **`DonneesTraitementNonMedicamenteux`** (séances) — page `/medicaments`.
- **`DonneesTransit`** — `/transit` (ancien suivi par événements ; le suivi quotidien passe désormais par le bilan) ; **`DonneesActivitePhysique`** — `/activite`.
- **`BilanQuotidien`** (humeur, stress, fatigue, pas, douleur moyenne, hydratation, alimentation, et une catégorie
  **transit** facultative : selles avec type de Bristol 1-7, crampes d'estomac et ballonnements avec intensité) — `/bilan-quotidien`,
  avec des objectifs bien-être réglables dans `/parametres`.
- Accueil `/` (carnet : dernières entrées), agenda `/agenda` (Google Calendar), export PDF `/export`.
- Notifications **Web Push standard** envoyées par le serveur (clés VAPID, sans service tiers) : chaque appareil s'abonne
  depuis `/parametres` ; rappel du bilan à l'heure choisie, envoyé seulement si le bilan du jour n'est pas rempli
  (job Quartz toutes les 15 min). Sur iOS (16.4+), uniquement dans l'app ajoutée à l'écran d'accueil.
  Entités : **`AbonnementPush`** (un par appareil, endpoint unique, rattaché au carnet) et **`PreferenceRappel`**
  (une par carnet : rappel actif, heure locale, fuseau IANA, date du dernier rappel envoyé).

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
  (build Docker ; poussée sur GHCR avec les tags `latest`, `main`, `main-<sha>` et `sha-<court>` uniquement sur `main`) → **deployer**
  (SSH vers le VPS, `docker compose -f docker-compose.prod.yml pull app && up -d`, puis contrôle de `https://monendoapp.fr/health`).
- Le VPS (`~/app`, hors dépôt) fournit `docker-compose.prod.yml` avec trois services sur un réseau interne :
  `db` (SQL Server, port non exposé, mot de passe via `DB_PASSWORD` du `.env` écrit par la CI), `app` (image GHCR
  `:latest`, écoute en HTTP sur le port 80 interne) et `nginx` (seul service exposé, 80/443, TLS). Montés dans `app` :
  `config/appsettings.Production.json` → `/app/appsettings.Production.json` (**obligatoire**, `optional: false`),
  `keys/` → `/app/keys` (Data Protection), `logs/` → `/app/Logs`. Les secrets de production vivent uniquement sur le VPS
  (`config/`, `.env`, variables du compose) : ne jamais les demander ni les recopier.
- Le healthcheck Docker du compose de prod appelle `curl`, absent de l'image aspnet : il est toujours en échec, se fier
  au contrôle `/health` de la CI. Une modification du compose de prod se fait à la main sur le VPS.
- **Les migrations EF sont appliquées automatiquement au démarrage en production** : elles doivent être rétro-compatibles ;
  toute migration destructive (DROP, colonne supprimée) doit être signalée explicitement avant merge.
- Rollback : repointer l'image du service `app` sur un tag précédent (`sha-…` ou `main-<sha>`), puis `docker compose up -d`.
  Les images antérieures à 2026-09 chargent encore `serviceAccountKey.json` au démarrage : garder ce montage tant qu'un tel retour est envisageable.
- Ne jamais pousser sur `main` ni ouvrir/merger une PR sans demande explicite. Travailler sur une branche.

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
- `npm run build` (client), `dotnet build -c Release` et `dotnet test` (serveur) verts ; toute règle métier nouvelle côté serveur a ses tests.
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
- `revue-pr` — évaluer une PR existante avant merge (conflits, fichiers parasites, migrations, cohérence front/back, impact déploiement).
- `capitaliser` — en fin de tâche, reporter ce qui a été appris dans CLAUDE.md, les skills ou la mémoire.

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
- Ne jamais mettre de backticks Markdown dans une chaîne bash entre guillemets doubles (substitution de commande silencieuse) :
  écrire ou modifier le Markdown avec les outils d'édition de fichiers.
- Beaucoup de fichiers sont en CRLF : un script de remplacement doit normaliser (`\r\n` → `\n`) puis restaurer les fins de ligne.
- Chemins trop longs lors d'un checkout d'anciens commits (dossier `packages/` historique) : `git -c core.longpaths=true …`.
- **Plusieurs sessions Claude peuvent travailler en parallèle dans le même dossier** : ne jamais changer de branche,
  rebaser ou réécrire l'historique dans la copie principale sans vérifier `git status` / `git worktree list` ; travailler
  dans un worktree dédié (`git worktree add ../MonEndoVue-<sujet> -b <branche> origin/main`) puis le supprimer après merge.
- Pas de Docker sur le poste : le build d'image n'est validé que par la CI d'une PR (job `image`, sans push).
- `gh` est authentifié (jeton dans le trousseau Windows, scopes `repo` et `workflow`) : l'utiliser pour lire PR, checks et runs.
  Si `gh auth status` signale un jeton invalide, demander à l'utilisateur de lancer lui-même
  `gh auth login -h github.com -p https -w` (connexion par navigateur) ; ne jamais demander ni manipuler de jeton.
  Ouvrir ou merger une PR reste soumis à une demande explicite.
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
