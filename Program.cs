using Microsoft.EntityFrameworkCore;
using TechnoVIS.Data;
using TechnoVIS.Services;

using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Configure QuestPDF license
QuestPDF.Settings.License = LicenseType.Community;

// Register Services
builder.Services.AddOpenApi();
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
});
builder.Services.AddScoped<ScoringService>();
builder.Services.AddScoped<ExcelImportService>();
builder.Services.AddScoped<PdfExportService>();
builder.Services.AddScoped<CsvExportService>();


// CORS configuration for local development
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Database configuration
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING");
}

if (!string.IsNullOrWhiteSpace(connectionString) && !connectionString.Contains("technovis.db"))
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(connectionString));
}
else
{
    // SQLite local database provider for seamless local execution
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite("Data Source=technovis.db"));
}

var app = builder.Build();

// Ensure database is created and migrations are applied
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();
}

// HTTP Pipeline
app.UseCors("AllowAll");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Serve wwwroot static assets (index.html, styles.css, app.js, fallback.js)
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseRouting();
app.MapControllers();

// Health Check Endpoint
app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "TechnoVIS Maintenance API", time = DateTime.Now }));

app.Run();
