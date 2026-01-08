import { sveltekit } from '@sveltejs/kit/vite';
import { defineConfig } from 'vite';
import tailwindcss from '@tailwindcss/vite'; // 1. Add this import

export default defineConfig({
	plugins: [
		tailwindcss(), // 2. Add this to the plugins list
		sveltekit()
	],
	server: {
		proxy: {
			// This tells Vite: "Any request starting with /api, send it to C#"
			'/api': {
				target: 'http://localhost:5007',
				changeOrigin: true,
				secure: false
			}
		}
	}
});