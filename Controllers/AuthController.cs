using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using TechnoVIS.Data;
using TechnoVIS.Models;
using TechnoVIS.Services;

namespace TechnoVIS.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        AppDbContext context,
        IEmailService emailService,
        ILogger<AuthController> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }


    // ============================================================
    // 1. LOGIN (Cookie Authentication)
    // ============================================================

    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("LoginPolicy")]
    public async Task<IActionResult> Login([FromBody] LoginDto model)
    {
        var rawIdentifier = model.Identifier ?? model.Email;

        if (string.IsNullOrWhiteSpace(rawIdentifier) || string.IsNullOrWhiteSpace(model.Password))
        {
            return BadRequest(new
            {
                message = "Identifiant et mot de passe requis."
            });
        }

        var identifier = rawIdentifier.Trim().ToLowerInvariant();

        var user = await _context.Utilisateurs
            .Include(u => u.Technicien)
            .FirstOrDefaultAsync(u =>
                u.Email.ToLower() == identifier ||
                (u.Technicien != null && u.Technicien.Matricule.ToLower() == identifier));

        if (user == null)
        {
            _logger.LogWarning("Tentative de connexion échouée : identifiant {Identifier} inconnu.", identifier);
            return Unauthorized(new
            {
                message = "Identifiant ou mot de passe incorrect."
            });
        }

        if (user.Role == "Technicien" &&
            (user.Technicien == null || !string.Equals(user.Technicien.Statut, "Actif", StringComparison.OrdinalIgnoreCase)))
        {
            _logger.LogWarning("Connexion refusée : compte technicien inactif pour {Identifier}.", identifier);
            return Unauthorized(new
            {
                message = "Ce compte technicien a été désactivé. Veuillez contacter votre responsable."
            });
        }

        var passwordHasher = new PasswordHasher<Utilisateur>();
        var verificationResult = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, model.Password);

        if (verificationResult == PasswordVerificationResult.Failed)
        {
            _logger.LogWarning("Tentative de connexion échouée : mot de passe invalide pour {Identifier}.", identifier);
            return Unauthorized(new
            {
                message = "Identifiant ou mot de passe incorrect."
            });
        }

        var nomComplet = user.Role == "Technicien" && user.Technicien != null
            ? $"{user.Technicien.Prenom} {user.Technicien.Nom}".Trim()
            : user.Role == "Responsable"
                ? "Responsable Maintenance"
                : user.Email;

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role),
            new("role", user.Role),
            new(ClaimTypes.Name, nomComplet)
        };

        if (user.TechnicienId.HasValue)
        {
            claims.Add(new Claim("TechnicienId", user.TechnicienId.Value.ToString()));
        }

        if (user.Technicien != null && !string.IsNullOrWhiteSpace(user.Technicien.Matricule))
        {
            claims.Add(new Claim("Matricule", user.Technicien.Matricule));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7),
            IssuedUtc = DateTimeOffset.UtcNow,
            AllowRefresh = true
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal,
            authProperties);

        _logger.LogInformation("Connexion réussie pour {Email} ({Role}).", user.Email, user.Role);

        return Ok(new
        {
            message = "Connexion réussie.",
            user = new
            {
                user.Id,
                user.Email,
                user.Role,
                user.TechnicienId,
                NomComplet = nomComplet,
                Matricule = user.Technicien?.Matricule ?? "",
                Base = user.Technicien?.Base ?? ""
            }
        });
    }


    // ============================================================
    // 2. LOGOUT
    // ============================================================

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok(new { message = "Déconnexion réussie." });
    }


    // ============================================================
    // 3. CURRENT USER (ME)
    // ============================================================

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!int.TryParse(userIdString, out var userId))
        {
            return Unauthorized(new { message = "Session invalide." });
        }

        var user = await _context.Utilisateurs
            .Include(u => u.Technicien)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            return NotFound(new { message = "Utilisateur introuvable." });
        }

        var nomComplet = user.Role == "Technicien" && user.Technicien != null
            ? $"{user.Technicien.Prenom} {user.Technicien.Nom}".Trim()
            : user.Role == "Responsable"
                ? "Responsable Maintenance"
                : user.Email;

        return Ok(new
        {
            user.Id,
            user.Email,
            user.Role,
            user.TechnicienId,
            NomComplet = nomComplet,
            Matricule = user.Technicien?.Matricule ?? "",
            Base = user.Technicien?.Base ?? "",
            user.DateCreation
        });
    }


    // ============================================================
    // 4. FORGOT PASSWORD (Token cryptographique + Hash SHA-256 + Email)
    // ============================================================

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto model)
    {
        if (string.IsNullOrWhiteSpace(model.Email))
        {
            return BadRequest(new { message = "L'adresse e-mail est requise." });
        }

        var email = model.Email.Trim().ToLowerInvariant();

        var user = await _context.Utilisateurs
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email);

        // Si l'utilisateur n'existe pas, renvoyer le message générique sans envoyer d'e-mail
        if (user == null)
        {
            _logger.LogInformation("Demande de réinitialisation pour un e-mail non enregistré : {Email}", email);
            return Ok(new
            {
                message = "Si un compte correspond à cette adresse, un lien de réinitialisation vous a été envoyé."
            });
        }

        // Générer 32 octets aléatoires cryptographiquement sûrs
        var rawToken = Convert.ToHexString(RandomNumberGenerator.GetBytes(32)).ToLowerInvariant();
        var tokenHash = HashToken(rawToken);

        var resetToken = new PasswordResetToken
        {
            UtilisateurId = user.Id,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddMinutes(30),
            CreatedAt = DateTime.UtcNow
        };

        _context.PasswordResetTokens.Add(resetToken);
        await _context.SaveChangesAsync();

        var requestBaseUrl = $"{Request.Scheme}://{Request.Host}";
        var resetLink = $"{requestBaseUrl}/reset-password.html?token={rawToken}";

        await _emailService.SendPasswordResetEmailAsync(user.Email, resetLink);

        _logger.LogInformation("Lien de réinitialisation généré et envoyé à {Email}.", user.Email);

        return Ok(new
        {
            message = "Si un compte correspond à cette adresse, un lien de réinitialisation vous a été envoyé."
        });
    }


    // ============================================================
    // 5. RESET PASSWORD (Vérification Hash + Nouveau mot de passe)
    // ============================================================

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto model)
    {
        if (string.IsNullOrWhiteSpace(model.Token) ||
            string.IsNullOrWhiteSpace(model.NewPassword) ||
            string.IsNullOrWhiteSpace(model.ConfirmPassword))
        {
            return BadRequest(new
            {
                message = "Token, nouveau mot de passe et confirmation requis."
            });
        }

        if (model.NewPassword != model.ConfirmPassword)
        {
            return BadRequest(new
            {
                message = "Les deux mots de passe ne correspondent pas."
            });
        }

        if (model.NewPassword.Length < 8)
        {
            return BadRequest(new
            {
                message = "Le mot de passe doit comporter au moins 8 caractères."
            });
        }

        var tokenHash = HashToken(model.Token.Trim());

        var tokenRecord = await _context.PasswordResetTokens
            .Include(t => t.Utilisateur)
            .FirstOrDefaultAsync(t =>
                t.TokenHash == tokenHash &&
                t.UsedAt == null &&
                t.ExpiresAt > DateTime.UtcNow);

        if (tokenRecord == null || tokenRecord.Utilisateur == null)
        {
            return BadRequest(new
            {
                message = "Le lien de réinitialisation est invalide ou a expiré."
            });
        }

        var hasher = new PasswordHasher<Utilisateur>();
        tokenRecord.Utilisateur.PasswordHash = hasher.HashPassword(tokenRecord.Utilisateur, model.NewPassword);
        tokenRecord.UsedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Mot de passe réinitialisé avec succès pour l'utilisateur {Email}.", tokenRecord.Utilisateur.Email);

        return Ok(new
        {
            message = "Votre mot de passe a été modifié avec succès."
        });
    }


    // ============================================================
    // 6. CHANGE PASSWORD (Utilisateur authentifié)
    // ============================================================

    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto model)
    {
        if (string.IsNullOrWhiteSpace(model.CurrentPassword) || string.IsNullOrWhiteSpace(model.NewPassword))
        {
            return BadRequest(new { message = "L'ancien et le nouveau mot de passe sont requis." });
        }

        if (model.NewPassword.Length < 8)
        {
            return BadRequest(new { message = "Le nouveau mot de passe doit contenir au moins 8 caractères." });
        }

        var userIdString = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdString, out var userId))
        {
            return Unauthorized(new { message = "Session invalide." });
        }

        var user = await _context.Utilisateurs.FindAsync(userId);
        if (user == null)
        {
            return NotFound(new { message = "Utilisateur introuvable." });
        }

        var hasher = new PasswordHasher<Utilisateur>();
        var verificationResult = hasher.VerifyHashedPassword(user, user.PasswordHash, model.CurrentPassword);

        if (verificationResult == PasswordVerificationResult.Failed)
        {
            return BadRequest(new { message = "Mot de passe actuel incorrect." });
        }

        user.PasswordHash = hasher.HashPassword(user, model.NewPassword);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Mot de passe modifié avec succès." });
    }


    // ============================================================
    // Helpers
    // ============================================================

    private static string HashToken(string token)
    {
        var bytes = Encoding.UTF8.GetBytes(token.Trim().ToLowerInvariant());
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}


// ============================================================
// DTOs
// ============================================================

public class LoginDto
{
    public string? Identifier { get; set; }
    public string? Email { get; set; }
    public string Password { get; set; } = string.Empty;
}

public class ForgotPasswordDto
{
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordDto
{
    public string Token { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class ChangePasswordDto
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}