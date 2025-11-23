# Mode Hors Ligne - Documentation Technique

## Vue d'ensemble

L'application implémente un système de mode hors ligne complet permettant aux utilisateurs de continuer à utiliser l'application même sans connexion internet. Toutes les modifications effectuées hors ligne sont automatiquement synchronisées lorsque la connexion est rétablie.

## Architecture

Le système repose sur trois composants principaux :

```
┌─────────────────────────────────────────────────────────┐
│                    Service Worker                       │
│  • Cache des assets statiques (HTML, CSS, JS)           │
│  • Cache des requêtes GET (Stale-While-Revalidate)      │
│  • Background Sync pour les requêtes POST/PUT/DELETE    │
└─────────────────────────────────────────────────────────┘
                            ↕
┌─────────────────────────────────────────────────────────┐
│                   IndexedDB Storage                     │
│  • carnetData: Données de santé en cache                │
│  • calendarEvents: Événements calendrier en cache       │
│  • pendingOperations: File d'attente des opérations     │
└─────────────────────────────────────────────────────────┘
                            ↕
┌─────────────────────────────────────────────────────────┐
│                Services & Composables                   │
│  • offlineStorage.ts: Gestion IndexedDB                 │
│  • syncService.ts: Synchronisation des opérations       │
│  • useSync.ts: Composable réutilisable                  │
│  • useCrudOperations.ts: Opérations CRUD offline        │
└─────────────────────────────────────────────────────────┘
```

## 1. Service Worker (Workbox)

### Génération et Configuration

#### Comment le Service Worker est Créé

Le Service Worker **n'est PAS écrit manuellement**. Il est **généré automatiquement** par le plugin **Vite PWA** lors du build.

**Workflow** :
```
┌─────────────────────────────────────┐
│  vite.config.ts                     │
│  • Configuration Vite PWA           │
│  • Stratégies de cache Workbox     │
└──────────────┬──────────────────────┘
               │
         npm run build / dev
               ↓
┌──────────────▼──────────────────────┐
│  Plugin Vite PWA                    │
│  • Lit la configuration             │
│  • Génère le Service Worker         │
│  • Applique les règles Workbox     │
└──────────────┬──────────────────────┘
               │
               ↓
┌──────────────▼──────────────────────┐
│  dev-dist/sw.js (Fichier généré)   │
│  • Service Worker Workbox           │
│  • ⚠️ NE JAMAIS MODIFIER CE FICHIER│
│  • Sera écrasé au prochain build   │
└─────────────────────────────────────┘
```

**Fichier généré**: `dev-dist/sw.js` (développement) ou `dist/sw.js` (production)

#### Configuration dans vite.config.ts

Pour modifier le comportement du Service Worker, on configure Vite PWA :

```typescript
// vite.config.ts
import { VitePWA } from 'vite-plugin-pwa'

export default defineConfig({
  plugins: [
    VitePWA({
      registerType: 'autoUpdate',
      includeAssets: ['favicon.ico', 'apple-touch-icon.png'],

      // Manifest PWA
      manifest: {
        name: 'MonEndo',
        short_name: 'MonEndo',
        description: 'Application de suivi endométriose',
        theme_color: '#ffffff',
        icons: [/* ... */]
      },

      // 🔧 Configuration Workbox (Service Worker)
      workbox: {
        runtimeCaching: [
          {
            // Stratégie pour les requêtes GET API
            urlPattern: /^https:\/\/monendoapp\.fr\/api\/.*/,
            handler: 'StaleWhileRevalidate',
            options: {
              cacheName: 'api-cache',
              expiration: {
                maxEntries: 100,
                maxAgeSeconds: 60 * 60 * 24 * 7 // 7 jours
              }
            }
          },
          {
            // Background Sync pour POST/PUT/DELETE
            urlPattern: /^https:\/\/monendoapp\.fr\/api\/.*/,
            handler: 'NetworkOnly',
            method: 'POST',
            options: {
              backgroundSync: {
                name: 'api-queue',
                options: {
                  maxRetentionTime: 24 * 60 // 24 heures
                }
              }
            }
          }
        ]
      }
    })
  ]
})
```

#### ⚠️ Important : Ne Pas Modifier sw.js Directement

```
❌ MAUVAISE PRATIQUE
Éditer dev-dist/sw.js ou dist/sw.js
→ Changements perdus au prochain build

✅ BONNE PRATIQUE
Modifier vite.config.ts
→ Changements persistants et versionnés
```

### Stratégies de Cache

