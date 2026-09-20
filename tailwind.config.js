/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./Views/**/*.cshtml",
    "./wwwroot/**/*.js"
  ],
  darkMode: 'class',
  theme: {
    extend: {
      fontFamily: {
        cairo: ["'Cairo'", 'sans-serif'],
      },
      colors: {
        brand: {
          50:  '#fef2f2',
          100: '#fde8e8',
          200: '#fcd4d4',
          300: '#f9a8a8',
          400: '#f47171',
          500: '#ea4444',
          600: '#d5212a',
          700: '#b91c2c',
          800: '#991b1b',
          900: '#7f1d1d',
        },
      },
      boxShadow: {
        'card': '0 1px 3px rgba(15,23,42,.06), 0 4px 16px rgba(15,23,42,.06)',
        'card-hover': '0 4px 8px rgba(15,23,42,.08), 0 12px 28px rgba(15,23,42,.10)',
        'sidebar': '2px 0 16px rgba(15,23,42,.10)',
      }
    },
  },
  safelist: [
    // Toast notification dynamic classes
    'bg-green-600',
    'bg-red-600',
    'bg-amber-500',
    'bg-blue-600',
    // Status modal accents & buttons
    'from-red-500',
    'to-red-700',
    'from-green-500',
    'to-green-700',
    'bg-red-50',
    'bg-green-50',
    'text-red-600',
    'text-green-600',
    'hover:bg-red-700',
    'hover:bg-green-700',
    'shadow-red-600/20',
    'shadow-green-600/20',
    // Dynamic spinners & loading states
    'animate-spin',
    'opacity-25',
    'opacity-75',
    'cursor-not-allowed',
    // Interactive multi-step wizard
    'bg-slate-200',
    'bg-green-500',
    'border-green-500',
    'border-brand-600',
    'bg-brand-600',
    'shadow-brand-600/30',
    'text-green-600',
    'text-brand-600',
    'border-slate-200',
    'text-slate-400',
    // Dynamic form validation & alert feedback
    'border-red-400',
    'bg-red-50',
    'border-slate-200',
    'bg-slate-50',
    'border-green-400',
    'bg-green-50/30',
    'text-green-700',
    'border-green-200',
    'text-red-700',
    'border-red-200',
    // Modal transitions & visibility toggles
    'opacity-0',
    'opacity-100',
    'pointer-events-none',
    'scale-95',
    'scale-100'
  ],
  plugins: [],
};
