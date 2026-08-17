using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using TechnoVIS.Data;
using TechnoVIS.Models;

namespace TechnoVIS.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(AppDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto model)
        {
            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Password))
            {
                return BadRequest(new { message = "Identifiant (Email ou Matricule) et mot de passe requis." });
            }

            var identifier = model.Email.Trim().ToLower();

            var user = await _context.Utilisateurs
                .Include(u => u.Technicien)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == identifier || (u.Technicien != null && u.Technicien.Matricule.ToLower() == identifier));

            // Si l'utilisateur n'existe pas encore mais que le technicien existe en base
            if (user == null)
            {
                var tech = await _context.Techniciens
                    .FirstOrDefaultAsync(t => t.Matricule.ToLower() == identifier || (!string.IsNullOrEmpty(t.Email) && t.Email.ToLower() == identifier));

                if (tech != null)
                {
                    var hasher = new PasswordHasher<Utilisateur>();
                    user = new Utilisateur
                    {
                        Email = string.IsNullOrEmpty(tech.Email) ? $"{tech.Matricule.ToLower()}@technovis.ma" : tech.Email.ToLower(),
                        Role = "Technicien",
                        TechnicienId = tech.Id,
                        DateCreation = DateTime.UtcNow,
                        Technicien = tech
                    };
                    user.PasswordHash = hasher.HashPassword(user, "Tech2026!");
                    _context.Utilisateurs.Add(user);
                    await _context.SaveChangesAsync();
                }
            }

            if (user == null)
            {
                return Unauthorized(new { message = "Identifiant ou mot de passe incorrect." });
            }

            var passwordHasher = new PasswordHasher<Utilisateur>();
            var verifyResult = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, model.Password);

            // Tolérance pour mot de passe par défaut technicien ou mot de passe direct
            if (verifyResult == PasswordVerificationResult.Failed)
            {
                if (user.Role == "Technicien" && (model.Password == "Tech2026!" || model.Password == "1234" || (user.Technicien != null && model.Password == user.Technicien.Matricule)))
                {
                    // Mot de passe accepté
                }
                else
                {
                    return Unauthorized(new { message = "Identifiant ou mot de passe incorrect." });
                }
            }

            string nomComplet = user.Role == "Technicien" && user.Technicien != null
                ? $"{user.Technicien.Prenom} {user.Technicien.Nom}".Trim()
                : "Responsable Maintenance";

            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, user.Role),
                new("role", user.Role),
                new("technicienId", user.TechnicienId?.ToString() ?? ""),
                new("nomComplet", nomComplet)
            };

            var secretKey = _configuration["Jwt:SecretKey"] 
                ?? Environment.GetEnvironmentVariable("JWT_SECRET_KEY") 
                ?? "TechnoVIS_SuperSecretKey_Production2026_IndustrialSecurityKey!";
            
            var issuer = _configuration["Jwt:Issuer"] ?? "TechnoVIS_API";
            var audience = _configuration["Jwt:Audience"] ?? "TechnoVIS_App";

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddHours(12);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            return Ok(new LoginResponseDto
            {
                Token = tokenString,
                Email = user.Email,
                Role = user.Role,
                TechnicienId = user.TechnicienId,
                NomComplet = nomComplet,
                Matricule = user.Technicien?.Matricule ?? "",
                Expiration = expires
            });
        }

        [HttpGet("techniciens-auth-list")]
        public async Task<IActionResult> GetTechniciensAuthList()
        {
            var techniciens = await _context.Techniciens
                .Select(t => new
                {
                    t.Id,
                    t.Matricule,
                    t.Nom,
                    t.Prenom,
                    NomComplet = $"{t.Prenom} {t.Nom}".Trim(),
                    t.Email,
                    t.Base
                })
                .ToListAsync();

            return Ok(techniciens);
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto model)
        {
            if (!ModelState.IsValid || string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Password))
            {
                return BadRequest(new { message = "Email et mot de passe requis." });
            }

            var existing = await _context.Utilisateurs
                .AnyAsync(u => u.Email.ToLower() == model.Email.Trim().ToLower());

            if (existing)
            {
                return BadRequest(new { message = "Un utilisateur avec cet email existe déjà." });
            }

            var user = new Utilisateur
            {
                Email = model.Email.Trim().ToLower(),
                Role = string.IsNullOrWhiteSpace(model.Role) ? "Technicien" : model.Role,
                TechnicienId = model.TechnicienId,
                DateCreation = DateTime.UtcNow
            };

            var hasher = new PasswordHasher<Utilisateur>();
            user.PasswordHash = hasher.HashPassword(user, model.Password);

            _context.Utilisateurs.Add(user);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Utilisateur créé avec succès.", id = user.Id, email = user.Email, role = user.Role });
        }
    }

    public class RegisterDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = "Technicien";
        public int? TechnicienId { get; set; }
    }

    public class LoginDto
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponseDto
    {
        public string Token { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public int? TechnicienId { get; set; }
        public string NomComplet { get; set; } = string.Empty;
        public string Matricule { get; set; } = string.Empty;
        public DateTime Expiration { get; set; }
    }
}
