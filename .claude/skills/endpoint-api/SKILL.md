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
   - Ressource propre à l'utilisatrice connectée (réglages, appareils) : **aucun identifiant de carnet en entrée**, carnet déduit
     de la session (modèle : `CarnetDeAsync` dans `Services/WebPush/NotificationsService.cs`).
3. **Contrat** : DTO d'entrée (`Dto/XxxDto.cs`) avec uniquement les champs modifiables ; ViewModel de sortie
   (`ViewModels/XxxViewModel.cs`). Jamais `Id`/`CarnetSanteId`/`PhotoUrl` modifiables via le body d'un PUT.
4. **SOLID** (voir `MonEndoVue.Server/CLAUDE.md`) :
   - le contrôleur ne fait que du HTTP : pas d'`AppDbContext`, pas de requête ;
   - la logique et les requêtes vont dans un service du domaine (`Services/XxxService.cs`, `AddScoped` dans `Program.cs`),
     qui renvoie un `ResultatOperation` que le contrôleur traduit par `VersReponse` (modèle : `NotificationsService`) ;
   - la validation métier va dans une classe dédiée ;
   - tout accès extérieur passe par une interface injectée, et l'heure par `TimeProvider`.
   
   Si l'endpoint touche un contrôleur existant non conforme, le remettre d'aplomb dans un commit `refactor(…)` séparé.
5. **Erreurs** : `BadRequest(new { message = "…" })` en français, `NotFound()`, `NoContent()`, `CreatedAtAction` ; jamais `ex.Message`.
6. **Logs** : `ILogger<T>` structuré, identifiants numériques seulement (pas d'email, de token ni de contenu de santé).
7. **Entrées sensibles** : jamais de mot de passe/token en query string ; endpoints d'auth avec `[EnableRateLimiting("auth")]`.
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
