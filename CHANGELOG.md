# Journal des versions

Toutes les évolutions notables de MonEndo. Format inspiré de [Keep a Changelog](https://keepachangelog.com/fr/1.1.0/),
versions en [SemVer](https://semver.org/lang/fr/) (voir la section Déploiement de `CLAUDE.md`).

## [1.3.0] - non publiée

### Ajouté
- Consentement aux données de santé : une case (jamais cochée d'avance) à l'inscription ; les comptes existants
  donnent leur accord à leur prochaine visite, avant de retrouver leur suivi.
- Politique de confidentialité et mentions légales, lisibles sans compte depuis la connexion, l'inscription et les
  Paramètres : quelles données, pourquoi, qui y accède, combien de temps, et comment exercer ses droits.

### Sécurité
- Les polices et les icônes sont servies par MonEndo lui-même : plus aucune page ne contacte Google Fonts, qui
  recevait jusqu'ici l'adresse IP à chaque visite.
- Sauvegarde chiffrée de la base chaque nuit, copiée hors du serveur (Azure, en Europe) et conservée 30 jours.

## [1.2.1] - 2026-09-27

### Sécurité
- La politique de sécurité du contenu (CSP) est désormais appliquée : le navigateur bloque toute ressource non prévue.
- Le passage de l'adresse http:// à https:// garde le nom du site, et chaque en-tête de sécurité n'est plus envoyé qu'une fois.

### Modifié
- Cycle, photos d'acné : une photo HEIC (iPhone) est convertie en JPG par le navigateur lui-même, sans bibliothèque
  supplémentaire ; sur un navigateur qui ne lit pas le HEIC, la photo est envoyée telle quelle.

## [1.2.0] - 2026-09-27

### Corrigé
- Sur ordinateur, la barre de navigation ne recouvre plus le bord gauche des pages (boutons et calendrier devenus
  inaccessibles sur les écrans de moins de 1800 pixels de large).
- Activité : une séance peut de nouveau être modifiée depuis le tableau affiché sur ordinateur.
- Tableaux (activité, douleurs, cycle, traitements, transit) : le texte saisi s'affiche toujours tel quel, sans mise en forme.

### Sécurité
- Politique de sécurité du contenu (CSP) déclarée, en mode observation : le navigateur signale toute ressource non prévue.
- Paramètres : plus aucune image chargée depuis un site tiers.
- Connexion : un compte est verrouillé pendant 15 minutes après cinq mots de passe erronés de suite.
- Les journaux techniques du serveur sont conservés 30 jours au maximum, puis supprimés automatiquement ; ils
  enregistrent moins de détails en production.

## [1.1.1] - 2026-09-27

### Modifié
- Agenda : les événements récurrents apparaissent à chaque occurrence.

### Sécurité
- Agenda : les rendez-vous sont lus par le serveur, sans clé Google dans l'application ; le bloc
  « Prochains rendez-vous » de l'accueil n'apparaît que pour un compte associé à un agenda.

## [1.1.0] - 2026-09-27

### Ajouté
- Bilan quotidien : un bilan déjà enregistré, y compris un bilan passé, peut être modifié ou complété (bouton
  « Modifier » sur le détail du jour). Quitter une saisie non enregistrée demande une confirmation.
- Bilan quotidien : bouton « Comme hier » qui reprend le transit, l'alimentation, les pas et l'hydratation de la veille.
- Bilan quotidien : historique par semaine ou par mois. Un calendrier montre chaque jour en couleur selon la douleur
  ou les émotions, avec les jours de règles et les jours sans bilan ; les courbes séparent la douleur (0 à 10) de la
  fatigue, du stress et des émotions (0 à 5) et affichent les règles en fond.

### Modifié
- Bilan quotidien rempli sur un seul écran : seules la douleur et les émotions sont nécessaires (deux touchers suffisent).
  Fatigue et stress se choisissent en pastilles ; transit, alimentation, pas et hydratation (avec des choix rapides)
  sont regroupés dans un bloc « Corps » facultatif, les notes dans un bloc à part.
- Une mesure non renseignée reste vide au lieu de valoir 0 : les moyennes, objectifs, graphiques et l'export PDF ne
  comptent que les valeurs réellement saisies.
- Un seul bilan par jour, et pas de bilan pour un jour à venir.
- Bilan quotidien : une seule navigation dans le temps pilote toute la page, onglet « Analyse & Tendances » compris
  (fin des sélecteurs de semaine séparés).
- Bilan quotidien, « Analyse & Tendances » : le score de bien-être sur 100 et ses couleurs rouge / orange / vert sont
  remplacés par des tendances neutres. Chaque indicateur affiche sa moyenne et son évolution par rapport à la semaine
  ou au mois précédent, les repères personnels indiquent le nombre de jours où ils sont atteints, et des observations
  factuelles rapprochent les jours de douleur des jours de règles.
- Paramètres : les objectifs bien-être deviennent des « repères personnels ».

### Corrigé
- Un bilan rempli pour un jour passé pouvait s'afficher la veille du jour choisi.

## [1.0.1] - 2026-09-26

### Corrigé
- Bilan quotidien : le graphique d'évolution et l'analyse affichent les bilans dans l'ordre des dates (un bilan saisi
  pour un jour passé apparaissait après les plus récents).

## [1.0.0] - 2026-09-26

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

### Corrigé
- Bilan quotidien : les options d'alimentation et le type de selles choisis sont de nouveau mis en évidence.
- Bilan quotidien : tailles d'icônes homogènes entre les étapes et les cartes du récapitulatif.

### Sécurité
- Les identifiants (connexion, inscription, changement de mot de passe) sont transmis uniquement dans le corps des requêtes.
- Limitation du nombre de tentatives sur les points d'accès d'authentification ; aucune adresse e-mail dans les journaux de connexion.
