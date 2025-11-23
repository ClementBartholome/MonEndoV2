import { ref, onMounted } from 'vue';
import { useToast } from '@/shared/components/ui/toast';
import offlineStorage from '@/shared/services/offlineStorage';
import syncService from '@/shared/services/syncService';

/**
 * Composable for managing offline synchronization
 * Provides sync functionality and pending operations tracking
 */
export function useSync() {
  const { toast } = useToast();
  const pendingOperationsCount = ref(0);
  const isSyncing = ref(false);

  /**
   * Update the count of pending operations
   */
  const updatePendingCount = async () => {
    pendingOperationsCount.value = await offlineStorage.getPendingOperationsCount();
  };

  /**
   * Perform synchronization of all pending operations
   */
  const performSync = async () => {
    if (isSyncing.value) {
      console.log('Sync already in progress');
      return;
    }

    isSyncing.value = true;

    try {
      const result = await syncService.syncPendingOperations();

      // Update pending count
      await updatePendingCount();

      if (result.total > 0) {
        if (result.successful === result.total) {
          toast({
            title: 'Synchronisation réussie',
            description: `${result.successful} opération(s) synchronisée(s)`,
            variant: 'custom',
          });
        } else if (result.successful > 0) {
          toast({
            title: 'Synchronisation partielle',
            description: `${result.successful}/${result.total} opération(s) synchronisée(s)`,
            variant: 'custom',
          });
        } else {
          toast({
            title: 'Échec de synchronisation',
            description: `Impossible de synchroniser les opérations`,
            variant: 'custom',
          });
        }
      }
    } catch (error) {
      console.error('Sync error:', error);
      toast({
        title: 'Erreur',
        description: 'Une erreur est survenue lors de la synchronisation',
        variant: 'custom',
      });
    } finally {
      isSyncing.value = false;
    }
  };

  /**
   * Helper function to handle API calls with offline support
   * Automatically saves to IndexedDB when offline
   */
  const handleOfflineOperation = async <T>(
    apiCall: () => Promise<T>,
    options: {
      endpoint: string;
      method: 'POST' | 'PUT' | 'DELETE';
      data?: any;
      resourceId?: number | string;
      onSuccess?: (response: T) => void;
      onOfflineQueued?: () => void;
      successMessage?: string;
      errorMessage?: string;
    }
  ): Promise<T | undefined> => {
    const {
      endpoint,
      method,
      data,
      resourceId,
      onSuccess,
      onOfflineQueued,
      successMessage,
      errorMessage,
    } = options;

    try {
      const response = await apiCall();
      if (onSuccess) {
        onSuccess(response);
      }
      return response;
    } catch (error: any) {
      // Check if this is a network error (offline mode)
      const isNetworkError =
        error?.code === 'ERR_NETWORK' ||
        error?.message === 'Network Error' ||
        !navigator.onLine;

      if (isNetworkError) {
        // Save operation to IndexedDB for later sync
        try {
          await offlineStorage.savePendingOperation({
            type: method === 'POST' ? 'create' : method === 'PUT' ? 'update' : 'delete',
            endpoint,
            method,
            data,
            resourceId,
          });

          await updatePendingCount();

          if (onOfflineQueued) {
            onOfflineQueued();
          }

          const message = successMessage || 'Opération enregistrée (sera synchronisée)';
          toast({
            title: 'Succès',
            description: message,
            variant: 'custom',
          });

          console.log(`✅ ${endpoint} operation saved to IndexedDB for sync`);
          return undefined;
        } catch (dbError) {
          console.error('Failed to save operation to IndexedDB:', dbError);
          toast({
            title: 'Erreur',
            description: 'Impossible de sauvegarder hors ligne',
            variant: 'custom',
          });
          throw dbError;
        }
      } else {
        // Show error for non-network errors
        toast({
          title: 'Erreur',
          description: errorMessage || 'Une erreur est survenue',
          variant: 'custom',
        });
        console.error('API error:', error);
        throw error;
      }
    }
  };

  onMounted(async () => {
    await updatePendingCount();
  });

  return {
    pendingOperationsCount,
    isSyncing,
    performSync,
    updatePendingCount,
    handleOfflineOperation,
  };
}
