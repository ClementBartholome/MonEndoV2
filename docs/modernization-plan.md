# MonEndo - Plan global de modernisation

## Vision produit
MonEndo devient un compagnon quotidien de sante: simple a utiliser, orientee action, et fiable en contexte reel (mobile, stress, fatigue).

Ce plan combine:
- UX/UI moderne (rapidite de saisie, lisibilite, micro-interactions utiles)
- fonctionnalites a forte valeur (adherence, prevention, insights)
- qualite engineering (separation des responsabilites, SOLID, testabilite)
- securite et privacy by design

## Principe produit non negociable
- Mobile-first: les ecrans critiques doivent etre d'abord penses pour les terminaux <= 425px.
- Priorite lisibilite sur densite: aucune information cle ne doit etre tronquee sur mobile.
- Desktop ensuite: enrichissement progressif (grilles, details, densite) sans degrader l'experience mobile.

## Convention de commit
- Format obligatoire: `type(perimetre): court message descriptif` (types: feat, fix, refactor, style, docs, test, chore, ci, build, perf)
- Messages en francais.
- Reference complete (et regles de travail des agents): `CLAUDE.md` a la racine du depot.

## Decisions (2026-09)
- PWA, cache service worker et mode hors ligne abandonnes : code retire. Seul le service worker de push (push-sw.js) est conserve.
- Generation de types TypeGen abandonnee : les types TypeScript sont maintenus a la main.
- Firebase/FCM, OneSignal, Zapier et SignalR retires ; notifications Web Push standard (VAPID) envoyees par le serveur.

## Reference de marche (patterns apps sante populaires)
Patterns repris des apps de suivi sante/cycle et chronic care:
- onboarding progressif (2 a 5 ecrans, puis personnalisation continue)
- quick logging en 1-2 taps
- routines et rappels adaptatifs (pas seulement des notifications fixes)
- vues longitudinales (semaine/mois/trimestre) et tendances explicables
- insights actionnables, non anxiogenes
- privacy by design (controle utilisateur, minimisation, transparence)

## KPI cibles (12 semaines)
- Activation J7: +20%
- Retention M1: +15%
- Entrees hebdomadaires par utilisatrice: +30%
- Temps median de saisie d'une entree: < 45s
- Taux de succes des appels API critiques: > 99%
- Crash-free sessions: > 99.5%

## Priorites (par horizon)

### Horizon 0-4 semaines (quick wins)
1. Uniformiser l'experience de saisie
   - placeholders explicites
   - presets contextuels (duree, intensite)
   - CTA d'etat vide vers action utile
2. Uniformiser l'affichage data mobile/desktop
   - cards mobiles + table desktop
   - KPI de section (volume, frequence, duree)
3. Renforcer securite baseline
   - rate limiting API/auth
   - headers securite web
   - validation JWT stricte quand config presente

### Horizon 1-3 mois (valeur clinique + architecture)
1. Insights transverses
   - correlations douleur <-> sommeil <-> activite <-> cycle
   - score de stabilite hebdo
2. Adherence intelligente
   - routines personnalisees
   - rappels adaptatifs selon habitudes
3. Refactor architecture progressive
   - Front: extraire logique metier des pages vers composables/services
   - Back: Controller -> Service -> Repository
   - DTO stricts et contrats types de bout en bout

### Horizon 3-6 mois (maturite)
1. Journal intelligent
   - suggestions de saisie proactive
   - detection de "trous" de donnees avec relance douce
2. Partage medecin ameliore
   - exports axes decision (timeline, tendances, episodes)
3. Observabilite produit
   - instrumentation events UX
   - suivi des funnels et taux d'echec sync/API

## Prochaines grosses etapes validees

1. Correlation des donnees et utilite clinique
   - croiser douleur, cycle, activite, transit, bilan quotidien
   - produire des hypotheses explicables (sans sur-promesse medicale)
   - mettre en avant des signaux utiles avant/pendant un rendez-vous medical

2. Parcours "Rendez-vous medecin"
   - renforcer l'export PDF (synthese decisionnelle, timeline, points marquants)
   - inclure une page de preparation consultation (questions, historique recent, red flags)
   - rendre l'export accessible en 1 tap depuis la homepage

