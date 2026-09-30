# API MonEndo (ASP.NET Core 8) — conventions

Complète le [CLAUDE.md racine](../CLAUDE.md). S'applique à tout le code de `MonEndoVue.Server/`.

## Organisation
- `Controllers/` : un contrôleur par entité du carnet (`DonneesDouleursController`, `SymptomesCycleController`…),
  `AccountController` (auth) et `NotificationsController` (abonnements push, réglage des rappels).
- `Services/` : logique métier (`CarnetSanteService`, validateurs comme `BilanTransitValidator`), auth (`TokenService`),
  stockage photos (`AzureBlobStorageService`), extensions (`ControllerSecurityExtensions`, `UserExtensions`).
- `Models/` : entités EF. `Dto/` : entrées (`*Dto`). `ViewModels/` : sorties (`*ViewModel`).
- `Data/AppDbContext.cs` : DbSets + relations en Fluent API. `Migrations/` : migrations EF (SQL Server uniquement).
- `Jobs/` (Quartz : `RappelBilanJob` toutes les 15 min, `SuppressionComptesInactifsJob` chaque nuit à 3 h 30) et `Services/WebPush/` (envoi Web Push derrière `IEnvoiPush`,
  logique des endpoints dans `NotificationsService`, boucle d'envoi des rappels dans `NotificationsPushService`).
- **Ajouter un type de rappel** : une valeur de `TypeRappel`, une classe `IRegleRappel` dans `Services/WebPush/Rappels/`
  (calendrier par défaut, message avec l'URL à ouvrir, « suivi déjà fait ? »), son `AddScoped<IRegleRappel, …>` dans
  `Program.cs` et ses tests ; côté client, une entrée dans `features/parametres/config/rappels.ts`. Rien d'autre à toucher.

## Couches (cible pour tout nouveau code)
- **Contrôleur mince** : validation d'entrée, appel du service, mapping vers la réponse HTTP. Le service renvoie un
  `ResultatOperation` (`Services/ResultatOperation.cs`, statut métier sans dépendance HTTP) que le contrôleur traduit avec
  `VersReponse` (`Controllers/ResultatOperationExtensions.cs`). Modèle : `NotificationsController` / `NotificationsService`.
- **Service** : logique métier et requêtes EF, toujours scopées par carnet. Enregistré en `AddScoped` dans `Program.cs`.
- Pas de repository générique au-dessus d'EF : n'extraire une classe d'accès aux données que si des requêtes sont partagées entre services.
- **Entrée en DTO, sortie en ViewModel** : ne pas binder ni retourner d'entité EF dans le nouveau code (évite le sur-postage
  de `Id`, `CarnetSanteId`, `PhotoUrl`…). L'existant retourne encore des entités : ne pas étendre ce pattern.

## SOLID côté serveur
- **Responsabilité unique**
  - Un contrôleur ne fait que du HTTP : il reçoit un DTO, appelle **un** service et traduit le résultat en réponse
    (`Ok`, `NotFound`, `Forbid`…). Il n'injecte pas `AppDbContext` et n'écrit pas de requête LINQ (nouveau code).
  - Un service par domaine métier (`BilanQuotidienService`, `NotificationsPushService`…) ; au-delà de ~200 lignes ou de
    responsabilités hétérogènes (lecture, export, cache), le découper.
  - Les règles de validation métier vont dans une classe dédiée, pure et testable (modèle : `BilanTransitValidator`).
- **Ouvert/fermé** : un nouveau canal ou comportement s'ajoute par une nouvelle implémentation ou une nouvelle entrée
  de configuration (politiques de débit, `IEnvoiPush`), pas en modifiant un `switch` existant.
- **Substitution** : les faux de test (`FauxEnvoiPush`, `HorlogeFixe`) respectent exactement le contrat de l'abstraction.
- **Interfaces ciblées** : une interface expose ce dont un consommateur a besoin, pas plus (modèle : `IEnvoiPush`, une méthode).
- **Inversion des dépendances**
  - Une interface seulement quand elle sert un besoin concret (**KISS**) :
    - dépendance externe à remplacer dans les tests (push : `IEnvoiPush`) ;
    - plusieurs implémentations réelles (règles de rappel : `IRegleRappel`).
    
    Heure via `TimeProvider` (jamais `DateTime.Now` dans le nouveau code), appels HTTP via un `HttpClient` typé.
    Un service métier pur (ex. `TokenService`) reste une classe concrète.
  - `AppDbContext` est l'abstraction d'accès aux données : il s'injecte dans les services, sans repository générique
    par-dessus (voir « Couches »).
  - Pas de `new` d'un service dans le code applicatif : tout passe par l'injection de dépendances (`Program.cs`).
- **Dette connue** (lot C de la roadmap), à résorber quand on touche la zone :
  - les contrôleurs injectent `AppDbContext` et contiennent des requêtes (sauf `NotificationsController`, déjà conforme) ;
  - `CarnetSanteService` mélange lecture du carnet, page d'accueil, export PDF et cache ;
  - **Les photos ne sont jamais servies par leur adresse de stockage** : `GET Acne/photos/{id}` (`AcneService.PhotoAsync`) les
    lit par `IStockagePhotos`, vérifie le carnet en base et déduit le type de l'extension ; les view models exposent ce chemin
    (`AcneService.CheminPhoto`), jamais `PhotoUrl`. Le conteneur Azure doit rester **privé** (réglage à vérifier sur le portail) ;
    la CSP n'autorise plus aucun domaine de stockage pour les images.
  - Limites de débit (`PolitiquesDebit`) : une fenêtre **par utilisatrice** (ou par adresse pour un anonyme, lue dans
    `X-Forwarded-For` de nginx), jamais une fenêtre commune à toute l'application.
  - `AzureBlobStorageService` : lecture et suppression par URL passent par `IStockagePhotos` (`Services/Photos/`,
    `Support/FauxStockagePhotos` en test) ; l'upload et la suppression d'une photo de symptôme l'appellent encore directement ;
  - `DateTime.Now` subsiste dans l'authentification.

## Style C#
- Namespace **file-scoped**, **primary constructors** pour l'injection, `Nullable` activé (déclarer `?` ou `required`).
- Contrôleur : `[Route("[controller]")]`, `[ApiController]`, `[Authorize]` sur la classe ; segments d'action en kebab-case
  (`by-month`, `last-entries`) et contraintes typées (`{id:int}`).
- Actions `async` retournant `Task<ActionResult<T>>` / `Task<IActionResult>` ; propager un `CancellationToken` dans le nouveau code.
- Noms métier en français (`CarnetSante`, `Intensite`), verbes techniques en anglais (`GetByMonth`, `UploadFileAsync`), suffixe `Async` sur les méthodes asynchrones.
- Utilisatrice courante : `User.GetCurrentUserId()` (`Services/UserExtensions.cs`). Pas de `ClaimsPrincipal.Current`.
- Dates : `DateTime.UtcNow` pour tout ce qui est technique (expiration, audit).

## Cloisonnement par carnet (obligatoire)
Toute action qui touche une donnée d'un carnet vérifie la propriété avec `ValidateCarnetAccess`
(`Services/ControllerSecurityExtensions.cs`). Pour une modification ou une suppression, la vérification porte sur l'entité
**chargée en base**, jamais sur le corps de la requête. Modèle : `PutSymptomeCycle` dans `Controllers/SymptomesCycleController.cs`.

Pour une ressource propre à l'utilisatrice connectée (réglages, appareils…), préférer **déduire le carnet de la session**
sans accepter d'identifiant en entrée : aucun IDOR possible. Modèle : `CarnetDeAsync` dans `Services/WebPush/NotificationsService.cs`.

**Nouveau code (SOLID)** : le contrôle de propriété se fait **dans le service** (`EstProprietaireAsync` sur l'entité chargée,
statut `Interdit`/`Introuvable` traduit par le contrôleur) : voir le gabarit du skill `endpoint-api` (`templates.md`).
Le code existant utilise encore `ValidateCarnetAccess` dans les contrôleurs. Dans tous les cas : copier uniquement les champs
modifiables, jamais `Entry(dto).State = Modified`, jamais de changement de `CarnetSanteId`.
- GET par id : charger, `NotFound()` si absent, **puis** vérifier le carnet.
- POST : vérifier le `CarnetSanteId` reçu **et** les clés étrangères (ex. `MedicamentId` doit appartenir au même carnet).
- Refus d'accès : `Forbid()` **sans argument** (son paramètre est un nom de schéma d'authentification ; passer un message
  provoque une erreur 500).

