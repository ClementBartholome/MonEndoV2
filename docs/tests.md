# Tests et contrôles qualité de MonEndo

Vue d'ensemble de tout ce qui vérifie l'application : quoi, où, quand, et si c'est bloquant.
Conventions détaillées : [MonEndoVue.Server/CLAUDE.md](../MonEndoVue.Server/CLAUDE.md) (section Tests) et
[monendovue.client/CLAUDE.md](../monendovue.client/CLAUDE.md) (section Vérifications).

Répartition : le **serveur** (règles métier, cloisonnement, réponses de l'API) est couvert par des tests xUnit ; l'**interface**
(ce que voit et fait l'utilisatrice, calculs affichés compris) par des tests E2E Playwright avec une API simulée.

## Vue d'ensemble

| Contrôle | Outil | Emplacement | Commande locale | En CI | Bloquant |
|---|---|---|---|---|---|
| Tests serveur | xUnit, EF Core InMemory | `MonEndoVue.Server.Tests/` | `dotnet test` (racine) | job `verifier` | oui |
| Couverture serveur | coverlet → SonarCloud | rapport OpenCover | `dotnet test --collect:"XPlat Code Coverage;Format=opencover"` | job `verifier` + check SonarCloud | seuil 80 % du nouveau code (check de PR) |
| **Tests E2E de l'interface** | Playwright (Chromium), API simulée | `monendovue.client/tests/` | `npm run test:e2e` | job `e2e` : PR vers `main`, ou PR avec l'étiquette `e2e` | check de PR |
| Type-check client (code + tests E2E) | vue-tsc, tsc | `tsconfig.app.json`, `tests/tsconfig.test.json` | `npm run type-check` | job `verifier` (via `npm run build`) | oui |
| Build client | Vite | — | `npm run build` | job `verifier` | oui |
| Lint client | ESLint | — | `npx eslint <fichiers>` | job `verifier` | non (`continue-on-error`) |
| Build de l'image | Docker | `Dockerfile` | — (pas de Docker sur le poste) | job `image` | oui |
| Secrets commités | GitGuardian (application GitHub) | — | — | check de PR | alerte |
| Santé après déploiement | `curl` sur `/health` | `ci.yml`, job `deployer` | `curl https://monendoapp.fr/health` | après chaque déploiement | oui (le job échoue) |

« Bloquant » : l'échec fait échouer le job ; sur `main`, les jobs suivants (`image`, `deployer`) ne tournent pas.
La branche `main` n'a pas de protection GitHub : un check rouge n'empêche pas techniquement de merger une PR, il faut
donc le regarder avant le merge (skill `revue-pr`).

## Tests serveur (xUnit)

Projet `MonEndoVue.Server.Tests` (net8.0), environ 250 cas. Aucun accès réseau ni base réelle : EF Core InMemory et
faux clients HTTP. Outils partagés dans `Support/`.

| Famille | Ce qui est vérifié | Exemples |
|---|---|---|
| Règles métier pures | validateurs statiques, cas limites en `[Theory]` | `Services/BilanTransitValidatorTests.cs`, `BilanMesuresValidatorTests.cs` |
| Services et contrôleurs | logique métier sur une base InMemory avec deux utilisatrices (`CarnetDeTest`) | `Controllers/BilanQuotidienControllerSaisieTests.cs`, `Services/HistoriqueBilansServiceTests.cs` |
| **Cloisonnement par carnet** | un fichier par contrôleur de données : 404 si absent, refus sur le carnet d'une autre, clés étrangères d'un autre carnet refusées | `Controllers/*CloisonnementTests.cs` |
| Authentification | vraie pile Identity sur InMemory (`IdentityDeTest`) | `Controllers/AccountControllerIdentifiantsTests.cs` |
| Notifications | vraies clés P-256, faux envoi (`FauxEnvoiPush`), heure fixe (`HorlogeFixe`) | `Services/NotificationsPushServiceTests.cs`, `ReglesRappelTests.cs` |
| Services externes | `HttpMessageHandler` factice à la place du réseau | `Services/WebPushServiceTests.cs`, `Services/AgendaServiceTests.cs` |
| Sécurité transverse | politiques de débit, extensions de contrôle d'accès | `Services/PolitiquesDebitTests.cs`, `ControllerSecurityExtensionsTests.cs` |

Couverture : SonarCloud exige 80 % sur le nouveau code (plan gratuit, non modifiable). Toute ligne C# ajoutée hors
migration doit être exécutée par un test.

## Tests E2E de l'interface (Playwright, API simulée)

Playwright ouvre l'application dans Chromium et la pilote comme une utilisatrice (clics, saisie, lecture de l'écran).
Chaque test tourne deux fois : **mobile 375px** et **desktop**.

- **API simulée** (`tests/support/faux-serveur.ts`) : l'application tourne sur un serveur Vite dédié (port 5174,
  `VITE_DOCKER=true`), ses appels API sont interceptés et reçoivent des réponses simulées. Chaque test a son propre état :
  une donnée ajoutée puis relue ou supprimée se comporte comme avec la vraie API. Ni serveur .NET, ni base, ni compte.