3. Audit securite et hardening complet
   - revue auth/session/cookies/JWT
   - revue OWASP Top 10 (XSS, CSRF, injections, exposition de donnees)
   - revue gestion secrets, logs, permissions et dependances (CVE)

4. Redaction des tests end-to-end (Playwright)
   - couvrir les parcours critiques (auth, saisie, modification, suppression)
   - prioriser les scenarios mobile-first sur les ecrans <= 425px
   - integrer ces tests dans la CI pour securiser les regressions avant merge

5. Autres idees a fort impact
   - timeline photos acné (semaine/mois) pour suivre l'evolution de facon elegante
   - vue comparative avant/apres sur une periode selectionnee
   - filtres utiles (periode, intensite, presence de photo) pour faciliter le suivi
   - mode "resume hebdo" partageable (patient + medecin)
   - alertes intelligentes basees sur inactivite et derive des objectifs
   - personnalisation plus fine des objectifs (par section et par phase du cycle)

## Architecture cible

### Frontend (Vue)
- `features/*`: orchestration UI par domaine
- `shared/components`: composants purement visuels
- `shared/composables`: logique reutilisable UI
- `shared/services`: I/O et API
- `shared/types`: contrats stricts

Regles:
- pas de logique metier lourde dans les composants de page
- composants presentational sans effet de bord
- composables testables et idempotents
- eviter `any`, preferer types dedies

### Backend (.NET)
- Controllers: validation input + mapping I/O
- Services: logique metier
- Repositories: acces donnees
- DTO/Contracts: front-safe, versionnables

Regles:
- authorization et controle d'acces systematiques
- erreurs standardisees
- logs structures sans donnees sensibles

## Securite et privacy by design
- minimisation des donnees stockees
- hardening HTTP (headers, HTTPS strict, CORS precise)
- rate limiting anti-abus
- validations strictes input server-side
- audit trail des operations sensibles
- revue periodique des secrets et permissions

## Backlog priorise (actionnable)

### Lot A - UX impact immediate
- [x] Composant reutilisable `SectionKpiHeader` pour toutes les pages metier
- [x] Composant reutilisable `EmptyStateAction` (etat vide + CTA)
- [ ] Filtre rapide transversal (Tous / Important / Cette semaine)

