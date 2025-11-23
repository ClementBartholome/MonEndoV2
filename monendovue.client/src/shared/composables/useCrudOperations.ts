import { ref, type Ref } from 'vue';
import { useToast } from '@/shared/components/ui/toast';
import offlineStorage from '@/shared/services/offlineStorage';

interface DeleteOptions {
  successMessage?: string;
  errorMessage?: string;
  updateEntries?: boolean;
  endpoint?: string; // e.g., 'DonneesDouleurs'
}

interface CreateOptions<T> {
  createFunction: (data: any) => Promise<T>;
  formatForApi?: (data: any) => any;
  formatForDisplay?: (data: any, response: T) => any;
  successMessage?: string;
  errorMessage?: string;
  endpoint?: string; // e.g., 'DonneesDouleurs'
}

interface UpdateOptions<T> {
  updateFunction: (id: number | string, data: any) => Promise<T>;
  formatForApi?: (data: any) => any;
  formatForDisplay?: (data: any, response: T) => any;
  successMessage?: string;
  errorMessage?: string;
  endpoint?: string; // e.g., 'DonneesDouleurs'
}

export function useCrudOperations<T = any>(entries: Ref<any[]>) {
  const { toast } = useToast();
  const isDeleting = ref(false);
  const isCreating = ref(false);
  const isUpdating = ref(false);

  const deleteEntry = async (
    id: number | string,
    deleteFunction: (id: number | string) => Promise<void>,
    options: DeleteOptions = {}
  ) => {
    const {
      successMessage = 'Entrée supprimée avec succès',
      errorMessage = 'Une erreur est survenue lors de la suppression',
      updateEntries = true,
      endpoint,
    } = options;

    isDeleting.value = true;

    try {
      await deleteFunction(id);

      if (updateEntries) {
        entries.value = entries.value.filter((entry) => entry.id !== id);
      }

      toast({
        title: 'Succès',
        description: successMessage,
        variant: 'custom',
      });

      return true;
    } catch (error: any) {
      // Check if this is a network error (offline mode)
      const isNetworkError = error?.code === 'ERR_NETWORK' ||
                             error?.message === 'Network Error' ||
                             !navigator.onLine;

      if (isNetworkError && endpoint) {
        // Save operation to IndexedDB for later sync
        try {
          await offlineStorage.savePendingOperation({
            type: 'delete',
            endpoint,
            method: 'DELETE',
            resourceId: id,
          });

          // Optimistically remove from UI
          if (updateEntries) {
            entries.value = entries.value.filter((entry) => entry.id !== id);
          }

          console.log(`✅ Delete operation saved to IndexedDB for sync: ${endpoint}/${id}`);

          // Show success message (it will sync when online)
          toast({
            title: 'Succès',
            description: successMessage + ' (sera synchronisée)',
            variant: 'custom',
          });

          return true;
        } catch (dbError) {
          console.error('Failed to save delete operation to IndexedDB:', dbError);
          toast({
            title: 'Erreur',
            description: 'Impossible de sauvegarder hors ligne',
            variant: 'custom',
          });
          return false;
        }
      } else if (isNetworkError) {
        // No endpoint provided - fallback to old behavior
        console.log('✅ Delete queued for background sync (no endpoint)');
        return true;
      } else {
        // Show error for non-network errors
        toast({
          title: 'Erreur',
          description: errorMessage,
          variant: 'custom',
        });
        console.error('Delete error:', error);
        return false;
      }
    } finally {
      isDeleting.value = false;
    }
  };

  const createEntry = async (data: any, options: CreateOptions<T>) => {
    const {
      createFunction,
      formatForApi,
      formatForDisplay,
      successMessage = 'Entrée créée avec succès',
      errorMessage = 'Une erreur est survenue lors de la création',
      endpoint,
    } = options;

    isCreating.value = true;

    try {
      const apiData = formatForApi ? formatForApi(data) : data;
      const response = await createFunction(apiData);

      const displayData = formatForDisplay
        ? formatForDisplay(data, response)
        : { ...data, id: (response as any).id };

      entries.value.push(displayData);

      toast({
        title: 'Succès',
        description: successMessage,
        variant: 'custom',
      });

      return response;
    } catch (error: any) {
      // Check if this is a network error (offline mode)
      const isNetworkError = error?.code === 'ERR_NETWORK' ||
                             error?.message === 'Network Error' ||
                             !navigator.onLine;

      if (isNetworkError && endpoint) {
        // Save operation to IndexedDB for later sync
        try {
          const apiData = formatForApi ? formatForApi(data) : data;

          await offlineStorage.savePendingOperation({
            type: 'create',
            endpoint,
            method: 'POST',
            data: apiData,
          });

          console.log(`✅ Create operation saved to IndexedDB for sync: ${endpoint}`);

          // Show success message (it will sync when online)
          toast({
            title: 'Succès',
            description: successMessage + ' (sera synchronisée)',
            variant: 'custom',
          });

          // Don't add to UI for creates - wait for sync to complete
          // This avoids having temporary IDs
          return undefined;
        } catch (dbError) {
          console.error('Failed to save create operation to IndexedDB:', dbError);
          toast({
            title: 'Erreur',
            description: 'Impossible de sauvegarder hors ligne',
            variant: 'custom',
          });
          throw dbError;
        }
      } else if (isNetworkError) {
        // No endpoint provided - fallback to old behavior
        console.log('✅ Request queued for background sync (no endpoint)');
        return undefined;
      } else {
        // Show error for non-network errors
        toast({
          title: 'Erreur',
          description: errorMessage,
          variant: 'custom',
        });
        console.error('Create error:', error);
        throw error;
      }
    } finally {
      isCreating.value = false;
    }
  };

  const updateEntry = async (
    id: number | string,
    data: any,
    options: UpdateOptions<T>
  ) => {
    const {
      updateFunction,
      formatForApi,
      formatForDisplay,
      successMessage = 'Entrée mise à jour avec succès',
      errorMessage = 'Une erreur est survenue lors de la mise à jour',
      endpoint,
    } = options;

    isUpdating.value = true;

    try {
      const apiData = formatForApi ? formatForApi(data) : data;
      const response = await updateFunction(id, apiData);

      const entryIndex = entries.value.findIndex((entry) => entry.id === id);
      if (entryIndex !== -1) {
        const displayData = formatForDisplay
          ? formatForDisplay(data, response)
          : { ...data, id };

        entries.value[entryIndex] = displayData;
      }

      toast({
        title: 'Succès',
        description: successMessage,
        variant: 'custom',
      });

      return response;
    } catch (error: any) {
      // Check if this is a network error (offline mode)
      const isNetworkError = error?.code === 'ERR_NETWORK' ||
                             error?.message === 'Network Error' ||
                             !navigator.onLine;

      if (isNetworkError && endpoint) {
        // Save operation to IndexedDB for later sync
        try {
          const apiData = formatForApi ? formatForApi(data) : data;

          await offlineStorage.savePendingOperation({
            type: 'update',
            endpoint,
            method: 'PUT',
            data: apiData,
            resourceId: id,
          });

          // Optimistically update in UI
          const entryIndex = entries.value.findIndex((entry) => entry.id === id);
          if (entryIndex !== -1) {
            const displayData = formatForDisplay
              ? formatForDisplay(data, {} as T)
              : { ...data, id };

            entries.value[entryIndex] = displayData;
          }

          console.log(`✅ Update operation saved to IndexedDB for sync: ${endpoint}/${id}`);

          // Show success message (it will sync when online)
          toast({
            title: 'Succès',
            description: successMessage + ' (sera synchronisée)',
            variant: 'custom',
          });

          return undefined;
        } catch (dbError) {
          console.error('Failed to save update operation to IndexedDB:', dbError);
          toast({
            title: 'Erreur',
            description: 'Impossible de sauvegarder hors ligne',
            variant: 'custom',
          });
          throw dbError;
        }
      } else if (isNetworkError) {
        // No endpoint provided - fallback to old behavior
        console.log('✅ Update queued for background sync (no endpoint)');
        return undefined;
      } else {
        // Show error for non-network errors
        toast({
          title: 'Erreur',
          description: errorMessage,
          variant: 'custom',
        });
        console.error('Update error:', error);
        throw error;
      }
    } finally {
      isUpdating.value = false;
    }
  };

  return {
    deleteEntry,
    createEntry,
    updateEntry,
    isDeleting,
    isCreating,
    isUpdating,
  };
}
