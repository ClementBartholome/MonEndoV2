import { ref, type Ref } from 'vue';
import { useToast } from '@/shared/components/ui/toast';

interface DeleteOptions {
  successMessage?: string;
  errorMessage?: string;
  updateEntries?: boolean;
}

interface CreateOptions<T> {
  createFunction: (data: any) => Promise<T>;
  formatForApi?: (data: any) => any;
  formatForDisplay?: (data: any, response: T) => any;
  successMessage?: string;
  errorMessage?: string;
}

interface UpdateOptions<T> {
  updateFunction: (id: number | string, data: any) => Promise<T>;
  formatForApi?: (data: any) => any;
  formatForDisplay?: (data: any, response: T) => any;
  successMessage?: string;
  errorMessage?: string;
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
    } catch (error) {
      toast({
        title: 'Erreur',
        description: errorMessage,
        variant: 'custom',
      });
      console.error('Delete error:', error);
      return false;
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
    } catch (error) {
      toast({
        title: 'Erreur',
        description: errorMessage,
        variant: 'custom',
      });
      console.error('Create error:', error);
      throw error;
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
    } catch (error) {
      toast({
        title: 'Erreur',
        description: errorMessage,
        variant: 'custom',
      });
      console.error('Update error:', error);
      throw error;
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
