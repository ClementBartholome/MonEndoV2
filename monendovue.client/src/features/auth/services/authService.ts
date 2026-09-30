import axios from 'axios';

const API_URL = import.meta.env.VITE_DOCKER === 'true'
    ? '/' // Même serveur : chemins absolus, justes quelle que soit la page (ex. /medicaments/12)
    : (import.meta.env.MODE === 'production' ? import.meta.env.VITE_API_URL_PROD : import.meta.env.VITE_API_URL);

const authService = {
    async login(email: string, password: string) {
        try {
            const response = await axios.post(`${API_URL}Account/login`, { email, password }, {
                headers: {
                    'Content-Type': 'application/json'
                },
                withCredentials: true
            });

            if (response.status === 200) {
                return response.data;
            }
        } catch (error: any) {
            console.error(error);
            if (error.response) {
                console.error(error.response.data);
            }
            return null;
        }
    },

    async register(email: string, password: string, consentementDonneesSante: boolean) {
        try {
            const response = await axios.post(`${API_URL}Account/register`, { email, password, consentementDonneesSante }, {
                headers: {
                    'Content-Type': 'application/json'
                },
                withCredentials: true
            });

            if (response.status === 200) {
                return response.data;
            } else {
                throw new Error(response.data.$values[0].description);
            }
        } catch (error: any) {
            if (error.response && error.response.status === 400) {
                throw new Error(error.response.data?.$values?.[0] ?? "Requête invalide.");
            }
            throw error;
        }
    },

    async logout() {
        try {
            await axios.post(`${API_URL}Account/logout`, {
                headers: {
                    'Content-Type': 'application/json'
                },
                withCredentials: true
            });
        } catch (error) {
            console.error(error);
        }
    },
    
    async changePassword(currentPassword: string, newPassword: string) {
        try {
            const response = await axios.post(`${API_URL}Account/change-password`, { currentPassword, newPassword }, {
                headers: {
                    'Content-Type': 'application/json'
                },
                withCredentials: true
            });

            if (response.status === 200) {
                return response.data;
            } else {
                throw new Error(response.data.$values[0].description);
            }
        } catch (error: any) {
            if (error.response && error.response.status === 400) {
                throw new Error(error.response.data?.$values?.[0] ?? "Requête invalide.");
            }
            throw error;
        }
    }
};

export default authService;