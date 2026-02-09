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
        'background-light': '#fafafa',
        'background-dark': '#0d1117',
        'brand-bg': '#111318',
        'surface-dark': '#161b22',
        'surface-dark-hover': '#1c2333',
        'border-dark': '#21262d',
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
