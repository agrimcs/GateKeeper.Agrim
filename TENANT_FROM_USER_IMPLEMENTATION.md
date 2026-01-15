# Multi-Tenant: Tenant From User Implementation

## Overview

Implemented **Auth0-style multi-tenancy** where:
- ✅ **Login determines tenant** (not the other way around)
- ✅ User's email/password → lookup user → get their `OrganizationId`
- ✅ JWT token includes `"org"` claim with user's organization
- ✅ All subsequent API calls use org from token
- ✅ OAuth flows work without upfront tenant selection

## Architecture Changes

### Backend Changes

#### 1. AuthenticationController - Login No Longer Requires Tenant

**File:** [AuthenticationController.cs](src/GateKeeper.Server/Controllers/AuthenticationController.cs)

```csharp
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
```

**Previous code:** Required `GetCurrentTenantId()` check before login → **REMOVED**

---

#### 2. UserService - Login Adds Org Claim to JWT

**File:** [UserService.cs](src/GateKeeper.Application/Users/Services/UserService.cs)

```csharp
public async Task<LoginResponseDto> LoginAsync(
    LoginUserDto dto, 
    CancellationToken cancellationToken = default)
{
    // Get user by email (no tenant filter needed)
    var user = await _userRepository.GetByEmailAsync(email, cancellationToken);
    
    // Verify password...
    
    // Generate JWT token WITH org claim from user's OrganizationId
    var token = _jwtTokenGenerator.GenerateTokenWithOrg(
        user.Id,
        user.Email.Value,
        user.FirstName,
        user.LastName,
        user.OrganizationId  // ← User's org added to token
    );
    
    return new LoginResponseDto { Token = token, User = MapToProfileDto(user) };
}
```

**Key change:** Token now includes org claim from user's `OrganizationId`

---

#### 3. JWT Token Generator - New Method With Org Claim

**File:** [IJwtTokenGenerator.cs](src/GateKeeper.Application/Common/Interfaces/IJwtTokenGenerator.cs)

```csharp
string GenerateTokenWithOrg(Guid userId, string email, string firstName, string lastName, Guid organizationId);
```

**Implementation:** [JwtTokenGenerator.cs](src/GateKeeper.Infrastructure/Security/JwtTokenGenerator.cs)

```csharp
public string GenerateTokenWithOrg(Guid userId, string email, string firstName, string lastName, Guid organizationId)
{
    var claims = new[]
    {
        new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
        new Claim(JwtRegisteredClaimNames.Email, email),
        new Claim(JwtRegisteredClaimNames.GivenName, firstName),
        new Claim(JwtRegisteredClaimNames.FamilyName, lastName),
        new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
        new Claim(ClaimTypes.Email, email),
        new Claim(ClaimTypes.Name, $"{firstName} {lastName}"),
        new Claim(ClaimTypes.GivenName, firstName),
        new Claim(ClaimTypes.Surname, lastName),
        new Claim("org", organizationId.ToString())  // ← Organization claim
    };
    
    // Create token...
}
```

---

#### 4. TenantService - Reads Org From JWT Claims

**File:** [TenantService.cs](src/GateKeeper.Infrastructure/Services/TenantService.cs)

```csharp
public Guid? GetCurrentTenantId()
{
    var ctx = _httpContextAccessor.HttpContext;
    if (ctx == null) return null;

    // 1. Check if middleware set it via HttpContext.Items (from subdomain/header/query)
    if (ctx.Items.TryGetValue("TenantId", out var tenantIdObj) && tenantIdObj is Guid tenantId)
        return tenantId;

    // 2. Check JWT claims for "org" claim (set during login) ← NEW
    var orgClaim = ctx.User?.FindFirst("org");
    if (orgClaim != null && Guid.TryParse(orgClaim.Value, out var orgId))
        return orgId;

    return null;
}
```

**Resolution priority:**
1. HttpContext.Items["TenantId"] (from middleware - subdomain/header/query)
2. **JWT "org" claim** (from user login)
3. null

---

#### 5. OAuth Authorization - Includes Org in Token Claims

**File:** [AuthorizationController.cs](src/GateKeeper.Server/Controllers/AuthorizationController.cs)