- **Session** : un faux utilisateur est posé dans `localStorage` (fixture `tests/support/fixtures.ts`) ; l'horloge est fixée
  (`MAINTENANT`) pour que les parcours ne dépendent pas du jour.
- **Garde-fou** : un appel API sans réponse simulée fait échouer le test (appel oublié ou contrat modifié).
- **Limite** : si le format d'une réponse change côté serveur sans que la simulation suive, le test reste vert. Les données
  simulées sont typées avec les types TypeScript du client (alignés sur les contrats C#) et doivent reproduire le format
  réel : un tableau C# (`ToArrayAsync`) arrive en tableau JSON, une `List` en `{ $values }`.
- **CI** : job `e2e`, sur les PR vers `main` (livraison d'une version ou hotfix) et sur toute PR à laquelle on ajoute
  l'étiquette `e2e` (`gh pr edit <n> --add-label e2e`), en parallèle de `verifier`.
  En cas d'échec, le rapport Playwright est joint au run (artefact `playwright-report`).
- Couverture actuelle : page Activité (affichage, ajout, modification, suppression). Parcours principaux : issue #23.

## Contrôles manuels (Definition of Done)

- Chaque écran touché, à **375px et en desktop**, clavier compris.
- Test de bout en bout d'une branche en local (API + base de dev + Vite), l'utilisateur se connectant lui-même.
- Migrations d'une version testées sur une base jetable avant la livraison (skill `release`).
- Revue de sécurité (skill `revue-securite`) quand l'auth, un endpoint ou un upload est touché.

## Limites connues et suites

- Parcours E2E encore limités à la page Activité : issue #23 (connexion, bilan quotidien, douleurs, cycle, traitements, export).
- Aucun test ne fait dialoguer la vraie interface avec le vrai serveur : le contrat entre les deux repose sur les types
  TypeScript alignés à la main et sur le test local de bout en bout avant une livraison.
- Pas de tests d'intégration HTTP (`WebApplicationFactory`) : le routage, `[Authorize]` et les codes de refus réels ne
  sont vérifiés qu'indirectement.
- EF Core InMemory ne reproduit pas SQL Server (contraintes, traduction des requêtes) : les migrations et requêtes
  sensibles se vérifient sur la base de dev locale.
- Pas de protection de la branche `main` (checks obligatoires) : voir « Vue d'ensemble ».

## Où ajouter un test

| Je modifie… | J'ajoute… |
|---|---|
| une règle métier ou un service serveur | un test xUnit dans `MonEndoVue.Server.Tests/Services/` |
| un endpoint qui lit ou modifie un carnet | ses cas dans `Controllers/<Controleur>CloisonnementTests.cs` |
| un appel à un service externe | un test avec un `HttpMessageHandler` factice |
| un écran ou un parcours (saisie, affichage, calcul affiché) | un test dans `monendovue.client/tests/<page>.spec.ts` et ses routes simulées dans `tests/support/` |
| le format d'une réponse de l'API | le type TypeScript **et** la simulation correspondante dans `tests/support/` |
