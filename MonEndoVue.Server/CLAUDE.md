# API MonEndo (ASP.NET Core 8) — conventions

Complète le [CLAUDE.md racine](../CLAUDE.md). S'applique à tout le code de `MonEndoVue.Server/`.

## Organisation
- `Controllers/` : un contrôleur par entité du carnet (`DonneesDouleursController`, `SymptomesCycleController`…) + `AccountController` (auth).
- `Services/` : logique métier (`CarnetSanteService`), auth (`TokenService`), stockage photos (`AzureBlobStorageService`),
  notifications (`NotificationService`), extensions (`ControllerSecurityExtensions`, `UserExtensions`).
- `Models/` : entités EF. `Dto/` : entrées (`*Dto`). `ViewModels/` : sorties (`*ViewModel`).
- `Data/AppDbContext.cs` : DbSets + relations en Fluent API. `Migrations/` : migrations EF (SQL Server uniquement).
- `Jobs/` (Quartz, rappel 21h) et `Hubs/` (SignalR `/notificationHub`).

## Couches (cible pour tout nouveau code)
- **Contrôleur mince** : validation d'entrée, appel du service, mapping vers la réponse HTTP.
- **Service** : logique métier et requêtes EF, toujours scopées par carnet. Enregistré en `AddScoped` dans `Program.cs`.
- Pas de repository générique au-dessus d'EF : n'extraire une classe d'accès aux données que si des requêtes sont partagées entre services.
- **Entrée en DTO, sortie en ViewModel** : ne pas binder ni retourner d'entité EF dans le nouveau code (évite le sur-postage
  de `Id`, `CarnetSanteId`, `PhotoUrl`…). L'existant retourne encore des entités : ne pas étendre ce pattern.

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

```csharp
[HttpPut("{id:int}")]
public async Task<IActionResult> Put(int id, DonneesXxxDto dto, CancellationToken ct)
{
    var existing = await context.DonneesXxx.FindAsync([id], ct);
    if (existing is null) return NotFound();

    var securityCheck = await this.ValidateCarnetAccess(carnetSanteService, existing.CarnetSanteId);
    if (securityCheck != null) return securityCheck;

    // Copier uniquement les champs modifiables ; jamais Entry(dto).State = Modified,
    // jamais de changement de CarnetSanteId.
    existing.Intensite = dto.Intensite;
    existing.Commentaire = dto.Commentaire;

    await context.SaveChangesAsync(ct);
    carnetSanteService.InvalidateCache(existing.CarnetSanteId);
    return NoContent();
}
```
- GET par id : charger, `NotFound()` si absent, **puis** vérifier le carnet.
- POST : vérifier le `CarnetSanteId` reçu **et** les clés étrangères (ex. `MedicamentId` doit appartenir au même carnet).
- Refus d'accès : `Forbid()` **sans argument** (son paramètre est un nom de schéma d'authentification ; passer un message
  provoque une erreur 500).

## Erreurs et réponses
- `BadRequest(new { message = "Message en français" })` : format lu par `useDialogForm.getErrorDescription` côté client.
- `NotFound()`, `NoContent()` après PUT/DELETE, `CreatedAtAction` après POST, `ValidationProblem` pour la validation de modèle.
- Jamais `ex.Message` ni stack trace dans une réponse : logger l'exception, renvoyer un message générique.
- La sérialisation JSON utilise `ReferenceHandler.Preserve` : les listes arrivent côté client sous `{ $values: [...] }`.
  Ne pas changer ce réglage sans migrer tout le client.

## Logs
- `ILogger<T>` avec templates structurés (`"… {SymptomeId}"`), jamais de concaténation ni de `Console.WriteLine`.
- Interdits dans les logs : email, nom d'utilisateur, token, code OAuth, contenu d'une donnée de santé. Les identifiants numériques suffisent.

## Sécurité transverse
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
- **Générer une migration sans lire les secrets** : l'outil EF démarre l'hôte, qui exige `appsettings.{Environment}.json`.
  Plutôt que de lire ou copier `appsettings.Development.json` (interdit), créer temporairement dans `MonEndoVue.Server/`
  un `appsettings.DesignTime.json` contenant uniquement une chaîne de connexion factice (fichier ignoré par git, l'écrire
  avec node pour un JSON valide), lancer `ASPNETCORE_ENVIRONMENT=DesignTime dotnet ef migrations add Nom`, puis le supprimer.
  Aucune connexion à une base n'est nécessaire pour `migrations add`.

## Configuration
Chargée depuis `appsettings.{Environment}.json` (**obligatoire**, non versionné), variables d'environnement puis user-secrets.
Clés attendues (noms seulement) : `ConnectionStrings:DefaultConnection`, `AzureBlobStorage:ConnectionString`,
`AzureBlobStorage:ContainerName` (ou variables `AZURE_STORAGE_CONNECTION_STRING`/`AZURE_CONTAINER_NAME`),
`Authentication:Schemes:Bearer:{Secret,ValidIssuer,ValidAudiences}`, `Jwt:Key`, `RootUser:{UserName,Email,Password}`,
`OneSignal:ApiKey`, `GoogleApi:{ClientId,ClientSecret}`. Ne jamais lire ni afficher les valeurs.

## Tests
Projet `MonEndoVue.Server.Tests` (xUnit, **net8.0** comme la CI et le Dockerfile), lancé par `dotnet test` et par la CI.
- Règles métier pures (validateurs statiques, ex. `BilanTransitValidator`) : tests unitaires dans `Services/`, nommés
  `Methode_Situation_ResultatAttendu` en français, `[Theory]` pour les cas limites.
- Contrôleurs et services : tests avec EF InMemory via `Support/CarnetDeTest.cs` (base isolée + utilisatrice connectée et
  son carnet + une autre utilisatrice et son carnet `AutreCarnetSanteId` ; `ContexteAuthentifie()` / `ContexteAnonyme()`
  pour instancier un contrôleur). Modèles : `Controllers/BilanQuotidienControllerTransitTests.cs`,
  `Services/CarnetSanteServiceExportTests.cs`.
- **Cloisonnement** : tout contrôleur de données du carnet a son fichier `Controllers/<Controleur>CloisonnementTests.cs`
  (GET/PUT : `NotFound` si absent, `Forbid` sur le carnet de l'autre sans rien modifier, copie des champs sans changer de
  carnet ; clés étrangères d'un autre carnet refusées). Un nouvel endpoint ou champ modifiable s'y ajoute.
- **Quality gate SonarCloud (plan gratuit, non modifiable) : 80 % de couverture sur le nouveau code.** La CI envoie la couverture
  (coverlet, OpenCover) ; migrations et client sont exclus. Donc **toute ligne C# ajoutée ou modifiée hors migration doit être
  exécutée par un test**, sinon le check Sonar de la PR échoue. Vérifier en local :
  `dotnet test --collect:"XPlat Code Coverage;Format=opencover"`.
- Prochaine étape : tests d'intégration avec `WebApplicationFactory` (routage, `[Authorize]`, code HTTP réel des refus).
