using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
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
                return BadRequest(new { message = "Email et mot de passe requis." });
            }

            var user = await _context.Utilisateurs
                .Include(u => u.Technicien)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == model.Email.Trim().ToLower());

            if (user == null)
            {
                return Unauthorized(new { message = "Adresse email ou mot de passe incorrect." });
            }

            var hasher = new PasswordHasher<Utilisateur>();
            var verifyResult = hasher.VerifyHashedPassword(user, user.PasswordHash, model.Password);

            if (verifyResult == PasswordVerificationResult.Failed)
            {
                return Unauthorized(new { message = "Adresse email ou mot de passe incorrect." });
            }

            // Determine Full Name for identity badge
            string nomComplet = "Responsable Maintenance";
            if (user.Role == "Technicien" && user.Technicien != null)
            {
                nomComplet = $"{user.Technicien.Prenom} {user.Technicien.Nom}";
            }

            // Create JWT Claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("role", user.Role),
                new Claim("technicienId", user.TechnicienId?.ToString() ?? ""),
                new Claim("nomComplet", nomComplet)
            };

            var secretKey = _configuration["Jwt:SecretKey"] 
                ?? Environment.GetEnvironmentVariable("JWT_SECRET_KEY") 
                ?? "TechnoVIS_Super_Secret_JWT_Key_2026_ECS_Maintenance_Security_Token!";
            
            var issuer = _configuration["Jwt:Issuer"] ?? "TechnoVIS_API";
            var audience = _configuration["Jwt:Audience"] ?? "TechnoVIS_App";

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddHours(8);

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
                Expiration = expires
            });
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
        public string Role { get; set; } = "Technicien"; // "Responsable" or "Technicien"
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
        public DateTime Expiration { get; set; }
    }
}