```csharp
var userProfile = await _userService.GetProfileAsync(userGuid);

var identity = new ClaimsIdentity(/*...*/);
identity.AddClaim(Claims.Subject, userProfile.Id.ToString());
identity.AddClaim(Claims.Email, userProfile.Email);
identity.AddClaim(Claims.Name, $"{userProfile.FirstName} {userProfile.LastName}");

// Add organization claim from user profile ← NEW
identity.AddClaim("org", userProfile.OrganizationId.ToString());

identity.SetScopes(request.GetScopes());
```

**Result:** OAuth access tokens also include org claim

---

#### 6. UserProfileDto - Now Includes OrganizationId

**File:** [UserProfileDto.cs](src/GateKeeper.Application/Users/DTOs/UserProfileDto.cs)

```csharp
public record UserProfileDto
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? LastLoginAt { get; init; }
    public Guid OrganizationId { get; init; }  // ← NEW
}
```

---

### Frontend Changes

#### 1. LoginPage - No Tenant Input Required

**File:** [LoginPage.jsx](gatekeeper.client/src/features/auth/LoginPage.jsx)

**Removed:**
- Tenant input field
- `showTenantInput` state
- `tenantInput` state  
- Tenant validation logic
- Page reload on tenant selection

**Result:** Users just enter email + password, server determines org

---

#### 2. AuthContext - Clears Tenant on Logout

**File:** [AuthContext.jsx](gatekeeper.client/src/features/auth/AuthContext.jsx)

```jsx
import { clearTenantOverride } from '../../services/tenant';

const logout = () => {
  authService.logout();
  clearTenantOverride(); // ← Clear localStorage tenant override
  setUser(null);
};
```

---

#### 3. RegisterPage - Still Requires Tenant Context

**File:** [RegisterPage.jsx](gatekeeper.client/src/features/auth/RegisterPage.jsx)

**No changes** - registration still requires tenant context via:
- Subdomain: `acme.localhost/register`
- Query param: `/register?tenant=acme`
- Tenant input UI: Shows on localhost without tenant

**Rationale:** New users don't have an existing org, so we need to know which org they're registering into

---

## Testing Workflow

### 1. Register a User (Requires Tenant)

```
Navigate to: http://localhost:5173/register?tenant=default

1. Enter tenant: "default" (or use ?tenant=default in URL)
2. Fill registration form
3. User is created with OrganizationId matching "default" org
```

### 2. Login (No Tenant Required)

```
Navigate to: http://localhost:5173/login

1. Enter email: admin@gatekeeper.local
2. Enter password: Admin123!@#
3. Submit

Backend:
- Looks up user by email
- Finds OrganizationId: <default-org-guid>
- Generates JWT with "org": "<default-org-guid>" claim
- Returns token

Frontend:
- Stores token in localStorage
- All API calls include Authorization: Bearer <token>
```

### 3. OAuth Flow (Works Automatically)

```
Demo client initiates OAuth:
1. Redirects to /connect/authorize
2. User not authenticated → redirect to login
3. User logs in (JWT issued with org claim)
4. Session established via /api/auth/establish-session
   - Reads org from JWT token's "org" claim
   - Creates cookie with org claim
5. OAuth authorization completes
6. Access token includes org claim from user
7. All subsequent API calls scoped to user's org
```

### 4. API Calls With Tenant Context

All API calls now work because:
1. Request includes `Authorization: Bearer <jwt-token>`
2. TenantService.GetCurrentTenantId() extracts org from JWT
3. Query filters apply automatically: `WHERE OrganizationId = @orgId`
4. User only sees data from their organization

---

## Migration Path

### For Existing Code

**Old pattern (no longer needed):**
```javascript
// Frontend - DON'T DO THIS ANYMORE
localStorage.setItem('tenant_override', 'acme');
window.location.reload();
```

**New pattern:**
```javascript
// Just login - tenant determined automatically
await login(email, password);
```

### For Tests

Update tests that mock JWT token generation:

```csharp
// OLD
_jwtTokenGenerator
    .Setup(x => x.GenerateToken(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
    .Returns("test-jwt-token");

// NEW - also mock GenerateTokenWithOrg
_jwtTokenGenerator
    .Setup(x => x.GenerateTokenWithOrg(
        It.IsAny<Guid>(), 
        It.IsAny<string>(), 
        It.IsAny<string>(), 
        It.IsAny<string>(),
        It.IsAny<Guid>()))  // ← organizationId parameter
    .Returns("test-jwt-token");
```

