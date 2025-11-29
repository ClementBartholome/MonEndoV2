import { type Ref } from 'vue';
import { useToast } from '@/shared/components/ui/toast';

interface DialogFormOptions<T> {
  submitFunction: (data: any) => Promise<T>;
  successMessage?: string;
  errorMessage?: string;
  onSuccess?: (response: T) => void;
  onError?: (error: any) => void;
  resetFormData?: () => void;
}

export function useDialogForm<T = any>(dialogRef: Ref<boolean>) {
  const { toast } = useToast();

  const submitForm = async (data: any, options: DialogFormOptions<T>) => {
    const {
      submitFunction,
      successMessage = 'Formulaire soumis avec succès',
      errorMessage = 'Une erreur est survenue lors de la soumission',
      onSuccess,
      onError,
      resetFormData,
    } = options;

    try {
      const response = await submitFunction(data);

      // Show success toast
      toast({
        title: 'Succès',
        description: successMessage,
        variant: 'custom',
      });

      // Execute custom success callback if provided
      if (onSuccess) {
        onSuccess(response);
      }

      // Reset form data if reset function is provided
      if (resetFormData) {
        resetFormData();
      }

      // Close dialog
      dialogRef.value = false;

      return response;
    } catch (error: any) {
      console.error('Form submission error:', error);

      // Show error toast
      toast({
        title: 'Erreur',
        description: errorMessage,
        variant: 'destructive',
      });

      // Execute custom error callback if provided
      if (onError) {
        onError(error);
      }

      throw error;
    }
  };

  return {
    submitForm,
  };
}
