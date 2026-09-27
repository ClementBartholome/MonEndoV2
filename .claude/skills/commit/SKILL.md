---
name: commit
description: Préparer et créer un commit MonEndo conforme — vérification du diff, découpage des sujets mélangés, recherche de secrets/PII/console.log/any, build selon la zone touchée, message `type(perimetre): message` en français sans trailer d'attribution. À utiliser quand l'utilisateur demande de commiter, ou avant de proposer un commit.
---

# Commit MonEndo

## 1. Examiner
- `git status` et `git diff` (plus `git diff --staged`). Ne jamais inclure des fichiers que l'utilisatrice n'a pas demandé de commiter.
- **Un commit = un sujet.** Si le diff mélange fonctionnalité, refacto, nettoyage ou plusieurs domaines, proposer un
  découpage (`git add <fichiers>` ou par hunks) avant d'aller plus loin.

## 2. Contrôler le contenu ajouté
Sur les lignes ajoutées uniquement (`git diff --staged -U0 | grep '^+'`) :
- secrets ou identifiants (clés, mots de passe, tokens, chaînes de connexion, emails réels) → **bloquant** ;
- fichiers qui ne doivent pas être versionnés (`appsettings*.json`, `.env*`, `keys/`, `serviceAccountKey.json`, logs, `bin/`, `obj/`, `dist/`) → **bloquant** ;
- `console.log`, `debugger`, nouveaux `any`, `v-html`, `TODO` sans contexte → à signaler ;
- description d'une faille non corrigée (dépôt public) → **bloquant**, la déplacer dans `docs/private/`.

## 3. Vérifier selon la zone touchée
- `monendovue.client/` : `npm run build` (type-check + build), `npm run test:e2e` si un écran est touché, et `npx eslint <fichiers modifiés>`.
- `MonEndoVue.Server/` : `dotnet build -c Release` ; si une migration est incluse, la relire (appliquée automatiquement en production).
- Contrat C# modifié : le type TS correspondant doit être mis à jour dans le même commit.
- Signaler tout échec au lieu de le contourner (pas de `--no-verify`).

## 4. Rédiger le message
- Format : `type(perimetre): message court` — **en français**, sans point final, ~72 caractères max pour la première ligne.
- Types : `feat`, `fix`, `refactor`, `style`, `docs`, `test`, `chore`, `ci`, `build`, `perf`.
- Périmètre en kebab-case : dossier de feature (`cycle`, `douleurs`, `medicament`, `bilan-quotidien`, `transit`, `activite`,
  `auth`, `parametres`, `export`, `schedule`) ou transverse (`securite`, `front`, `back`, `serveur`, `ci`, `docker`, `deps`, `docs`).
- Corps facultatif : puces courtes expliquant le pourquoi et les points d'attention (migration destructive, changement de comportement).
- **Aucun trailer `Co-Authored-By` ni mention de Claude.**

## 5. Valider avec l'utilisatrice
- Présenter les fichiers inclus et le message, puis commiter après accord.
- **Ne jamais pousser** sans demande explicite : un push sur `main` déclenche le déploiement en production.
  Proposer plutôt une branche et une PR (la CI construit alors l'image sans déployer).
