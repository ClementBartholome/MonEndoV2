import { type Ref } from 'vue';
import { useToast } from '@/shared/components/ui/toast';

interface DialogFormOptions<T> {
  submitFunction: (data: any) => Promise<T>;
  successMessage?: string;
  errorMessage?: string;
  onSuccess?: (response: T) => void | Promise<void>;
  onError?: (error: any) => void;
  resetFormData?: () => void;
}

export function useDialogForm<T = any>(dialogRef: Ref<boolean>) {
  const { toast } = useToast();

  const getErrorDescription = (error: any, fallback: string): string => {
    const responseData = error?.response?.data;

    if (typeof responseData?.message === 'string' && responseData.message.length > 0) {
      return responseData.message;
    }

    if (typeof responseData?.title === 'string' && responseData.title.length > 0) {
      return responseData.title;
    }

    const validationErrors = responseData?.errors;
    if (validationErrors && typeof validationErrors === 'object') {
      const firstEntry = Object.values(validationErrors).find((value) => Array.isArray(value) && value.length > 0) as string[] | undefined;
      if (firstEntry && typeof firstEntry[0] === 'string') {
        return firstEntry[0];
      }
    }

    if (typeof error?.message === 'string' && error.message.length > 0) {
      return error.message;
    }

    return fallback;
  };

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
        await onSuccess(response);
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
        description: getErrorDescription(error, errorMessage),
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
