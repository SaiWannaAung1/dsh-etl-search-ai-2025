using DshEtlSearch.Api.Configuration;
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

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();