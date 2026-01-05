using DshEtlSearch.Core.Features.Ingestion;
using DshEtlSearch.Core.Interfaces.Infrastructure;
using DshEtlSearch.Core.Interfaces.Services;
using DshEtlSearch.Infrastructure.Data.SQLite;
using DshEtlSearch.Infrastructure.ExternalServices;
using DshEtlSearch.Infrastructure.ExternalServices.Ceh;
using DshEtlSearch.Infrastructure.FileProcessing.Extractor; // Or .Archives, depending on where your ZipExtractionService is
using DshEtlSearch.Infrastructure.FileProcessing.Parsers;
using Microsoft.EntityFrameworkCore;

namespace DshEtlSearch.Api.Configuration
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            // --- 1. Database Context (SQLite) ---
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite(connectionString, b => b.MigrationsAssembly("DshEtlSearch.Infrastructure")));

            // --- 2. Repositories ---
            services.AddScoped<IMetadataRepository, SqliteMetadataRepository>();

            // --- 3. Parsers & Factory ---
            // FIX: Register the Interface (IMetadataParserFactory) implementation
            services.AddSingleton<IMetadataParserFactory, MetadataParserFactory>();

            // --- 4. External Services (HTTP Clients) ---
            // FIX: Removed old 'IDownloader'. We use ICehCatalogueClient now.
            services.AddHttpClient<ICehCatalogueClient, CehCatalogueClient>();
            
            // --- 5. File Processing ---
            // FIX: Register 'IArchiveProcessor' (what Orchestrator wants) to 'ZipExtractionService' (what we have)
            services.AddScoped<IArchiveProcessor, ZipExtractionService>();

            // --- 6. Orchestrator ---
            // FIX: Register 'IEtlService' mapping to 'EtlOrchestrator'
            services.AddScoped<IEtlService, EtlOrchestrator>();
            
            
            
            return services;
        }
    }
}