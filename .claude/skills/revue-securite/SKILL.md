---
name: revue-securite
description: Revue de sécurité ciblée MonEndo (application de santé, dépôt public) sur un diff, une branche ou une zone de code — cloisonnement par carnet (IDOR), authentification et cookies, secrets VITE_ et fichiers, logs et données personnelles, XSS, uploads, CORS et en-têtes, dépendances. À utiliser avant de commiter un changement qui touche l'auth, un endpoint, un upload ou des données de santé, ou quand on demande un audit sécurité.
---

# Revue de sécurité MonEndo

Contexte : données de santé (RGPD art. 9), **dépôt GitHub public**, déploiement automatique en production à chaque push sur `main`.
Complète le skill générique `/security-review` avec les points propres au projet.

## 1. Périmètre
- Par défaut : `git diff main...HEAD` + modifications non commitées. Sinon, la zone demandée.
- Lister les fichiers touchés par catégorie : contrôleurs/services, modèles/migrations, auth, client (pages, services), config/CI/Docker.

## 2. Points de contrôle
**Cloisonnement (priorité 1)**
- Chaque action qui lit ou écrit une donnée vérifie le carnet via `ValidateCarnetAccess`.
- PUT/DELETE/GET par id : vérification sur l'entité **chargée en base**, pas sur le body ; pas de `Entry(x).State = Modified` sur un objet reçu.
- Clés étrangères reçues (ex. `MedicamentId`) contrôlées comme appartenant au même carnet.
- Aucune requête sans filtre `CarnetSanteId` sur des données de carnet.

**Authentification et autorisation**
- `[Authorize]` présent ; tout `[AllowAnonymous]`, hub SignalR ou endpoint technique justifié.
- Aucune action « admin » accessible à une utilisatrice ordinaire (changement de mot de passe d'autrui, envoi de notifications…).
- Mots de passe et tokens jamais en query string ni dans une URL de redirection ; cookies `HttpOnly`, `Secure`, `SameSite`.
- Rate limiting `auth` sur les endpoints d'authentification.

**Secrets et données personnelles**
- Aucun secret dans le code, les tests, `docker-compose*.yml`, la CI ni une variable `VITE_*` (publique dans le bundle).
- Aucun email, token, identifiant de test ou donnée de santé dans les logs, les messages d'erreur renvoyés ou les commits.
- `localStorage`/IndexedDB sans token ni donnée de santé.

**Front**
- Pas de `v-html`, pas de rendu HTML de texte saisi (DataTables, tooltips de graphiques).
- Pas de nouvelle URL construite avec des données sensibles.

**Uploads et fichiers**
- Taille et type validés côté serveur ; nom de blob généré par le serveur ; jamais de suppression à partir d'une URL fournie par le client.
- Photos de santé non accessibles publiquement sans contrôle.

**Configuration et infrastructure**
- CORS : pas d'origine de développement ajoutée pour la production, pas de wildcard avec credentials.
- En-têtes de sécurité conservés ; `/health` sans information sensible.
- Migrations : pas d'opération destructive non signalée (appliquées automatiquement en production).
- Dépendances ajoutées : maintenues, sans préversion en production ; `npm audit` / avertissements NuGet consultés.

## 3. Restitution
- Classer chaque constat : **P0** (exploitable, données d'autrui ou compte), **P1** (affaiblissement sérieux), **P2** (hygiène).
- Pour chaque constat : fichier:ligne, scénario d'exploitation concret, correctif proposé.
- **Les constats exploitables non corrigés vont uniquement dans `docs/private/audit-securite.md`** (ignoré par git) et dans
  la réponse à l'utilisateur. Jamais dans un fichier versionné, un message de commit, une PR ou une issue publique.
- Un correctif de faille se commite avec un message neutre (`fix(securite): renforce le contrôle d'accès des mises à jour`)
  sans décrire l'exploitation.
