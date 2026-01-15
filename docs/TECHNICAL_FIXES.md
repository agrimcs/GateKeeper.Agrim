# Technical Fixes Required for MVP Completion

**Date:** January 14, 2026  
**Purpose:** Specific code changes needed to complete OAuth flow  
**Priority Order:** Sorted by criticality

---

## Fix 1: Add JWT Token Service (CRITICAL)

### Problem
Login endpoint validates credentials but doesn't return an access token. Users can't maintain authenticated sessions.

### Solution

#### 1.1 Create Token Service Interface

**File:** `GateKeeper.Application/Common/Interfaces/ITokenService.cs`

```csharp
namespace GateKeeper.Application.Common.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(Guid userId, string email, string firstName, string lastName);
    string GenerateRefreshToken();
    ClaimsPrincipal? ValidateToken(string token);
}
```

#### 1.2 Implement JWT Token Service

**File:** `GateKeeper.Infrastructure/Security/JwtTokenService.cs`

```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using GateKeeper.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace GateKeeper.Infrastructure.Security;

public class JwtTokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly SymmetricSecurityKey _key;

    public JwtTokenService(IConfiguration configuration)
    {
        _configuration = configuration;
        var secretKey = _configuration["Jwt:SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey not configured");
        _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
    }

    public string GenerateAccessToken(Guid userId, string email, string firstName, string lastName)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Name, $"{firstName} {lastName}"),
            new(ClaimTypes.GivenName, firstName),
            new(ClaimTypes.Surname, lastName),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var credentials = new SigningCredentials(_key, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(60), // 1 hour
            SigningCredentials = credentials,
            Issuer = _configuration["Jwt:Issuer"],
            Audience = _configuration["Jwt:Audience"],
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        
        try
        {
            var principal = tokenHandler.ValidateToken(token, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidAudience = _configuration["Jwt:Audience"],
                IssuerSigningKey = _key,
                ClockSkew = TimeSpan.Zero,
            }, out _);

            return principal;
        }
        catch
        {
            return null;
        }
    }
}
```

#### 1.3 Register Service in DependencyInjection

**File:** `GateKeeper.Infrastructure/DependencyInjection.cs`

Add after password hasher registration:

```csharp
// JWT Token Service
services.AddScoped<ITokenService, JwtTokenService>();
```

#### 1.4 Configure JWT Settings

**File:** `GateKeeper.Server/appsettings.json`

Add JWT configuration:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\ProjectsV13;Database=GateKeeperDb;Trusted_Connection=True;MultipleActiveResultSets=true"
  },
  "Jwt": {
    "SecretKey": "YourSuperSecretKeyThatIsAtLeast32CharactersLong!",
    "Issuer": "https://localhost:7001",
    "Audience": "https://localhost:7001"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

#### 1.5 Configure JWT Authentication

**File:** `GateKeeper.Server/Program.cs`

Replace authentication configuration:

```csharp
// JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:SecretKey"]!)),
        ClockSkew = TimeSpan.Zero
    };
});
```

Add this import at the top:

```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
```

#### 1.6 Update Login Response DTO

**File:** `GateKeeper.Application/Users/DTOs/LoginResponseDto.cs` (Create new file)

```csharp
namespace GateKeeper.Application.Users.DTOs;

public record LoginResponseDto
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string AccessToken { get; init; } = string.Empty;
    public string TokenType { get; init; } = "Bearer";
    public int ExpiresIn { get; init; } = 3600; // 1 hour in seconds
}
```

#### 1.7 Update UserService Login Method

**File:** `GateKeeper.Application/Users/Services/UserService.cs`

Update Login method to accept ITokenService and return LoginResponseDto:

```csharp
private readonly ITokenService _tokenService;

public UserService(
    IUserRepository userRepository,
    IPasswordHasher passwordHasher,
    ITokenService tokenService)
{
    _userRepository = userRepository;
    _passwordHasher = passwordHasher;
    _tokenService = tokenService;
}

public async Task<LoginResponseDto> LoginAsync(LoginUserDto dto)
{
    var user = await _userRepository.GetByEmailAsync(dto.Email)
        ?? throw new UnauthorizedException("Invalid email or password");

    if (!_passwordHasher.Verify(dto.Password, user.PasswordHash))
    {
        throw new UnauthorizedException("Invalid email or password");
    }

    var accessToken = _tokenService.GenerateAccessToken(
        user.Id,
        user.Email.Value,
        user.FirstName,
        user.LastName
    );

    return new LoginResponseDto
    {
        UserId = user.Id,
        Email = user.Email.Value,
        FirstName = user.FirstName,
        LastName = user.LastName,
        AccessToken = accessToken,
    };
}
```

#### 1.8 Update AuthenticationController

**File:** `GateKeeper.Server/Controllers/AuthenticationController.cs`

Update Login method return type:

```csharp
[HttpPost("login")]
public async Task<ActionResult<LoginResponseDto>> Login([FromBody] LoginUserDto dto)
{
    var result = await _userService.LoginAsync(dto);
    return Ok(result);
}
```

---

## Fix 2: Handle Unauthenticated OAuth Requests (CRITICAL)

### Problem
Authorization endpoint returns 401 when user isn't authenticated. Need proper redirect flow.

### Solution

#### 2.1 Update AuthorizationController

**File:** `GateKeeper.Server/Controllers/AuthorizationController.cs`

Replace Authorize method:

```csharp
[HttpGet("~/connect/authorize")]
[HttpPost("~/connect/authorize")]
[IgnoreAntiforgeryToken]
public async Task<IActionResult> Authorize()
{
    var request = HttpContext.GetOpenIddictServerRequest() ??
        throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

    // Get user from authenticated session
    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    
    if (string.IsNullOrEmpty(userId))
    {
        // Store OAuth request parameters and redirect to login
        var parameters = Request.HasFormContentType
            ? Request.Form.ToDictionary(p => p.Key, p => p.Value.ToString())
            : Request.Query.ToDictionary(p => p.Key, p => p.Value.ToString());

        // In a real implementation, store these parameters in session/cache
        // For MVP, redirect to login page with returnUrl
        var returnUrl = $"/connect/authorize?{Request.QueryString}";
        
        // For API, return challenge
        // For web UI, would redirect to login page
        return Challenge(
            authenticationSchemes: JwtBearerDefaults.AuthenticationScheme,
            properties: new AuthenticationProperties
            {
                RedirectUri = returnUrl
            });
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
    identity.SetResources(await _scopeManager.ListResourcesAsync(identity.GetScopes()).ToListAsync());

    var principal = new ClaimsPrincipal(identity);

    // Return authorization response with code
    return SignIn(principal, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
}
```

Add this field and update constructor:

```csharp
private readonly IOpenIddictScopeManager _scopeManager;

public AuthorizationController(
    UserService userService,
    IOpenIddictScopeManager scopeManager)
{
    _userService = userService;
    _scopeManager = scopeManager;
}
```

Add imports:

```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;
using OpenIddict.Abstractions;
```

---

## Fix 3: Protect API Endpoints (HIGH PRIORITY)

### Problem
API endpoints don't require authentication. Anyone can access them.

### Solution

Add `[Authorize]` attributes to controllers:

#### 3.1 ClientsController

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize] // Add this
public class ClientsController : ControllerBase
```

#### 3.2 UsersController

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize] // Add this
public class UsersController : ControllerBase
```

