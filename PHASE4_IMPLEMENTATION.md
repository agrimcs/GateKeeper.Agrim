# Phase 4: API Layer & OAuth Server - Implementation Guide

**Estimated Time:** 3-4 hours  
**Goal:** Create REST API controllers and integrate OpenIddict OAuth2/OIDC server  
**Prerequisites:** Phase 1 (Domain), Phase 2 (Application), and Phase 3 (Infrastructure) completed

---

## Objectives

By the end of Phase 4, you will have:
- ✅ REST API controllers for Users and Clients
- ✅ Authentication endpoints (register, login, profile)
- ✅ OpenIddict OAuth2/OIDC server configured
- ✅ OAuth authorization and token endpoints working
- ✅ Global exception handling middleware
- ✅ CORS configuration for React frontend
- ✅ JWT authentication setup
- ✅ Complete API ready for frontend integration

---

## High-Level Architecture

```
┌─────────────────────────────────────────────────────┐
│              GateKeeper.Server (API)                │
│                                                     │
│  ┌────────────────┐      ┌──────────────────┐     │
│  │  Controllers   │─────→│ Application      │     │
│  │  - Auth        │      │ Services         │     │
│  │  - Users       │      │ - UserService    │     │
│  │  - Clients     │      │ - ClientService  │     │
│  └────────────────┘      └──────────────────┘     │
│                                                     │
│  ┌────────────────┐      ┌──────────────────┐     │
│  │  OAuth         │─────→│ OpenIddict       │     │
│  │  Endpoints     │      │ Server           │     │
│  │  /authorize    │      │                  │     │
│  │  /token        │      │                  │     │
│  └────────────────┘      └──────────────────┘     │
│                                                     │
│  ┌────────────────────────────────────────────┐   │
│  │       Exception Handling Middleware        │   │
│  └────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────┘
```

---

## Task 1: Update Project Dependencies

### Update GateKeeper.Server.csproj

Add these NuGet packages:

```xml
<ItemGroup>
  <ProjectReference Include="..\GateKeeper.Application\GateKeeper.Application.csproj" />
  <ProjectReference Include="..\GateKeeper.Infrastructure\GateKeeper.Infrastructure.csproj" />
</ItemGroup>

<ItemGroup>
  <!-- OpenIddict packages for OAuth2/OIDC -->
  <PackageReference Include="OpenIddict.AspNetCore" Version="5.8.0" />
  <PackageReference Include="OpenIddict.EntityFrameworkCore" Version="5.8.0" />
  
  <!-- Authentication -->
  <PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="9.0.0" />
  
  <!-- Already included in template -->
  <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="9.0.0">
    <PrivateAssets>all</PrivateAssets>
    <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
  </PackageReference>
</ItemGroup>
```

---

## Task 2: Configure OpenIddict in Infrastructure

**File:** `GateKeeper.Infrastructure/DependencyInjection.cs`

Add OpenIddict configuration to the existing `AddInfrastructure` method:

```csharp
public static IServiceCollection AddInfrastructure(
    this IServiceCollection services,
    IConfiguration configuration)
{
    // ... existing DbContext configuration ...

    // OpenIddict configuration
    services.AddOpenIddict()
        .AddCore(options =>
        {
            options.UseEntityFrameworkCore()
                .UseDbContext<ApplicationDbContext>();
        })
        .AddServer(options =>
        {
            // Enable the authorization and token endpoints
            options.SetAuthorizationEndpointUris("/connect/authorize")
                   .SetTokenEndpointUris("/connect/token")
                   .SetUserinfoEndpointUris("/connect/userinfo");

            // Enable flows
            options.AllowAuthorizationCodeFlow()
                   .AllowRefreshTokenFlow();

            // Require PKCE for public clients
            options.RequireProofKeyForCodeExchange();

            // Register scopes (permissions)
            options.RegisterScopes("openid", "profile", "email", "offline_access");

            // Configure token lifetimes
            options.SetAccessTokenLifetime(TimeSpan.FromMinutes(15))
                   .SetAuthorizationCodeLifetime(TimeSpan.FromMinutes(5))
                   .SetRefreshTokenLifetime(TimeSpan.FromDays(30));

            // Register signing and encryption credentials
            options.AddDevelopmentEncryptionCertificate()
                   .AddDevelopmentSigningCertificate();

            // Register ASP.NET Core host
            options.UseAspNetCore()
                   .EnableAuthorizationEndpointPassthrough()
                   .EnableTokenEndpointPassthrough()
                   .EnableUserinfoEndpointPassthrough();
        })
        .AddValidation(options =>
        {
            options.UseLocalServer();
            options.UseAspNetCore();
        });

    // ... rest of existing infrastructure registration ...
    
    return services;
}
```

