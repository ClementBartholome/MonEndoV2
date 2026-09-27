# Tests et contrôles qualité de MonEndo

Vue d'ensemble de tout ce qui vérifie l'application : quoi, où, quand, et si c'est bloquant.
Conventions détaillées : [MonEndoVue.Server/CLAUDE.md](../MonEndoVue.Server/CLAUDE.md) (section Tests) et
[monendovue.client/CLAUDE.md](../monendovue.client/CLAUDE.md) (section Vérifications).

## Vue d'ensemble

| Contrôle | Outil | Emplacement | Commande locale | En CI | Bloquant |
|---|---|---|---|---|---|
| Tests serveur | xUnit, EF Core InMemory | `MonEndoVue.Server.Tests/` | `dotnet test` (racine) | job `verifier` | oui |
| Couverture serveur | coverlet → SonarCloud | rapport OpenCover | `dotnet test --collect:"XPlat Code Coverage;Format=opencover"` | job `verifier` + check SonarCloud | seuil 80 % du nouveau code (check de PR) |
| Tests unitaires client | Playwright **sans navigateur** | `monendovue.client/src/**/__tests__/*.spec.ts` | `npm run test:unit` | job `verifier` | oui |
| Type-check client (code + tests) | vue-tsc | `tsconfig.app.json`, `tsconfig.unit.json` | `npm run type-check` | job `verifier` (via `npm run build`) | oui |
| Build client | Vite | — | `npm run build` | job `verifier` | oui |
| Lint client | ESLint | — | `npx eslint <fichiers>` | job `verifier` | non (`continue-on-error`) |
| Tests E2E | Playwright (Chromium, Firefox, WebKit) | `monendovue.client/tests/` | `npm run test:e2e` | **non** | — |
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

## Tests unitaires client (Playwright sans navigateur)

`playwright.unit.config.ts` : fichiers `src/<dossier>/__tests__/<module>.spec.ts`, alias `@/` résolu par
`tsconfig.unit.json`. Pas de navigateur, de serveur ni de compte : ils tournent partout, CI comprise.

Limite : le code est chargé par Node, pas par Vite. Seules les **fonctions pures** (`utils/`, `config/`) se testent ici ;
un module qui utilise `import.meta.env`, importe un `.vue` ou `apiService` passe par les E2E.

## Tests E2E (Playwright, local uniquement)

`playwright.config.ts` : projet `setup` (`tests/auth.setup.ts`, connexion puis `storageState`) puis Chromium, Firefox et
WebKit en desktop. Le serveur Vite est lancé automatiquement ; l'API doit tourner à côté (procédure « Tester une branche
de bout en bout » du [CLAUDE.md](../CLAUDE.md)).

- Compte **local** fourni par `E2E_EMAIL` / `E2E_PASSWORD`, jamais la production, aucun identifiant versionné.
- Couverture actuelle : parcours ajout / lecture / suppression de la page Activité.
- Pas exécutés en CI (il faudrait une API et une base de test dans le pipeline).

## Contrôles manuels (Definition of Done)

- Chaque écran touché, à **375px et en desktop**, clavier compris.
- Test de bout en bout d'une branche en local (API + base de dev + Vite), l'utilisateur se connectant lui-même.
- Migrations d'une version testées sur une base jetable avant la livraison (skill `release`).
- Revue de sécurité (skill `revue-securite`) quand l'auth, un endpoint ou un upload est touché.

## Limites connues et suites

- Pas d'E2E en CI ni de projet mobile (375px) dans la config E2E : issue #23 pour les parcours principaux.
- Pas de couverture mesurée côté client.
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
| une fonction pure du client (`utils/`, `config/`) | un `__tests__/<module>.spec.ts` à côté |
| un parcours d'écran | un test E2E dans `monendovue.client/tests/` (local) |
