import { fileURLToPath, URL } from 'node:url';

import { defineConfig } from 'vite';
import plugin from '@vitejs/plugin-vue';
import fs from 'fs';
import path from 'path';
import child_process from 'child_process';
import vue from "@vitejs/plugin-vue"
import { VitePWA } from 'vite-plugin-pwa'

import tailwind from "tailwindcss"
import autoprefixer from "autoprefixer"

const baseFolder =
    process.env.APPDATA !== undefined && process.env.APPDATA !== ''
        ? `${process.env.APPDATA}/ASP.NET/https`
        : `${process.env.HOME}/.aspnet/https`;

const certificateArg = process.argv.map(arg => arg.match(/--name=(?<value>.+)/i)).filter(Boolean)[0];
const certificateName = certificateArg ? certificateArg.groups!.value : "monendovue.client";

if (!certificateName) {
    console.error('Invalid certificate name. Run this script in the context of an npm/yarn script or pass --name=<<app>> explicitly.')
    process.exit(-1);
}

const certFilePath = path.join(baseFolder, `${certificateName}.pem`);
const keyFilePath = path.join(baseFolder, `${certificateName}.key`);

if (!fs.existsSync(baseFolder)) {
    fs.mkdirSync(baseFolder, { recursive: true });
}

if (!fs.existsSync(certFilePath) || !fs.existsSync(keyFilePath)) {
    if (0 !== child_process.spawnSync('dotnet', [
        'dev-certs',
        'https',
        '--export-path',
        certFilePath,
        '--format',
        'Pem',
        '--no-password',
    ], { stdio: 'inherit', }).status) {
        throw new Error("Could not create certificate.");
    }
}

// https://vitejs.dev/config/
export default defineConfig({
    css: {
        postcss: {
            plugins: [tailwind(), autoprefixer()],
        },
    },
    plugins: [
        vue(),
        // VitePWA({
        //     registerType: 'autoUpdate',
        //     devOptions: {
        //         enabled: true,
        //         type: 'module',
        //         navigateFallback: 'index.html'
        //     },
        //     workbox: {
        //         globPatterns: ['**/*.{js,css,html,ico,png,svg,woff,woff2}'],
        //         navigateFallback: 'index.html',
        //         navigateFallbackDenylist: [/^\/_/, /\/[^/?]+\.[^/]+$/, /@vite/, /\/src\//],
        //         runtimeCaching: [
        //             // Stale-While-Revalidate for GET API requests
        //             {
        //                 urlPattern: ({ request, url }) => {
        //                     return request.method === 'GET' &&
        //                            url.protocol === 'https:' &&
        //                            url.hostname === 'monendoapp.fr' &&
        //                            url.pathname.startsWith('/api/');
        //                 },
        //                 handler: 'StaleWhileRevalidate',
        //                 options: {
        //                     cacheName: 'api-cache',
        //                     expiration: {
        //                         maxEntries: 100,
        //                         maxAgeSeconds: 60 * 60 * 24 * 7 // 7 days
        //                     },
        //                     cacheableResponse: {
        //                         statuses: [0, 200]
        //                     }
        //                 }
        //             },
        //             // NetworkOnly with Background Sync for all write operations (POST, PUT, DELETE, PATCH)
        //             // Single route with single queue to avoid duplicate-queue-name error
        //             {
        //                 urlPattern: ({ request, url }) => {
        //                     const isWriteMethod = ['POST', 'PUT', 'DELETE', 'PATCH'].includes(request.method);
        //                     const isApiUrl = url.protocol === 'https:' &&
        //                                    url.hostname === 'monendoapp.fr' &&
        //                                    url.pathname.startsWith('/api/');
        //                     return isWriteMethod && isApiUrl;
        //                 },
        //                 handler: 'NetworkOnly',
        //                 options: {
        //                     backgroundSync: {
        //                         name: 'api-queue',
        //                         options: {
        //                             maxRetentionTime: 24 * 60 // Retry for up to 24 hours (in minutes)
        //                         }
        //                     }
        //                 }
        //             },
        //             // Stale-While-Revalidate for Google Calendar API
        //             {
        //                 urlPattern: ({ request, url }) => {
        //                     return request.method === 'GET' &&
        //                            url.protocol === 'https:' &&
        //                            url.hostname === 'www.googleapis.com' &&
        //                            url.pathname.startsWith('/calendar/');
        //                 },
        //                 handler: 'StaleWhileRevalidate',
        //                 options: {
        //                     cacheName: 'google-calendar-cache',
        //                     expiration: {
        //                         maxEntries: 50,
        //                         maxAgeSeconds: 60 * 60 * 4 // 4 hours
        //                     },
        //                     cacheableResponse: {
        //                         statuses: [0, 200]
        //                     }
        //                 }
        //             },
        //             {
        //                 urlPattern: ({ request, url }) => {
        //                     return request.method === 'GET' && 
        //                            url.protocol === 'https:' && 
        //                            url.hostname === 'fonts.googleapis.com';
        //                 },
        //                 handler: 'StaleWhileRevalidate',
        //                 options: {
        //                     cacheName: 'google-fonts-stylesheets',
        //                     expiration: {
        //                         maxEntries: 10,
        //                         maxAgeSeconds: 60 * 60 * 24 * 365 // 1 year
        //                     }
        //                 }
        //             },
        //             {
        //                 urlPattern: ({ request, url }) => {
        //                     return request.method === 'GET' && 
        //                            url.protocol === 'https:' && 
        //                            url.hostname === 'fonts.gstatic.com';
        //                 },
        //                 handler: 'CacheFirst',
        //                 options: {
        //                     cacheName: 'google-fonts-webfonts',
        //                     expiration: {
        //                         maxEntries: 30,
        //                         maxAgeSeconds: 60 * 60 * 24 * 365 // 1 year
        //                     }
        //                 }
        //             }
        //         ]
        //     },
        //     manifest: {
        //         name: 'MonEndo',
        //         short_name: 'MonEndo',
        //         description: 'Application de suivi des symptômes de l\'endométriose',
        //         theme_color: '#4DBA87',
        //         background_color: '#ffffff',
        //         display: 'standalone',
        //         start_url: '/',
        //         icons: [
        //             {
        //                 src: 'https://monendoapp.fr/assets/MonEndoIconMobile-Bx93s-ND.jpg',
        //                 sizes: '235x235',
        //                 type: 'image/png'
        //             }
        //         ]
        //     }
        // })
    ],
    resolve: {
        alias: {
            "@": path.resolve(__dirname, "./src"),
        },
        extensions: ['.vue', '.ts', '.js'],
    },
    server: {
        host: '0.0.0.0',
        https: {
            key: fs.readFileSync(keyFilePath),
            cert: fs.readFileSync(certFilePath),
        },
        port: 5173,
    },
    build: {
        outDir: 'dist',
    }
});