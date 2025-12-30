using DshEtlSearch.Core.Interfaces.Infrastructure;
using DshEtlSearch.Core.Interfaces.Application; // For IEtlOrchestrator
using DshEtlSearch.Infrastructure.Data.SQLite;
using DshEtlSearch.Infrastructure.FileProcessing.Downloader; // For CehDatasetDownloader
using DshEtlSearch.Infrastructure.FileProcessing.Extractor;  // For ZipExtractionService
using DshEtlSearch.Infrastructure.FileProcessing.Parsers;    // For MetadataParserFactory
using DshEtlSearch.Infrastructure.Services;                  // For EtlOrchestrator
using Microsoft.EntityFrameworkCore;

namespace DshEtlSearch.Api.Configuration
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
        {
            // 1. Register Database Context (SQLite)
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite(connectionString, b => b.MigrationsAssembly("DshEtlSearch.Infrastructure")));

            // 2. Register Repositories
            services.AddScoped<IMetadataRepository, SqliteMetadataRepository>();

            // 3. Register Parsers & Factory
            services.AddSingleton<MetadataParserFactory>();

            // 4. Register File Processing
            // IDownloader maps to CehDatasetDownloader
            services.AddHttpClient<IDownloader, CehDatasetDownloader>(); 
            
            // IExtractionService maps to ZipExtractionService
            services.AddScoped<IExtractionService, ZipExtractionService>();

            // 5. Register Orchestrator
            services.AddScoped<IEtlOrchestrator, EtlOrchestrator>();
            
            return services;
        }
    }
}