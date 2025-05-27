import axios from 'axios';
import type { Item } from '@/models/calendar-events/item';

const APP_URL = 'https://monendoapp.fr/'

const calendarOptions = {
    googleCalendarApiKey: import.meta.env.VITE_GOOGLE_CALENDAR_API_KEY,
    googleCalendarId: import.meta.env.VITE_GOOGLE_CALENDAR_ID
};

const googleApiService = {
    async getAuthorizationUrl(userId: string) {
        const response = await axios.get(`${APP_URL}GoogleApi/authenticate?userId=${userId}`);
        return response.data;
    },
    async getUpcomingEvents(accessToken) {
        const response = await axios.get('https://www.googleapis.com/calendar/v3/calendars/primary/events', {
            params: {
                maxResults: 4,
                singleEvents: true,
                orderBy: 'startTime',
                timeMin: new Date().toISOString(),
            }
        });

        return response.data.items;
    },
    async refreshAccessToken(userId: string, refreshToken: string) {
        const response = await axios.get(`${APP_URL}GoogleApi/refreshToken?userId=${userId}&refreshToken=${refreshToken}`);
        return response.data;
    },
    async getThreeNextEvents() {
        localStorage.removeItem('events');
        let pageToken;
        let allEvents: Item[] = [];
        const now = new Date().toISOString();
        do {
            const response = await fetch(`https://www.googleapis.com/calendar/v3/calendars/${calendarOptions.googleCalendarId}/events?key=${calendarOptions.googleCalendarApiKey}${pageToken ? '&pageToken=' + pageToken : ''}`);
            const data = await response.json();
            allEvents = allEvents.concat(data.items.filter(event => event.start.dateTime > now));
            pageToken = data.nextPageToken;
        } while (pageToken);

        allEvents = allEvents.filter(event => event.start.dateTime).sort((a, b) => Date.parse(a.start.dateTime!) - Date.parse(b.start.dateTime!));
        localStorage.setItem('events', JSON.stringify(allEvents));
        return allEvents.slice(0, 3);
    }
}

export default googleApiService;