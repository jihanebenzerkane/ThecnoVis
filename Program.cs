using Microsoft.EntityFrameworkCore;
using TechnoVIS.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddControllers();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");
}
if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Connection string introuvable (appsettings:ConnectionStrings:DefaultConnection ou variable d'env DB_CONNECTION_STRING).");
}

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();

// Vérifie que l'API tourne et que la connexion DB est bien configurée (pas testée ici, juste câblée).
app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

app.Run();