#### A. Assets Statiques (Precaching)
```javascript
workbox.precacheAndRoute([{
  "url": "index.html",
  "revision": "0.s1mdn3bdd28"
}])
```
- Tous les assets de l'application sont pré-cachés
- Disponibles instantanément même hors ligne

#### B. Requêtes GET (Stale-While-Revalidate)
```javascript
workbox.registerRoute(
  ({ request, url }) => {
    return request.method === "GET" &&
           url.pathname.startsWith("/api/")
  },
  new workbox.StaleWhileRevalidate({
    cacheName: "api-cache",
    plugins: [
      new workbox.ExpirationPlugin({
        maxEntries: 100,
        maxAgeSeconds: 604800  // 7 jours
      })
    ]
  })
)
```
**Comportement** :
1. Retourne immédiatement la version en cache (si disponible)
2. En arrière-plan, fait une requête réseau pour mettre à jour le cache
3. La prochaine requête aura la version à jour

#### C. Requêtes Write (POST/PUT/DELETE) - Background Sync
```javascript
workbox.registerRoute(
  ({ request, url }) => {
    const isWriteMethod = ["POST", "PUT", "DELETE", "PATCH"].includes(request.method)
    return isWriteMethod && url.pathname.startsWith("/api/")
  },
  new workbox.NetworkOnly({
    plugins: [
      new workbox.BackgroundSyncPlugin("api-queue", {
        maxRetentionTime: 1440  // 24 heures
      })
    ]
  })
)
```
**Comportement** :
1. Tente d'abord la requête réseau
2. Si échec (hors ligne), stocke dans IndexedDB
3. Rejoue automatiquement quand la connexion revient

⚠️ **Note**: Cette fonctionnalité du Service Worker est complémentaire à notre système IndexedDB personnalisé.

### Approche Hybride : Pourquoi Deux Systèmes ?

Notre architecture utilise **deux systèmes en parallèle** :

#### A. Service Worker (Workbox) - Généré Automatiquement

**Avantages** :
- ✅ Intercepte automatiquement toutes les requêtes HTTP
- ✅ Cache natif du navigateur (très performant)
- ✅ Background Sync intégré au navigateur
- ✅ Fonctionne même si JavaScript plante
- ✅ Pas de code à maintenir (généré)

**Limitations** :
- ❌ Contrôle limité sur la logique métier
- ❌ Difficile à déboguer (boîte noire)
- ❌ Pas de visibilité UI sur la queue
- ❌ Pas de customisation fine des erreurs
- ❌ Retry logic basique

#### B. Notre IndexedDB Personnalisé

**Avantages** :
- ✅ Contrôle total sur la logique de synchronisation
- ✅ Visibilité complète sur les opérations en attente
- ✅ UI pour afficher le statut (badge, bouton sync)
- ✅ Gestion d'erreurs personnalisée (validation, réseau, etc.)
- ✅ Retry logic configurable
- ✅ Peut ajouter des métadonnées (timestamp, retryCount)
- ✅ Facile à déboguer (console logs, IndexedDB visible)

**Limitations** :
- ❌ Plus de code à écrire et maintenir
- ❌ Nécessite initialisation manuelle
- ❌ Dépend de JavaScript actif

#### Stratégie Combinée

```
Requête API échoue (offline)
         ↓
    ┌────┴────┐
    │         │
    ↓         ↓
Service      Notre
Worker      Système
(Backup)   (Principal)
    │         │
    │         ↓
    │    IndexedDB
    │    pendingOperations
    │         │
    │         ↓
    │    UI Update
    │    (Badge +1)
    │         │
    └────┬────┘
         ↓
  Retour en ligne
         ↓
    ┌────┴────┐
    │         │
    ↓         ↓
Notre        Service
Système      Worker
sync()      sync()
(Priorité)  (Backup)
    │         │
    └────┬────┘
         ↓
    Toutes les
    opérations
    synchronisées
```

**Pourquoi cette redondance ?**

1. **Fiabilité maximale** : Si notre système échoue, le Service Worker prend le relai
2. **Meilleure UX** : Notre système offre un feedback visuel immédiat
3. **Flexibilité** : On peut customiser le comportement pour chaque endpoint
4. **Migration progressive** : Si on désactive notre système, le Service Worker continue de fonctionner

**Exemple concret** :