## Consentement aux données de santé
- Filtre MVC global `ExigeConsentementFilter` : une utilisatrice connectée dont le jeton ne porte pas la version en
  vigueur de la politique (claim `politique`, `Services/Consentement/PolitiqueConfidentialite.cs`) reçoit un 403
  `{ code: "consentement-requis" }`, que le client traduit en redirection vers `/consentement`.
- **Tout nouveau contrôleur est donc soumis au consentement.** Seuls l'authentification, le recueil du consentement et la
  suppression du compte en sont exemptés par `[SansConsentement]` : retirer son accord ne doit jamais être bloqué.
- Changer `PolitiqueConfidentialite.Version` (avec la date de `features/legal/config/editeur.ts`) redemande l'accord à toutes
  les utilisatrices : seulement pour une évolution importante de la politique.

## Droits sur les données (RGPD)
- `DonneesPersonnellesController` (`[SansConsentement]`) : export complet par `ExportDonneesService` (ZIP en fichier temporaire
  supprimé à la fermeture, `donnees.json` + `photos/` + `LISEZMOI.txt`, carnet déduit de la session) ;
  `SuppressionCompteController` (même préfixe `DonneesPersonnelles/`, `[SansConsentement]`) : suppression du compte par
  `SuppressionCompteService` (mot de passe exigé ; photos supprimées d'abord, abandon sans rien toucher si le
  stockage est indisponible ; puis toutes les entités du carnet et le compte en un seul `SaveChanges`).