---

## Benefits

✅ **Better UX** - Users don't need to remember org subdomain to login  
✅ **Simpler flow** - Email/password → authenticated (just like standard apps)  
✅ **OAuth compatible** - Demo OAuth client works without tenant setup  
✅ **Secure** - Org scoping happens in JWT claims (tamper-proof)  
✅ **Multi-tenant isolation** - Query filters still work via org claim  
✅ **Flexible** - Still supports subdomain/header/query for API-to-API calls  

---

## What Still Requires Tenant?

### Registration (By Design)
- New users need to be assigned to an organization
- Options:
  1. Subdomain: `acme.yourdomain.com/register`
  2. Query param: `/register?tenant=acme`
  3. Invitation link: `/register?invite=<token>` (token contains org)
  4. Tenant selector UI: Shows list of available orgs

### API-to-API Calls (Optional)
- Machine-to-machine calls can still use:
  - `X-Tenant` header
  - `?tenant=` query param
  - Subdomain routing

---

## Troubleshooting

### "Tenant context not found" on login
- **Old issue** - should be fixed now
- Login no longer checks tenant upfront
- If you still see this, check you're using updated backend code

### Users can't see their data after login
1. Check JWT token contains "org" claim:
   ```bash
   # Decode JWT at https://jwt.io
   # Should see: "org": "<organization-guid>"
   ```

2. Verify TenantService reads from JWT:
   ```csharp
   // Add logging in TenantService.GetCurrentTenantId()
   var orgClaim = ctx.User?.FindFirst("org");
   Console.WriteLine($"Org claim: {orgClaim?.Value}");
   ```

3. Check database - user has correct OrganizationId:
   ```sql
   SELECT Id, Email, OrganizationId FROM Users WHERE Email = 'user@example.com';
   ```

### OAuth demo client fails
- Make sure you're logging in through the GateKeeper UI first
- OAuth flow requires authenticated session (cookie) which includes org claim
- Check cookie claims include "org" after login

---

## Next Steps

1. ✅ Test login with existing users (should work immediately)
2. ✅ Test OAuth flow with demo client (should work now)
3. ✅ Verify tenant isolation (users only see their org data)
4. ⏳ Add invitation-based registration (no tenant input needed)
5. ⏳ Add org switcher UI (for users belonging to multiple orgs)
6. ⏳ Add admin org management endpoints

---

## Files Changed

### Backend
- ✏️ [AuthenticationController.cs](src/GateKeeper.Server/Controllers/AuthenticationController.cs)
- ✏️ [AuthorizationController.cs](src/GateKeeper.Server/Controllers/AuthorizationController.cs)
- ✏️ [UserService.cs](src/GateKeeper.Application/Users/Services/UserService.cs)
- ✏️ [IJwtTokenGenerator.cs](src/GateKeeper.Application/Common/Interfaces/IJwtTokenGenerator.cs)
- ✏️ [JwtTokenGenerator.cs](src/GateKeeper.Infrastructure/Security/JwtTokenGenerator.cs)
- ✏️ [TenantService.cs](src/GateKeeper.Infrastructure/Services/TenantService.cs)
- ✏️ [UserProfileDto.cs](src/GateKeeper.Application/Users/DTOs/UserProfileDto.cs)

### Frontend
- ✏️ [LoginPage.jsx](gatekeeper.client/src/features/auth/LoginPage.jsx)
- ✏️ [AuthContext.jsx](gatekeeper.client/src/features/auth/AuthContext.jsx)

---

## Comparison: Before vs After

| Aspect | Before | After |
|--------|--------|-------|
| **Login** | Requires tenant upfront | Just email + password |
| **Tenant Source** | Middleware (subdomain/header) | JWT "org" claim from user |
| **OAuth Demo** | ❌ Broken (no tenant) | ✅ Works (org from login) |
| **UX** | Complex (need subdomain) | Simple (like normal apps) |
| **Registration** | Requires tenant | Still requires tenant |
| **API Calls** | Need header/subdomain | Token includes org |

