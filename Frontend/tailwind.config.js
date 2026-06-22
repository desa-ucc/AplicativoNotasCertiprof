/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    "./src/**/*.{html,ts}",
  ],
  theme: {
    extend: {
      colors: {
        'avatar-petroleum': '#0f3747',
        'avatar-petroleum-dark': '#0B3A42',
        'avatar-lime': '#76bc21',
        'avatar-lime-dark': '#65a31c',
      }
    },
  },
  plugins: [],
}
