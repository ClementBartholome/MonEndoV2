---
name: capitaliser
description: En fin de tâche MonEndo (ou quand l'utilisateur le demande), identifier ce qui a été appris — décision produit, convention, piège d'outillage, correction ou préférence de l'utilisateur, information répétée — et le reporter au bon endroit (CLAUDE.md, skill, mémoire) pour ne jamais avoir à le redemander. À utiliser après toute tâche significative, après une correction de l'utilisateur, ou quand on demande de « retenir », « noter » ou « mettre à jour les instructions ».
---

# Capitaliser

But : l'utilisateur ne doit jamais répéter une consigne, ni Claude refaire la même erreur.

## 1. Faire l'inventaire de la tâche
Relire l'échange et lister, sans filtrer au début :
- corrections ou préférences exprimées par l'utilisateur (« n'ajoute plus… », « je préfère… ») ;
- décisions produit ou d'architecture tranchées (et leur raison) ;
- pièges rencontrés (commande qui échoue, outil manquant, contournement trouvé) ;
- informations fournies par l'utilisateur qui resserviront (infrastructure, contexte métier, comptes, usages réels) ;
- instructions existantes qui se sont révélées fausses, incomplètes ou contradictoires.

## 2. Choisir la destination
| Nature | Destination |
|---|---|
| Préférence ou contexte personnel de l'utilisateur | Mémoire (`memory/`, un fichier par fait + ligne dans `MEMORY.md`) |
| Règle projet, décision d'architecture, fait sur l'infra | `CLAUDE.md` racine |
| Convention propre au serveur ou au client | `MonEndoVue.Server/CLAUDE.md` ou `monendovue.client/CLAUDE.md` |
| Piège d'environnement (Windows, Git Bash, outils) | Section « Pièges connus » du `CLAUDE.md` racine |
| Procédure qui se répète | Skill existant à compléter, sinon nouveau skill `.claude/skills/<nom>/SKILL.md` |
| Roadmap, backlog, décision datée | `docs/modernization-plan.md` |
| Faille ou secret | `docs/private/` uniquement (jamais dans un fichier versionné) |

Ne pas capitaliser : ce qui est déjà déductible du code ou de l'historique git, les détails ponctuels sans lendemain.

## 3. Écrire
- Mettre à jour l'existant plutôt qu'ajouter un doublon ; supprimer ce qui est devenu faux.
- Formuler en règle actionnable, avec le pourquoi en une ligne quand il n'est pas évident.
- Écrire le Markdown avec les outils d'édition de fichiers (pas via bash : backticks interprétés).
- Garder le `CLAUDE.md` racine sous ~200 lignes : déplacer le détail dans un fichier de couche ou un skill.
- Mémoire : frontmatter `name` / `description` / `metadata.type`, liens `[[autre-memoire]]`, pas de contenu versionnable.

## 4. Livrer
- Fichiers du dépôt : sur la branche de la tâche (commit séparé `docs(claude): …`) ou sur une branche dédiée, jamais sur `main`.
- Terminer la réponse par une ligne « Capitalisé : … » listant ce qui a été ajouté ou corrigé et où.
