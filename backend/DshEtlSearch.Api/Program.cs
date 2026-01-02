using DshEtlSearch.Api.Configuration;
using DshEtlSearch.Core.Interfaces.Services;
using DshEtlSearch.Infrastructure.Data.SQLite;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// --- [Custom Configuration] ---
// This calls the method we created in Step B
builder.Services.AddInfrastructureServices(builder.Configuration);
// ------------------------------

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Auto-migrate Database on Startup (Optional but useful for Dev)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated(); // Creates dsh-metadata.db if missing
}
// =======================================================================
//  👉 THIS IS WHERE RunBatchIngestionAsync IS RUN
// =======================================================================
// We create a temporary "scope" to get the database and ETL service
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        // Ask the DI container for the EtlOrchestrator
        var etlService = services.GetRequiredService<IEtlService>();

        Console.WriteLine("🚀 Starting Batch Ingestion from file...");
        
        // CALL THE METHOD HERE
        // .Wait() forces the app to finish this before accepting API requests
        etlService.RunBatchIngestionAsync().Wait();
        
        Console.WriteLine("✅ Batch Ingestion Finished!");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Error during startup: {ex.Message}");
    }
}
// =======================================================================

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();