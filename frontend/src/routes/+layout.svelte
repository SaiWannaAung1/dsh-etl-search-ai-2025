<script lang="ts">
	import '../app.css';
	import Navbar from '$lib/components/Navbar.svelte';
	let { children } = $props();
</script>

<div class="min-h-screen bg-gray-50 flex flex-col">
	<Navbar />

	<main class="flex-grow container mx-auto px-4 py-8">
		<div class="bg-white rounded-xl shadow-sm border border-gray-100 p-6">
			{@render children()}
		</div>
	</main>

	<footer class="py-6 text-center text-sm text-gray-400 border-t bg-white">
		DSH ETL Search AI &copy; 2026 
	</footer>
</div>