- **Accueil** : `AccueilService` (`Services/Accueil/`) ; la position dans le cycle est calculée par `CycleDuJour` (classe pure,
  testée) : un oubli d'un jour ne coupe pas les règles, rien au-delà de 60 jours, aucune prédiction.
- **Comptes inactifs** : `ApplicationUser.DerniereActiviteLe` est mise à jour à chaque ouverture ou prolongation de session
  (`OuvrirSessionAsync`) ; `ComptesInactifsService` supprime chaque nuit les comptes sans activité depuis 2 ans (durée
  annoncée par la politique de confidentialité : la changer dans les deux). Une date nulle n'est jamais supprimée.
- **Toute nouvelle entité ou donnée enregistrée s'ajoute à l'export** (`LireDonneesAsync`) **et à la suppression**
  (`MarquerDonneesDuCarnetAsync`), avec leurs tests (`SuppressionCompteServiceTests.CreerCompteRempli`), dans la même PR.

## Erreurs et réponses
- `BadRequest(new { message = "Message en français" })` : format lu par `useDialogForm.getErrorDescription` côté client.
- `NotFound()`, `NoContent()` après PUT/DELETE, `CreatedAtAction` après POST, `ValidationProblem` pour la validation de modèle.
- Jamais `ex.Message` ni stack trace dans une réponse : logger l'exception, renvoyer un message générique.
- La sérialisation JSON utilise `ReferenceHandler.Preserve` : les listes arrivent côté client sous `{ $values: [...] }`.
  Ne pas changer ce réglage sans migrer tout le client.

## Logs
- `ILogger<T>` avec templates structurés (`"… {SymptomeId}"`), jamais de concaténation ni de `Console.WriteLine`.
- Interdits dans les logs : email, nom d'utilisateur, token, code OAuth, contenu d'une donnée de santé. Les identifiants numériques suffisent.
- Serilog (configuré dans `Program.cs`) : hors dev, `Information` pour l'application et `Warning` pour `Microsoft`/`System` ;
  un log utile en production doit donc être au moins `Information`. Fichiers `Logs/MonEndoVue-AAAAMMJJ.log` purgés après 30 jours.

