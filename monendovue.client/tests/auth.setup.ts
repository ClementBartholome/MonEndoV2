import { test as setup } from '@playwright/test';

const authFile = 'playwright/.auth/user.json';

setup('authenticate', async ({ request }) => {
    await request.post('https://monendoapp.fr/login?email=testuser@gmail.com&password=Password123$', {
        headers: {
            'Content-Type': 'application/json'
        },
    });
    await request.storageState({ path: authFile });
});

/*

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
 */