### Lot B - Data presentation
- [ ] Timeline mensuelle unifiee (douleurs, symptomes, activite, traitements)
- [ ] Cartes d'insights hebdo (2-3 max, explicables)
- [ ] Comparaison glissante 4 semaines
- [x] Suivi photo acné en fenetre glissante multi-mois (comparaison visuelle au-dela du mois courant)
- [x] Humeur du bilan en emotions multiples (1 a 3 parmi 10) et emotion de la semaine (issue #2) ; anciens bilans conserves
- [x] Bilan quotidien en un ecran (douleur et emotions obligatoires, le reste facultatif et null si non renseigne,
  « Comme hier ») et modification d'un bilan passe, un seul bilan par jour (issues #11, #14)
- [x] Historique des bilans par semaine ou par mois : calendrier (douleur ou emotions, jours de regles), courbes sur deux
  echelles avec les regles en fond, une seule periode pour toute la page (issue #12)
- [x] Tendances neutres a la place du score de bien-etre : moyenne et evolution par indicateur vs periode precedente,
  reperes personnels, observations factuelles douleur / regles (issue #13)

### Lot C - Engineering quality
- [ ] Suppression progressive des `any` critiques
- [ ] Normalisation handlers `onDelete` / `onEdit` (`string | number`)
- [ ] Tests E2E mobile des flux de saisie principaux
- [ ] SOLID serveur : controleurs sans `AppDbContext` ni requetes (logique dans un service par domaine)
- [ ] SOLID serveur : decouper `CarnetSanteService` (lecture carnet / page d'accueil / export PDF)
- [ ] SOLID serveur : abstraction du stockage des photos (`AzureBlobStorageService`) pour tester l'upload sans Azure (pas d'interface pour `TokenService` : KISS)
- [ ] SOLID serveur : `TimeProvider` a la place de `DateTime.Now` (authentification)
- [ ] SOLID client : `authService` / `tokenService` via l'instance axios de `apiService`, methodes `apiService` typees
- [ ] SOLID client : decoupage de `CyclePage`, `MedicamentPage` (pattern model/actions) ; `BilanQuotidienPage` fait (1.1.0)

### Lot D - Security baseline
- [x] Rate limiting endpoint-level (politique `auth` reellement appliquee aux endpoints d'authentification, 2026-09)
- [x] Security headers globaux
- [x] Validation JWT stricte conditionnelle
- [x] Revue obsolete API de credentials Google (supprimee avec Firebase, 2026-09)

### Lot E - Notifications
- [x] Notifications push generiques pour toutes les utilisatrices (rappels personnalises), envoyees cote serveur
  - idealement sans service tiers : Web Push standard (VAPID) avec un service worker dedie au push
  - aucune cle secrete cote client ; preferences de rappel par utilisatrice
- [x] Rappel hebdomadaire de la photo de suivi acné (remplace le Zap), rappels generalises (`IRegleRappel`), notification qui ouvre la bonne page
- [ ] Supprimer la table `PreferencesRappel` (non mappee depuis `GeneraliseRappels`) en 1.1, par une migration dediee, une fois
  la 1.0.0 stable en production (le point de retour devient alors la 1.0.0, qui n'utilise plus cette table)
- [x] Configuration VAPID validee au demarrage (avertissement explicite, aucune route en echec)

## Ce qui est deja implemente dans cette iteration
- UX medicaments/sessions non medicamenteuses amelioree (`MedicamentPage.vue`):
  - CTA d'etat vide, presets de duree, cards mobile, KPI section, desactivation submit si incomplet
- Typage partage `GenericCardList` avec extraction vers `src/shared/types/card.ts`
- Durcissement backend (`Program.cs`):
  - rate limiting (`api`, `auth`)
  - headers de securite web
  - validation JWT plus stricte quand issuer/audience configures
- Homepage (`Carnet.vue`) revue:
  - cards harmonisees avec infos "derniere entree" + "il y a ..."
  - badge "A mettre a jour" selon seuil d'inactivite par section
  - compromis mobile: cards cote a cote, puis 1 colonne <= 425px pour lisibilite complete
  - clic rendez-vous avec adresse -> ouverture Google Maps (itineraire)
  - acces Export PDF reintegre sur la homepage (card dediee)
- Passe mobile-first <=425px appliquee sur pages metier:
  - `CyclePage.vue`: tabs/legende/selecteur mois/champs date-heure adaptes
  - `DouleursPage.vue`: en-tete et formulaire date-heure adaptes
  - `MedicamentPage.vue`: en-tetes, formulaires et actions de listes adaptes
- Passe mobile-first et utilite metier sur nouvelles zones:
  - `TransitPage.vue`: cards mobile + KPI de section + formulaire adapte <=425px
  - `BilanQuotidienPage.vue`: stepper/tabs adaptes <=425px + resume KPI du jour selectionne
- Personnalisation Analyse & Tendances:
  - `ParametresPage.vue`: section "Objectifs bien-etre" (hydratation, pas, stress, fatigue, douleur)
  - `DashboardBilanQuotidien.vue`: calcul des objectifs/insights pilote par ces cibles utilisateur
- Separation UX du suivi acné (dans `CyclePage.vue`):
  - onglet dedie `Acné` distinct des autres symptomes
  - conservation du quick-add acné et des actions de periode en cours dans cet onglet
  - nouvelle galerie "Evolution photo" avec comparaison rapide de 2 photos
- Iteration 2026-09 (nettoyage, securite, transit, notifications):
  - depot nettoye (gitignore unique, fichiers parasites retires), PWA / mode hors ligne / TypeGen / Firebase retires
  - pipeline CI/CD en 3 jobs (verifier, image, deployer) avec controle `/health` apres deploiement
  - bilan quotidien : categorie transit facultative (selles et echelle de Bristol expliquee, crampes, ballonnements),
    recapitulatif et export PDF ; migration additive, ancienne page `/transit` conservee
  - controle d'acces des donnees du carnet renforce sur toutes les modifications, identifiants de connexion transmis dans
    le corps des requetes, limitation de debit dediee aux endpoints d'authentification
  - premier projet de tests serveur (xUnit) execute par la CI, couverture envoyee a SonarCloud (seuil 80 % sur le nouveau code)
  - notifications Web Push standard envoyees par le serveur : abonnement par appareil depuis Parametres (guide
    d'installation iOS), rappel du bilan a l'heure choisie seulement si le bilan du jour n'est pas rempli, notification de test
  - rappel hebdomadaire de la photo de suivi acne (jour et heure au choix, seulement sans photo depuis 7 jours) ; chaque
    notification ouvre sa page (`/bilan-quotidien`, `/cycle?onglet=acne`)

## Definition of Done (pour chaque lot)
- UX: test manuel mobile + desktop + accessibilite clavier
- Qualite: `npm run build` et `dotnet test` verts (couverture Sonar >= 80 % sur le nouveau code) + tests E2E concernes
- Documentation: README, CLAUDE.md, skills et cette roadmap mis a jour dans la meme PR que l'evolution
- Securite: revue headers/CORS/rate limits + logs sans donnees sensibles
- Produit: metrique avant/apres mesuree sur 2 semaines

## Pratiques Vue/SOLID a appliquer (obligatoire)
- Regles completes (serveur et client) : sections « SOLID » de `MonEndoVue.Server/CLAUDE.md` et `monendovue.client/CLAUDE.md`.
- Limiter la taille des pages: extraire tout bloc UI metier depassant ~150-200 lignes vers un composant dedie.
- Garder la logique d'orchestration dans la page et deleguer le rendu aux composants presentational.
- Eviter la duplication de markup: reutiliser les composants partages (`GenericCardList`, `SectionKpiHeader`, etc.).
- Exposer des interfaces/props explicites pour chaque composant extrait (Single Responsibility).
- Garder des handlers stables et simples (`onEdit`, `onDelete`, `onPhotoClick`) pour minimiser le couplage.
- Ajouter une verification `npm run type-check` apres chaque extraction significative.

## Journal des modifications recentes (session courante)
- `CyclePage.vue`:
  - separation stricte des tabs `Symptomes` et `Acne` (plus de contenu melange)
  - correction du skeleton infini dans `Mes cycles` (stop loading en `finally`)
  - uniformisation des cartes acné/symptomes via `GenericCardList`
  - historique acné allege (pas de preview photo inline systematique)
  - comparaison photo acné plus visuelle (selection + vue cote a cote)
  - debut de refactor: extraction de l'onglet acné vers `AcneTabContent.vue`
  - ajout d'un select de mois dedie pour l'onglet acné (dissocie de `Symptomes`)
  - separation des sources de donnees acné/symptomes pour eviter les interferences de filtres
  - comparaison inter-mois active via fenetre glissante (navigation mois precedent/suivant + conservation de la comparaison si photos toujours visibles)
  - priorisation UX de la fenetre glissante: mois selectionne affiche en premier + indicateur de fenetre active
  - comparaison assistee: action rapide pour comparer les 2 photos les plus recentes
  - historique acné repositionne en vue secondaire (panneau repliable par defaut)
  - historique acné cible d'abord les entrees sans photo pour eviter le doublon avec la galerie
- `GenericCardList.vue`:
  - ajout des props `hideTitle` et `hideIcon` pour des variantes compactes de cartes
- `DouleursPage.vue`:
  - correction UX mobile: bouton `+` aligne sur la meme ligne que le titre de section
- `AcneTabSection.vue`:
  - orchestration allegee: branchement des evenements + rendu du dialog "terminer la periode"
  - suppression de la logique metier lourde de ce composant
- `useAcneTracking.ts` (nouveau composable):
  - centralise la logique metier acné (chargement multi-mois, quick-add, comparaison photo, historique, periodes, suppression)
  - expose un `model` et des `actions` testables/reutilisables
- `AcneTabContent.vue`:
  - API simplifiee de nombreuses props vers 2 props (`model`, `actions`) pour reduire le couplage et la prop-drilling
- `acne-tab.ts` (nouveau type partage):
  - contrat explicite `AcneTabModel` / `AcneTabActions` pour fiabiliser l'integration et la maintenance
