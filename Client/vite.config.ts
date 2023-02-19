import { defineConfig  } from 'vite'

export default defineConfig({
    base: '/dist/',
    server: {
        strictPort: true,
        hmr: {
          clientPort: 5173,
          protocol: 'ws'
        }
      }
})