<script lang="ts">
    import { onMount } from 'svelte';
    import { fade, fly } from 'svelte/transition';
    import { page } from '$app/stores';
    import { DatasetDetailVM } from "$lib/viewmodels/DatasetDetailVM.svelte";
    import {FileDataVM} from "$lib/viewmodels/FileDataVM.svelte";

    const vm = new DatasetDetailVM();
    const vm2 = new FileDataVM();
    onMount(() => {
        const id = $page.params.id;
        if (id) {
            vm.load(id);
        }
    });
</script>

{#if vm.isLoading}
    <div class="flex flex-col items-center justify-center min-h-[400px]">
        <div class="w-12 h-12 border-4 border-blue-200 border-t-blue-600 rounded-full animate-spin"></div>
        <p class="mt-4 text-gray-500 font-medium">Loading dataset details...</p>
    </div>
{:else if vm.error}
    <div class="max-w-2xl mx-auto my-10 p-6 bg-red-50 border border-red-200 rounded-2xl text-center">
        <h2 class="text-red-800 font-bold text-lg">Unable to Load Data</h2>
        <p class="text-red-600 mt-2">{vm.error}</p>
        <button onclick={() => window.location.reload()} class="mt-4 px-4 py-2 bg-red-600 text-white rounded-lg">Try Again</button>
    </div>
{:else if vm.data}
    <div class="max-w-6xl mx-auto px-4 py-10">
        <header class="mb-10">
            <h1 class="text-4xl font-extrabold text-gray-900 leading-tight">{vm.data.title}</h1>

            <div class="flex flex-wrap gap-2 mt-4">
                {#each vm.keywordList as keyword}
                    <span class="px-3 py-1 bg-blue-50 text-blue-700 text-xs font-bold rounded-full border border-blue-100">
                        {keyword}
                    </span>
                {/each}
            </div>
        </header>

        <div class="grid grid-cols-1 lg:grid-cols-3 gap-12">
            <div class="lg:col-span-2 space-y-10">
                <section>
                    <h2 class="text-xl font-bold text-gray-800 mb-4 border-b pb-2">Abstract</h2>
                    <p class="text-gray-700 leading-relaxed whitespace-pre-wrap">
                        {vm.data.abstract}
                    </p>
                </section>

                <section class="bg-gray-50 p-6 rounded-2xl border border-gray-200">
                    <h2 class="text-lg font-bold text-gray-800 mb-4">Files & Resources</h2>
                    <div class="flex flex-wrap gap-4">
                        {#if vm.data.resourceUrl}
                            <a href={vm.data.resourceUrl} target="_blank"
                               class="flex items-center gap-2 px-6 py-3 bg-blue-600 hover:bg-blue-700 text-white font-bold rounded-xl transition-all shadow-md">
                                <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-4l-4 4m0 0l-4-4m4 4V4" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></svg>
                                Download Data File
                            </a>
                        {/if}

                        <a href="https://data-package.ceh.ac.uk/sd/{vm.data.documentId}.zip" target="_blank"
                           class="flex items-center gap-2 px-6 py-3 bg-blue-600 hover:bg-blue-700 text-white font-bold rounded-xl transition-all shadow-md">
                            <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-4l-4 4m0 0l-4-4m4 4V4" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></svg>
                            Supporting Documents
                        </a>

                        <button
                                onclick={() => vm2.loadFiles(vm.data?.datasetId ?? "")}
                                class="flex items-center gap-2 px-6 py-3 bg-blue-600 hover:bg-blue-700 active:scale-95 text-white font-bold rounded-xl transition-all shadow-md group"
                        >
                            <svg class="w-5 h-5 text-blue-100 group-hover:text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                                <path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
                            </svg>

                            <span>View Data File</span>
                        </button>
                    </div>
                </section>
            </div>

            <aside class="space-y-6">
                <div class="bg-white p-6 rounded-2xl border border-gray-100 shadow-sm">
                    <h3 class="text-xs font-bold text-gray-400 uppercase tracking-widest mb-4">Dataset Info</h3>
                    <div class="space-y-4">
                        <div>
                            <span class="block text-xs text-gray-400 font-medium">Published Date</span>
                            <span class="text-gray-900 font-semibold">{vm.formattedDate}</span>
                        </div>
                        <div>
                            <span class="block text-xs text-gray-400 font-medium">File Identifier</span>
                            <span class="text-gray-600 font-mono text-[10px] break-all">{vm.data.documentId}</span>
                        </div>
                    </div>
                </div>

                <div class="bg-white p-6 rounded-2xl border border-gray-100 shadow-sm">
                    <h3 class="text-xs font-bold text-gray-400 uppercase tracking-widest mb-4">Contributors</h3>
                    <div class="space-y-4">
                        {#each vm.authorList as author}
                            <div class="border-l-2 border-blue-500 pl-3">
                                <p class="text-sm font-bold text-gray-900">{author.name}</p>
                                <p class="text-xs text-gray-500">{author.organization}</p>
                            </div>
                        {/each}
                    </div>
                </div>
            </aside>
        </div>
    </div>
{/if}

{#if vm2.showModal}
    <div
            transition:fade={{ duration: 200 }}
            class="fixed inset-0 z-[100] flex items-center justify-center p-4 bg-slate-900/60 backdrop-blur-md"
            onclick={(e) => e.target === e.currentTarget && vm2.close()}
    >
        <div
                transition:fly={{ y: 30, duration: 400, opacity: 0 }}
        class="bg-white w-full max-w-2xl rounded-3xl shadow-2xl flex flex-col max-h-[80vh] overflow-hidden border border-slate-200 pointer-events-auto"
        >
        <div class="px-6 py-5 border-b border-slate-100 flex items-center justify-between bg-white">
            <div class="flex items-center gap-3">
                <div class="w-10 h-10 bg-blue-50 text-blue-600 rounded-xl flex items-center justify-center">
                    <svg class="w-6 h-6" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M5 19a2 2 0 01-2-2V7a2 2 0 012-2h4l2 2h4a2 2 0 012 2v1m-6 9a3 3 0 100-6 3 3 0 000 6z"/></svg>
                </div>
                <div>
                    <h3 class="text-xl font-bold text-slate-800">Associated Files</h3>
                    <p class="text-xs text-slate-500 font-medium">Download supporting data resources</p>
                </div>
            </div>
            <button
                    onclick={() => vm2.close()}
                    class="p-2 hover:bg-slate-100 rounded-lg transition-colors text-slate-400 hover:text-slate-600"
            >
                <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2.5" d="M6 18L18 6M6 6l12 12"/></svg>
            </button>
        </div>

        <div class="flex-1 overflow-y-auto p-4 custom-scrollbar bg-slate-50/30">
            {#if vm2.isLoading}
                <div class="flex flex-col items-center justify-center py-20">
                    <div class="w-10 h-10 border-3 border-blue-600 border-t-transparent rounded-full animate-spin"></div>
                    <p class="mt-4 text-sm text-slate-500">Loading resources...</p>
                </div>
            {:else if vm2.error}
                <div class="m-4 p-4 bg-red-50 text-red-700 rounded-xl text-sm border border-red-100">
                    {vm2.error}
                </div>
            {:else if vm2.files && vm2.files.length > 0}
                <div class="space-y-2">
                    {#each vm2.files as file, i}
                        <div class="flex items-center justify-between p-3 bg-white border border-slate-100 rounded-xl hover:shadow-sm hover:border-blue-200 transition-all group">
                            <div class="flex items-center gap-3 min-w-0">
                                <span class="text-[10px] font-bold text-slate-300 w-4">{i + 1}</span>
                                <div class="text-blue-500 group-hover:text-blue-600">
                                    <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"/></svg>
                                </div>
                                <span class="text-sm font-semibold text-slate-700 truncate pr-2">
                                        {file.fileName}
                                    </span>
                            </div>
                            <a
                                    href={file.storagePath}
                                    target="_blank"
                                    class="shrink-0 flex items-center gap-1.5 px-4 py-1.5 bg-slate-100 hover:bg-blue-600 hover:text-white text-slate-600 text-xs font-bold rounded-lg transition-all"
                            >
                                <svg class="w-4 h-4" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path stroke-linecap="round" stroke-linejoin="round" stroke-width="2" d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-4l-4 4m0 0l-4-4m4 4V4"/></svg>
                                Download
                            </a>
                        </div>
                    {/each}
                </div>
            {:else}
                <div class="text-center py-16">
                    <p class="text-slate-400 text-sm italic">No files available for this dataset.</p>
                </div>
            {/if}
        </div>

        <div class="px-6 py-4 bg-white border-t border-slate-100 flex justify-end">
            <button
                    onclick={() => vm2.close()}
                    class="px-5 py-2 text-sm font-bold text-slate-500 hover:text-slate-800 transition-colors"
            >
                Close
            </button>
        </div>
    </div>
    </div>
{/if}

<style>
    .custom-scrollbar::-webkit-scrollbar { width: 6px; }
    .custom-scrollbar::-webkit-scrollbar-thumb { background: #e2e8f0; border-radius: 10px; }
    .custom-scrollbar::-webkit-scrollbar-thumb:hover { background: #cbd5e1; }
</style>