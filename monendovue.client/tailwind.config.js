const animate = require("tailwindcss-animate")

/** @type {import('tailwindcss').Config} */
module.exports = {
    darkMode: ["class"],
    safelist: ["dark"],
    prefix: "",

    content: [
        './pages/**/*.{ts,tsx,vue}',
        './components/**/*.{ts,tsx,vue}',
        './app/**/*.{ts,tsx,vue}',
        './src/**/*.{ts,tsx,vue}',
        "./node_modules/flowbite/**/*.js"
    ],

    theme: {
        container: {
            center: true,
            padding: "2rem",
            screens: {
                "2xl": "1400px",
            },
        },
        extend: {
            // Couleur d'accent (--button) utilisable avec opacité : border-button, bg-button/15, ring-button/50...
            // Pas de textColor : la classe text-button existante désigne la couleur de texte des boutons (--button-text).
            borderColor: { button: "rgb(var(--button-rgb) / <alpha-value>)" },
            backgroundColor: { button: "rgb(var(--button-rgb) / <alpha-value>)" },
            ringColor: { button: "rgb(var(--button-rgb) / <alpha-value>)" },
            colors: {
                // Tokens MonEndo (src/assets/tokens.css) : bg-surface, text-texte-3, border-trait, bg-teinte-douleur-fond,
                // text-teinte-douleur, bg-intensite-6… Préférer ces classes aux couleurs Tailwind brutes (blue-100…).
                fond: "var(--couleur-fond)",
                surface: { DEFAULT: "var(--couleur-surface)", 2: "var(--couleur-surface-2)" },
                trait: "var(--couleur-trait)",
                texte: { DEFAULT: "var(--couleur-texte)", 2: "var(--couleur-texte-2)", 3: "var(--couleur-texte-3)" },
                lien: { DEFAULT: "var(--couleur-lien)", survol: "var(--couleur-lien-survol)" },
                contour: "var(--couleur-contour)",
                danger: "var(--couleur-danger)",
                teinte: {"bilan":{"DEFAULT":"var(--teinte-bilan)","fond":"var(--teinte-bilan-fond)"},"douleur":{"DEFAULT":"var(--teinte-douleur)","fond":"var(--teinte-douleur-fond)"},"regles":{"DEFAULT":"var(--teinte-regles)","fond":"var(--teinte-regles-fond)"},"symptome":{"DEFAULT":"var(--teinte-symptome)","fond":"var(--teinte-symptome-fond)"},"traitement":{"DEFAULT":"var(--teinte-traitement)","fond":"var(--teinte-traitement-fond)"},"neutre":{"DEFAULT":"var(--teinte-neutre)","fond":"var(--teinte-neutre-fond)"}},
                intensite: {"0":"var(--intensite-0)","1":"var(--intensite-1)","2":"var(--intensite-2)","3":"var(--intensite-3)","4":"var(--intensite-4)","5":"var(--intensite-5)","6":"var(--intensite-6)","7":"var(--intensite-7)","8":"var(--intensite-8)","9":"var(--intensite-9)","10":"var(--intensite-10)"},
                border: "hsl(var(--border))",
                input: "hsl(var(--input))",
                ring: "hsl(var(--ring))",
                background: "hsl(var(--background))",
                foreground: "hsl(var(--foreground))",
                primary: {
                    DEFAULT: "hsl(var(--primary))",
                    foreground: "hsl(var(--primary-foreground))",
                },
                secondary: {
                    DEFAULT: "hsl(var(--secondary))",
                    foreground: "hsl(var(--secondary-foreground))",
                },
                destructive: {
                    DEFAULT: "hsl(var(--destructive))",
                    foreground: "hsl(var(--destructive-foreground))",
                },
                muted: {
                    DEFAULT: "hsl(var(--muted))",
                    foreground: "hsl(var(--muted-foreground))",
                },
                accent: {
                    DEFAULT: "hsl(var(--accent))",
                    foreground: "hsl(var(--accent-foreground))",
                },
                popover: {
                    DEFAULT: "hsl(var(--popover))",
                    foreground: "hsl(var(--popover-foreground))",
                },
                card: {
                    DEFAULT: "hsl(var(--card))",
                    foreground: "hsl(var(--card-foreground))",
                },
            },
            borderRadius: {
                xl: "calc(var(--radius) + 4px)",
                lg: "var(--radius)",
                md: "calc(var(--radius) - 2px)",
                sm: "calc(var(--radius) - 4px)",
                'card': '32px',
                petit: 'var(--rayon-petit)',
                moyen: 'var(--rayon)',
                grand: 'var(--rayon-grand)',
            },
            boxShadow: {
                // Une seule élévation dans toute l'application.
                elevation: "var(--elevation)",
            },
            keyframes: {
                "accordion-down": {
                    from: {height: 0},
                    to: {height: "var(--radix-accordion-content-height)"},
                },
                "accordion-up": {
                    from: {height: "var(--radix-accordion-content-height)"},
                    to: {height: 0},
                },
                "collapsible-down": {
                    from: {height: 0},
                    to: {height: 'var(--radix-collapsible-content-height)'},
                },
                "collapsible-up": {
                    from: {height: 'var(--radix-collapsible-content-height)'},
                    to: {height: 0},
                },
            },
            animation: {
                "accordion-down": "accordion-down 0.2s ease-out",
                "accordion-up": "accordion-up 0.2s ease-out",
                "collapsible-down": "collapsible-down 0.2s ease-in-out",
                "collapsible-up": "collapsible-up 0.2s ease-in-out",
            },
        },
    },
    plugins: [
        animate,
        require('flowbite/plugin')
    ],
}