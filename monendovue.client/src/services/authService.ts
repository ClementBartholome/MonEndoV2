import axios from 'axios';

const API_URL = import.meta.env.MODE === 'production' ? import.meta.env.VITE_API_URL_PROD : import.meta.env.VITE_API_URL;

const authService = {
    async login(email: string, password: string) {
        try {
            const response = await axios.post(`${API_URL}Account/login?email=${encodeURIComponent(email)}&password=${encodeURIComponent(password)}`, null, {
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

    async register(email: string, password: string) {
        try {
            const response = await axios.post(`${API_URL}Account/register?email=${encodeURIComponent(email)}&password=${encodeURIComponent(password)}`, null, {
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
                throw new Error(error.response.data.$values[0]);
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
            const response = await axios.post(`${API_URL}Account/change-password?currentPassword=${encodeURIComponent(currentPassword)}&newPassword=${encodeURIComponent(newPassword)}`, null, {
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
                throw new Error(error.response.data.$values[0]);
            }
            throw error;
        }
    }
};

export default authService;