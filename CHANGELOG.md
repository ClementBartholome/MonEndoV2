# Journal des versions

Toutes les évolutions notables de MonEndo. Format inspiré de [Keep a Changelog](https://keepachangelog.com/fr/1.1.0/),
versions en [SemVer](https://semver.org/lang/fr/) (voir la section Déploiement de `CLAUDE.md`).

## [1.5.0] - non publiée

### Ajouté
- Bilan quotidien : de nouvelles catégories **facultatives** à compléter si tu le souhaites (urinaire, suite du transit, saignements hors règles,
  nuit et journée, rapports). Les données sont prêtes côté serveur (enregistrement, historique, export, synthèse du PDF) ; l'écran de saisie
  suit dans la même version.
- Paramètres › **Catégories du bilan** : tu choisis ce qui apparaît dans ton bilan quotidien (un interrupteur par catégorie, réglage gardé sur
  ton appareil). Masquer une catégorie n'efface rien : tes réponses déjà enregistrées restent dans l'historique et dans ton export.
- Règles : tu peux préciser le **flux** de chaque jour de règles (traces, léger, moyen, abondant) et noter s'il y a eu des **caillots**. C'est
  facultatif, enregistré dès que tu touches un choix, et le calendrier montre le flux par des points. Ces précisions apparaissent dans ton
  export de données et dans la synthèse pour ton rendez-vous (nombre de jours par niveau, jamais d'interprétation).

### Corrigé
- Se connecter sur un appareil ne déconnecte plus les autres : chaque appareil (téléphone, ordinateur…) a sa propre session, qui se
  renouvelle seule. Si l'application est coupée au moment d'un renouvellement, elle retrouve sa session au lieu de te redemander
  ton mot de passe.

### Sécurité
- Les sessions des appareils sont stockées sous forme d'empreinte (plus aucun jeton en clair en base) et un changement de mot de passe
  déconnecte les autres appareils, pas celui que tu utilises.
- Changer son mot de passe ou supprimer son compte compte désormais les mots de passe erronés comme à la connexion : après plusieurs
  essais ratés, la fonction est bloquée quelques minutes, y compris avec le bon mot de passe.
- Les photos sont reconnues d'après leur contenu réel et non d'après leur nom ou le type annoncé par l'appareil : un fichier qui n'est pas une
  image (JPEG, PNG, WebP ou HEIC) est refusé.
- Les dates des prises et des séances de soin doivent être plausibles (ni avant 2000, ni après demain).
- L'authentification n'accepte plus les adresses locales de développement en production, la clé de signature des sessions est lue à un seul
  endroit et les échecs d'authentification ordinaires ne sont plus journalisés comme des erreurs.

## [1.4.0] - 2026-10-03

### Ajouté
- Accueil des nouvelles utilisatrices : une page de bienvenue (« Ta douleur, noir sur blanc. ») pour une première visite, une inscription
  repensée (les engagements sur tes données avant la case de consentement, règles du mot de passe cochées pendant la saisie, œil pour
  l'afficher, messages d'erreur clairs et au bon endroit), puis une étape facultative pour activer le rappel du bilan du soir (sur iPhone,
  elle explique d'abord l'installation sur l'écran d'accueil) et une carte « Pour bien démarrer » sur l'accueil, que l'on peut fermer.
- Page « Une suggestion ? Un bug ? » (menu « Plus » : « Suggestion ou bug ») : deux boutons qui préparent un e-mail à
  l'éditeur, avec l'objet, la page d'où tu viens, la version de MonEndo et ton appareil déjà indiqués, et des conseils
  pour décrire ce qui s'est passé. L'adresse est aussi affichée, avec un bouton « Copier », si ton appareil n'a pas de
  messagerie. Rien de ton suivi n'est joint ; la page rappelle qu'une capture d'écran peut montrer tes données.
- Page « S'informer sur l'endométriose » (menu « Plus », et lien dans la carte « Pour bien démarrer ») : quelques repères validants et
  des liens classés vers des sources publiques fiables (Ameli, Santé.fr, HAS, OMS, associations) pour comprendre la maladie, parler de
  sa douleur en consultation et connaître ses droits. Les liens s'ouvrent dans un nouvel onglet, sans rien transmettre de ton suivi.

- Agenda Google : depuis les Paramètres, tu peux lier ton compte Google (connexion Google, sans mot de passe à donner à MonEndo)
  puis choisir **le calendrier à afficher**, par exemple un calendrier réservé à tes rendez-vous médicaux. MonEndo ne lit que ce
  calendrier, en lecture seule ; ton prochain rendez-vous apparaît sur l'accueil et l'ensemble sur la page Agenda. Tu peux changer de
  calendrier ou délier ton agenda à tout moment (l'accès est alors retiré chez Google). Google affiche d'abord un écran « application
  non validée » : l'application l'explique avant de t'y envoyer.

- Préparer un rendez-vous depuis l'agenda : un bouton « Préparer ce rendez-vous » (sur le prochain rendez-vous et dans le détail de
  chacun) ouvre l'export du PDF avec la période réglée du rendez-vous précédent jusqu'à aujourd'hui, le rappel du rendez-vous visé et tes
  questions prêtes à être saisies. Rien du rendez-vous n'apparaît dans l'adresse de la page.

### Modifié
- Page de connexion refaite dans le même style ; les pages s'ouvrent désormais en haut de l'écran au lieu de garder le défilement de la précédente.
- Page Agenda repensée : la liste des rendez-vous à venir, regroupés par jour (demain, cette semaine, plus tard), remplace le calendrier
  d'origine ; chaque rendez-vous s'ouvre dans un panneau (lieu, itinéraire, lien vers Google Agenda). Le calendrier du mois, plus lisible
  sur téléphone, devient une vue secondaire. L'Agenda a sa place dans le menu « Plus » et l'accueil y renvoie. Des messages clairs
  expliquent quand l'agenda n'est pas lié, qu'aucun calendrier n'est choisi, qu'il n'y a rien à venir ou que Google ne répond pas.
- Politique de confidentialité mise à jour (1er octobre 2026) : elle explique ce que devient un e-mail envoyé à
  l'éditeur (conservé au plus 12 mois, utilisé seulement pour te répondre).

### Corrigé
- Actualiser la page Cycle ou la page Activité affichait un message technique à la place de la page.
- Après une mise à jour de MonEndo, une indisponibilité de quelques secondes ne déconnecte plus : la session est
  conservée et l'application réessaie. Si la connexion échoue parce que le service est momentanément indisponible,
  le message ne met plus en cause l'email ou le mot de passe.

### Modifié
- Nettoyage interne : retrait d'anciennes routes de l'API qui n'étaient plus utilisées.

## [1.3.0] - 2026-09-30

### Ajouté
- Nouvel accueil « Aujourd'hui » : jour de règles ou du cycle, bilan du jour (à faire ou son résumé), ajout rapide
  d'une douleur, des règles ou d'un symptôme, prises de traitement prévues ce jour-là, notées en un geste, prochain
  rendez-vous et quelques faits de la semaine, sans interprétation.
- Consentement aux données de santé : une case (jamais cochée d'avance) à l'inscription ; les comptes existants
  donnent leur accord à leur prochaine visite, avant de retrouver leur suivi.
- Paramètres, « Mes données » : téléchargement de tout ce qui a été noté dans MonEndo (fichier lisible et photos de
  suivi, dans une archive ZIP).
- Paramètres, « Mes données » : suppression définitive du compte et de toutes ses données (photos comprises), confirmée
  par le mot de passe ; aussi proposée sur la page d'accord, pour qui ne souhaite pas le donner.
- Politique de confidentialité et mentions légales, lisibles sans compte depuis la connexion, l'inscription et les
  Paramètres : quelles données, pourquoi, qui y accède, combien de temps, et comment exercer ses droits.
- Traitements, à la manière de l'app Santé d'Apple : chaque médicament a sa fréquence (tous les jours, certains jours
  de la semaine, tous les N jours ou au besoin) et ses horaires ; la page et l'accueil ne montrent que les prises prévues
  aujourd'hui, regroupées par moment, à noter « Pris » ou « Ignorer » (et à annuler en cas d'erreur). Les traitements au
  besoin et les soins (kiné, ostéo…) se notent en un geste ; les traitements terminés restent consultables.
- Historique de chaque traitement, mois par mois : prises faites sur les prévues, ignorées, et le même compte le mois
  précédent ; une prise ou une séance notée par erreur se retire depuis la liste.
- Historique des cycles : durée moyenne du cycle et des règles, écart entre le plus court et le plus long, et chaque cycle
  regroupé par année avec une barre qui montre les jours de règles et les jours de douleur forte (aucune prédiction).

- Préparer un rendez-vous, refait : choix de la période (1 mois, 3 mois ou dates libres, un an au plus) et des rubriques,
  champ « Mes questions » placé en première page. Le PDF commence par une synthèse lisible en deux minutes (règles,
  douleurs par type avec leurs jours de règles, traitements, bilans, activité), puis un tableau jour par jour par mois
  et les notes des bilans. Uniquement ce que tu as noté, sans interprétation.

- Paramètres, refaits : les réglages sont regroupés en lignes (rappels, suivi, compte, mes données) et chacune s'ouvre
  dans un panneau ; le même panneau sert à changer de mot de passe ou à supprimer son compte.
- Transit : l'ancien suivi reste consultable, mois par mois, sans nouvelle saisie (le transit se note dans le bilan
  quotidien) ; une entrée peut toujours être supprimée.

### Corrigé
- Historique des cycles : les jours de douleur forte d'un cycle s'affichent correctement (des repères en trop pouvaient
  apparaître).
- Changement de mois rapide : seule la dernière réponse s'affiche (plus de mois mélangés sous un mauvais titre).
- Modifier un ancien traitement arrêté ne le remet plus en cours ; le jour où un traitement est arrêté, ses prises prévues
  restent visibles.
- Téléchargement de mes données : le fichier n'est plus annulé sur iPhone. PDF de rendez-vous : pas de page vide pour un
  mois sans donnée, accords au singulier.
- Graphiques du bilan lisibles sur un mois complet : un graphique en barres par indicateur (douleur, fatigue, stress,
  émotions difficiles) au lieu de courbes superposées.

### Modifié
- Cycle : page repensée pour le téléphone. Onglet Règles : le cycle en cours, un calendrier où un toucher ajoute ou
  retire un jour de règles, et l'historique des derniers cycles avec leur durée moyenne (sans aucune prédiction).
  Onglet Symptômes : les symptômes du mois regroupés par jour et une saisie rapide depuis le bas de l'écran, aussi
  depuis la tuile « Symptôme » de l'accueil.
- Les mois se choisissent aussi directement (touche le nom du mois) pour revenir vite loin en arrière, sur les pages
  Douleurs, Cycle et Bilan ; l'historique des cycles et les photos d'acné plus anciennes s'affichent à la demande, pour que
  les pages restent rapides au fil des années.
- Le rappel hebdomadaire de la photo d'acné n'est envoyé que pendant un épisode en cours.
- Suivi de l'acné repensé : plus besoin de la noter chaque jour. Un épisode commence (« L'acné revient ») et dure
  jusqu'à « Ça s'est calmé » ; la photo de la semaine, la comparaison avant / après sur 1, 3 ou 6 mois et l'historique
  des épisodes restent sur le même onglet. Les jours déjà notés sont regroupés en épisodes, sans rien perdre.
- Douleurs : page repensée pour le téléphone, avec les chiffres du mois, un graphique jour par jour qui montre les
  règles, les douleurs regroupées par jour et une saisie rapide (type, intensité, moment) qui s'ouvre depuis le bas de
  l'écran, aussi depuis la tuile « Douleur » de l'accueil ; modification et suppression depuis la même fenêtre.
- Boutons principaux d'un rose plus doux, icônes bien centrées, plus d'air en haut des pages (le bouton « Revenir en
  arrière » disparaît : la navigation est toujours à portée) ; une prise de traitement notée depuis l'accueil est confirmée
  par un message.
- Navigation repensée sur mobile : Accueil, Bilan, Douleurs et Cycle en bas de l'écran, et un menu « Plus » pour
  les traitements, l'activité, la préparation d'un rendez-vous, le transit et les paramètres (avec la déconnexion) ;
  sur ordinateur, toutes les rubriques dans la barre latérale.
- Couleurs harmonisées et plus lisibles : une teinte par rubrique, textes et liens plus contrastés, champs de
  formulaire mieux délimités, intensité de la douleur sur une seule teinte (plus foncé = plus fort).
- Activité : page repensée pour le téléphone, avec les séances du mois par jour et une saisie en quelques touches
  (type, durée, intensité douce, modérée ou soutenue, et l'effet sur la douleur). Les anciennes intensités de 1 à 10 sont
  reprises sur trois niveaux.
- Formes et états harmonisés : mêmes arrondis pour tous les boutons et champs, pour toutes les cartes ; ce qui est fait
  (prise notée, bilan rempli) s'affiche en texte avec une coche verte et ne ressemble plus à un bouton, et le bouton de
  prise dit « Je l'ai pris ».
- Les icônes gardent leur taille pendant le chargement de la page (plus de décalage ni de défilement horizontal).
- Douleur, émotions et indicateurs du bilan gardent la même couleur d'un écran à l'autre ; l'échelle de douleur du bilan
  a le même liseré de couleur que dans les autres saisies ; les actions se nomment par un verbe (« Noter une prise »,
  « Noter une séance ») ; le focus au clavier est visible partout.
- Bilan : la page s'ouvre sur l'historique, avec « Remplir le bilan de ce jour » ; la saisie s'ouvre directement depuis
  « Faire mon bilan » de l'accueil et depuis le rappel du soir.
- Navigation : la rubrique en cours est marquée d'un fond neutre, le rose est réservé aux boutons d'action.
- Sur ordinateur, les textes des nouvelles pages grandissent ensemble (les titres restent plus grands que le reste).
- Lisibilité et accessibilité, après une revue d'ensemble : dans les graphiques, les jours de règles sont un repère sous
  l'axe (ils ne se confondent plus avec une barre) et les barres gardent leur pleine couleur ; le calendrier des émotions
  passe au violet des émotions, avec des chiffres bien contrastés ; les choix d'un formulaire ont un bord visible ;
  onglets, liens et jours du calendrier sont plus faciles à toucher.

### Sécurité
- Les photos de suivi de l'acné ne sont plus lues par un lien direct vers le stockage : MonEndo les sert lui-même, à la
  seule personne connectée qui les a prises.
- La limite de requêtes est maintenant propre à chaque utilisatrice (ou à chaque adresse pour une connexion), et non plus
  partagée par toute l'application.
- La déconnexion invalide aussi le jeton de renouvellement de la session.
- Les jetons de connexion ne sont plus renvoyés dans les réponses de l'API : ils ne vivent que dans des cookies HttpOnly,
  hors de portée des scripts de la page. Un point d'accès inutilisé qui recevait ces jetons est supprimé.
- Les polices et les icônes sont servies par MonEndo lui-même : plus aucune page ne contacte Google Fonts, qui
  recevait jusqu'ici l'adresse IP à chaque visite.
- Un compte sans aucune connexion pendant 2 ans est supprimé automatiquement, avec toutes ses données.
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
