import { defineConfig } from 'vite';

// HoboLife client build config.
// `base: './'` keeps asset paths relative so the static build works on any host
// (Cloudflare Pages, GitHub Pages project pages, Netlify, a sub-path, etc.).
export default defineConfig({
  base: './',
  server: {
    host: true,
    port: 5173,
  },
  build: {
    target: 'es2020',
    outDir: 'dist',
    sourcemap: true,
  },
});
