/** @type {import('tailwindcss').Config} */
export default {
  content: [
    "./index.html",
    "./src/**/*.{vue,js,ts,jsx,tsx}",
  ],
  darkMode: 'class',
  theme: {
    extend: {
      colors: {
        primary: '#00ae8c',
        navy: '#111318',
        teal: '#00ae8c',
        'background-light': '#f8fafc',
        'background-dark': '#0f172a',
        'brand-bg': '#111318',
      },
      fontFamily: {
        display: ['-apple-system', 'BlinkMacSystemFont', 'Segoe UI', 'Roboto', 'Helvetica Neue', 'Arial', 'sans-serif'],
      },
      backgroundImage: {
        'brand-gradient': 'linear-gradient(135deg, #111318 0%, #00ae8c 100%)',
      }
    },
  },
  plugins: [],
}