#### 3.3 AuthenticationController Profile Endpoint

```csharp
[HttpGet("profile/{id}")]
[Authorize] // Add this
public async Task<ActionResult<UserProfileDto>> GetProfile(Guid id)
```

---

## Fix 4: Add Client Secret Storage (MEDIUM PRIORITY)

### Problem
Client secrets are returned on creation but not stored in OpenIddict's format.

### Solution

#### 4.1 Update ClientService to Use OpenIddict Manager

**File:** `GateKeeper.Application/Clients/Services/ClientService.cs`

This requires integrating with `IOpenIddictApplicationManager` to store clients in OpenIddict's format. For MVP, the current implementation works, but production would need this.

**Recommendation:** Document as technical debt for post-MVP.

---

## Fix 5: Add Scope Management (MEDIUM PRIORITY)

### Problem
Scopes are referenced but not registered in OpenIddict.

### Solution

#### 5.1 Create Scope Seeding

**File:** `GateKeeper.Infrastructure/Persistence/ApplicationDbContextSeed.cs`

Add scope seeding:

```csharp
public static async Task SeedScopesAsync(IServiceProvider serviceProvider)
{
    var scopeManager = serviceProvider.GetRequiredService<IOpenIddictScopeManager>();

    var scopes = new[] { "openid", "profile", "email", "offline_access" };

    foreach (var scopeName in scopes)
    {
        if (await scopeManager.FindByNameAsync(scopeName) == null)
        {
            await scopeManager.CreateAsync(new OpenIddictScopeDescriptor
            {
                Name = scopeName,
                DisplayName = scopeName switch
                {
                    "openid" => "OpenID",
                    "profile" => "User Profile",
                    "email" => "Email Address",
                    "offline_access" => "Offline Access",
                    _ => scopeName
                }
            });
        }
    }
}
```

Call this in Program.cs after database migrations.

---

## Implementation Checklist

### Critical (Must Have for MVP)
- [ ] **Fix 1:** Implement JWT Token Service (all 8 steps)
- [ ] **Fix 2:** Update Authorization endpoint to handle unauthenticated users
- [ ] **Fix 3:** Add [Authorize] attributes to protect endpoints
- [ ] **Phase 5:** Build React frontend (see PHASE5_IMPLEMENTATION.md)

### High Priority (Needed for Complete OAuth Flow)
- [ ] **Fix 4:** Integrate OpenIddict Application Manager for client storage
- [ ] **Fix 5:** Register and seed OAuth scopes

### Medium Priority (Post-MVP)
- [ ] Add refresh token functionality
- [ ] Implement token revocation
- [ ] Add rate limiting
- [ ] Add comprehensive logging

---

## Testing After Fixes

### 1. Test JWT Authentication
```bash
# Register user
POST /api/auth/register

# Login - should return access token
POST /api/auth/login

# Use token to access protected endpoint
GET /api/clients
Authorization: Bearer <token>
```

### 2. Test OAuth Flow
```bash
# 1. Get access token from login
POST /api/auth/login

# 2. Make authorization request with Bearer token
GET /connect/authorize?client_id=...&redirect_uri=...&response_type=code&scope=openid
Authorization: Bearer <token>

# 3. Exchange code for token
POST /connect/token
```

---

## Estimated Implementation Time

| Fix | Time | Priority |
|-----|------|----------|
| Fix 1: JWT Token Service | 2 hours | CRITICAL |
| Fix 2: OAuth Authorization | 1 hour | CRITICAL |
| Fix 3: Protect Endpoints | 30 min | CRITICAL |
| Fix 4: Client Secret Storage | 2 hours | HIGH |
| Fix 5: Scope Management | 1 hour | HIGH |
| **Total Backend Fixes** | **6-7 hours** | |
| **Phase 5: React Frontend** | **4-6 hours** | CRITICAL |
| **Total to MVP** | **10-13 hours** | |

---

## Summary

These technical fixes bridge the gap between your current state (working API endpoints) and a complete OAuth2/OIDC identity provider. The most critical fix is implementing JWT tokens so users can maintain authenticated sessions. After that, the React frontend (Phase 5) will complete the MVP.
