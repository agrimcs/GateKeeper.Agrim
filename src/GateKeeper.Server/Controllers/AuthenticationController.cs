using GateKeeper.Application.Users.DTOs;
using GateKeeper.Application.Users.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using GateKeeper.Application.Common;
using GateKeeper.Domain.Interfaces;
using GateKeeper.Domain.Entities;

namespace GateKeeper.Server.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthenticationController : ControllerBase
{
    private readonly UserService _userService;
    private readonly ITenantService _tenantService;
    private readonly IOrganizationRepository _organizationRepository;
    private readonly IClientRepository _clientRepository;

    public AuthenticationController(
        UserService userService,
        ITenantService tenantService,
        IOrganizationRepository organizationRepository,
        IClientRepository clientRepository)
    {
        _userService = userService;
        _tenantService = tenantService;
        _organizationRepository = organizationRepository;
        _clientRepository = clientRepository;
    }

    /// <summary>
    /// Register a new user account
    /// Creates a new organization if organizationName and subdomain are provided
    /// Otherwise uses existing tenant context from subdomain/header
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterUserDto dto)
    {
        // Check for tenant context first
        var tenantId = _tenantService.GetCurrentTenantId();
        
        // If no tenant context, check if user wants to create a new organization
        if (tenantId == null)
        {
            // Check if organization details are provided
            if (!string.IsNullOrWhiteSpace(dto.OrganizationName) && 
                !string.IsNullOrWhiteSpace(dto.OrganizationSubdomain))
            {
                // Validate subdomain is unique
                var existingOrg = await _organizationRepository.GetBySubdomainAsync(dto.OrganizationSubdomain.ToLower().Trim());
                if (existingOrg != null)
                {
                    return BadRequest(new { message = $"Organization subdomain '{dto.OrganizationSubdomain}' is already taken. Please choose another." });
                }

                // Create new organization
                var newOrg = new Organization
                {
                    Id = Guid.NewGuid(),
                    Name = dto.OrganizationName.Trim(),
                    Subdomain = dto.OrganizationSubdomain.ToLower().Trim(),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    BillingPlan = "Free",
                    SettingsJson = System.Text.Json.JsonSerializer.Serialize(new OrganizationSettings 
                    { 
                        AllowSelfSignup = true 
                    })
                };

                await _organizationRepository.AddAsync(newOrg);
                await _organizationRepository.SaveChangesAsync();

                // Set this organization as tenant context for the registration
                HttpContext.Items["TenantId"] = newOrg.Id;
            }
            else
            {
                // No tenant context and no organization details provided
                return BadRequest(new
                {
                    message = "Organization details required. Please provide organization name and subdomain to create a new organization.",
                    hint = "Include organizationName and organizationSubdomain in your request, or register from a tenant subdomain."
                });
            }
        }

        var user = await _userService.RegisterAsync(dto);
        return Ok(user);
    }

    /// <summary>
    /// Login with email and password
    /// User's organization is determined from their email lookup
    /// </summary>
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginUserDto dto)
    {
        // Login no longer requires upfront tenant - we determine it from the user
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

            // --- Organization check for OAuth client flows ---
            if (!string.IsNullOrWhiteSpace(dto.ReturnUrl))
            {
                try
                {
                    // Parse client_id from returnUrl
                    var baseUri = new Uri($"http://dummy"); // base needed for relative URIs
                    var fullUri = new Uri(baseUri, dto.ReturnUrl);
                    var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(fullUri.Query);
                    if (query.TryGetValue("client_id", out var clientId) && !string.IsNullOrWhiteSpace(clientId))
                    {
                        var client = await _clientRepository.GetByClientIdAsync(clientId.ToString());
                        var tenantId = _tenantService.GetCurrentTenantId();
                        if (client != null && tenantId.HasValue && client.OrganizationId != tenantId.Value)
                        {
                            return BadRequest(new
                            {
                                message = "This OAuth client belongs to a different organization. Register a client for your organization or ask an admin to add your organization to this client."
                            });
                        }
                    }
                }
                catch { /* Ignore parse errors, do not block session */ }
            }
            // --- End org check ---

            // Create claims for cookie authentication (include tenant/org from JWT)
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Email, userEmail ?? ""),
                new Claim(ClaimTypes.Name, userEmail ?? "")
            };

            var tenantId2 = _tenantService.GetCurrentTenantId();
            if (tenantId2 != null && tenantId2.HasValue)
            {
                claims.Add(new Claim("org", tenantId2.Value.ToString()));
            }

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
