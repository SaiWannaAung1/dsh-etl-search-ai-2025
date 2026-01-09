<script lang="ts">
    import { ChatVM } from '$lib/viewmodels/ChatVM.svelte';
    import { fly, fade } from 'svelte/transition';
    import { tick } from 'svelte';

    const vm = new ChatVM();
    let scrollContainer: HTMLElement;

    // Reactive scroll management: whenever messages length changes, scroll to bottom
    $effect(() => {
        if (vm.messages.length > 0 || vm.isLoading) {
            tick().then(() => {
                scrollContainer?.scrollTo({
                    top: scrollContainer.scrollHeight,
                    behavior: 'smooth'
                });
            });
        }
    });
</script>

<div class="max-w-3xl mx-auto h-[85vh] flex flex-col bg-white shadow-2xl rounded-[2rem] overflow-hidden border border-slate-200 mt-4">

    <header class="bg-white border-b px-8 py-4 flex items-center justify-between">
        <div class="flex items-center gap-3">
            <div class="w-3 h-3 bg-green-500 rounded-full animate-pulse"></div>
            <h1 class="text-lg font-semibold text-slate-800">CEH Research Assistant</h1>
        </div>
        <button
                onclick={() => vm.clearHistory()}
                class="text-xs font-medium text-slate-400 hover:text-red-500 transition-colors"
        >
            Clear Conversation
        </button>
    </header>

    <div
            bind:this={scrollContainer}
            class="flex-1 overflow-y-auto p-6 space-y-6 bg-slate-50/50 scroll-smooth"
    >
        {#if vm.messages.length === 0}
            <div class="h-full flex flex-col items-center justify-center text-slate-400 space-y-2">
                <span class="text-4xl">🔬</span>
                <p class="text-sm">Ask me about datasets, files, or specific research metrics.</p>
            </div>
        {/if}

        {#each vm.messages as msg}
            <div class="flex {msg.role === 'user' ? 'justify-end' : 'justify-start'}">
                <div
                        in:fly={{ y: 15, duration: 400 }}
                        class="p-4 rounded-[1.5rem] max-w-[85%] shadow-sm relative
                    {msg.role === 'user' 
                        ? 'bg-blue-600 text-white rounded-tr-none' 
                        : 'bg-white border border-slate-200 text-slate-800 rounded-tl-none'}"
                >
                    <p class="text-sm leading-relaxed whitespace-pre-wrap">{msg.content}</p>

                    {#if msg.sources && msg.sources.length > 0}
                        <div class="mt-4 pt-3 border-t border-slate-100">
                            <p class="text-[10px] font-bold uppercase tracking-wider opacity-60 mb-2">Attached Resources</p>
                            <div class="flex flex-wrap gap-2">
                                {#each msg.sources as source}
                                    <a
                                            href={source.storagePath}
                                            target="_blank"
                                            class="flex items-center gap-2 px-3 py-1.5 rounded-full text-[11px] font-medium transition-all
                                        {msg.role === 'user' ? 'bg-white/20 hover:bg-white/30 text-white' : 'bg-slate-100 hover:bg-blue-50 text-blue-700 hover:text-blue-800'}"
                                    >
                                        <svg class="w-3 h-3" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path d="M12 10v6m0 0l-3-3m3 3l3-3m2 8H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></svg>
                                        {source.fileName}
                                    </a>
                                {/each}
                            </div>
                        </div>
                    {/if}
                </div>
            </div>
        {/each}

        {#if vm.isLoading}
            <div in:fade class="flex justify-start">
                <div class="bg-white border border-slate-200 p-4 rounded-[1.5rem] rounded-tl-none shadow-sm flex gap-1">
                    <span class="w-1.5 h-1.5 bg-slate-300 rounded-full animate-bounce"></span>
                    <span class="w-1.5 h-1.5 bg-slate-300 rounded-full animate-bounce [animation-delay:0.2s]"></span>
                    <span class="w-1.5 h-1.5 bg-slate-300 rounded-full animate-bounce [animation-delay:0.4s]"></span>
                </div>
            </div>
        {/if}
    </div>

    <div class="p-6 bg-white border-t border-slate-100">
        <form
                onsubmit={(e) => { e.preventDefault(); vm.sendMessage(); }}
                class="flex items-center gap-3 bg-slate-100 p-2 rounded-[1.25rem] border border-transparent focus-within:border-blue-400 focus-within:bg-white transition-all duration-200"
        >
            <input
                    bind:value={vm.userInput}
                    placeholder="Type your message..."
                    disabled={vm.isLoading}
                    class="flex-1 px-4 py-2 bg-transparent text-sm focus:outline-none disabled:cursor-not-allowed"
            />
            <button
                    type="submit"
                    disabled={!vm.userInput.trim() || vm.isLoading}
                    class="bg-blue-600 text-white p-2.5 rounded-xl hover:bg-blue-700 disabled:bg-slate-300 disabled:shadow-none shadow-lg shadow-blue-200 transition-all active:scale-95"
            >
                <svg class="w-5 h-5" fill="none" stroke="currentColor" viewBox="0 0 24 24"><path d="M5 12h14M12 5l7 7-7 7" stroke-width="2.5" stroke-linecap="round" stroke-linejoin="round"/></svg>
            </button>
        </form>
    </div>
</div>

<style>
    /* Clean scrollbar for the message area */
    .overflow-y-auto::-webkit-scrollbar {
        width: 6px;
    }
    .overflow-y-auto::-webkit-scrollbar-track {
        background: transparent;
    }
    .overflow-y-auto::-webkit-scrollbar-thumb {
        background: #e2e8f0;
        border-radius: 10px;
    }
    
</style>