import react from '@vitejs/plugin-react';
import { defineConfig } from 'vitest/config';

// Fejlesztéskor az /api kérések a gatewayre (Traefik, http://localhost) mennek, így nincs CORS.
export default defineConfig({
  plugins: [react()],
  server: {
    proxy: {
      '/api': 'http://localhost',
    },
  },
  test: {
    environment: 'jsdom',
    setupFiles: ['./src/test/setup.ts'],
    clearMocks: true,
  },
});
