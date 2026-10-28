using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using App.Domain;
namespace App.Controllers;
[ApiController, Route("api/account")]
public sealed class AccountController(UserManager<AppUser> users, SignInManager<AppUser> signIn, IAntiforgery antiforgery) : ControllerBase
{
    [HttpGet("session"), AllowAnonymous]
    public async Task<IActionResult> Session()
    {
        var token = antiforgery.GetAndStoreTokens(HttpContext).RequestToken;
        var user = await users.GetUserAsync(User);
        return Ok(new { csrfToken = token, authenticated = user is not null,
            user = user is null ? null : new { user.Id, user.DisplayName, user.Email, roles = await users.GetRolesAsync(user) } });
    }
    [HttpPost("login"), AllowAnonymous]
    public async Task<IActionResult> Login(LoginInput input)
    {
        var user = await users.FindByEmailAsync(input.Email);
        if (user is null) return Unauthorized(new { detail = "Invalid credentials." });
        var result = await signIn.PasswordSignInAsync(user, input.Password, false, lockoutOnFailure: true);
        return result.Succeeded ? Ok(new { signedIn = true }) : Unauthorized(new { detail = "Invalid credentials or account temporarily locked." });
    }
    [HttpPost("logout"), Authorize]
    public async Task<IActionResult> Logout() { await signIn.SignOutAsync(); return NoContent(); }
    public sealed record LoginInput([Required, EmailAddress] string Email, [Required] string Password);
}
