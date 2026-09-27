---
name: release
description: Préparer, livrer et, si besoin, annuler une version numérotée de MonEndo (SemVer) — branche release/X.Y.Z, intégration des sujets par PR, numéro de version, CHANGELOG, PR vers main (= déploiement), tag vX.Y.Z, GitHub Release, contrôle de /health, rollback par tag d'image. À utiliser quand l'utilisateur parle de release, de version, de livraison groupée, de tag ou de hotfix.
---

# Release MonEndo

Rappel : **merger sur `main` = déployer en production** (migrations EF appliquées au démarrage).

## Choisir le numéro (SemVer)
Versionnage fin, les versions s'enchaînant vite (décision du 2026-09-27) :
- **MAJEUR** (2.0.0) : réservé à une fonctionnalité ou une refonte vraiment majeure (ex. refonte du design, liaison des
  comptes externes à grande échelle), ou à une rupture visible par l'utilisatrice (données perdues, fonctionnalité retirée).
  Une migration destructive invisible pour l'utilisatrice (table inutilisée) ne justifie pas un saut majeur : elle est
  signalée dans la PR et dans la section « Annuler ».
- **MINEUR** (1.1.0) : nouvelles fonctionnalités, améliorations, migrations (additives ou destructives invisibles).
- **CORRECTIF** (1.0.1) : correctifs seulement. Un **hotfix** part de `main` (`hotfix/X.Y.Z`), sans passer par une branche de release.
  - Sur la branche du hotfix : numéro de version (mêmes fichiers qu'une version) et section du CHANGELOG **déjà datée**
    avant la PR vers `main` (pas de commit possible sur `main` après coup).
  - Configuration du VPS exigée par le correctif : faite et vérifiée **avant** le merge (la PR le dit en tête).
  - PR vers `main` avec le même titre et la même description qu'une version (voir « Livrer », étape 2).
  - Après la livraison, merger `main` dans la `release/X.Y.Z` en cours. Conflits attendus sur les numéros de version :
    reprendre `package.json`, `package-lock.json` et le `.csproj` de `main` (dépendances à jour) puis remettre le numéro
    de la version en cours ; garder « ours » réintroduirait une dépendance retirée par le correctif. CHANGELOG : les deux sections.

## Préparer
1. Worktree dédié : `git worktree add ../MonEndoVue-release -b release/X.Y.Z origin/main`, puis `git push -u origin release/X.Y.Z`.
2. Numéro de version dans **les deux fichiers**, dans le même commit `chore(release): passe en version X.Y.Z` :
   `monendovue.client/package.json` (`version`, injecté dans l'UI par Vite via `__APP_VERSION__`) et
   `MonEndoVue.Server/MonEndoVue.Server.csproj` (`<Version>`).
3. Chaque sujet arrive par une **PR dont la base est `release/X.Y.Z`** (`gh pr create --base release/X.Y.Z`, ou
   `gh pr edit <n> --base release/X.Y.Z` pour une PR existante). La CI (verifier + image sans publication) tourne sur ces PR.
4. Chaque PR complète la section `[X.Y.Z] - non publiée` de `CHANGELOG.md` (Ajouté / Modifié / Corrigé / Sécurité),
   en français, du point de vue de l'utilisatrice, sans décrire de faille non corrigée.
5. Pour intégrer une branche en retard : merger `release/X.Y.Z` dedans (pas de rebase d'une branche déjà poussée).

## Valider
- Test local de bout en bout de la branche complète (procédure du `CLAUDE.md` racine), migrations comprises.
- Migrations de la version testées sur une **base jetable** (jamais la base de dev) :
  1. `dotnet ef database update <dernière migration de main>` avec `--connection "…Database=MonEndo_RecetteXYZ…"` ;
  2. insérer au `sqlcmd -I` des lignes représentatives des données existantes ;
  3. `dotnet ef database update`, puis vérifier les données, les nouvelles règles, `dotnet ef migrations has-pending-model-changes`
     et la requête de retour arrière de la section « Annuler » ;
  4. supprimer la base (`DROP DATABASE`).
- Revue sécurité et revue de justesse du diff `origin/main...release/X.Y.Z` : sur un gros diff, les confier à deux subagents
  en parallèle (lecture seule), puis recouper chaque constat dans le code avant de le retenir.
- Skill `revue-pr` sur la PR `release/X.Y.Z` → `main` : liste des migrations (additives ?), impact du déploiement,
  actions manuelles sur le VPS (configuration, secrets) à faire **avant** le merge.

## Livrer (uniquement sur demande explicite de l'utilisateur)
1. Dater la section du CHANGELOG (`## [X.Y.Z] - AAAA-MM-JJ`) sur la branche de release.
2. PR `release/X.Y.Z` (ou `hotfix/X.Y.Z`) → `main`, merge (commit de merge, pas de squash : l'historique des sujets est conservé).
   **Titre : `release: MonEndo X.Y.Z`** (jamais le nom de la branche proposé par défaut par GitHub). Description :
   `## MonEndo X.Y.Z` suivi de la section du CHANGELOG, précédée pour un hotfix des actions faites sur le VPS
   (ou « aucune »), puis migrations (« aucune » si c'est le cas) et impact du déploiement.
   `gh pr create --base main --title "release: MonEndo X.Y.Z" --body-file <fichier>`.
3. Attendre le job `deployer` et `https://monendoapp.fr/health` = `Healthy`.
4. Tag sur le commit de merge : `git tag -a vX.Y.Z <sha> -m "MonEndo X.Y.Z"` puis `git push origin vX.Y.Z` :
   la CI publie l'image `ghcr.io/…:vX.Y.Z` (sans redéployer).
5. `gh release create vX.Y.Z --title "MonEndo X.Y.Z" --notes-file <section du CHANGELOG>`.
6. Nettoyer : GitHub supprime seul la branche distante d'une PR mergée (réglage « delete head branches » du dépôt).
   En local, pour chaque branche intégrée à la version, vérifier `git merge-base --is-ancestor <branche> origin/main` et un
   worktree propre, puis `git worktree remove`, `git branch -D` et `git fetch --prune origin`.
   Mettre `main` à jour sans changer la branche de la copie principale : `git fetch origin main:main`.
   Ne pas toucher aux branches non mergées ni à celle de la copie principale sans accord (d'autres sessions peuvent y travailler).

## Annuler (rollback)
Sur le VPS, repointer le service `app` du `docker-compose.prod.yml` sur l'image de la version précédente
(`ghcr.io/clementbartholome/monendov2:vX.Y.Z`, ou `sha-<court>` pour une image antérieure aux versions),
puis `docker compose -f docker-compose.prod.yml up -d app`. Une migration déjà appliquée n'est pas annulée : c'est pour
cela qu'elles doivent rester rétro-compatibles. Revenir ensuite à `:latest` au déploiement suivant.

Points d'attention connus :
- **Retour à une image antérieure à la 1.0.0** : les bilans saisis depuis la 1.0.0 ont `Mood` à NULL, que l'ancienne image
  (propriété `Mood` obligatoire) ne sait pas lire. Avant de repointer l'image, exécuter sur la base de prod
  `UPDATE BilansQuotidiens SET Mood = 'Neutre' WHERE Mood IS NULL` (sans effet sur la 1.0.0 et suivantes, où les émotions
  priment sur `Mood`). **Depuis la 1.2.0, ce retour n'est plus possible tel quel** : ces images lisent la table
  `PreferencesRappel`, supprimée par `SupprimePreferencesRappel`. Le plus ancien point de retour est la 1.0.0.
- **Retour à une image antérieure à la 1.1.0** : `StressPro`, `StressPerso`, `Fatigue`, `Pas` et `Hydratation` sont
  nullables depuis la 1.1.0 (migration `RendFacultativesMesuresBilan`), l'ancienne image plante sur une valeur nulle.
  Avant de repointer l'image, exécuter sur la base de prod
  `UPDATE BilansQuotidiens SET StressPro = ISNULL(StressPro, 0), StressPerso = ISNULL(StressPerso, 0), Fatigue = ISNULL(Fatigue, 0), Pas = ISNULL(Pas, 0), Hydratation = ISNULL(Hydratation, 0)`
  (les anciennes versions comptaient de toute façon une valeur absente pour 0 ; la distinction « non renseigné » est perdue).
- Toute version qui rend un champ nullable ou supprime une table ajoute ici sa propre consigne de retour arrière.
