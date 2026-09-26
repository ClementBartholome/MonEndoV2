---
name: endpoint-api
description: Ajouter ou modifier un endpoint de l'API ASP.NET Core de MonEndo de façon sûre — cloisonnement par carnet, DTO/ViewModel, gestion d'erreurs, logs sans PII, migration EF, type TypeScript et méthode apiService côté client. À utiliser dès qu'on touche un contrôleur, un service, un modèle EF ou une migration.
---

# Endpoint API MonEndo

Lire d'abord `MonEndoVue.Server/CLAUDE.md` et la section sécurité du CLAUDE.md racine.

## Checklist de conception
1. **Qui accède ?** `[Authorize]` sur le contrôleur. Un endpoint anonyme ou réservé à un rôle doit être justifié par écrit dans la PR.
2. **À quelles données ?** Tout est rattaché à un `CarnetSante`. Identifier comment le carnet est vérifié :
   - GET liste : `carnetSanteId` en route + `ValidateCarnetAccess` avant la requête.
   - GET/PUT/DELETE par id : **charger l'entité**, `NotFound()` si absente, puis `ValidateCarnetAccess(entity.CarnetSanteId)`.
   - POST : `ValidateCarnetAccess(dto.CarnetSanteId)` + vérifier que chaque clé étrangère (ex. `MedicamentId`) appartient au même carnet.
3. **Contrat** : DTO d'entrée (`Dto/XxxDto.cs`) avec uniquement les champs modifiables ; ViewModel de sortie
   (`ViewModels/XxxViewModel.cs`). Jamais `Id`/`CarnetSanteId`/`PhotoUrl` modifiables via le body d'un PUT.
4. **Logique** dans un service (`Services/XxxService.cs`, `AddScoped` dans `Program.cs`) si elle dépasse le simple CRUD.
5. **Erreurs** : `BadRequest(new { message = "…" })` en français, `NotFound()`, `NoContent()`, `CreatedAtAction` ; jamais `ex.Message`.
6. **Logs** : `ILogger<T>` structuré, identifiants numériques seulement (pas d'email, de token ni de contenu de santé).
7. **Entrées sensibles** : jamais de mot de passe/token en query string ; endpoints d'auth avec `[EnableRateLimiting(PolitiquesDebit.Auth)]` ; identifiants lus depuis un DTO `[FromBody]`
   (modèle : `Dto/IdentifiantsDto.cs`), tests avec `Support/IdentityDeTest.cs`.
8. **Uploads** : réutiliser la validation photo de `SymptomesCycleController` et `AzureBlobStorageService`.

Gabarits : [templates.md](templates.md).

## Si le modèle EF change
1. Modifier le modèle et, si besoin, `AppDbContext.OnModelCreating` (Fluent API).
2. `dotnet ef migrations add NomEnPascalCase --project MonEndoVue.Server` (ex. `AjouteHydratationCible`).
3. **Relire la migration** : elle sera appliquée automatiquement au démarrage en production. Préférer des changements
   additifs (colonne nullable, valeur par défaut) ; signaler explicitement toute suppression de colonne/table.
4. Ne jamais modifier une migration déjà déployée.

## Côté client (même commit que le contrat C#)
1. Mettre à jour ou créer le type dans `monendovue.client/src/features/<domaine>/types/` (écrit à la main, miroir du DTO/ViewModel).
2. Ajouter une méthode typée dans `shared/services/apiService.ts` (`Promise<T>`, listes sous `$values`).

## Vérifier
- `dotnet build -c Release` sans nouvelle erreur ni nouvel avertissement, puis `dotnet test`.
- Toute règle de validation ajoutée est extraite dans une classe testable (modèle : `Services/BilanTransitValidator.cs`)
  et couverte dans `MonEndoVue.Server.Tests`.
- Tester l'endpoint en local (Swagger en Development ou via l'UI) : cas nominal, id inexistant (404), **carnet d'une autre
  utilisatrice (403)**, payload invalide (400), sans authentification (401).
- `npm run type-check` si le client est touché.
- Passer le skill `revue-securite` sur le diff.
