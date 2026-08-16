using Microsoft.EntityFrameworkCore;
using TechnoVIS.Data;
using TechnoVIS.Services;
using QuestPDF.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

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

// JWT Authentication Configuration
var jwtKey = builder.Configuration["Jwt:SecretKey"] 
    ?? Environment.GetEnvironmentVariable("JWT_SECRET_KEY") 
    ?? "TechnoVIS_Super_Secret_JWT_Key_2026_ECS_Maintenance_Security_Token!";

var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "TechnoVIS_API";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "TechnoVIS_App";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
});

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
        options.UseSqlServer(connectionString)
               .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));
}
else
{
    // SQLite local database provider for seamless local execution
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlite("Data Source=technovis.db")
               .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));
}

var app = builder.Build();

// Ensure database is created and migrations are applied
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();

    // Conditional initialization: Create default admin account only if table is empty
    if (!dbContext.Utilisateurs.Any())
    {
        var defaultAdminEmail = app.Configuration["Admin:Email"] ?? "admin@ecs.ma";
        var defaultAdminPassword = app.Configuration["Admin:Password"] 
            ?? Environment.GetEnvironmentVariable("ADMIN_DEFAULT_PASSWORD") 
            ?? "Admin123!";

        var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<TechnoVIS.Models.Utilisateur>();
        var adminUser = new TechnoVIS.Models.Utilisateur
        {
            Email = defaultAdminEmail,
            Role = "Responsable",
            TechnicienId = null,
            DateCreation = DateTime.UtcNow
        };
        adminUser.PasswordHash = hasher.HashPassword(adminUser, defaultAdminPassword);

        dbContext.Utilisateurs.Add(adminUser);
        dbContext.SaveChanges();
    }
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
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Health Check Endpoint
app.MapGet("/health", () => Results.Ok(new { status = "ok", service = "TechnoVIS Maintenance API", time = DateTime.Now }));

app.Run();