## Sécurité transverse
- En-têtes de sécurité et **Content-Security-Policy** dans `Services/EntetesSecurite.cs` (posés sur toutes les réponses).
  La CSP est **bloquante** depuis la 1.2.1 (`Report-Only` en 1.2.0) : une ressource non déclarée ne se charge pas. Toute nouvelle ressource
  tierce (police, image, API appelée par le navigateur) s'y déclare, ou mieux, s'héberge localement. Vérification : client
  buildé (`VITE_DOCKER=true npx vite build` puis `npx vite preview`), en-tête ajouté aux documents par un script Playwright
  (`route.fetch` puis `route.fulfill`), toutes les pages parcourues à 375px et 1280px, messages « Content Security Policy »
  relevés dans la console. Le serveur de dev Vite (scripts en ligne) ne convient pas pour cette vérification.
  En production, un écouteur `securitypolicyviolation` posé dans la console puis `$router.push` sur chaque route
  (session de l'utilisateur) relève aussi les violations. Pas de bibliothèque qui lance un worker depuis une URL `blob:`
  ou évalue du code (`new Function`) : c'est pour cela que `heic2any` a été retiré (HEIC converti par le navigateur).
  Seul HSTS est posé par nginx (`deploy/nginx.conf`) : un en-tête posé à la fois par nginx et par l'app est en double, donc invalide.
- Rate limiting : politiques `api` (par défaut) et `auth` (20 req/min), constantes dans `Services/PolitiquesDebit.cs`.
  La politique `api` n'est posée que sur les endpoints qui n'en déclarent pas (`PolitiquesDebit.AppliquerParDefaut`) :
  un `[EnableRateLimiting(PolitiquesDebit.Auth)]` sur une action est donc réellement appliqué. À mettre sur tout endpoint
  d'authentification ou d'envoi coûteux. Vérifier en local par une rafale de requêtes (429 attendu au-delà de la limite).
- Identifiants et mots de passe dans le **corps** des requêtes, jamais en query string.
- Uploads photo : réutiliser `IsPhotoValid`/`ResolveFileExtension` (`SymptomesCycleController`) et `AzureBlobStorageService`
  (chemin `symptomes/{carnetSanteId}/{guid}{ext}`). Ne jamais supprimer un blob à partir d'une URL fournie par le client.
- `/health` est anonyme et hors rate limit (sonde du déploiement) : ne pas y exposer d'information.

## Base de données et migrations
- `dotnet ef migrations add NomEnPascalCase --project MonEndoVue.Server` (en français : `AjouteColonneX`, `SupprimeTableY`).
- Relations et contraintes dans `AppDbContext.OnModelCreating` ; les données filles cascadent depuis `CarnetSante`
  (`DeleteBehavior.Restrict` quand plusieurs chemins de cascade existent).
- **Appliquées automatiquement au démarrage en production** : migrations rétro-compatibles, relire le fichier généré,
  signaler toute opération destructive. Ne jamais modifier une migration déjà déployée.
- En développement, les migrations ne sont pas appliquées au démarrage (`dotnet ef database update` à la main).
- **Supprimer une table hors modèle** (cas de `PreferencesRappel`, supprimée en 1.2.0 par `SupprimePreferencesRappel`) :
  `migrations add` génère une migration vide (modèle et snapshot inchangés) ; écrire à la main le `DropTable` et, dans
  le `Down`, le `CreateTable` du schéma d'origine. La tester sur une base jetable avec des lignes (`sqlcmd -I`).
- **Rollback après une colonne rendue nullable** : une image antérieure lit la colonne comme non nullable et plante dès
  qu'une valeur nulle est enregistrée (cas de `RendFacultativesMesuresBilan`). Le signaler dans la PR et ajouter la
  requête de remise à niveau à exécuter avant un retour arrière dans le skill `release` (section « Annuler »).
- **Générer une migration sans lire les secrets** : l'outil EF démarre l'hôte, qui exige `appsettings.{Environment}.json`.
  Plutôt que de lire ou copier `appsettings.Development.json` (interdit), créer temporairement dans `MonEndoVue.Server/`
  un `appsettings.DesignTime.json` contenant uniquement une chaîne de connexion factice (fichier ignoré par git, l'écrire
  avec node pour un JSON valide), lancer `ASPNETCORE_ENVIRONMENT=DesignTime dotnet ef migrations add Nom`, puis le supprimer.
  Aucune connexion à une base n'est nécessaire pour `migrations add`.
- **Pages et API partagent l'espace des chemins** (routage insensible à la casse : la page `/cycle` et `GET Cycle`). Sans garde,
  un rafraîchissement affichait le JSON de l'API (bug de la 1.3.0 sur `/cycle` et `/activite`). `NavigationVersSpa` sert la
  page d'entrée à toute navigation `Accept: text/html`, **avant** `UseRouting` (appel explicite, à ne pas déplacer : sinon le
  routage choisit l'endpoint en premier). Un appel de l'API ne doit donc jamais passer par une navigation de page.

## Configuration
Chargée depuis `appsettings.{Environment}.json` (**obligatoire**, non versionné), variables d'environnement puis user-secrets.
Clés attendues (noms seulement) : `ConnectionStrings:DefaultConnection`, `AzureBlobStorage:ConnectionString`,
`AzureBlobStorage:ContainerName` (ou variables `AZURE_STORAGE_CONNECTION_STRING`/`AZURE_CONTAINER_NAME`),
`Authentication:Schemes:Bearer:{Secret,ValidIssuer,ValidAudiences}`, `Jwt:Key`, `RootUser:{UserName,Email,Password}` (compte créé au démarrage en **développement** seulement),
`WebPush:{Subject,PublicKey,PrivateKey}` (clés VAPID ; absentes ou invalides — sujet sans `mailto:`/`https:`, clés ≠ 87/43 caractères — = notifications désactivées
 avec un avertissement au démarrage, sans bloquer ni faire échouer les routes ;
 en dev via `dotnet user-secrets`), `Agenda:CleApi` (clé API Google Calendar) et `Agenda:Calendriers:<id de l'utilisatrice>` (identifiant du calendrier affiché ; sans entrée, pas d'agenda : 404). En production, dans `config/app.env` sous la forme `Agenda__CleApi=…` et `Agenda__Calendriers__<id>=…`. Ne jamais lire ni afficher les valeurs.

## Tests
Projet `MonEndoVue.Server.Tests` (xUnit, **net8.0** comme la CI et le Dockerfile), lancé par `dotnet test` et par la CI.
- Règles métier pures (validateurs statiques, ex. `BilanTransitValidator`) : tests unitaires dans `Services/`, nommés
  `Methode_Situation_ResultatAttendu` en français, `[Theory]` pour les cas limites.
- Contrôleurs et services : tests avec EF InMemory via `Support/CarnetDeTest.cs` (base isolée + utilisatrice connectée et
  son carnet + une autre utilisatrice et son carnet `AutreCarnetSanteId` ; `ContexteAuthentifie()` / `ContexteAnonyme()`
  pour instancier un contrôleur). Modèles : `Controllers/BilanQuotidienControllerTransitTests.cs`,
  `Services/CarnetSanteServiceExportTests.cs`.
- Authentification : `Support/IdentityDeTest.cs` (vraie pile Identity sur EF InMemory, `CreerController`, `CreerUtilisatrice`).
- Notifications : `Support/PushDeTest.cs` (vraies clés P-256, `FauxEnvoiPush`, `HorlogeFixe` pour injecter l'heure via
  `TimeProvider`). Tout code dépendant de l'heure reçoit un `TimeProvider` (jamais `DateTime.Now` direct) pour être testable ;
  un client HTTP externe se teste avec un `HttpMessageHandler` factice (modèle : `Services/WebPushServiceTests.cs`).
- **Cloisonnement** : tout contrôleur de données du carnet a son fichier `Controllers/<Controleur>CloisonnementTests.cs`
  (GET/PUT : `NotFound` si absent, `Forbid` sur le carnet de l'autre sans rien modifier, copie des champs sans changer de
  carnet ; clés étrangères d'un autre carnet refusées). Un nouvel endpoint ou champ modifiable s'y ajoute.
- **Quality gate SonarCloud (plan gratuit, non modifiable) : 80 % de couverture sur le nouveau code.** La CI envoie la couverture
  (coverlet, OpenCover) ; migrations et client sont exclus. Donc **toute ligne C# ajoutée ou modifiée hors migration doit être
  exécutée par un test**, sinon le check Sonar de la PR échoue. Vérifier en local :
  `dotnet test --collect:"XPlat Code Coverage;Format=opencover"`.
  Un cas défensif impossible (`if (x == null) throw` après une vérification qui le garantit, `catch` d'une exception
  jamais levée) compte comme ligne non couverte : l'écrire en une expression (`?? throw`) ou le supprimer plutôt
  que de chercher à le tester. Lignes non couvertes d'une PR : `https://sonarcloud.io/api/sources/lines?key=ClementBartholome_MonEndoV2:<chemin>&pullRequest=<n>`
  (`isNew` et `lineHits: 0`).
- **GitGuardian** analyse chaque commit d'une PR : un littéral affecté à un champ de mot de passe (`Password = "Xyz1!…"`)
  est signalé comme secret, et le reste tant que le commit est dans l'historique (faux positif à classer par l'utilisateur
  dans le tableau de bord GitGuardian). Dériver les mots de passe de test de `IdentityDeTest.MotDePasseValide`
  (`MotDePasseValide + "-erreur"` pour un mauvais mot de passe).
- **Sonar, note de sécurité** (bloquante) : pas de `Path.GetTempFileName()` (S5445 : `GetTempPath()` + `GetRandomFileName()`
  en `FileMode.CreateNew`) ; un cookie effacé reprend `HttpOnly`, `Secure` et `SameSite` (S2092, S3330).
- Prochaine étape : tests d'intégration avec `WebApplicationFactory` (routage, `[Authorize]`, code HTTP réel des refus).