```typescript
// L'utilisateur crée une douleur hors ligne

// 1️⃣ useCrudOperations détecte l'échec réseau
catch (error) {
  if (isNetworkError) {
    // Sauvegarde dans NOTRE IndexedDB
    await offlineStorage.savePendingOperation({...})
    // → Badge affiche "1 opération en attente"
  }
}

// 2️⃣ En parallèle, le Service Worker intercepte aussi
self.addEventListener('fetch', (event) => {
  if (event.request.method === 'POST') {
    // Workbox BackgroundSyncPlugin enregistre aussi
    // → Queue "api-queue" contient l'opération
  }
})

// 3️⃣ Retour en ligne
// Notre système sync en premier
await syncService.syncPendingOperations()
// → Opération synchronisée
// → Supprimée de notre IndexedDB
// → Badge mis à jour

// Le Service Worker sync aussi (mais l'opération a déjà réussi)
// → Pas de doublon car l'API retourne 200 OK
```

## 2. IndexedDB - Stockage Local

### Structure de la Base de Données

```typescript
Database: 'MonEndoOffline'
├── carnetData (keyPath: 'id')
│   ├── Index: carnetSanteId
│   └── Index: timestamp
│
├── calendarEvents (keyPath: 'id')
│   └── Index: timestamp
│
└── pendingOperations (keyPath: 'id')
    ├── Index: timestamp
    └── Index: type
```

### Object Stores

#### A. `carnetData` - Cache des Données de Santé

**Structure** :
```typescript
{
  id: "carnet-123",           // ID unique
  carnetSanteId: 123,         // ID du carnet de santé
  data: {                     // Données complètes
    donneesDouleur: {...},
    donneesActivite: {...},
    // ...
  },
  timestamp: 1701234567890    // Date de sauvegarde
}
```

**Durée de vie**: 24 heures
**Usage**: Affichage du tableau de bord hors ligne

**Code** :
```typescript
// Sauvegarder
await offlineStorage.saveCarnetData(carnetSanteId, data)

// Récupérer (avec vérification de fraîcheur)
const cachedData = await offlineStorage.getCarnetData(carnetSanteId)
```

#### B. `calendarEvents` - Cache des Événements Calendrier

**Structure** :
```typescript
{
  id: "calendar-events",
  events: [
    {
      id: "event1",
      summary: "RDV Gynécologue",
      start: { dateTime: "2025-01-20T14:00:00" },
      // ...
    }
  ],
  timestamp: 1701234567890
}
```

**Durée de vie**: 2 heures
**Usage**: Affichage des prochains rendez-vous

#### C. `pendingOperations` - File d'Attente des Opérations

**Structure** :
```typescript
{
  id: "create-DonneesDouleurs-1701234567890-abc123",
  type: 'create' | 'update' | 'delete',
  endpoint: 'DonneesDouleurs',      // Nom de l'endpoint API
  method: 'POST' | 'PUT' | 'DELETE',
  data: {                           // Payload pour POST/PUT
    typeDouleur: 'Pelvienne',
    intensite: 7,
    // ...
  },
  resourceId: 456,                  // ID pour PUT/DELETE
  timestamp: 1701234567890,         // Date de création
  retryCount: 0                     // Nombre de tentatives
}
```

**Durée de vie**: Jusqu'à synchronisation réussie
**Usage**: Stockage des opérations en attente de sync

## 3. Service de Synchronisation

### syncService.ts

Gère la synchronisation des opérations en attente.

#### Méthode Principale: `syncPendingOperations()`

**Algorithme** :
```
1. Vérifier si une sync est déjà en cours → Annuler si oui
2. Vérifier la connexion → Annuler si hors ligne
3. Récupérer toutes les opérations en attente depuis IndexedDB
4. Traiter chaque opération séquentiellement (pour maintenir l'ordre)
5. Pour chaque opération :
   a. Exécuter la requête API correspondante
   b. Si succès → Supprimer de IndexedDB
   c. Si échec :
      - Erreur réseau → Incrémenter retry, garder dans la queue
      - Erreur validation (400/422) → Supprimer (ne réussira jamais)
      - Autre erreur → Incrémenter retry, garder dans la queue
6. Retourner statistiques de synchronisation
```

**Code** :
```typescript
const result = await syncService.syncPendingOperations()
// result = {
//   total: 5,
//   successful: 4,
//   failed: 1,
//   errors: [...]
// }
```

#### Mapping Endpoint → Méthode API

```typescript
private async executePost(endpoint: string, data: any) {
  switch (endpoint) {
    case 'DonneesDouleurs':
      return apiService.postDonneesDouleurs(data)
    case 'DonneesActivitePhysique':
      return apiService.postDonneesActivitePhysique(data)
    // ... autres endpoints
  }
}
```

