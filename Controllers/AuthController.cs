using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;
using TechnoVIS.Data;
using TechnoVIS.Models;

namespace TechnoVIS.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ILogger<AuthController> _logger;
    private readonly Microsoft.Extensions.Caching.Memory.IMemoryCache _cache;

    public AuthController(
        AppDbContext context,
        ILogger<AuthController> logger,
        Microsoft.Extensions.Caching.Memory.IMemoryCache cache)
    {
        _context = context;
        _logger = logger;
        _cache = cache;
    }

    // ============================================================
    // LOGIN
    // ============================================================

    /// <summary>
    /// Authentification par Email ou Matricule Technicien.
    /// La session est gérée avec un cookie sécurisé ASP.NET Core.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("LoginPolicy")]
    public async Task<IActionResult> Login([FromBody] LoginDto model)
    {
        if (!ModelState.IsValid ||
            string.IsNullOrWhiteSpace(model.Email) ||
            string.IsNullOrWhiteSpace(model.Password))
        {
            return BadRequest(new
            {
                message = "Identifiant (Email ou Matricule) et mot de passe requis."
            });
        }

        var identifier = model.Email.Trim().ToLowerInvariant();

        var user = await _context.Utilisateurs
            .Include(u => u.Technicien)
            .FirstOrDefaultAsync(u =>
                u.Email.ToLower() == identifier ||
                (
                    u.Technicien != null &&
                    u.Technicien.Matricule.ToLower() == identifier
                ));

        if (user == null)
        {
            _logger.LogWarning(
                "Tentative de connexion échouée : identifiant {Identifier} inconnu.",
                identifier);

            return Unauthorized(new
            {
                message = "Identifiant ou mot de passe incorrect."
            });
        }

        // --------------------------------------------------------
        // Un technicien doit être actif pour se connecter.
        // --------------------------------------------------------

        if (user.Role == "Technicien" &&
            (
                user.Technicien == null ||
                !string.Equals(
                    user.Technicien.Statut,
                    "Actif",
                    StringComparison.OrdinalIgnoreCase)
            ))
        {
            _logger.LogWarning(
                "Connexion refusée : compte technicien inactif pour {Identifier}.",
                identifier);

            return Unauthorized(new
            {
                message =
                    "Ce compte technicien a été désactivé. Veuillez contacter un responsable."
            });
        }

        // --------------------------------------------------------
        // Vérification du mot de passe
        // --------------------------------------------------------

        var passwordHasher = new PasswordHasher<Utilisateur>();

        var verificationResult =
            passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                model.Password);

        if (verificationResult == PasswordVerificationResult.Failed)
        {
            _logger.LogWarning(
                "Tentative de connexion échouée : mot de passe invalide pour {Identifier}.",
                identifier);

            return Unauthorized(new
            {
                message = "Identifiant ou mot de passe incorrect."
            });
        }

        // --------------------------------------------------------
        // Création de la session utilisateur
        // --------------------------------------------------------

        var nomComplet =
            user.Role == "Technicien" && user.Technicien != null
                ? $"{user.Technicien.Prenom} {user.Technicien.Nom}".Trim()
                : user.Role == "Responsable"
                    ? "Responsable Maintenance"
                    : user.Email;

        var claims = new List<Claim>
        {
            new(
                ClaimTypes.NameIdentifier,
                user.Id.ToString()),

            new(
                ClaimTypes.Email,
                user.Email),

            new(
                ClaimTypes.Role,
                user.Role),

            new(
                "role",
                user.Role),

            new(
                "technicienId",
                user.TechnicienId?.ToString() ?? ""),

            new(
                "matricule",
                user.Technicien?.Matricule ?? ""),

            new(
                "nomComplet",
                nomComplet)
        };

        var claimsIdentity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        var authenticationProperties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authenticationProperties);

        _logger.LogInformation(
            "Connexion réussie pour {Email} avec le rôle {Role}.",
            user.Email,
            user.Role);

        return Ok(new LoginResponseDto
        {
            Email = user.Email,
            Role = user.Role,
            TechnicienId = user.TechnicienId,
            NomComplet = nomComplet,
            Matricule = user.Technicien?.Matricule ?? "",
            Expiration = DateTime.UtcNow.AddHours(8)
        });
    }


    // ============================================================
    // LOGOUT
    // ============================================================

    /// <summary>
    /// Déconnexion de la session utilisateur.
    /// </summary>
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);

        return Ok(new
        {
            message = "Déconnexion réussie."
        });
    }


    // ============================================================
    // CURRENT USER
    // ============================================================

    /// <summary>
    /// Récupère les informations de l'utilisateur connecté
    /// via son cookie d'authentification.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userIdString =
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!int.TryParse(userIdString, out var userId))
        {
            return Unauthorized(new
            {
                message = "Session invalide."
            });
        }

        var user = await _context.Utilisateurs
            .Include(u => u.Technicien)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            return NotFound(new
            {
                message = "Utilisateur introuvable."
            });
        }

        var nomComplet =
            user.Role == "Technicien" && user.Technicien != null
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
    // CHANGE PASSWORD
    // ============================================================

    /// <summary>
    /// Modification du mot de passe de l'utilisateur connecté.
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordDto model)
    {
        if (string.IsNullOrWhiteSpace(model.CurrentPassword) ||
            string.IsNullOrWhiteSpace(model.NewPassword))
        {
            return BadRequest(new
            {
                message =
                    "L'ancien et le nouveau mot de passe sont requis."
            });
        }

        if (model.NewPassword.Length < 8)
        {
            return BadRequest(new
            {
                message =
                    "Le nouveau mot de passe doit contenir au moins 8 caractères."
            });
        }

        var userIdString =
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (!int.TryParse(userIdString, out var userId))
        {
            return Unauthorized(new
            {
                message = "Session invalide."
            });
        }

        var user = await _context.Utilisateurs.FindAsync(userId);

        if (user == null)
        {
            return NotFound(new
            {
                message = "Utilisateur introuvable."
            });
        }

        var hasher = new PasswordHasher<Utilisateur>();

        var verificationResult =
            hasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                model.CurrentPassword);

        if (verificationResult == PasswordVerificationResult.Failed)
        {
            return BadRequest(new
            {
                message = "Mot de passe actuel incorrect."
            });
        }

        user.PasswordHash =
            hasher.HashPassword(
                user,
                model.NewPassword);

        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Mot de passe modifié avec succès."
        });
    }


    // ============================================================
    // REGISTER
    // ============================================================

    /// <summary>
    /// Création d'un compte utilisateur.
    /// Réservé aux Responsables.
    /// </summary>
    [HttpPost("register")]
    [Authorize(Roles = "Responsable")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterDto model)
    {
        if (!ModelState.IsValid ||
            string.IsNullOrWhiteSpace(model.Email) ||
            string.IsNullOrWhiteSpace(model.Password))
        {
            return BadRequest(new
            {
                message = "Email et mot de passe requis."
            });
        }

        if (model.Password.Length < 8)
        {
            return BadRequest(new
            {
                message =
                    "Le mot de passe doit contenir au moins 8 caractères."
            });
        }

        var email = model.Email.Trim().ToLowerInvariant();

        var existingUser = await _context.Utilisateurs
            .AnyAsync(u => u.Email.ToLower() == email);

        if (existingUser)
        {
            return Conflict(new
            {
                message =
                    "Un utilisateur avec cet email existe déjà."
            });
        }

        // --------------------------------------------------------
        // Roles autorisés
        // --------------------------------------------------------

        var role = string.IsNullOrWhiteSpace(model.Role)
            ? "Technicien"
            : model.Role.Trim();

        if (role != "Technicien" &&
            role != "Responsable")
        {
            return BadRequest(new
            {
                message = "Rôle utilisateur invalide."
            });
        }

        // --------------------------------------------------------
        // Un compte Technicien doit être lié à un technicien.
        // --------------------------------------------------------

        if (role == "Technicien")
        {
            if (!model.TechnicienId.HasValue)
            {
                return BadRequest(new
                {
                    message =
                        "TechnicienId est obligatoire pour un compte technicien."
                });
            }

            var technicienExists =
                await _context.Techniciens
                    .AnyAsync(t =>
                        t.Id == model.TechnicienId.Value);

            if (!technicienExists)
            {
                return BadRequest(new
                {
                    message =
                        "Le technicien indiqué n'existe pas."
                });
            }

            var accountExists =
                await _context.Utilisateurs
                    .AnyAsync(u =>
                        u.TechnicienId ==
                        model.TechnicienId.Value);

            if (accountExists)
            {
                return Conflict(new
                {
                    message =
                        "Ce technicien possède déjà un compte."
                });
            }
        }

        // --------------------------------------------------------
        // Création du compte
        // --------------------------------------------------------

        var user = new Utilisateur
        {
            Email = email,
            Role = role,
            TechnicienId =
                role == "Technicien"
                    ? model.TechnicienId
                    : null,
            DateCreation = DateTime.UtcNow
        };

        var hasher = new PasswordHasher<Utilisateur>();

        user.PasswordHash =
            hasher.HashPassword(
                user,
                model.Password);

        _context.Utilisateurs.Add(user);

        await _context.SaveChangesAsync();

        _logger.LogInformation(
            "Création d'un compte utilisateur {Email} avec le rôle {Role}.",
            user.Email,
            user.Role);

        return Ok(new
        {
            message = "Utilisateur créé avec succès.",
            id = user.Id,
            email = user.Email,
            role = user.Role
        });
    }


    // ============================================================
    // FORGOT PASSWORD
    // ============================================================

    /// <summary>
    /// Demande de réinitialisation de mot de passe.
    /// Vérifie si l'adresse e-mail existe dans la base.
    /// Si oui, génère un code de vérification à 6 chiffres (valable 15 min).
    /// Si non, refuse la demande.
    /// </summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto model)
    {
        if (string.IsNullOrWhiteSpace(model.Email))
        {
            return BadRequest(new
            {
                message = "L'adresse e-mail est requise."
            });
        }

        var email = model.Email.Trim().ToLowerInvariant();

        var user = await _context.Utilisateurs
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email);

        if (user == null)
        {
            _logger.LogWarning("Demande mot de passe oublié refusée : email {Email} inconnu.", email);
            return NotFound(new
            {
                message = "Aucun compte associé à cette adresse e-mail n'a été trouvé."
            });
        }

        // Génération d'un code de vérification à 6 chiffres
        var verificationCode = Random.Shared.Next(100000, 999999).ToString();
        var cacheKey = $"pwd_reset_{email}";

        _cache.Set(cacheKey, verificationCode, TimeSpan.FromMinutes(15));

        _logger.LogInformation(
            "Code de vérification généré pour réinitialisation de mot de passe ({Email}) : {Code}",
            email,
            verificationCode);

        return Ok(new
        {
            message = "Un e-mail contenant le code de vérification a été envoyé à votre adresse.",
            email = user.Email
        });
    }


    // ============================================================
    // RESET PASSWORD
    // ============================================================

    /// <summary>
    /// Réinitialisation effective du mot de passe avec le code de vérification.
    /// </summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto model)
    {
        if (string.IsNullOrWhiteSpace(model.Email) ||
            string.IsNullOrWhiteSpace(model.Code) ||
            string.IsNullOrWhiteSpace(model.NewPassword))
        {
            return BadRequest(new
            {
                message = "E-mail, code de vérification et nouveau mot de passe requis."
            });
        }

        if (model.NewPassword.Length < 8)
        {
            return BadRequest(new
            {
                message = "Le nouveau mot de passe doit contenir au moins 8 caractères."
            });
        }

        var email = model.Email.Trim().ToLowerInvariant();
        var cacheKey = $"pwd_reset_{email}";

        if (!_cache.TryGetValue(cacheKey, out string? cachedCode) ||
            !string.Equals(cachedCode, model.Code.Trim(), StringComparison.Ordinal))
        {
            return BadRequest(new
            {
                message = "Le code de vérification est invalide ou a expiré."
            });
        }

        var user = await _context.Utilisateurs
            .FirstOrDefaultAsync(u => u.Email.ToLower() == email);

        if (user == null)
        {
            return NotFound(new
            {
                message = "Compte utilisateur introuvable."
            });
        }

        var hasher = new PasswordHasher<Utilisateur>();
        user.PasswordHash = hasher.HashPassword(user, model.NewPassword);

        await _context.SaveChangesAsync();

        // Supprimer le code après utilisation
        _cache.Remove(cacheKey);

        _logger.LogInformation("Mot de passe réinitialisé avec succès pour {Email}.", email);

        return Ok(new
        {
            message = "Votre mot de passe a été réinitialisé avec succès. Vous pouvez maintenant vous connecter."
        });
    }
}


// ============================================================
// DTOs
// ============================================================

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


public class ChangePasswordDto
{
    public string CurrentPassword { get; set; } = string.Empty;

    public string NewPassword { get; set; } = string.Empty;
}


public class LoginResponseDto
{
    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public int? TechnicienId { get; set; }

    public string NomComplet { get; set; } = string.Empty;

    public string Matricule { get; set; } = string.Empty;

    public DateTime Expiration { get; set; }
}


public class ForgotPasswordDto
{
    public string Email { get; set; } = string.Empty;
}


public class ResetPasswordDto
{
    public string Email { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public string NewPassword { get; set; } = string.Empty;
}