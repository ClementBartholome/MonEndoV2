import { ref, watch, onMounted, type Ref } from 'vue';
import { useDateTimeFormat } from './useDateTimeFormat';
import offlineStorage from '@/shared/services/offlineStorage';

interface UseMonthDataOptions<T> {
  fetchFunction: (month: number, year: number) => Promise<T[]>;
  transformData?: (data: T[]) => any[];
  immediate?: boolean;
  dataType?: string;
}

export function useMonthData<T = any>(options: UseMonthDataOptions<T>) {
  const { fetchFunction, transformData, immediate = true, dataType } = options;
  const { getCurrentMonthYear } = useDateTimeFormat();

  const selectedMonthYear = ref(getCurrentMonthYear());
  const entries = ref<any[]>([]) as Ref<any[]>;
  const isLoading = ref(false);
  const error = ref<Error | null>(null);

  const fetchData = async (month: number, year: number) => {
    isLoading.value = true;
    error.value = null;

    if (dataType) {
      const cachedData = await offlineStorage.getMonthData(dataType, month, year);
      if (cachedData) {
        entries.value = cachedData;
        isLoading.value = false;
      }
    }

    if (navigator.onLine) {
      try {
        const data = await fetchFunction(month, year);
        const processedData = transformData ? transformData(data) : data;
        entries.value = processedData;

        if (dataType) {
          await offlineStorage.saveMonthData(dataType, month, year, processedData);
        }
      } catch (err) {
        error.value = err as Error;
        console.error('Error fetching month data:', err);
        if (!dataType || entries.value.length === 0) {
          entries.value = [];
        }
      } finally {
        isLoading.value = false;
      }
    } else if (!dataType) {
      entries.value = [];
      isLoading.value = false;
    } else {
      isLoading.value = false;
    }
  };

  const refetch = () => {
    const [year, month] = selectedMonthYear.value.split('-').map(Number);
    return fetchData(month, year);
  };

  watch(selectedMonthYear, (newValue) => {
    const [year, month] = newValue.split('-').map(Number);
    fetchData(month, year);
  });

  if (immediate) {
    onMounted(() => {
      refetch();
    });
  }

  return {
    selectedMonthYear,
    entries,
    isLoading,
    error,
    fetchData,
    refetch,
  };
}
