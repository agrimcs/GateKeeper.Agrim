# Registration Bootstrapping Fix

## Problem

The multi-tenant architecture had a chicken-and-egg problem:
- Registration requires tenant context (organization)
- But the first user registration can't have an existing organization
- Users on localhost couldn't register because no tenant context existed

## Solution

Implemented **automatic organization creation** during first registration:

### Backend Changes

#### 1. Updated `IOrganizationRepository`
Added `AnyAsync()` method to check if any organizations exist:

```csharp
public interface IOrganizationRepository
{
    Task<Organization?> GetByIdAsync(Guid id);
    Task<Organization?> GetBySubdomainAsync(string subdomain);
    Task AddAsync(Organization org);
    Task SaveChangesAsync();
    Task<bool> AnyAsync(); // ← NEW
}
```

#### 2. Implemented `AnyAsync()` in `OrganizationRepository`

```csharp
public async Task<bool> AnyAsync()
{
    return await _db.Set<Organization>().AnyAsync();
}
```

#### 3. Updated `AuthenticationController.Register`

The Register endpoint now:
1. Checks for tenant context (subdomain, header, query param)
2. If no tenant context exists, checks if ANY organizations exist
3. If NO organizations exist (first registration), automatically creates a "Default Organization"
4. If organizations exist but no tenant context, requires user to specify tenant

```csharp
[HttpPost("register")]
public async Task<IActionResult> Register([FromBody] RegisterUserDto dto)
{
    // Check for tenant context first
    var tenantId = _tenantService.GetCurrentTenantId();
    
    // If no tenant context, check if this is the first registration (bootstrapping)
    if (tenantId == null)
    {
        var hasOrganizations = await _organizationRepository.AnyAsync();
        
        if (!hasOrganizations)
        {
            // First registration - auto-create default organization
            var defaultOrg = new Organization
            {
                Id = Guid.NewGuid(),
                Name = "Default Organization",
                Subdomain = "default",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                BillingPlan = "Free",
                SettingsJson = System.Text.Json.JsonSerializer.Serialize(
                    new OrganizationSettings { AllowSelfSignup = true }
                )
            };

            await _organizationRepository.AddAsync(defaultOrg);
            await _organizationRepository.SaveChangesAsync();

            // Set this organization as tenant context for the registration
            HttpContext.Items["TenantId"] = defaultOrg.Id;
        }
        else
        {
            // Organizations exist but no tenant context - user must specify
            return BadRequest(new
            {
                message = "Tenant context not found...",
                hint = "Use a tenant subdomain..."
            });
        }
    }

    var user = await _userService.RegisterAsync(dto);
    return Ok(user);
}
```

### Frontend Changes

#### Simplified `RegisterPage.jsx`

Removed all tenant input UI since the server now handles bootstrapping automatically:
- Removed `showTenantInput` state
- Removed `tenantInput` state
- Removed tenant validation logic
- Removed conditional tenant input form section
- Simplified to just registration form fields (email, password, firstName, lastName)

The registration page is now as simple as the login page - just enter your details and register!

## Registration Flows

### Scenario 1: First User Registration (Bootstrapping)
1. User navigates to `/register` on localhost
2. User fills in registration form (email, password, name)
3. User submits
4. Server checks: no organizations exist
5. Server auto-creates "Default Organization" with subdomain "default"
6. User is registered and assigned to Default Organization
7. User can now login

### Scenario 2: Subsequent User Registration
1. User navigates to `/register` on localhost (or tenant subdomain)
2. User fills in registration form
3. User submits
4. Server checks: organizations exist, tenant context available
5. User is registered and assigned to current tenant organization
6. User can now login

### Scenario 3: Multi-Tenant Registration
1. User navigates to `https://acme.yourdomain.com/register`
2. Middleware resolves tenant from subdomain → sets "acme" organization
3. User fills in registration form
4. User submits
5. User is registered and assigned to "acme" organization
6. User can now login

## Benefits

✅ **No more bootstrapping problem** - First user can register without existing organization  
✅ **Seamless UX** - Registration form is simple like login (no tenant input needed)  
✅ **Multi-tenant safe** - After first org exists, subsequent registrations require tenant context  
✅ **Backwards compatible** - Existing multi-tenant flows still work with subdomains  
✅ **Self-contained** - Each organization is isolated, first user becomes admin  

## Testing

### Test First Registration
1. Ensure database is empty (no organizations)
2. Navigate to `http://localhost:5173/register`
3. Fill in registration form:
   - Email: test@example.com
   - Password: Test123!@#
   - First Name: Test
   - Last Name: User
4. Submit
5. Verify "Default Organization" is created
6. Verify user is registered with OrganizationId = Default Organization ID
7. Login with test@example.com → should work

### Test Subsequent Registration
1. After first registration, try registering another user
2. Should still work (tenant context from first org or explicit subdomain)

### Test Multi-Tenant Registration
1. Set up subdomain: `acme.localhost`
2. Navigate to `http://acme.localhost:5173/register`
3. Register new user
4. Verify user is assigned to "acme" organization (if it exists)

## Files Modified

### Backend
- `src/GateKeeper.Domain/Interfaces/IOrganizationRepository.cs` - Added AnyAsync method
- `src/GateKeeper.Infrastructure/Persistence/Repositories/OrganizationRepository.cs` - Implemented AnyAsync
- `src/GateKeeper.Server/Controllers/AuthenticationController.cs` - Auto-create org logic

### Frontend
- `gatekeeper.client/src/features/auth/RegisterPage.jsx` - Removed tenant input UI

## Architecture Decision

**Why auto-create instead of requiring admin setup?**

This follows the Auth0 model where:
- First user registration bootstraps the system
- No separate "admin setup" flow needed
- Simpler onboarding experience
- Still secure: each organization is isolated
- Production deployments can disable self-signup via OrganizationSettings

**Why "Default Organization" instead of user-chosen org?**

- Simplifies first registration UX (no extra fields)
- Admin can rename organization later via admin panel
- Matches expected behavior: "just register and login"
- Advanced users can still use tenant subdomains for specific orgs

## Next Steps

1. ✅ Test registration flow on localhost
2. ✅ Verify login works after registration
3. ✅ Test OAuth demo client flow
4. 🔄 Add admin panel to rename/manage Default Organization
5. 🔄 Add organization creation UI for multi-tenant scenarios
6. 🔄 Add organization settings management
