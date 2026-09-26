---
name: revue-pr
description: Évaluer une pull request MonEndo existante et dire si elle peut être mergée — conflits avec main, fichiers parasites commités, migrations EF (destructives ? cohérentes avec le modèle ?), cohérence front/back du contrat, conformité aux conventions, tests, impact sur le déploiement en production. À utiliser quand l'utilisateur demande de regarder, relire, reviewer ou évaluer une PR, ou avant tout merge.
---

# Revue de PR MonEndo

Rappel : **merger sur `main` = déployer en production** (migrations EF appliquées au démarrage). La question à trancher
est toujours « qu'est-ce qui casse ou se perd en prod si on merge maintenant ? ».

## 1. Récupérer la PR sans toucher à la copie de travail
- `gh` n'est pas authentifié : métadonnées via l'API publique
  `curl -s https://api.github.com/repos/ClementBartholome/MonEndoV2/pulls/<n>` (titre, description, `mergeable`, `mergeable_state`,
  nombre de fichiers) et l'issue liée (`/issues/<n>`) pour le besoin réel.
- `git fetch origin main pull/<n>/head:pr-<n>` puis `git log --oneline origin/main..pr-<n>` et `git diff --stat $(git merge-base origin/main pr-<n>) pr-<n>`.
- Pour construire ou tester : worktree temporaire dans le scratchpad (`git -c core.longpaths=true worktree add --detach <dossier> pr-<n>`),
  supprimé à la fin. Ne jamais faire de checkout dans la copie principale (d'autres sessions peuvent y travailler).

## 2. Points de contrôle
**Hygiène**
- Conflits avec `main` (`mergeable: false` ou base ancienne) : lister les fichiers en conflit probable.
- Fichiers parasites : `obj/`, `bin/`, `dist/`, `packages/`, `*.user`, secrets, logs. Le volume de lignes est souvent gonflé par eux : raisonner sur le diff utile.
- Retours d'éléments abandonnés : attributs TypeGen (`[ExportTsInterface]`, `[TsOptional]`), code PWA/offline, Firebase.
- Modification de `.github/workflows/ci.yml` ou du `Dockerfile` : cohérente avec le pipeline actuel ?

**Base de données**
- Migration relue ligne à ligne : opérations destructives (`DropColumn`, `DropTable`, `AlterColumn` restrictif), conversions de
  données (`Sql(...)`) correctes sur le fond, `Down` réaliste.
- Cohérence modèle ↔ snapshot ↔ migration : une propriété retirée du modèle mais dont la colonne `NOT NULL` reste en base fait
  échouer toutes les insertions.
- Valeurs existantes compatibles avec les nouvelles règles de validation (ex. anciennes intensités).

**Contrat front/back**
- Chaque champ renommé ou supprimé côté C# : chercher ses usages côté client (`grep -rn "<champ>" monendovue.client/src`),
  y compris accueil (`Carnet.vue`) et export PDF (`ExportPdfPage.vue`). Un backend livré sans son front casse la prod.
- Types TS mis à jour à la main dans le même changement.

**Qualité et sécurité**
- Conventions de `CLAUDE.md` (couches, DTO, erreurs `{ message }`, logs, `any`, taille des composants, mobile-first).
- Cloisonnement par carnet sur tout endpoint touché (skill `revue-securite` si auth, endpoint ou upload).
- Tests : présents pour les règles métier, projet de tests en **net8.0**, exécutés par `dotnet test`.

**Besoin**
- Relire l'issue : la solution répond-elle au besoin exprimé (et pas à une interprétation) ? Signaler les écarts de conception.

## 3. Vérifier concrètement
Dans le worktree temporaire : `dotnet build -c Release`, `dotnet test`, `npm ci && npm run build` si le client est touché.

## 4. Verdict
- Réponse claire en tête : **merger / merger après corrections / ne pas merger**.
- **Bloquants** (casse ou perte de données en prod), **à corriger**, **questions produit** ; chaque point avec fichier:ligne et scénario concret.
- Proposer la suite : corrections sur la branche existante, ou reprise sur une branche propre depuis `main` quand la PR est
  trop en retard ou repose sur une mauvaise conception (ce fut le cas de la PR #4, reprise dans `feat/bilan-transit`).
- Ne rien publier sur GitHub au nom de l'utilisateur ; lui fournir le texte si une revue écrite est utile.
