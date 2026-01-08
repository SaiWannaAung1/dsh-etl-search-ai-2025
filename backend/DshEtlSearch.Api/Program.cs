using DshEtlSearch.Api.Configuration;
using DshEtlSearch.Core.Interfaces.Infrastructure;
using DshEtlSearch.Core.Interfaces.Services;
using DshEtlSearch.Infrastructure.Data.SQLite;
using DshEtlSearch.Infrastructure.Data.VectorStore;
using DshEtlSearch.Infrastructure.ExternalServices;
using DshEtlSearch.Infrastructure.ExternalServices.Ai;
using DshEtlSearch.Infrastructure.ExternalServices.Ceh;
using DshEtlSearch.Infrastructure.ExternalServices.GoogleDrive; // Correct namespace for OnnxEmbeddingService
using Microsoft.EntityFrameworkCore;
using Qdrant.Client;

var builder = WebApplication.CreateBuilder(args);

// =========================================================
// 1. REGISTER SERVICES (Must be BEFORE builder.Build())
// =========================================================

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// A. Custom Infrastructure (SQL, Metadata, etc.)
builder.Services.AddInfrastructureServices(builder.Configuration);

// B. Register Qdrant Client (Using appsettings.json or defaults)
builder.Services.AddSingleton<QdrantClient>(sp => 
{
    var config = sp.GetRequiredService<IConfiguration>();
    var host = config["Qdrant:Host"] ?? "localhost";
    var port = int.Parse(config["Qdrant:Port"] ?? "6334");
    var apiKey = config["Qdrant:ApiKey"];
    var https = bool.Parse(config["Qdrant:Https"] ?? "false");
    
    // Returns the High-Level Client
    return new QdrantClient(host, port, https, apiKey: string.IsNullOrWhiteSpace(apiKey) ? null : apiKey);
});

// --- 1. Define the Policy Name ---
const string myAllowSpecificOrigins = "_myAllowSpecificOrigins";

// --- 2. Add CORS Service ---
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: myAllowSpecificOrigins,
        policy =>
        {
            policy.WithOrigins("http://localhost:5173") // Your Svelte dev URL
                .AllowAnyHeader()
                .AllowAnyMethod();
        });
});


builder.Services.AddScoped<ILlmService, GeminiLlmService>();
// C. Register Vector Store Wrapper
builder.Services.AddScoped<IVectorStore, QdrantVectorStore>();

// D. Register Embedding Service (THE MISSING PIECE)
// We use Singleton because loading the AI model takes time/memory
builder.Services.AddSingleton<IEmbeddingService, OnnxEmbeddingService>();

builder.Services.AddSingleton<IGoogleDriveService, GoogleDriveService>();

// =========================================================
// 2. BUILD APP
// =========================================================
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// =========================================================
// 3. DATABASE MIGRATION (Runs on Startup)
// =========================================================
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated(); // Creates dsh-metadata.db if missing
}

// =========================================================
// 4. RUN ETL PROCESS (Runs on Startup)
// =========================================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var etlService = services.GetRequiredService<IEtlService>();
        Console.WriteLine("🚀 Starting Batch Ingestion...");
        
        // Use async/await properly if possible, or .Wait() for console blocking
        etlService.RunBatchIngestionAsync().Wait();
        
        Console.WriteLine("✅ Batch Ingestion Finished!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error during startup: {ex.ToString()}");
    }
}


// UseCors MUST be between UseRouting and UseAuthorization
app.UseCors(myAllowSpecificOrigins);
app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();