## 4. Composables Réutilisables

### useSync.ts

Composable Vue.js pour gérer la synchronisation dans les composants.

**Exports** :
```typescript
const {
  pendingOperationsCount,    // Ref<number>: Nombre d'opérations en attente
  isSyncing,                  // Ref<boolean>: Sync en cours?
  performSync,                // Function: Déclencher sync manuelle
  updatePendingCount,         // Function: Rafraîchir le compteur
  handleOfflineOperation,     // Function: Helper pour API calls
} = useSync()
```

#### Helper: `handleOfflineOperation()`

Simplifie la gestion des appels API avec support offline.

**Utilisation** :
```typescript
await handleOfflineOperation(
  () => apiService.postJourRegle(data),  // Fonction API
  {
    endpoint: 'JourRegle',               // Nom de l'endpoint
    method: 'POST',                      // Méthode HTTP
    data: data,                          // Données à envoyer
    onSuccess: (response) => {           // Callback si succès
      periodMarked.value = true
    },
    onOfflineQueued: () => {             // Callback si mise en queue
      periodMarked.value = true
    },
    successMessage: 'Règles marquées',   // Message toast
    errorMessage: 'Erreur',
  }
)
```

**Ce qu'il fait** :
1. Tente l'appel API
2. Si succès → Appelle `onSuccess()`
3. Si échec réseau :
   - Sauvegarde dans IndexedDB
   - Appelle `onOfflineQueued()`
   - Affiche toast "sera synchronisée"
   - Met à jour le compteur
4. Si autre erreur → Affiche toast d'erreur

### useCrudOperations.ts

Composable pour les opérations CRUD avec support offline intégré.

**Configuration requise** :
```typescript
const { deleteEntry, createEntry, updateEntry } = useCrudOperations(entries)

// Ajouter l'endpoint dans les options !
await deleteEntry(id, apiService.deleteDonneesDouleurs, {
  endpoint: 'DonneesDouleurs',  // ← REQUIS pour offline
  successMessage: '...',
})
```

**Comportement offline** :
- **DELETE**: Supprime de l'UI immédiatement + sauvegarde l'opération
- **CREATE**: Sauvegarde l'opération, attend la sync pour l'ID réel
- **UPDATE**: Met à jour l'UI immédiatement + sauvegarde l'opération

## 5. Flux Complet d'une Opération

### Exemple: Ajout d'une Douleur Hors Ligne

#### Étape 1: Action Utilisateur
```vue
<!-- DouleursPage.vue -->
<script setup>
const { createEntry } = useCrudOperations(entries)

const onSubmit = () => {
  createEntry(values, {
    createFunction: (data) => apiService.postDonneesDouleurs(data),
    endpoint: 'DonneesDouleurs',  // ← Requis
    // ...
  })
}
</script>
```

#### Étape 2: Tentative API (Échec)
```typescript
// useCrudOperations.ts
try {
  const response = await apiService.postDonneesDouleurs(data)
  // ✅ Success → Ajouter à entries
} catch (error) {
  // ❌ Network error détecté
  if (error.code === 'ERR_NETWORK') {
    // Sauvegarder pour sync
  }
}
```

#### Étape 3: Sauvegarde dans IndexedDB
```typescript
await offlineStorage.savePendingOperation({
  id: "create-DonneesDouleurs-1701234567890-xyz",
  type: 'create',
  endpoint: 'DonneesDouleurs',
  method: 'POST',
  data: {
    typeDouleur: 'Pelvienne',
    intensite: 7,
    date: '2025-01-15T10:30:00',
    carnetSanteId: 123
  },
  timestamp: 1701234567890,
  retryCount: 0
})
```

#### Étape 4: Feedback Utilisateur
```typescript
toast({
  title: 'Succès',
  description: 'Douleur ajoutée (sera synchronisée)',
  variant: 'custom',
})

// Mettre à jour le badge
pendingOperationsCount.value = await offlineStorage.getPendingOperationsCount()
// → Badge affiche "1"
```

#### Étape 5: Retour en Ligne
```typescript
// Carnet.vue - Auto-sync
window.addEventListener('online', async () => {
  await performSync()
})
```

#### Étape 6: Synchronisation
```typescript
// syncService.ts
const operations = await offlineStorage.getPendingOperations()
// → [{ id: "create-DonneesDouleurs-...", ... }]

for (const op of operations) {
  // Exécuter POST /api/DonneesDouleurs
  const response = await apiService.postDonneesDouleurs(op.data)

  // Supprimer de IndexedDB
  await offlineStorage.removePendingOperation(op.id)
}

// Toast de confirmation
toast({
  title: 'Synchronisation réussie',
  description: '1 opération(s) synchronisée(s)',
})
```

