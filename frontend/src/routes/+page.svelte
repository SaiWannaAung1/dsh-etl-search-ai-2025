<script lang="ts">
    import { SearchVM } from '$lib/viewmodels/SearchVM.svelte';
    const vm = new SearchVM();

    // Helper to format authors
    function formatAuthors(authorsStr: string) {
        if (!authorsStr) return "Unknown Authors";
        const parts = authorsStr.split(' / ');
        if (parts.length <= 1) return parts[0];
        return `${parts[0]} +${parts.length - 1} more`;
    }

    // Helper to format score
    const formatScore = (score: number) => Math.round(score * 100) + '% Match';
</script>

<section class="max-w-4xl mx-auto py-8 px-4">
    <div class="flex gap-3 mb-8 p-2 bg-white rounded-xl shadow-sm border border-gray-100">
        <input
                type="text"
                bind:value={vm.query}
                onkeydown={(e) => e.key === 'Enter' && vm.search()}
                placeholder="Search thousands of scientific datasets..."
                class="flex-1 px-4 py-2 outline-none text-gray-700 bg-transparent"
        />
        <button
                onclick={() => vm.search()}
                class="px-6 py-2 bg-blue-600 hover:bg-blue-700 text-white font-medium rounded-lg transition-all disabled:bg-gray-400"
                disabled={vm.isSearching}
        >
            {vm.isSearching ? 'Searching...' : 'Search'}
        </button>
    </div>

    <div class="flex flex-col gap-5">
        {#each vm.results as item}
            <div class="bg-white border border-gray-200 rounded-xl p-5 hover:shadow-md transition-shadow">
                <div class="flex justify-between items-start gap-4 mb-2">
                    <a
                            href="/dataset/{item.datasetId}"
                            class="text-lg font-bold text-blue-600 hover:underline decoration-2 underline-offset-4"
                    >
                        {item.title}
                    </a>
                    <span class="shrink-0 px-3 py-1 bg-green-50 text-green-700 text-xs font-bold rounded-full border border-green-100">
                        {formatScore(item.confidenceScore)}
                    </span>
                </div>

                <div class="flex items-center gap-2 mb-3 text-sm text-gray-500">
                    <svg xmlns="http://www.w3.org/2000/svg" class="w-4 h-4" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><path d="M16 21v-2a4 4 0 0 0-4-4H6a4 4 0 0 0-4 4v2"/><circle cx="9" cy="7" r="4"/><path d="M22 21v-2a4 4 0 0 0-3-3.87"/><path d="M16 3.13a4 4 0 0 1 0 7.75"/></svg>
                    <span class="font-medium text-gray-700">{formatAuthors(item.authors)}</span>
                </div>

                <p class="text-sm text-gray-600 leading-relaxed line-clamp-3 italic">
                    {item.previewAbstract}
                </p>

                <div class="mt-4 pt-4 border-t border-gray-50 flex justify-between items-center">
                    <span class="text-[10px] uppercase tracking-wider text-gray-400 font-mono">ID: {item.documentId}</span>
                    <button  class="text-xs font-semibold text-blue-500 hover:text-blue-700">View Full Metadata →</button>
                </div>
            </div>
        {:else}
            {#if !vm.isSearching}
                <div class="text-center py-20 bg-gray-50 rounded-2xl border-2 border-dashed border-gray-200">
                    <p class="text-gray-400">Discover wildlife, ecology, and climate data.</p>
                </div>
            {/if}
        {/each}
    </div>
</section>

<style>
    /* Add any custom material transitions here if needed */
    .line-clamp-3 {
        display: -webkit-box;
        -webkit-line-clamp: 3;
        -webkit-box-orient: vertical;
        overflow: hidden;
    }
</style>