**Note:** This configures OpenIddict to handle OAuth2/OIDC flows with PKCE required for security.

---

## Task 3: Update ApplicationDbContext for OpenIddict

**File:** `GateKeeper.Infrastructure/Persistence/ApplicationDbContext.cs`

```csharp
using GateKeeper.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GateKeeper.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Client> Clients => Set<Client>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply domain entity configurations
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
```

**Note:** OpenIddict will automatically create its tables (`OpenIddictApplications`, `OpenIddictAuthorizations`, `OpenIddictTokens`, `OpenIddictScopes`) when you run migrations.

---

## Task 4: Create New Migration for OpenIddict

```bash
cd GateKeeper.Infrastructure

dotnet ef migrations add AddOpenIddict --startup-project ../GateKeeper.Server/GateKeeper.Server.csproj

# Apply migration
dotnet ef database update --startup-project ../GateKeeper.Server/GateKeeper.Server.csproj
```

This creates OpenIddict's required tables in your database.

---

## Task 5: Configure Program.cs

**File:** `GateKeeper.Server/Program.cs`

Replace with complete configuration:

```csharp
using GateKeeper.Application.Users.Services;
using GateKeeper.Application.Clients.Services;
using GateKeeper.Infrastructure;
using GateKeeper.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Add Infrastructure layer (includes DbContext, Repositories, OpenIddict)
builder.Services.AddInfrastructure(builder.Configuration);

// Add Application layer services
builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<ClientService>();

// Add controllers
builder.Services.AddControllers();

// Configure CORS for React frontend
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173", "https://localhost:5173") // Vite dev server
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Add authentication
builder.Services.AddAuthentication();

// Add authorization
builder.Services.AddAuthorization();

// Build app
var app = builder.Build();

// Apply migrations automatically
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.MigrateAsync();
}

// Configure HTTP pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();

app.UseCors(); // Enable CORS

app.UseStaticFiles(); // For React build files

app.UseRouting();

app.UseAuthentication(); // Must come before Authorization
app.UseAuthorization();

app.MapControllers();

// Fallback to React app for client-side routing
app.MapFallbackToFile("/index.html");

app.Run();
```

---

## Task 6: Create Exception Handling Middleware

**File:** `GateKeeper.Server/Middleware/ExceptionHandlingMiddleware.cs`

```csharp
using System.Net;
using System.Text.Json;
using GateKeeper.Domain.Exceptions;
using GateKeeper.Application.Common.Exceptions;
using FluentValidation;

namespace GateKeeper.Server.Middleware;

/// <summary>
/// Global exception handling middleware.
/// Converts domain and application exceptions to proper HTTP responses.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, message) = exception switch
        {
            DomainException => (HttpStatusCode.BadRequest, exception.Message),
            ApplicationException => (HttpStatusCode.BadRequest, exception.Message),
            ValidationException validationEx => (HttpStatusCode.BadRequest, 
                string.Join("; ", validationEx.Errors.Select(e => e.ErrorMessage))),
            KeyNotFoundException => (HttpStatusCode.NotFound, "Resource not found"),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "Unauthorized"),
            _ => (HttpStatusCode.InternalServerError, "An error occurred processing your request")
        };

        _logger.LogError(exception, "Exception occurred: {Message}", exception.Message);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = new
        {
            error = message,
            statusCode = (int)statusCode
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(response));
    }
}
```

**Register in Program.cs** (add before `app.UseHttpsRedirection()`):

```csharp
app.UseMiddleware<ExceptionHandlingMiddleware>();
```

---

## Task 7: Create Controllers

### Authentication Controller

**File:** `GateKeeper.Server/Controllers/AuthenticationController.cs`