## 6. Interface Utilisateur

### Indicateurs Visuels

#### A. Bannière Hors Ligne
```vue
<!-- Carnet.vue -->
<div v-if="!isOnline" class="w-full mb-4 p-3 bg-yellow-100">
  <i class="material-symbols-outlined">cloud_off</i>
  Mode hors ligne - Affichage des dernières données sauvegardées
</div>
```

#### B. Bannière Opérations En Attente
```vue
<div v-if="pendingOperationsCount > 0" class="w-full mb-4 p-3 bg-blue-100">
  <span>{{ pendingOperationsCount }} opération(s) en attente</span>
  <Button @click="performSync" :disabled="isSyncing">
    {{ isSyncing ? 'Synchronisation...' : 'Synchroniser' }}
  </Button>
</div>
```

#### C. Messages Toast
- **Hors ligne** : "Mode hors ligne - Vos modifications seront synchronisées..."
- **Opération sauvegardée** : "[Action] (sera synchronisée)"
- **Retour en ligne** : "Connexion rétablie - Synchronisation en cours..."
- **Sync réussie** : "5 opération(s) synchronisée(s)"

### Composable: useOnlineStatus.ts

Gère les toasts automatiques lors des changements de connexion.

```typescript
const { isOnline } = useOnlineStatus()

// Affiche automatiquement :
// - Toast "Mode hors ligne" quand offline
// - Toast "Connexion rétablie" quand online
```

## 7. Implémentation dans un Nouveau Composant

### Checklist

1. ✅ **Importer le composable useSync**
```typescript
import { useSync } from '@/shared/composables/useSync'
const { handleOfflineOperation, pendingOperationsCount } = useSync()
```

2. ✅ **Utiliser useCrudOperations avec endpoint**
```typescript
const { createEntry } = useCrudOperations(entries)

await createEntry(data, {
  createFunction: apiService.postMonEndpoint,
  endpoint: 'MonEndpoint',  // ← IMPORTANT !
  // ...
})
```

3. ✅ **Pour les appels API directs**
```typescript
await handleOfflineOperation(
  () => apiService.maMethode(data),
  {
    endpoint: 'MonEndpoint',
    method: 'POST',
    data: data,
    onSuccess: (response) => { /* ... */ },
    onOfflineQueued: () => { /* ... */ },
  }
)
```

4. ✅ **Ajouter l'endpoint au syncService**
```typescript
// syncService.ts - executePost()
case 'MonEndpoint':
  return apiService.postMonEndpoint(data)
```

5. ✅ **Afficher les indicateurs UI**
```vue
<template>
  <!-- Badge compteur -->
  <span v-if="pendingOperationsCount > 0">
    {{ pendingOperationsCount }}
  </span>

  <!-- Bouton sync -->
  <Button @click="performSync">Synchroniser</Button>
</template>
```

## 8. Gestion des Erreurs

### Types d'Erreurs

#### A. Erreurs Réseau (Network Error)
**Détection** :
```typescript
const isNetworkError =
  error?.code === 'ERR_NETWORK' ||
  error?.message === 'Network Error' ||
  !navigator.onLine
```

**Action** :
- Sauvegarder l'opération dans IndexedDB
- Incrémenter `retryCount`
- Garder dans la queue

#### B. Erreurs de Validation (400, 422)
**Détection** :
```typescript
error?.response?.status === 400 ||
error?.response?.status === 422
```

**Action** :
- Supprimer de la queue (ne réussira jamais)
- Logger l'erreur
- Notifier l'utilisateur

#### C. Autres Erreurs (500, etc.)
**Action** :
- Incrémenter `retryCount`
- Garder dans la queue
- Logger l'erreur

### Stratégie de Retry

```typescript
// syncService.ts
if (operation.retryCount > 5) {
  // Abandonner après 5 tentatives
  await offlineStorage.removePendingOperation(operation.id)
  console.warn('Max retries reached, removing operation')
}
```

## 9. Limitations et Considérations

### Limitations Actuelles

#### 1. Pas de Détection de Conflits (Last-Write-Wins)

**Le problème** :

Imaginez qu'un utilisateur utilise l'application sur deux appareils différents :

