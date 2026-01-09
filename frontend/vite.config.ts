/// <reference types="vitest" />
import { sveltekit } from '@sveltejs/kit/vite';
import { defineConfig } from 'vitest/config'; // Change this line
import tailwindcss from '@tailwindcss/vite';
import { svelteTesting } from '@testing-library/svelte/vite';

export default defineConfig({
	plugins: [
		tailwindcss(),
		sveltekit(),
		// Only run svelteTesting plugin during tests
		
	],

	// 1. PROJECT SERVER CONFIG (Proxy for C#)
	server: {
		proxy: {
			'/api': {
				target: 'http://localhost:5007',
				changeOrigin: true,
				secure: false
			}
		}
	},

	// 2. VITEST CONFIG (The "Like This" Part)
	test: {
		environment: 'jsdom',
		globals: true, // Allows use of 'describe', 'it', 'expect' globally
		setupFiles: ['./vitest-setup.ts'],
		// Ensure Vitest processes Svelte 5 runes correctly
		alias: [{ find: /^svelte$/, replacement: 'svelte/internal' }]
	}
});