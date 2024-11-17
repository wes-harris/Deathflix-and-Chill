import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

export default defineConfig({
  plugins: [react()],
  server: {
    port: 3000, // Custom port if needed
  },
  build: {
    outDir: 'build', // Custom build output directory
  },
});
