
import { defineConfig } from 'vite';
import path from 'path';
import vue from "@vitejs/plugin-vue"
import { VitePWA } from 'vite-plugin-pwa'

import tailwind from "tailwindcss"
import autoprefixer from "autoprefixer"

// https://vitejs.dev/config/
export default defineConfig({
    css: {
        postcss: {
            plugins: [tailwind(), autoprefixer()],
        },
    },
    plugins: [
        vue(),
        VitePWA({
            registerType: 'autoUpdate',
            devOptions: {
                enabled: true,
                type: 'module',
                navigateFallback: 'index.html'
            },
            workbox: {
                globPatterns: ['**/*.{js,css,html,ico,png,svg,woff,woff2}'],
                navigateFallback: 'index.html',
                navigateFallbackDenylist: [/^\/_/, /\/[^/?]+\.[^/]+$/],
                runtimeCaching: [
                    {
                        urlPattern: /^https:\/\/monendoapp\.fr\/api\//,
                        handler: 'NetworkFirst',
                        options: {
                            cacheName: 'api-cache',
                            expiration: {
                                maxEntries: 100,
                                maxAgeSeconds: 60 * 60 * 24 // 24 hours
                            },
                            networkTimeoutSeconds: 3
                        }
                    },
                    {
                        urlPattern: /^https:\/\/www\.googleapis\.com\/calendar\//,
                        handler: 'NetworkFirst',
                        options: {
                            cacheName: 'google-calendar-cache',
                            expiration: {
                                maxEntries: 50,
                                maxAgeSeconds: 60 * 60 * 2 // 2 hours
                            },
                            networkTimeoutSeconds: 3
                        }
                    }
                ]
            },
            manifest: {
                name: 'MonEndo',
                short_name: 'MonEndo',
                description: 'Application de suivi des symptômes de l\'endométriose',
                theme_color: '#faeee7',
                background_color: '#f7e7e0',
                display: 'standalone',
                start_url: '/',
                icons: [
                    {
                        src: 'https://monendoapp.fr/assets/MonEndoIconMobile-Bx93s-ND.jpg',
                        sizes: '235x235',
                        type: 'image/png'
                    }
                ]
            }
        })
    ],
    resolve: {
        alias: {
            "@": path.resolve(__dirname, "./src"),
        },
        extensions: ['.vue', '.ts', '.js'],
    },
    server: {
        host: '0.0.0.0',
        port: 5173,
        // Remove HTTPS for development
    },
    build: {
        outDir: 'dist',
    }
});