```csharp
using GateKeeper.Application.Users.DTOs;
using GateKeeper.Application.Users.Services;
using Microsoft.AspNetCore.Mvc;

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
    public async Task<IActionResult> GetProfile(Guid id)
    {
        var profile = await _userService.GetProfileAsync(id);
        return Ok(profile);
    }
}
```

### Clients Controller

**File:** `GateKeeper.Server/Controllers/ClientsController.cs`

```csharp
using GateKeeper.Application.Clients.DTOs;
using GateKeeper.Application.Clients.Services;
using Microsoft.AspNetCore.Mvc;

namespace GateKeeper.Server.Controllers;

[ApiController]
[Route("api/clients")]
public class ClientsController : ControllerBase
{
    private readonly ClientService _clientService;

    public ClientsController(ClientService clientService)
    {
        _clientService = clientService;
    }

    /// <summary>
    /// Get all OAuth clients
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int skip = 0, [FromQuery] int take = 50)
    {
        var clients = await _clientService.GetAllAsync(skip, take);
        return Ok(clients);
    }

    /// <summary>
    /// Get OAuth client by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var client = await _clientService.GetByIdAsync(id);
        return Ok(client);
    }

    /// <summary>
    /// Register a new OAuth client
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Register([FromBody] RegisterClientDto dto)
    {
        var client = await _clientService.RegisterAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = client.Id }, client);
    }

    /// <summary>
    /// Update existing OAuth client
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateClientDto dto)
    {
        var client = await _clientService.UpdateAsync(id, dto);
        return Ok(client);
    }

    /// <summary>
    /// Delete OAuth client
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await _clientService.DeleteAsync(id);
        return NoContent();
    }
}
```

### Users Controller (Admin)

**File:** `GateKeeper.Server/Controllers/UsersController.cs`

```csharp
using GateKeeper.Application.Users.Services;
using Microsoft.AspNetCore.Mvc;

namespace GateKeeper.Server.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly UserService _userService;

    public UsersController(UserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// Get all users (admin endpoint)
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int skip = 0, [FromQuery] int take = 50)
    {
        var users = await _userService.GetAllAsync(skip, take);
        return Ok(users);
    }

    /// <summary>
    /// Get user by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var user = await _userService.GetProfileAsync(id);
        return Ok(user);
    }
}
```

---

## Task 8: Create OAuth Authorization Endpoint

**File:** `GateKeeper.Server/Controllers/AuthorizationController.cs`

```csharp
using GateKeeper.Application.Users.Services;
using Microsoft.AspNetCore;
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

    public AuthorizationController(UserService userService)
    {
        _userService = userService;
    }

    /// <summary>
    /// OAuth2 Authorization Endpoint
    /// Handles authorization code flow
    /// </summary>
    [HttpGet("~/connect/authorize")]
    [HttpPost("~/connect/authorize")]
    public async Task<IActionResult> Authorize()
    {
        var request = HttpContext.GetOpenIddictServerRequest() ??
            throw new InvalidOperationException("The OpenID Connect request cannot be retrieved.");

        // For simplicity, auto-approve for MVP
        // In production, show consent screen here
        
        // Get user from request (simplified - normally from authenticated session)
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (string.IsNullOrEmpty(userId))
        {
            // Redirect to login page in production
            return Challenge();
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

        // Set requested scopes
        identity.SetScopes(request.GetScopes());

        var principal = new ClaimsPrincipal(identity);

        // Return authorization response
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
}
```

---

## Task 9: Update appsettings.json

**File:** `GateKeeper.Server/appsettings.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=GateKeeperDb;Integrated Security=true;TrustServerCertificate=true;MultipleActiveResultSets=true"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Warning"
    }
  },
  "AllowedHosts": "*"
}
```

**File:** `GateKeeper.Server/appsettings.Development.json`

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=GateKeeperDb_Dev;Integrated Security=true;TrustServerCertificate=true;MultipleActiveResultSets=true"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Information",
      "Microsoft.EntityFrameworkCore": "Information"
    },
    "EnableSensitiveDataLogging": true
  }
}
```

---

## Task 10: Test Your API

### Build and Run

```bash
cd GateKeeper.Server
dotnet build
dotnet run
```

Server should start at: `https://localhost:7001`

### Test with Postman

