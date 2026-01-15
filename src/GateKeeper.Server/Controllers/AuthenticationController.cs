using GateKeeper.Application.Users.DTOs;
using GateKeeper.Application.Users.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GateKeeper.Server.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthenticationController : ControllerBase
{
    private readonly UserService _userService;

    public AuthenticationController(UserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// Register a new user account
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterUserDto dto)
    {
        var user = await _userService.RegisterAsync(dto);
        return Ok(user);
    }

    /// <summary>
    /// Login with email and password
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginUserDto dto)
    {
        var user = await _userService.LoginAsync(dto);
        return Ok(user);
    }

    /// <summary>
    /// Get current user profile
    /// </summary>
    [HttpGet("profile/{id}")]
    [Authorize]
    public async Task<IActionResult> GetProfile(Guid id)
    {
        var profile = await _userService.GetProfileAsync(id);
        return Ok(profile);
    }

    /// <summary>
    /// Establish cookie session for OAuth flows
    /// Called by React app after JWT login to create cookie for /connect/authorize
    /// </summary>
    [HttpPost("establish-session")]
    [Authorize] // Requires JWT token
    public async Task<IActionResult> EstablishSession([FromBody] EstablishSessionDto dto)
    {
        try
        {
            // User is already authenticated via JWT - get their ID
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(new { message = "Not authenticated" });
            }

            // Validate returnUrl to prevent open redirect
            if (!IsValidReturnUrl(dto.ReturnUrl))
            {
                return BadRequest(new { 
                    message = "Invalid return URL",
                    detail = $"ReturnUrl must start with /connect/authorize. Received: {dto.ReturnUrl}"
                });
            }

            // Create claims for cookie authentication
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Email, userEmail ?? ""),
                new Claim(ClaimTypes.Name, userEmail ?? "")
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var claimsPrincipal = new ClaimsPrincipal(claimsIdentity);

            // Sign in with cookie for OAuth flow
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                claimsPrincipal,
                new AuthenticationProperties
                {
                    IsPersistent = false, // Session cookie only
                    ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(10) // Short-lived for security
                });

            return Ok(new { returnUrl = dto.ReturnUrl });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { 
                message = "Failed to establish session",
                detail = ex.Message 
            });
        }
    }

    private bool IsValidReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrEmpty(returnUrl))
            return false;

        // Only allow relative URLs starting with /connect/authorize
        return returnUrl.StartsWith("/connect/authorize", StringComparison.OrdinalIgnoreCase);
    }
}

// DTO for establishing OAuth session
public class EstablishSessionDto
{
    public string ReturnUrl { get; set; } = string.Empty;
}
