using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TechnoVIS.Api.Contracts;
using TechnoVIS.Api.Models;
using TechnoVIS.Api.Services;

namespace TechnoVIS.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(UserManager<ApplicationUser> users, IJwtTokenService tokens) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<TokenResponse>> Register(RegisterRequest request)
    {
        var user = new ApplicationUser { UserName = request.Email, Email = request.Email, DisplayName = request.DisplayName };
        var result = await users.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var errors = result.Errors.ToDictionary(x => x.Code, x => new[] { x.Description });
            return BadRequest(new ValidationProblemDetails(errors));
        }
        await users.AddToRoleAsync(user, "Technician");
        return Ok(new TokenResponse(tokens.Create(user, new[] { "Technician" })));
    }

    [HttpPost("login")]
    public async Task<ActionResult<TokenResponse>> Login(LoginRequest request)
    {
        var user = await users.FindByEmailAsync(request.Email);
        if (user is null || !await users.CheckPasswordAsync(user, request.Password)) return Unauthorized();
        var roles = await users.GetRolesAsync(user);
        return Ok(new TokenResponse(tokens.Create(user, roles)));
    }
}
