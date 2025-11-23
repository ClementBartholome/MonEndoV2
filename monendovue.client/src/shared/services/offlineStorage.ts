interface CarnetData {
    id: string;
    carnetSanteId: number;
    data: any;
    timestamp: number;
}

interface CalendarEvent {
    id: string;
    events: any[];
    timestamp: number;
}

interface MonthData {
    id: string;
    dataType: string;
    month: number;
    year: number;
    data: any[];
    timestamp: number;
}

export interface PendingOperation {
    id: string;
    type: 'create' | 'update' | 'delete';
    endpoint: string; // e.g., 'DonneesDouleurs', 'DonneesActivitePhysique'
    method: 'POST' | 'PUT' | 'DELETE';
    data?: any; // the payload for POST/PUT
    resourceId?: number | string; // for PUT/DELETE operations
    timestamp: number;
    retryCount: number;
}

class OfflineStorageService {
    private dbName = 'MonEndoOffline';
    private version = 3;
    private db: IDBDatabase | null = null;

    async init(): Promise<void> {
        return new Promise((resolve, reject) => {
            const request = indexedDB.open(this.dbName, this.version);

            request.onerror = () => reject(request.error);
            request.onsuccess = () => {
                this.db = request.result;
                resolve();
            };

            request.onupgradeneeded = (event) => {
                const db = (event.target as IDBOpenDBRequest).result;

                // Store for Carnet health data
                if (!db.objectStoreNames.contains('carnetData')) {
                    const carnetStore = db.createObjectStore('carnetData', { keyPath: 'id' });
                    carnetStore.createIndex('carnetSanteId', 'carnetSanteId', { unique: false });
                    carnetStore.createIndex('timestamp', 'timestamp', { unique: false });
                }

                // Store for calendar events
                if (!db.objectStoreNames.contains('calendarEvents')) {
                    const calendarStore = db.createObjectStore('calendarEvents', { keyPath: 'id' });
                    calendarStore.createIndex('timestamp', 'timestamp', { unique: false });
                }

                // Store for pending operations (create/update/delete)
                if (!db.objectStoreNames.contains('pendingOperations')) {
                    const pendingStore = db.createObjectStore('pendingOperations', { keyPath: 'id' });
                    pendingStore.createIndex('timestamp', 'timestamp', { unique: false });
                    pendingStore.createIndex('type', 'type', { unique: false });
                }

                if (!db.objectStoreNames.contains('monthData')) {
                    const monthStore = db.createObjectStore('monthData', { keyPath: 'id' });
                    monthStore.createIndex('dataType', 'dataType', { unique: false });
                    monthStore.createIndex('timestamp', 'timestamp', { unique: false });
                }
            };
        });
    }

    async saveCarnetData(carnetSanteId: number, data: any): Promise<void> {
        if (!this.db) await this.init();
        
        const carnetData: CarnetData = {
            id: `carnet-${carnetSanteId}`,
            carnetSanteId,
            data,
            timestamp: Date.now()
        };

        return new Promise((resolve, reject) => {
            const transaction = this.db!.transaction(['carnetData'], 'readwrite');
            const store = transaction.objectStore('carnetData');
            const request = store.put(carnetData);

            request.onerror = () => reject(request.error);
            request.onsuccess = () => resolve();
        });
    }

    async getCarnetData(carnetSanteId: number): Promise<any | null> {
        if (!this.db) await this.init();

        return new Promise((resolve, reject) => {
            const transaction = this.db!.transaction(['carnetData'], 'readonly');
            const store = transaction.objectStore('carnetData');
            const request = store.get(`carnet-${carnetSanteId}`);

            request.onerror = () => reject(request.error);
            request.onsuccess = () => {
                const result = request.result as CarnetData | undefined;
                if (result && this.isDataFresh(result.timestamp, 24 * 60 * 60 * 1000)) { // 24 hours
                    resolve(result.data);
                } else {
                    resolve(null);
                }
            };
        });
    }

    async saveCalendarEvents(events: any[]): Promise<void> {
        if (!this.db) await this.init();
        
        const calendarEvent: CalendarEvent = {
            id: 'calendar-events',
            events,
            timestamp: Date.now()
        };

        return new Promise((resolve, reject) => {
            const transaction = this.db!.transaction(['calendarEvents'], 'readwrite');
            const store = transaction.objectStore('calendarEvents');
            const request = store.put(calendarEvent);

            request.onerror = () => reject(request.error);
            request.onsuccess = () => resolve();
        });
    }

    async getCalendarEvents(): Promise<any[] | null> {
        if (!this.db) await this.init();

        return new Promise((resolve, reject) => {
            const transaction = this.db!.transaction(['calendarEvents'], 'readonly');
            const store = transaction.objectStore('calendarEvents');
            const request = store.get('calendar-events');

            request.onerror = () => reject(request.error);
            request.onsuccess = () => {
                const result = request.result as CalendarEvent | undefined;
                if (result && this.isDataFresh(result.timestamp, 2 * 60 * 60 * 1000)) { // 2 heures
                    resolve(result.events);
                } else {
                    resolve(null);
                }
            };
        });
    }

