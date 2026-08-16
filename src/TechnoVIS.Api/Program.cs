using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TechnoVIS.Api.Data;
using TechnoVIS.Api.Models;
using TechnoVIS.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddRazorPages();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.Configure<AzureAiOptions>(builder.Configuration.GetSection(AzureAiOptions.SectionName));
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequiredLength = 8;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddSignInManager();

var jwt = builder.Configuration.GetSection("Jwt");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = jwt["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwt["Audience"],
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["SigningKey"]!)),
        ValidateLifetime = true
    });
builder.Services.AddAuthorization();
builder.Services.AddScoped<IAssignmentScoringService, AssignmentScoringService>();
builder.Services.AddScoped<IAssignmentExplanationService, TemplateAssignmentExplanationService>();
builder.Services.AddScoped<IOcrService, ManualReviewOcrService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

var app = builder.Build();
using (var scope = app.Services.CreateScope())
{
    var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (var role in new[] { "Admin", "Planner", "Technician" })
    {
        if (!await roles.RoleExistsAsync(role)) await roles.CreateAsync(new IdentityRole(role));
    }
}
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapRazorPages();
// Port local explicite : le port Windows par défaut 5000 est réservé sur cette machine.
app.Run("http://127.0.0.1:5278");
