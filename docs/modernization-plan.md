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

### Lot C - Engineering quality
- [ ] Suppression progressive des `any` critiques
- [ ] Normalisation handlers `onDelete` / `onEdit` (`string | number`)
- [ ] Tests E2E mobile des flux de saisie principaux

### Lot D - Security baseline
- [ ] Rate limiting endpoint-level
- [ ] Security headers globaux
- [ ] Validation JWT stricte conditionnelle
- [ ] Revue obsolete API de credentials Google

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
- Passe mobile-first <=425px appliquee sur pages metier:
  - `CyclePage.vue`: tabs/legende/selecteur mois/champs date-heure adaptes
  - `DouleursPage.vue`: en-tete et formulaire date-heure adaptes
  - `MedicamentPage.vue`: en-tetes, formulaires et actions de listes adaptes

## Definition of Done (pour chaque lot)
- UX: test manuel mobile + desktop + accessibilite clavier
- Qualite: `npm run type-check` vert + tests E2E concernes
- Securite: revue headers/CORS/rate limits + logs sans donnees sensibles
- Produit: metrique avant/apres mesuree sur 2 semaines


