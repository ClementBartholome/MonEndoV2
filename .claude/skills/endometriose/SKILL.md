---
name: endometriose
description: Connaissance métier MonEndo sur l'endométriose — la maladie, le parcours de soins en France, le vécu des personnes atteintes, ce qu'elles attendent d'une app de suivi, les apps existantes et leurs avis, le vocabulaire et le ton à employer. À utiliser avant toute décision produit, nouvelle donnée suivie, écran d'analyse ou de tendances, export pour le médecin, texte d'interface ou d'aide, et dès qu'une question porte sur la maladie elle-même.
---

# Endométriose — connaissance métier

Ce skill sert à **cadrer les décisions produit**, pas à informer médicalement les utilisatrices. Les faits viennent de
sources publiques fiables listées dans [sources.md](sources.md) (consultées le 2026-09-27) ; toute donnée ajoutée ici
doit y être sourcée et datée.

## Garde-fous (non négociables)
- **Aucun conseil médical, aucun diagnostic** : MonEndo aide à observer et à préparer la consultation ; le soignant interprète.
  Pas de score de probabilité d'endométriose, pas de recommandation de traitement ni de posologie.
- **Ton validant et non anxiogène** : la douleur décrite est réelle et légitime ; ne jamais la minimiser (« c'est normal
  d'avoir mal pendant les règles ») ni dramatiser (pas de rouge alarmant, pas de « dégradation », pas de pronostic).
- **Pas de chiffre médical dans l'interface sans source** ; préférer renvoyer vers Ameli, EndoFrance ou le soignant.
- **Données de santé sensibles** (RGPD art. 9) : les sujets intimes (sexualité, fertilité) restent facultatifs et discrets.
- Formulation inclusive quand c'est naturel (« personnes atteintes », « utilisatrice ») ; la maladie ne touche pas
  que des personnes qui se définissent comme femmes.

## La maladie en bref
- **Définition** : un tissu semblable à l'endomètre se développe hors de l'utérus et provoque inflammation et
  cicatrices (adhérences). Maladie **chronique**, hormono-dépendante, qui régresse en général après la ménopause.
- **Fréquence** : environ 10 % des personnes en âge de procréer (≈ 190 millions dans le monde selon l'OMS ;
  1,5 à 2,5 millions en France selon EndoFrance).
- **Causes** : inconnues à ce jour ; association documentée avec des dérèglements immunitaires.
- **Grande variabilité** : on parle « des » endométrioses. Formes asymptomatiques (découvertes lors d'un bilan
  d'infertilité) comme formes très invalidantes.

### Symptômes (à suivre séparément)
| Terme | Signification | Suivi dans MonEndo |
|---|---|---|
| Dysménorrhée | règles douloureuses (souvent le premier signe, dès l'adolescence) | douleurs + jours de règles |
| Douleurs pelviennes chroniques | hors règles, parfois autour de l'ovulation | douleurs (type pelvienne, abdominale, lombaire) |
| Dyspareunie | douleur pendant les rapports | type de douleur « Dyspareunie » |
| Dyschésie, troubles digestifs | douleur à la défécation, constipation, diarrhée, nausées | bilan quotidien (transit, Bristol), symptômes |
| Troubles urinaires | douleur à la miction, envies pressantes, sang dans les urines (souvent pendant les règles) | **non suivi aujourd'hui** |
| « Endobelly » | ballonnement abdominal soudain | bilan quotidien (ballonnements) |
| Fatigue chronique | retentit sur le moral et la concentration | bilan quotidien, symptôme « Fatigue » |
| Douleur neuropathique, douleur projetée | irritation nerveuse ; ex. épaule droite si atteinte du diaphragme | types de douleur dédiés |
| Infertilité | 30 à 40 % des cas (Ameli) ; 25 à 50 % des personnes infertiles ont une endométriose (OMS) | hors périmètre actuel |
| Retentissement psychique | anxiété, dépression, isolement | bilan (émotions, stress) |

Les soignants mesurent la douleur par **type** avec une échelle numérique **0-10** (EN/ENS) ou visuelle (EVA) : c'est
l'échelle de MonEndo, à conserver. Questionnaires spécialisés utilisés en consultation : EHP-5 / EHP-30 (qualité de vie),
DN4 (douleur neuropathique), QDSA (description de la douleur) — **ne pas les reproduire sans vérifier leur licence**.

## Diagnostic et parcours de soins (France)
- **Errance diagnostique** : 7 ans en moyenne entre premiers symptômes et diagnostic (enquête EndoVie 2020) ;
  4 à 12 ans selon l'OMS. Cause majeure : la **banalisation** de la douleur de règles, y compris par des soignants.
- **Étapes** : interrogatoire détaillé des symptômes → examen clinique → **échographie pelvienne** (1re intention) →
  **IRM** si besoin. Depuis 2022 (ESHRE), la cœlioscopie n'est plus l'examen de référence du diagnostic.
  **Endotest salivaire** : remboursé à titre expérimental depuis le 11/02/2025 (forfait innovation, 3 ans), réservé aux
  cas où l'imagerie est normale ou non concluante.
- Les recommandations (HAS 2017, ESHRE 2022, HUG) **encouragent la tenue d'un carnet de symptômes** avant les
  consultations : c'est la raison d'être de MonEndo et de son export PDF.
- **Organisation** : prise en charge pluridisciplinaire (médecin traitant, gynécologue, sage-femme, radiologue,
  algologue, psychologue…), **filières régionales** et centres de référence pour les formes complexes
  (stratégie nationale 2022-2025).
- **Droits** : ALD 31 (prise en charge à 100 % des formes invalidantes, sous conditions) ; RQTH et aménagement du poste ;
  « Mon soutien psy » pour les séances de psychologue.

## Traitements (pour comprendre ce que les utilisatrices saisissent)
- **Antalgiques** par paliers (paracétamol, AINS, puis plus forts) ; la douleur a souvent une composante neuropathique.
- **Hormonaux** (bloquent les règles pour calmer douleurs et lésions) : pilule œstroprogestative en continu, DIU au
  lévonorgestrel, microprogestatifs, implant, diénogest ; en 3e intention analogues de la GnRH (avec « add-back »).
- **Chirurgie** (souvent cœlioscopie) pour les douleurs résistantes ou l'infertilité ; l'hystérectomie ne guérit pas.
- **Non médicamenteux** en complément : activité physique régulière, alimentation de type méditerranéen, kinésithérapie,
  ostéopathie, acupuncture, yoga, relaxation, psychothérapie/TCC. Niveau de preuve variable : MonEndo **permet de
  suivre** ces approches (séances), **sans les recommander**.
- Les traitements changent souvent (effets indésirables, efficacité insuffisante) : l'historique « en cours / passé »
  et les dates de début/fin sont précieux pour le médecin.

## Vécu et retentissement
- Deux répondantes sur trois déclarent un impact sur leur vie sexuelle, sociale ou professionnelle (EndoVie 2020,
  1 557 répondantes) ; qualité de vie inférieure d'environ 20 % et près de 11 h de productivité perdues par semaine (FRE).
- Sommeil, absentéisme scolaire et professionnel, vie de couple, santé mentale : retentissement **global**, d'où l'intérêt
  du bilan quotidien au-delà de la seule douleur.
- Expérience récurrente de **ne pas être crue** : douleur minimisée avant le diagnostic, puis prise en charge centrée sur
  la fertilité plutôt que sur la douleur. Les patientes veulent arriver en consultation **avec des faits**.

## Ce qu'on attend d'une app de suivi → implications MonEndo
| Attente (sources : études d'usage, avis, recommandations) | Conséquence produit |
|---|---|
| Suivre règles, douleurs, transit, humeur, traitements, alimentation (enquête Scheck 2023) | couvert ; manquent les **troubles urinaires** et le **flux menstruel** (`JourRegle.FluxMenstruel` commenté) |
| Saisie rapide et régulière (la moitié des utilisatrices logge au moins chaque semaine) ; ne pas tout imposer d'emblée | presets, 1-2 taps, catégories facultatives du bilan, rappels configurables |
| Des faits partageables avec le soignant | export PDF lisible par un médecin en 2 minutes : par type de douleur, intensité 0-10, lien avec les règles, traitements et changements |
| Voir des liens (douleur ↔ cycle, activité, sommeil, alimentation) | tendances **descriptives** (« plus fréquent pendant les règles »), jamais causales ni prédictives |
| Confidentialité : méfiance forte envers les apps de règles (revente de données, transferts hors UE) | aucune publicité ni traceur tiers, hébergement maîtrisé, export et suppression par l'utilisatrice ; le dire clairement |
| Gratuité / pas de paywall sur l'essentiel | les critiques d'apps payantes portent sur le prix quand l'app bugue |
| Fiabilité | bugs, connexion impossible et support muet sont les premiers griefs dans les avis |
| Contenu fiable, se sentir comprise (« alliance thérapeutique ») | textes validants, renvoi vers sources officielles et associations plutôt que contenu médical maison |

## État des lieux des apps (2026-09)
| App | Positionnement | À retenir |
|---|---|---|
| **Luna for Health** (FR) | suivi quotidien + score d'aide au diagnostic certifié CE, téléconsultations | payant (49,99 € cité dans les avis) ; avis App Store FR 3,6/5 (17) : saisie jugée intuitive, mais bugs, accès au compte, support absent |
| **Shiny Deva** (FR) | questionnaire de dépistage validé | outil ponctuel, pas de suivi |
| **Lyv Endo**, **APAISIA** | accompagnement douleur / coach (hypnose, relaxation) | orientés autogestion, pas carnet médical |
| **Follow Metrios**, **No endo** | carnet numérique / questionnaires pré-consultation liés à des CHU | accès limité à certains centres |
| **Nabla** | journal de symptômes + consultations | généraliste santé des femmes |
| **Bearable** | tracker générique très personnalisable, corrélations | apprécié : personnalisation, pas de pub, interface calme ; premium payant ; en anglais |
| **QENDO, Matilda, CHARLI, Branch** | apps australiennes jugées de bonne qualité (Sirohi 2025) | rapports partageables avec les soignants |
| **Flo, Clue** | suivi de cycle grand public | critiques sur le partage de données avec des tiers (publicité, analytique) |

Constats des revues (Sirohi 2025, Tjandraprawira 2025) : la plupart des apps font du suivi de symptômes, peu citent
leurs sources, rares sont celles qui impliquent des soignants, la sécurité des données est rarement explicitée.
**Positionnement de MonEndo** : carnet gratuit, francophone, sans pub ni diagnostic, pensé pour la consultation, avec un
bilan quotidien global (humeur, fatigue, transit) et un suivi photo de l'acné que les concurrents n'ont pas.

## Mettre à jour ce skill
- Vérifier les chiffres avant de les réutiliser dans l'interface ; ajouter toute nouvelle source dans `sources.md` avec sa date.
- Ne pas copier de texte des sources : synthétiser (droits d'auteur).
- Relecture par l'utilisateur avant commit (dépôt public).
