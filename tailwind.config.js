/** @type {import('tailwindcss').Config} */
module.exports = {
    content: [
        "./**/*.{razor,html,cshtml}",
    ],
    theme: {
        extend: {
            fontFamily: {
                sans: ['Inter', 'ui-sans-serif', 'system-ui', '-apple-system', 'BlinkMacSystemFont', 'Segoe UI', 'Roboto', 'Arial', 'sans-serif'],
            },
            colors: {
                // Corporate Blue Theme
                primary: {
                    50: '#eff6ff',
                    100: '#dbeafe',
                    200: '#bfdbfe',
                    300: '#93c5fd',
                    400: '#60a5fa',
                    500: '#3b82f6', // Main Brand Color
                    600: '#2563eb', // Hover states
                    700: '#1d4ed8',
                    800: '#1e40af', // Sidebar Background
                    900: '#1e3a8a',
                    950: '#172554',
                },
                slate: {
                    50: '#f8fafc', // App Background
                    100: '#f1f5f9', // Card Borders
                    // ... default slate colors will handle text and accents
                }
            },
            boxShadow: {
                'card': '0 4px 6px -1px rgba(0, 0, 0, 0.05), 0 2px 4px -1px rgba(0, 0, 0, 0.03)',
                'card-hover': '0 10px 15px -3px rgba(0, 0, 0, 0.08), 0 4px 6px -2px rgba(0, 0, 0, 0.04)',
            },
            backgroundImage: {
                'gradient-radial': 'radial-gradient(var(--tw-gradient-stops))',
                'profile-card': 'linear-gradient(to right bottom, #3b82f6, #1d4ed8)',
            }
        },
    },
    plugins: [
        require('@tailwindcss/forms'), // Highly recommended for clean form inputs in HR apps
    ],
}