```
📱 Appareil A (hors ligne)          💻 Appareil B (hors ligne)
        ↓                                    ↓
  Modifie douleur #123              Modifie douleur #123
  intensité: 5 → 7                  intensité: 5 → 8
  commentaire: "Mieux"              commentaire: inchangé
        ↓                                    ↓
  Se connecte à 10h00               Se connecte à 10h05
        ↓                                    ↓
  Sync → serveur reçoit:            Sync → serveur reçoit:
  { intensité: 7,                   { intensité: 8,
    commentaire: "Mieux" }            commentaire: "" }
        ↓                                    ↓
  ✅ Sauvegardé                      ✅ Sauvegardé
                                            ↓
                              ❌ La modification de A est écrasée !
                              Résultat final:
                              { intensité: 8,
                                commentaire: "" }  ← Perte du commentaire
```

**Notre implémentation actuelle** :

```typescript
// Last-Write-Wins (Dernier qui écrit gagne)
// L'appareil qui synchronise en dernier écrase les modifications précédentes

const result = await syncService.syncPendingOperations()
// Pour chaque opération :
// 1. POST/PUT vers l'API
// 2. Aucune vérification de version
// 3. Le serveur accepte toujours la dernière écriture
```

**Ce que signifierait un "Merge Automatique"** :

Un système plus avancé détecterait et fusionnerait les conflits :

```typescript
// ✅ Système avec détection de conflits

// Chaque champ a sa propre version
interface DouleurVersioned {
  id: 123,
  intensite: {
    value: 8,
    lastModified: "2025-01-15T10:05:00",  // Appareil B plus récent
    version: 2
  },
  commentaire: {
    value: "Mieux",
    lastModified: "2025-01-15T10:00:00",  // Appareil A
    version: 1
  }
}

// Résultat du merge :
{
  intensité: 8,        // ← De B (plus récent)
  commentaire: "Mieux" // ← De A (seul à l'avoir modifié)
}
```

**Stratégies de résolution possibles** :

1. **Field-level versioning** (Versioning par champ)
```typescript
{
  intensite: { value: 7, version: 2, timestamp: "..." },
  commentaire: { value: "...", version: 1, timestamp: "..." }
}
// Garder la version la plus récente par champ
```

2. **Vector clocks** (Horloges vectorielles)
```typescript
{
  id: 123,
  data: { intensite: 7 },
  vectorClock: {
    appareilA: 2,  // 2 modifications sur appareil A
    appareilB: 1   // 1 modification sur appareil B
  }
}
```

3. **Conflict Resolution UI** (Interface de résolution)
```vue
<Dialog v-if="conflictDetected">
  <h2>Conflit détecté pour la douleur du 15/01</h2>

  <div>
    📱 Appareil A (10h00) : intensité = 7, commentaire = "Mieux"
    💻 Appareil B (10h05) : intensité = 8, commentaire = ""
  </div>

  <Button @click="keepA">Garder A</Button>
  <Button @click="keepB">Garder B</Button>
  <Button @click="merge">Fusionner (intensité=8, commentaire="Mieux")</Button>
</Dialog>
```

4. **CRDT (Conflict-free Replicated Data Types)**
```typescript
// Structures de données mathématiquement garanties sans conflits
import { YMap } from 'yjs'

const douleur = new YMap()
douleur.set('intensite', 7)  // Sur appareil A
douleur.set('intensite', 8)  // Sur appareil B
// → Fusion automatique selon les règles CRDT
```

**Pourquoi on ne l'a pas implémenté ?**

Cette fonctionnalité est **complexe** et nécessite :

- ✋ **Backend modifié** : Support de versioning, détection de conflits
- ✋ **Structure de données différente** : Versionning par champ
- ✋ **UI supplémentaire** : Interface de résolution de conflits
- ✋ **Tests exhaustifs** : Scénarios de conflits multiples
- ✋ **Complexité accrue** : Beaucoup plus de code à maintenir

**Pour une application de santé personnelle** :
- 🎯 Usage typique : 1 utilisateur = 1 appareil
- 🎯 Cas de conflit rare (modifications simultanées peu probables)
- 🎯 **Last-Write-Wins suffit pour le MVP**

**Solution de contournement actuelle** :

```
Utilisateur modifie sur appareil A
        ↓
   Sync immédiate
        ↓
Serveur à jour
        ↓
Si modification sur appareil B
        ↓
Fetch les dernières données d'abord
        ↓
Modification basée sur données à jour
        ↓
Pas de conflit !
```

**Évolution future possible** :

Si l'application évolue vers un usage multi-appareils intensif :

