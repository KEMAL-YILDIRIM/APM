/** @type {import('tailwindcss').Config} */
export default {
  content: ['./index.html', './src/**/*.{js,ts,jsx,tsx}'],
  darkMode: 'class',
  theme: {
    extend: {
      colors: {
        severity: {
          trace: '#9CA3AF',
          debug: '#60A5FA',
          info: '#22C55E',
          warn: '#EAB308',
          error: '#EF4444',
          fatal: '#7C3AED',
        },
      },
    },
  },
  plugins: [],
};
