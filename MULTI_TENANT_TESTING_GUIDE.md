# Multi-Tenant Testing Guide

## Prerequisites: Seed Your Database

The multi-tenant system requires organizations in your database. Run the seeder:

### Option 1: Automatic Seeding (Recommended for Dev)

The `ApplicationDbContextSeed.SeedAsync()` creates a default organization with:
- **Subdomain**: `default`
- **Name**: "Default Organization"  
- **Test User**: `admin@gatekeeper.local` / `Admin123!@#`

**Trigger seeding** - ensure Program.cs calls the seeder on startup (check if exists).

### Option 2: Manual SQL Insert

If seeder doesn't run automatically, execute this SQL:

```sql
-- Create a test organization
INSERT INTO Organizations (Id, Name, Subdomain, CustomDomain, IsActive, CreatedAt, BillingPlan, SettingsJson)
VALUES (
    NEWID(),
    'FunnyCorp',
    'funnycorp',
    NULL,
    1,
    GETUTCDATE(),
    'free',
    NULL
);

-- Verify it was created
SELECT Id, Name, Subdomain FROM Organizations;
```

**Copy the GUID** from the result - you'll need it for testing.

---

## Testing Workflow

### Step 1: Start the Backend
```bash
cd src/GateKeeper.Server
dotnet run
```

Server runs on: `https://localhost:44330` or `http://localhost:5294`

### Step 2: Start the Frontend
```bash
cd gatekeeper.client
npm run dev
```

Frontend runs on: `https://localhost:63461` (or check console output)

### Step 3: Register/Login with Tenant

#### 3a. Using the UI Input (Localhost)
1. Navigate to `https://localhost:63461/register`
2. You'll see a blue box asking for organization subdomain/ID
3. Enter `funnycorp` (or the subdomain you created)
4. Click "Continue" - page reloads with tenant set
5. Now fill out registration form normally

#### 3b. Using Query Parameter
Navigate directly to:
```
https://localhost:63461/register?tenant=funnycorp
```

The tenant is auto-detected from the URL.

#### 3c. Using Subdomain (Production-Style)
Add to your `hosts` file:
```
127.0.0.1 funnycorp.localhost
```

Then navigate to:
```
https://funnycorp.localhost:63461/register
```

---

## Debugging Tenant Resolution

### Check if Tenant is Being Sent

Open browser DevTools → Network tab → Make a request → Check:

**Request Headers:**
```
X-Tenant: funnycorp
```

**Query Params:**
```
?tenant=funnycorp
```

### Check Backend Logs

Add logging to TenantResolutionMiddleware to see what it receives:

```csharp
// In InvokeAsync method
Console.WriteLine($"[Tenant] Host: {context.Request.Host}");
Console.WriteLine($"[Tenant] X-Tenant Header: {context.Request.Headers["X-Tenant"]}");
Console.WriteLine($"[Tenant] Query Param: {context.Request.Query["tenant"]}");
```

### Check Database

Verify organizations exist:
```sql
SELECT Id, Name, Subdomain, IsActive FROM Organizations;
```

Verify user has OrganizationId:
```sql
SELECT Id, Email, OrganizationId FROM Users;
```

---

## Common Issues

### "Tenant context not found"
- **Cause**: No organization with that subdomain/ID exists in DB
- **Fix**: Run seeder or manual SQL insert above

### "GetCurrentTenantId is always null"
- **Cause**: Middleware not finding organization
- **Fix**: 
  1. Verify organization exists in DB
  2. Check browser sends X-Tenant header or ?tenant= param
  3. Ensure subdomain matches exactly (case-insensitive in DB query)

### Users table foreign key error
- **Cause**: Trying to register without valid OrganizationId
- **Fix**: Ensure tenant is resolved BEFORE hitting register endpoint

### CORS errors with malformed URL
- **Cause**: Old issue with getTenantBaseUrl (should be fixed now)
- **Fix**: Already fixed - tenant is now sent as header/query param, not in base URL

---

## Testing Multi-Tenant Isolation

### Create Multiple Organizations

```sql
-- Organization 1
INSERT INTO Organizations (Id, Name, Subdomain, IsActive, CreatedAt, BillingPlan)
VALUES (NEWID(), 'Acme Corp', 'acme', 1, GETUTCDATE(), 'free');

-- Organization 2  
INSERT INTO Organizations (Id, Name, Subdomain, IsActive, CreatedAt, BillingPlan)
VALUES (NEWID(), 'Wayne Enterprises', 'wayne', 1, GETUTCDATE(), 'premium');
```

### Test Isolation

1. Register user in `acme` tenant
2. Register user in `wayne` tenant  
3. Login as acme user - verify you only see acme's data
4. Login as wayne user - verify you only see wayne's data

### Verify Query Filters Work

Check logs for SQL queries - should see:
```sql
WHERE [OrganizationId] = @__tenantId_0
```

This proves tenant query filters are active.

---

## Next Steps

Once tenant resolution works:
1. Test OAuth flow with tenant-specific clients
2. Test user management (admin can only see org users)
3. Test client management (clients scoped to org)
4. Add organization switcher UI (if users can belong to multiple orgs)