1. Ajouter un champ `version` ou `updatedAt` à chaque ressource
2. Vérifier la version lors du PUT
3. Retourner 409 Conflict si version obsolète
4. Afficher UI de résolution à l'utilisateur

```typescript
// Évolution possible
await handleOfflineOperation(
  () => apiService.updateDouleur(id, {
    ...data,
    version: currentVersion  // ← Ajout
  }),
  {
    endpoint: 'DonneesDouleurs',
    onConflict: (serverData, localData) => {
      // Afficher dialog de résolution
      showConflictDialog(serverData, localData)
    }
  }
)
```

#### 2. Taille du stockage
   - IndexedDB limité par le quota du navigateur
   - Généralement 50MB+ selon le navigateur

3. **Durée de rétention**
   - Les opérations sont gardées 24h max dans le Service Worker
   - Pas de limite dans notre IndexedDB personnalisé

4. **Validation côté client**
   - Important que les validations client matchent celles du serveur
   - Évite les rejets lors de la sync

### Bonnes Pratiques

✅ **Toujours spécifier l'endpoint**
```typescript
// ❌ Mauvais
await createEntry(data, {
  createFunction: apiService.post
})

// ✅ Bon
await createEntry(data, {
  createFunction: apiService.post,
  endpoint: 'MonEndpoint'  // ← Requis !
})
```

✅ **Valider les données avant sauvegarde**
```typescript
const formSchema = z.object({
  intensite: z.number().min(1).max(10),
  // ... même validation que le backend
})
```

✅ **Gérer les callbacks offline**
```typescript
onOfflineQueued: () => {
  // Mettre à jour l'UI optimistiquement
  // Mais ne pas assumer que l'ID sera disponible
}
```

✅ **Tester le mode offline**
```bash
# Dans Chrome DevTools
Network > Offline
```

## 10. Débug

### Outils

#### A. Chrome DevTools

**Application > Storage**
- IndexedDB > MonEndoOffline
  - Voir toutes les opérations en attente
  - Supprimer manuellement si besoin

**Application > Service Workers**
- Voir le Service Worker actif
- Désinstaller / Recharger

**Network > Offline**
- Simuler mode hors ligne

#### B. Logs Console

Tous les services loguent leurs actions :
```
✅ DonneesDouleurs operation saved to IndexedDB for sync
✅ Starting sync of 3 pending operations...
✅ Successfully synced operation create-DonneesDouleurs-...
⚠️ Network error syncing operation, will retry later
❌ Validation error for operation, removing from queue
```

### Commandes Utiles

```javascript
// Dans la console Chrome

// Voir toutes les opérations en attente
const db = await indexedDB.open('MonEndoOffline', 2)
// Puis dans l'onglet Application > IndexedDB

// Forcer une synchronisation
await syncService.syncPendingOperations()

// Vider le cache
await caches.delete('api-cache')

// Compter les opérations
await offlineStorage.getPendingOperationsCount()
```

## 11. Tests

### Scénarios à Tester

1. ✅ **Mode hors ligne basique**
   - Passer hors ligne
   - Créer une entrée
   - Vérifier le badge compteur
   - Revenir en ligne
   - Vérifier la sync automatique

2. ✅ **Opérations multiples**
   - Créer 3-4 entrées hors ligne
   - Vérifier le compteur
   - Sync manuelle
   - Vérifier que toutes sont synced

3. ✅ **Erreurs de validation**
   - Créer une entrée invalide hors ligne
   - Revenir en ligne
   - Vérifier qu'elle est supprimée de la queue

4. ✅ **Persistance**
   - Créer entrée hors ligne
   - Fermer l'onglet
   - Rouvrir
   - Vérifier que l'opération est toujours en attente

5. ✅ **Cache lecture**
   - Charger le dashboard en ligne
   - Passer hors ligne
   - Recharger la page
   - Vérifier que les données sont affichées

## 12. Diagrammes

### Diagramme de Séquence - Création Hors Ligne

```
Utilisateur    Composant    useCrud    IndexedDB    Toast
    |              |            |          |          |
    |─ Submit ────>|            |          |          |
    |              |─ create ──>|          |          |
    |              |            |─ API ✗  |          |
    |              |            |          |          |
    |              |            |─ save ──>|          |
    |              |            |<──────── |          |
    |              |            |          |          |
    |              |            |────── toast ──────> |
    |              |<─ success ─|          |          |
    |<─ feedback ──|            |          |          |
    |              |            |          |          |

[Utilisateur revient en ligne]

    |              |            |          |          |
    |           [Auto-sync déclenché]     |          |
    |              |─ sync ────────────────>          |
    |              |<──── operations ────── |         |
    |              |                        |         |
    |              |─ API POST ✓            |         |
    |              |─ remove ──────────────>|         |
    |              |────── toast ─────────────────────>
    |              |                        |         |
```

