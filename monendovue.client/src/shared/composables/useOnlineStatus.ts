import { ref, onMounted, onUnmounted } from 'vue';
import { useToast } from '@/shared/components/ui/toast';

/**
 * Composable for monitoring online/offline status and showing user feedback
 * Works with the PWA background sync to provide a seamless offline experience
 */
export function useOnlineStatus() {
  const { toast } = useToast();
  const isOnline = ref(navigator.onLine);

  let onlineToastId: any = null;
  let offlineToastId: any = null;

  const handleOnline = () => {
    isOnline.value = true;

    // Dismiss offline toast if it exists
    if (offlineToastId) {
      offlineToastId.dismiss();
      offlineToastId = null;
    }

    // Show online toast
    onlineToastId = toast({
      title: '🌐 Connexion rétablie',
      description: 'Synchronisation automatique en cours...',
      variant: 'custom',
    });

    // Auto-dismiss after 3 seconds
    setTimeout(() => {
      if (onlineToastId) {
        onlineToastId.dismiss();
        onlineToastId = null;
      }
    }, 3000);
  };

  const handleOffline = () => {
    isOnline.value = false;

    // Dismiss online toast if it exists
    if (onlineToastId) {
      onlineToastId.dismiss();
      onlineToastId = null;
    }

    // Show offline toast (persistent)
    offlineToastId = toast({
      title: '📵 Mode hors ligne',
      description: 'Vos modifications seront synchronisées automatiquement lorsque la connexion sera rétablie.',
      variant: 'custom',
    });
  };

  onMounted(() => {
    // Add online/offline event listeners
    window.addEventListener('online', handleOnline);
    window.addEventListener('offline', handleOffline);

    // Check initial status
    if (!navigator.onLine) {
      handleOffline();
    }
  });

  onUnmounted(() => {
    window.removeEventListener('online', handleOnline);
    window.removeEventListener('offline', handleOffline);

    // Clean up any active toasts
    if (onlineToastId) {
      onlineToastId.dismiss();
    }
    if (offlineToastId) {
      offlineToastId.dismiss();
    }
  });

  return {
    isOnline,
  };
}
