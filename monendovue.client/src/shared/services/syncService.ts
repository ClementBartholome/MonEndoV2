import offlineStorage, { type PendingOperation } from './offlineStorage';
import apiService from './apiService';

export interface SyncResult {
  total: number;
  successful: number;
  failed: number;
  errors: Array<{ operation: PendingOperation; error: any }>;
}

class SyncService {
  private isSyncing = false;

  /**
   * Synchronizes all pending operations with the server
   * @returns SyncResult with statistics about the sync operation
   */
  async syncPendingOperations(): Promise<SyncResult> {
    // Prevent concurrent sync operations
    if (this.isSyncing) {
      console.log('Sync already in progress, skipping...');
      return { total: 0, successful: 0, failed: 0, errors: [] };
    }

    // Check if we're online
    if (!navigator.onLine) {
      console.log('Cannot sync - device is offline');
      return { total: 0, successful: 0, failed: 0, errors: [] };
    }

    this.isSyncing = true;

    try {
      const pendingOperations = await offlineStorage.getPendingOperations();

      if (pendingOperations.length === 0) {
        console.log('No pending operations to sync');
        return { total: 0, successful: 0, failed: 0, errors: [] };
      }

      console.log(`Starting sync of ${pendingOperations.length} pending operations...`);

      const result: SyncResult = {
        total: pendingOperations.length,
        successful: 0,
        failed: 0,
        errors: []
      };

      // Process operations sequentially to maintain order
      for (const operation of pendingOperations) {
        try {
          await this.executeOperation(operation);
          await offlineStorage.removePendingOperation(operation.id);
          result.successful++;
          console.log(`✅ Successfully synced operation ${operation.id}`);
        } catch (error: any) {
          result.failed++;
          result.errors.push({ operation, error });

          // Check if this is a network error or validation error
          const isNetworkError = error?.code === 'ERR_NETWORK' ||
                                 error?.message === 'Network Error' ||
                                 !navigator.onLine;

          if (isNetworkError) {
            // Network error - keep the operation for retry
            await offlineStorage.incrementRetryCount(operation.id);
            console.log(`⚠️ Network error syncing operation ${operation.id}, will retry later`);
          } else if (error?.response?.status === 400 || error?.response?.status === 422) {
            // Validation error - remove the operation as it will never succeed
            await offlineStorage.removePendingOperation(operation.id);
            console.error(`❌ Validation error for operation ${operation.id}, removing from queue:`, error);
          } else {
            // Other error - increment retry count but keep the operation
            await offlineStorage.incrementRetryCount(operation.id);
            console.error(`❌ Error syncing operation ${operation.id}:`, error);
          }
        }
      }

      console.log(`Sync complete: ${result.successful}/${result.total} successful`);
      return result;
    } finally {
      this.isSyncing = false;
    }
  }

  /**
   * Execute a single pending operation against the API
   */
  private async executeOperation(operation: PendingOperation): Promise<any> {
    const { method, endpoint, data, resourceId } = operation;

    switch (method) {
      case 'POST':
        return this.executePost(endpoint, data);
      case 'PUT':
        return this.executePut(endpoint, resourceId!, data);
      case 'DELETE':
        return this.executeDelete(endpoint, resourceId!);
      default:
        throw new Error(`Unknown method: ${method}`);
    }
  }

  /**
   * Execute a POST operation
   */
  private async executePost(endpoint: string, data: any): Promise<any> {
    // Map endpoint to API service method
    switch (endpoint) {
      case 'DonneesDouleurs':
        return apiService.postDonneesDouleurs(data);
      case 'DonneesActivitePhysique':
        return apiService.postDonneesActivitePhysique(data);
      case 'BilanQuotidien':
        return apiService.postBilanQuotidien(data);
      case 'Medicament':
        return apiService.postMedicament(data);
      case 'DonneesMedicament':
        return apiService.postDonneesPriseMedicament(data);
      case 'DonneesTransit':
        return apiService.postDonneesTransit(data);
      case 'JourRegle':
        return apiService.postJourRegle(data);
      case 'SymptomesCycle':
        return apiService.postDonneesSymptomesCycle(data);
      default:
        throw new Error(`Unknown POST endpoint: ${endpoint}`);
    }
  }

  /**
   * Execute a PUT operation
   */
  private async executePut(endpoint: string, resourceId: number | string, data: any): Promise<any> {
    switch (endpoint) {
      case 'Medicament':
        return apiService.putDonneesMedicament(Number(resourceId), data);
      case 'DonneesDouleurs':
        return apiService.editDonneesDouleurs(Number(resourceId), data);
      default:
        throw new Error(`Unknown PUT endpoint: ${endpoint}`);
    }
  }

  /**
   * Execute a DELETE operation
   */
  private async executeDelete(endpoint: string, resourceId: number | string): Promise<any> {
    switch (endpoint) {
      case 'DonneesActivitePhysique':
        return apiService.deleteDonneesActivitePhysique(Number(resourceId));
      case 'DonneesDouleurs':
        return apiService.deleteDonneesDouleurs(Number(resourceId));
      case 'DonneesMedicament':
        return apiService.deleteDonneesMedicament(Number(resourceId));
      case 'DonneesTransit':
        return apiService.deleteDonneesTransit(Number(resourceId));
      case 'SymptomesCycle':
        return apiService.deleteSymptomeCycle(Number(resourceId));
      default:
        throw new Error(`Unknown DELETE endpoint: ${endpoint}`);
    }
  }

  /**
   * Get the count of pending operations
   */
  async getPendingCount(): Promise<number> {
    return offlineStorage.getPendingOperationsCount();
  }

  /**
   * Get all pending operations
   */
  async getPendingOperations(): Promise<PendingOperation[]> {
    return offlineStorage.getPendingOperations();
  }

  /**
   * Check if sync is currently in progress
   */
  get isSyncInProgress(): boolean {
    return this.isSyncing;
  }
}

export default new SyncService();