**1. Register a User**
```http
POST https://localhost:7001/api/auth/register
Content-Type: application/json

{
  "email": "john.doe@example.com",
  "password": "SecurePass123!",
  "firstName": "John",
  "lastName": "Doe"
}
```

**2. Login**
```http
POST https://localhost:7001/api/auth/login
Content-Type: application/json

{
  "email": "john.doe@example.com",
  "password": "SecurePass123!"
}
```

**3. Register OAuth Client**
```http
POST https://localhost:7001/api/clients
Content-Type: application/json

{
  "displayName": "My Test App",
  "type": "Public",
  "redirectUris": [
    "http://localhost:5173/callback",
    "https://oauth.pstmn.io/v1/callback"
  ],
  "allowedScopes": ["openid", "profile", "email"]
}
```

**4. Test OAuth Flow**
```http
GET https://localhost:7001/connect/authorize
  ?client_id=<client_id_from_registration>
  &redirect_uri=https://oauth.pstmn.io/v1/callback
  &response_type=code
  &scope=openid profile email
  &code_challenge=<PKCE_code_challenge>
  &code_challenge_method=S256
```

---

## Task 11: Verify OpenIddict Discovery

OpenIddict automatically creates a discovery document:

```http
GET https://localhost:7001/.well-known/openid-configuration
```

You should see:
```json
{
  "issuer": "https://localhost:7001/",
  "authorization_endpoint": "https://localhost:7001/connect/authorize",
  "token_endpoint": "https://localhost:7001/connect/token",
  "userinfo_endpoint": "https://localhost:7001/connect/userinfo",
  "grant_types_supported": ["authorization_code", "refresh_token"],
  "response_types_supported": ["code"],
  "scopes_supported": ["openid", "profile", "email", "offline_access"]
}
```

---

## Phase 4 Checklist

Before moving to Phase 5 (React frontend), verify:

- [ ] ✅ All controllers created (Auth, Users, Clients, Authorization)
- [ ] ✅ OpenIddict configured and migrations applied
- [ ] ✅ Exception handling middleware working
- [ ] ✅ CORS configured for React frontend
- [ ] ✅ POST /api/auth/register works
- [ ] ✅ POST /api/auth/login works
- [ ] ✅ POST /api/clients creates OAuth clients
- [ ] ✅ GET /.well-known/openid-configuration returns discovery document
- [ ] ✅ OAuth authorize endpoint responds (returns 401 if not authenticated - expected)
- [ ] ✅ No build errors or warnings
- [ ] ✅ Database has OpenIddict tables (OpenIddictApplications, etc.)

---

## Key Architectural Points

### Controller Responsibilities
- **Thin controllers** - Only handle HTTP concerns (routing, status codes)
- **Delegate to services** - Business logic in Application layer
- **Return DTOs** - Never expose domain entities directly

### OpenIddict Integration
- **Passthrough mode** - Controllers handle authorization logic
- **Entity Framework storage** - Uses your existing DbContext
- **PKCE enforcement** - Required for all public clients
- **Development certificates** - Auto-generated for signing/encryption

### Security
- **CORS** - Restricted to localhost:5173 (React dev server)
- **HTTPS** - Required in production
- **Exception handling** - Never leak sensitive info in errors

---

## Common Issues & Solutions

### Issue: OpenIddict tables not created
**Solution:** Run migrations: `dotnet ef database update`

### Issue: CORS errors in browser
**Solution:** Verify React app runs on port 5173, check CORS policy in Program.cs

### Issue: "Cannot retrieve OpenID Connect request"
**Solution:** Ensure OpenIddict middleware is registered before controllers

### Issue: Validation errors not returning
**Solution:** Check FluentValidation is registered in Application layer

---

## Summary

Phase 4 delivers:

✅ **REST API** - Full CRUD for Users and Clients  
✅ **OAuth2/OIDC** - Authorization Code Flow with PKCE  
✅ **OpenIddict** - Production-ready OAuth server  
✅ **Exception Handling** - Clean error responses  
✅ **CORS** - Frontend integration ready  

Your backend is now **complete and ready for the React frontend (Phase 5)**.

---

**Estimated Completion Time:** 3-4 hours  
**Phase 4 Status:** 🎯 Ready for Implementation  
**Next Phase:** Phase 5 - React Frontend (Coming Next)
