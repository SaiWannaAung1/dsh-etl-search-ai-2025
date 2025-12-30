using DshEtlSearch.Core.Interfaces.Infrastructure;
using DshEtlSearch.Infrastructure.Data.SQLite;
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

            // Register Parsers & Factory
            services.AddSingleton<MetadataParserFactory>();

            // Register File Processing
            services.AddHttpClient<IDownloader, CehDatasetDownloader>(); // Uses HttpClient Factory
            services.AddScoped<IExtractionService, ZipExtractionService>();

            // Register Orchestrator
            services.AddScoped<IEtlOrchestrator, EtlOrchestrator>();
            
            return services;
        }
    }
}