### Diagramme d'Architecture

```
┌──────────────────────────────────────────────────────┐
│                   Frontend (Vue.js)                  │
│                                                      │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐   │
│  │ Carnet.vue  │  │DouleursPage │  │  Autres...  │   │
│  └──────┬──────┘  └──────┬──────┘  └──────┬──────┘   │
│         │                │                 │         │
│         └────────────────┼─────────────────┘         │
│                          │                           │
│              ┌───────────▼────────────┐              │
│              │   useSync.ts           │              │
│              │   useCrudOperations.ts │              │
│              └───────────┬────────────┘              │
│                          │                           │
│         ┌────────────────┼────────────────┐          │
│         │                │                │          │
│    ┌────▼─────┐   ┌─────▼──────┐   ┌────▼─────┐      │
│    │apiService│   │offlineStore│   │syncService│     │
│    └────┬─────┘   └─────┬──────┘   └────┬─────┘      │
│         │               │                │           │
└─────────┼───────────────┼────────────────┼────────── ┘
          │               │                │
          │         ┌─────▼──────┐         │
          │         │ IndexedDB  │         │
          │         └────────────┘         │
          │                                │
    ┌─────▼────────────────────────────────▼─────┐
    │          Service Worker (Workbox)          │
    │  • Precache assets                         │
    │  • Stale-While-Revalidate (GET)            │
    │  • Background Sync (POST/PUT/DELETE)       │
    └─────┬──────────────────────────────────────┘
          │
    ┌─────▼─────┐
    │ Backend   │
    │ API       │
    └───────────┘
```

### Pourquoi ne pas utiliser uniquement le Service Worker ?

Le Service Worker (Workbox) est excellent pour le cache et la synchronisation de base, mais :

- ❌ Pas de visibilité UI sur les opérations en attente
- ❌ Pas de contrôle fin sur la logique de retry
- ❌ Difficile à déboguer (logs limités)
- ❌ Pas de customisation par endpoint

Notre système IndexedDB offre un contrôle total et une meilleure UX.

Le fichier `dev-dist/sw.js` est **généré automatiquement** par Vite PWA.

**Modifier** : `vite.config.ts` (section `workbox`)

### Les modifications sont-elles versionnées ?

Non, le système actuel utilise **Last-Write-Wins** :
- Pas de versioning par opération
- Pas de détection de conflits
- La dernière synchronisation écrase les précédentes

C'est acceptable pour un usage mono-appareil.

### Que se passe-t-il si je ferme l'application avec des opérations en attente ?

✅ **Les opérations sont persistées** dans IndexedDB.
- Elles restent en attente même après fermeture
- Au prochain lancement + connexion, elles seront synchronisées
- Le badge affichera le nombre d'opérations en attente

### Combien de temps les opérations sont-elles gardées ?

- **Notre IndexedDB** : Pas de limite de temps (jusqu'à sync réussie)
- **Service Worker** : Maximum 24 heures (configuré dans Workbox)

### Puis-je voir les opérations en attente ?

✅ **Oui**, dans Chrome DevTools :

1. **F12** → Application → Storage → IndexedDB
2. Ouvrir `MonEndoOffline` → `pendingOperations`
3. Voir toutes les opérations avec leurs détails

### Que se passe-t-il si une validation échoue côté serveur ?

```
Opération en attente → Sync → API retourne 400/422
                                    ↓
                    Erreur de validation détectée
                                    ↓
                    Opération supprimée de la queue
                                    ↓
                    Log d'erreur + notification utilisateur
```

L'opération est **retirée** car elle ne réussira jamais.

### Comment ajouter un nouvel endpoint ?

4 étapes simples :

1. **Ajouter dans useCrudOperations** :
```typescript
await createEntry(data, {
  endpoint: 'MonNouvelEndpoint', 
  // ...
})
```

2. **Ajouter dans syncService.ts** :
```typescript
case 'MonNouvelEndpoint':
  return apiService.postMonNouvelEndpoint(data)
```

3. **Ajouter dans apiService.ts** (si pas déjà fait) :
```typescript
async postMonNouvelEndpoint(data: any) {
  return this.request('POST', 'MonNouvelEndpoint', data)
}
```

