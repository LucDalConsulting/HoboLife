import { defineConfig } from 'vite';
import { viteSingleFile } from 'vite-plugin-singlefile';

// Bundles the entire game into ONE self-contained index.html that runs by
// double-clicking it (file://) — no server, no install. Used for quick
// playtesting. The normal `npm run build` is still what deploys to the web.
export default defineConfig({
  plugins: [viteSingleFile()],
  build: {
    target: 'es2020',
    outDir: 'dist-standalone',
    sourcemap: false,
    cssCodeSplit: false,
    assetsInlineLimit: 100000000,
    reportCompressedSize: false,
  },
});
