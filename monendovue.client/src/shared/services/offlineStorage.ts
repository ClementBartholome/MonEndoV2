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

class OfflineStorageService {
    private dbName = 'MonEndoOffline';
    private version = 1;
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
    
}

export default new OfflineStorageService();