    private isDataFresh(timestamp: number, maxAge: number): boolean {
        return Date.now() - timestamp < maxAge;
    }

    // Pending Operations Management
    async savePendingOperation(operation: Omit<PendingOperation, 'id' | 'timestamp' | 'retryCount'>): Promise<string> {
        if (!this.db) await this.init();

        const id = `${operation.type}-${operation.endpoint}-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`;
        const pendingOp: PendingOperation = {
            ...operation,
            id,
            timestamp: Date.now(),
            retryCount: 0
        };

        return new Promise((resolve, reject) => {
            const transaction = this.db!.transaction(['pendingOperations'], 'readwrite');
            const store = transaction.objectStore('pendingOperations');
            const request = store.add(pendingOp);

            request.onerror = () => reject(request.error);
            request.onsuccess = () => resolve(id);
        });
    }

    async getPendingOperations(): Promise<PendingOperation[]> {
        if (!this.db) await this.init();

        return new Promise((resolve, reject) => {
            const transaction = this.db!.transaction(['pendingOperations'], 'readonly');
            const store = transaction.objectStore('pendingOperations');
            const request = store.getAll();

            request.onerror = () => reject(request.error);
            request.onsuccess = () => {
                const operations = request.result as PendingOperation[];
                // Sort by timestamp to maintain order
                operations.sort((a, b) => a.timestamp - b.timestamp);
                resolve(operations);
            };
        });
    }

    async getPendingOperationsCount(): Promise<number> {
        if (!this.db) await this.init();

        return new Promise((resolve, reject) => {
            const transaction = this.db!.transaction(['pendingOperations'], 'readonly');
            const store = transaction.objectStore('pendingOperations');
            const request = store.count();

            request.onerror = () => reject(request.error);
            request.onsuccess = () => resolve(request.result);
        });
    }

    async removePendingOperation(id: string): Promise<void> {
        if (!this.db) await this.init();

        return new Promise((resolve, reject) => {
            const transaction = this.db!.transaction(['pendingOperations'], 'readwrite');
            const store = transaction.objectStore('pendingOperations');
            const request = store.delete(id);

            request.onerror = () => reject(request.error);
            request.onsuccess = () => resolve();
        });
    }

    async incrementRetryCount(id: string): Promise<void> {
        if (!this.db) await this.init();

        return new Promise((resolve, reject) => {
            const transaction = this.db!.transaction(['pendingOperations'], 'readwrite');
            const store = transaction.objectStore('pendingOperations');
            const getRequest = store.get(id);

            getRequest.onerror = () => reject(getRequest.error);
            getRequest.onsuccess = () => {
                const operation = getRequest.result as PendingOperation;
                if (operation) {
                    operation.retryCount++;
                    const putRequest = store.put(operation);
                    putRequest.onerror = () => reject(putRequest.error);
                    putRequest.onsuccess = () => resolve();
                } else {
                    resolve();
                }
            };
        });
    }

    async clearAllPendingOperations(): Promise<void> {
        if (!this.db) await this.init();

        return new Promise((resolve, reject) => {
            const transaction = this.db!.transaction(['pendingOperations'], 'readwrite');
            const store = transaction.objectStore('pendingOperations');
            const request = store.clear();

            request.onerror = () => reject(request.error);
            request.onsuccess = () => resolve();
        });
    }

    async saveMonthData(dataType: string, month: number, year: number, data: any[]): Promise<void> {
        if (!this.db) await this.init();

        const monthData: MonthData = {
            id: `${dataType}-${year}-${month}`,
            dataType,
            month,
            year,
            data,
            timestamp: Date.now()
        };

        return new Promise((resolve, reject) => {
            const transaction = this.db!.transaction(['monthData'], 'readwrite');
            const store = transaction.objectStore('monthData');
            const request = store.put(monthData);

            request.onerror = () => reject(request.error);
            request.onsuccess = () => resolve();
        });
    }

    async getMonthData(dataType: string, month: number, year: number): Promise<any[] | null> {
        if (!this.db) await this.init();

        return new Promise((resolve, reject) => {
            const transaction = this.db!.transaction(['monthData'], 'readonly');
            const store = transaction.objectStore('monthData');
            const request = store.get(`${dataType}-${year}-${month}`);

            request.onerror = () => reject(request.error);
            request.onsuccess = () => {
                const result = request.result as MonthData | undefined;
                if (result && this.isDataFresh(result.timestamp, 24 * 60 * 60 * 1000)) {
                    resolve(result.data);
                } else {
                    resolve(null);
                }
            };
        });
    }

}

export default new OfflineStorageService();