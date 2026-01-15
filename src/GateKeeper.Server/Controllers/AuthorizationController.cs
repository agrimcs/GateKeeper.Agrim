using GateKeeper.Application.Users.Services;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using System.Security.Claims;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace GateKeeper.Server.Controllers;

[ApiController]
public class AuthorizationController : ControllerBase
{
    private readonly UserService _userService;
    private readonly IOpenIddictScopeManager _scopeManager;
    private readonly IConfiguration _configuration;

    public AuthorizationController(
        UserService userService,
        IOpenIddictScopeManager scopeManager,
        IConfiguration configuration)
    {
        _userService = userService;
        _scopeManager = scopeManager;
        _configuration = configuration;
    }

    /// <summary>
    /// OAuth2 Authorization Endpoint
    /// Handles authorization code flow
    /// </summary>
    [HttpGet("~/connect/authorize")]
    [HttpPost("~/connect/authorize")]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Authorize()
    {
        var request = HttpContext.GetOpenIddictServerRequest() ??
            throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        // Try to authenticate with cookie scheme first (for OAuth flows)
        var cookieAuth = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        
        string? userId = null;
        
        if (cookieAuth.Succeeded)
        {
            userId = cookieAuth.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
        else
        {
            // Fallback to JWT (for API access)
            userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
        
        if (string.IsNullOrEmpty(userId))
        {
            // User not authenticated - redirect to login
            var returnUrl = Request.Path.Value + Request.QueryString.Value;
            var clientAppUrl = _configuration["ClientApp:Url"] ?? "";
            var loginUrl = $"{clientAppUrl}/login?returnUrl={Uri.EscapeDataString(returnUrl)}";
            
            return Redirect(loginUrl);
        }

        var userGuid = Guid.Parse(userId);
        var userProfile = await _userService.GetProfileAsync(userGuid);

        // Create claims principal
        var identity = new ClaimsIdentity(
            authenticationType: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            nameType: Claims.Name,
            roleType: Claims.Role);

        identity.AddClaim(Claims.Subject, userProfile.Id.ToString());
        identity.AddClaim(Claims.Email, userProfile.Email);
        identity.AddClaim(Claims.Name, $"{userProfile.FirstName} {userProfile.LastName}");
        identity.AddClaim(Claims.GivenName, userProfile.FirstName);
        identity.AddClaim(Claims.FamilyName, userProfile.LastName);

        // Set requested scopes
        identity.SetScopes(request.GetScopes());

        // Set resources (if any)
        var resources = new List<string>();
        await foreach (var resource in _scopeManager.ListResourcesAsync(identity.GetScopes()))
        {
            resources.Add(resource);
        }
        identity.SetResources(resources);

        var principal = new ClaimsPrincipal(identity);

        // Return authorization response with code
        return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    /// <summary>
    /// OAuth2 Token Endpoint
    /// Exchanges authorization code for access token
    /// </summary>
    [HttpPost("~/connect/token")]
    public async Task<IActionResult> Exchange()
    {
        var request = HttpContext.GetOpenIddictServerRequest() ??
            throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        if (request.IsAuthorizationCodeGrantType() || request.IsRefreshTokenGrantType())
        {
            // Retrieve the claims principal from the authorization code or refresh token
            var principal = (await HttpContext.AuthenticateAsync(
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)).Principal;

            if (principal == null)
            {
                return Forbid(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
            }

            // Return access token
            return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        throw new InvalidOperationException("The specified grant type is not supported.");
    }

    /// <summary>
    /// UserInfo Endpoint
    /// Returns user claims
    /// </summary>
    [HttpGet("~/connect/userinfo")]
    [HttpPost("~/connect/userinfo")]
    [Authorize(AuthenticationSchemes = OpenIddictServerAspNetCoreDefaults.AuthenticationScheme)]
    public async Task<IActionResult> Userinfo()
    {
        var userId = User.FindFirst(Claims.Subject)?.Value;
        
        if (string.IsNullOrEmpty(userId))
        {
            return Challenge(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
        }

        var userGuid = Guid.Parse(userId);
        var userProfile = await _userService.GetProfileAsync(userGuid);

        return Ok(new
        {
            sub = userProfile.Id,
            email = userProfile.Email,
            name = $"{userProfile.FirstName} {userProfile.LastName}",
            given_name = userProfile.FirstName,
            family_name = userProfile.LastName
        });
    }

    /// <summary>
    /// Logout Endpoint
    /// Ends user session and redirects back to client
    /// </summary>
    [HttpGet("~/connect/logout")]
    [HttpPost("~/connect/logout")]
    public async Task<IActionResult> Logout()
    {
        var postLogoutRedirectUri = Request.Query["post_logout_redirect_uri"].ToString();

        // Sign out from cookie authentication (clears OAuth session)
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        // Redirect back to the client app
        if (!string.IsNullOrEmpty(postLogoutRedirectUri))
        {
            return Redirect(postLogoutRedirectUri);
        }

        // Default redirect to React app login
        return Redirect($"{_configuration["ClientApp:Url"]}/login");
    }
}