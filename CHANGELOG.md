# Journal des versions

Toutes les évolutions notables de MonEndo. Format inspiré de [Keep a Changelog](https://keepachangelog.com/fr/1.1.0/),
versions en [SemVer](https://semver.org/lang/fr/) (voir la section Déploiement de `CLAUDE.md`).

## [1.0.0] - non publiée

Première version numérotée de l'application.

### Ajouté
- Numéro de version affiché en bas de la page Paramètres.
- Rappel hebdomadaire de la photo de suivi de l'acné, au jour et à l'heure choisis, envoyé seulement si aucune photo
  d'acné n'a été ajoutée depuis 7 jours (remplace le rappel envoyé auparavant par un service externe).
- Chaque notification ouvre directement la page concernée (bilan du jour, onglet Acné du cycle).
- Émotion de la semaine dans le récapitulatif du bilan : tendance plutôt agréable, plus difficile ou en demi-teinte,
  avec les émotions les plus fréquentes.
- Ligne « Humeur » dans l'export PDF mensuel.

### Modifié
- Bilan quotidien : l'humeur (positive, neutre, négative) est remplacée par le choix de 1 à 3 émotions parmi dix
  (joie, calme, soulagement, motivation, fierté, tristesse, anxiété, irritabilité, frustration, découragement).
  Les bilans déjà saisis gardent leur humeur, toujours affichée et prise en compte dans les graphiques.
- Notes personnelles du bilan : jusqu'à 1000 caractères (au lieu de 100).
- Réglages des rappels dans Paramètres : une carte par rappel (activation, heure, jour pour un rappel hebdomadaire) ;
  le réglage existant du rappel du bilan est conservé.
- Une configuration des notifications incorrecte côté serveur désactive seulement les notifications, avec un message
  explicite dans les journaux, au lieu de bloquer la page de réglages.

### Sécurité
