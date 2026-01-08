<script lang="ts">
    import { page } from '$app/state'; // Svelte 5 way to get current URL

    // Navigation items
    const navItems = [
        { name: 'Search', path: '/' },
        { name: 'Chat', path: '/chat' }
    ];
</script>

<nav class="sticky top-0 z-50 w-full bg-white shadow-md border-b border-gray-100">
    <div class="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8">
        <div class="flex justify-between h-16 items-center">

            <div class="flex items-center gap-2">
                <div class="bg-blue-600 p-2 rounded-lg">
                    <svg class="w-6 h-6 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                        <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
                    </svg>
                </div>
                <span class="text-xl font-bold tracking-tight text-gray-900">DSH <span class="text-blue-600">ETL</span></span>
            </div>

            <div class="hidden md:flex space-x-8">
                {#each navItems as item}
                    <a
                            href={item.path}
                            class="relative px-1 py-2 text-sm font-medium transition-colors duration-200 
						{page.url.pathname === item.path ? 'text-blue-600' : 'text-gray-500 hover:text-blue-500'}"
                    >
                        {item.name}

                        {#if page.url.pathname === item.path}
                            <div class="absolute bottom-0 left-0 w-full h-0.5 bg-blue-600 rounded-full"></div>
                        {/if}
                    </a>
                {/each}
            </div>

            <div class="flex items-center gap-4">
				<span class="flex items-center gap-1.5 text-xs font-medium text-green-600 bg-green-50 px-2 py-1 rounded-full">
					<span class="w-2 h-2 bg-green-500 rounded-full animate-pulse"></span>
					API Online
				</span>
            </div>
        </div>
    </div>
</nav>