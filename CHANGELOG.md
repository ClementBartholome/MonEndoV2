# Journal des versions

Toutes les évolutions notables de MonEndo. Format inspiré de [Keep a Changelog](https://keepachangelog.com/fr/1.1.0/),
versions en [SemVer](https://semver.org/lang/fr/) (voir la section Déploiement de `CLAUDE.md`).

## [1.0.0] - non publiée

Première version numérotée de l'application.

### Ajouté
- Numéro de version affiché en bas de la page Paramètres.

### Modifié

### Sécurité
- Les identifiants de connexion et de réinitialisation du mot de passe sont transmis uniquement dans le corps des requêtes.
- Limitation du nombre de tentatives sur les points d'accès d'authentification.
