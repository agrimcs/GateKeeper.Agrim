# GateKeeper - Manual Testing Guide

**Date:** January 14, 2026  
**Version:** Post-Phase 4 with JWT Authentication  
**Status:** Backend Complete, Frontend Pending

---

## ✅ Code Review Summary

### Implemented Features

#### 1. JWT Authentication System ✅
- **ITokenService Interface:** Defined in Application layer
- **JwtTokenGenerator:** Implemented in Infrastructure layer
- **JWT Configuration:** Present in appsettings.json
- **JWT Middleware:** Configured in Program.cs with proper validation
- **Login Returns Token:** LoginResponseDto includes JWT token
- **Protected Endpoints:** `[Authorize]` attributes on sensitive endpoints

#### 2. OAuth2/OIDC Server ✅
- **OpenIddict Integration:** Fully configured with proper lifetimes
- **Authorization Endpoint:** Handles authenticated users, returns 401 for unauthenticated
- **Token Endpoint:** Exchanges authorization codes for access tokens
- **UserInfo Endpoint:** Returns user claims
- **Scope Manager:** Integrated for resource validation
- **PKCE Support:** Required for public clients

#### 3. API Controllers ✅
- **AuthenticationController:** Register, login, profile (protected)
- **ClientsController:** Full CRUD with `[Authorize]` protection
- **UsersController:** List and get by ID with `[Authorize]` protection
- **AuthorizationController:** OAuth flow endpoints

#### 4. Security ✅
- **Password Hashing:** BCrypt implementation
- **JWT Token Generation:** Secure with configurable expiration
- **CORS:** Configured for React frontend (localhost:5173)
- **Exception Handling:** Global middleware

### Configuration Issues Found ⚠️

**JWT Configuration Key Mismatch:**
- JwtTokenService.cs expects: `Jwt:SecretKey`
- JwtTokenGenerator.cs expects: `Jwt:Secret`
- appsettings.json has: `Jwt:Secret`

**Resolution:** JwtTokenService.cs is not being used (IJwtTokenGenerator is registered instead). System is working correctly with JwtTokenGenerator.

---

## 🧪 Manual Testing Scenarios

### Prerequisites

1. **Start the Backend Server:**
   ```bash
   cd GateKeeper.Server
   dotnet run
   ```
   Server runs at: `http://localhost:5294` (or check console output)

2. **Testing Tool:** Use Postman, Insomnia, curl, or any REST client

3. **Database:** SQL Server LocalDB should be running (auto-migrates on startup)

---

## Test Suite 1: User Authentication Flow

### Test 1.1: Register New User ✅

**Endpoint:** `POST /api/auth/register`

**Request:**
```json
POST http://localhost:5294/api/auth/register
Content-Type: application/json

{
  "email": "alice@example.com",
  "password": "SecurePass123!",
  "firstName": "Alice",
  "lastName": "Johnson"
}
```

**Expected Response (200 OK):**
```json
{
  "id": "guid-here",
  "email": "alice@example.com",
  "firstName": "Alice",
  "lastName": "Johnson"
}
```

**What to Verify:**
- ✅ Status code is 200 OK
- ✅ Response contains user ID, email, first/last name
- ✅ Password is NOT in response
- ✅ User can be created successfully

**Error Cases to Test:**
```json
// Duplicate email (should return 400 or 409)
POST /api/auth/register
{
  "email": "alice@example.com",  // Same email again
  "password": "Test123!",
  "firstName": "Duplicate",
  "lastName": "User"
}

// Invalid email format (should return 400)
{
  "email": "not-an-email",
  "password": "Test123!",
  "firstName": "Bad",
  "lastName": "Email"
}

// Weak password (should return 400 if validation exists)
{
  "email": "weak@example.com",
  "password": "123",
  "firstName": "Weak",
  "lastName": "Password"
}
```

---

### Test 1.2: Login with Valid Credentials ✅

**Endpoint:** `POST /api/auth/login`

**Request:**
```json
POST http://localhost:5294/api/auth/login
Content-Type: application/json

{
  "email": "alice@example.com",
  "password": "SecurePass123!"
}
```

**Expected Response (200 OK):**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "user": {
    "id": "guid-here",
    "email": "alice@example.com",
    "firstName": "Alice",
    "lastName": "Johnson"
  },
  "tokenType": "Bearer",
  "expiresIn": 3600
}
```

**What to Verify:**
- ✅ Status code is 200 OK
- ✅ Response includes JWT token (long string)
- ✅ Token type is "Bearer"
- ✅ Expiration is 3600 seconds (1 hour)
- ✅ User profile information is included
- **⚠️ CRITICAL:** Copy the token value for next tests!

**Error Cases to Test:**
```json
// Wrong password (should return 401)
{
  "email": "alice@example.com",
  "password": "WrongPassword123!"
}

// Non-existent user (should return 401)
{
  "email": "nonexistent@example.com",
  "password": "SomePassword123!"
}

// Missing fields (should return 400)
{
  "email": "alice@example.com"
  // Missing password
}
```

---

### Test 1.3: Access Protected Endpoint with Token ✅

**Endpoint:** `GET /api/auth/profile/{userId}`

**Request:**
```http
GET http://localhost:5294/api/auth/profile/{userId-from-login}
Authorization: Bearer {token-from-login}
```

**Example with real values:**
```http
GET http://localhost:5294/api/auth/profile/12345678-1234-1234-1234-123456789abc
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Expected Response (200 OK):**
```json
{
  "id": "guid-here",
  "email": "alice@example.com",
  "firstName": "Alice",
  "lastName": "Johnson"
}
```

**What to Verify:**
- ✅ Status code is 200 OK with valid token
- ✅ Profile information returned

**Error Cases:**
```http
// No token (should return 401)
GET /api/auth/profile/{userId}

// Invalid token (should return 401)
GET /api/auth/profile/{userId}
Authorization: Bearer invalid-token-here

// Expired token (test after 1 hour, should return 401)
Authorization: Bearer {expired-token}
```

---

## Test Suite 2: OAuth Client Management

### Test 2.1: List Clients (Protected) ✅

**Endpoint:** `GET /api/clients`

**Request:**
```http
GET http://localhost:5294/api/clients?skip=0&take=50
Authorization: Bearer {token-from-login}
```

**Expected Response (200 OK):**
```json
[
  {
    "id": "guid",
    "clientId": "client-id-string",
    "displayName": "My Test App",
    "type": "Public",
    "redirectUris": [
      "http://localhost:5173/callback"
    ],
    "allowedScopes": ["openid", "profile", "email"]
  }
]
```

**What to Verify:**
- ✅ Returns 401 without token
- ✅ Returns 200 with valid token
- ✅ Returns array (empty or with clients)

---

### Test 2.2: Register OAuth Client ✅

**Endpoint:** `POST /api/clients`

**Request:**
```json
POST http://localhost:5294/api/clients
Authorization: Bearer {token-from-login}
Content-Type: application/json

{
  "displayName": "My React App",
  "type": "Public",
  "redirectUris": [
    "http://localhost:5173/callback",
    "http://localhost:5173/oauth/callback"
  ],
  "allowedScopes": ["openid", "profile", "email", "offline_access"]
}
```

**Expected Response (201 Created):**
```json
{
  "id": "new-guid",
  "clientId": "generated-client-id",
  "displayName": "My React App",
  "type": "Public",
  "redirectUris": [
    "http://localhost:5173/callback",
    "http://localhost:5173/oauth/callback"
  ],
  "allowedScopes": ["openid", "profile", "email", "offline_access"]
}
```

**For Confidential Client:**
```json
{
  "displayName": "My Server App",
  "type": "Confidential",
  "redirectUris": [
    "https://myapp.com/callback"
  ],
  "allowedScopes": ["openid", "profile", "email"]
}
```

**Expected Response (includes secret):**
```json
{
  "id": "new-guid",
  "clientId": "generated-client-id",
  "clientSecret": "generated-secret-value",  // ONLY for Confidential
  "displayName": "My Server App",
  "type": "Confidential",
  ...
}
```

**What to Verify:**
- ✅ Status code is 201 Created
- ✅ Client ID is auto-generated
- ✅ Client secret is returned for Confidential type
- ✅ Client secret is NOT returned for Public type
- ✅ Response includes Location header with resource URL
- **⚠️ Save the clientId for OAuth flow tests!**

**Error Cases:**
```json
// Invalid redirect URI (should return 400)
{
  "displayName": "Bad URIs",
  "type": "Public",
  "redirectUris": [
    "not-a-valid-uri"
  ],
  "allowedScopes": ["openid"]
}

// Missing required fields (should return 400)
{
  "displayName": "Incomplete",
  "type": "Public"
  // Missing redirectUris
}
```

---

### Test 2.3: Get Client by ID ✅

**Endpoint:** `GET /api/clients/{id}`

**Request:**
```http
GET http://localhost:5294/api/clients/{client-id-from-creation}
Authorization: Bearer {token-from-login}
```

**Expected Response (200 OK):**
```json
{
  "id": "guid",
  "clientId": "client-id",
  "displayName": "My React App",
  "type": "Public",
  "redirectUris": [...],
  "allowedScopes": [...]
}
```

**What to Verify:**
- ✅ Returns 401 without token
- ✅ Returns 200 with valid token
- ✅ Returns 404 for non-existent client ID

---

### Test 2.4: Update Client ✅

**Endpoint:** `PUT /api/clients/{id}`

**Request:**
```json
PUT http://localhost:5294/api/clients/{client-id}
Authorization: Bearer {token-from-login}
Content-Type: application/json

{
  "displayName": "Updated App Name",
  "redirectUris": [
    "http://localhost:5173/callback",
    "http://localhost:5173/new-callback"
  ],
  "allowedScopes": ["openid", "profile", "email"]
}
```

**Expected Response (200 OK):**
```json
{
  "id": "same-guid",
  "clientId": "same-client-id",
  "displayName": "Updated App Name",
  "type": "Public",
  "redirectUris": [
    "http://localhost:5173/callback",
    "http://localhost:5173/new-callback"
  ],
  "allowedScopes": ["openid", "profile", "email"]
}
```

**What to Verify:**
- ✅ Status code is 200 OK
- ✅ Changes are persisted
- ✅ Client ID doesn't change
- ✅ Client type doesn't change

---

### Test 2.5: Delete Client ✅

**Endpoint:** `DELETE /api/clients/{id}`

**Request:**
```http
DELETE http://localhost:5294/api/clients/{client-id}
Authorization: Bearer {token-from-login}
```

**Expected Response:**
- Status: 204 No Content (empty body)

**What to Verify:**
- ✅ Status code is 204 No Content
- ✅ Subsequent GET returns 404
- ✅ Client no longer in list

---

## Test Suite 3: OAuth2/OIDC Flow

### Test 3.1: Discovery Document ✅

**Endpoint:** `GET /.well-known/openid-configuration`

**Request:**
```http
GET http://localhost:5294/.well-known/openid-configuration
```

**Expected Response (200 OK):**
```json
{
  "issuer": "http://localhost:5294/",
  "authorization_endpoint": "http://localhost:5294/connect/authorize",
  "token_endpoint": "http://localhost:5294/connect/token",
  "userinfo_endpoint": "http://localhost:5294/connect/userinfo",
  "jwks_uri": "http://localhost:5294/.well-known/jwks",
  "grant_types_supported": [
    "authorization_code",
    "refresh_token"
  ],
  "response_types_supported": ["code"],
  "scopes_supported": [
    "openid",
    "profile",
    "email",
    "offline_access"
  ],
  "token_endpoint_auth_methods_supported": [...],
  "code_challenge_methods_supported": ["S256"]
}
```

**What to Verify:**
- ✅ Returns 200 OK without authentication
- ✅ Contains all OAuth2/OIDC endpoints
- ✅ Supports authorization_code grant type
- ✅ Supports PKCE (S256 code challenge method)

---

### Test 3.2: Authorization Endpoint (Without Auth) ⚠️

**Endpoint:** `GET /connect/authorize`

**Request:**
```http
GET http://localhost:5294/connect/authorize?client_id={your-client-id}&redirect_uri=http://localhost:5173/callback&response_type=code&scope=openid%20profile%20email&code_challenge=test123&code_challenge_method=S256
```

**Expected Response:**
- Status: 401 Unauthorized

**What to Verify:**
- ✅ Returns 401 when user is not authenticated
- ✅ This is correct behavior - user must login first

---

### Test 3.3: Authorization Endpoint (With Auth) ✅

**Endpoint:** `GET /connect/authorize`

**Request:**
```http
GET http://localhost:5294/connect/authorize?client_id={your-client-id}&redirect_uri=http://localhost:5173/callback&response_type=code&scope=openid%20profile%20email&code_challenge=test123&code_challenge_method=S256
Authorization: Bearer {token-from-login}
```

**Expected Response:**
- Status: 302 Redirect
- Location header with authorization code

**What to Verify:**
- ✅ Returns redirect response
- ✅ Location header contains `code=` parameter
- ✅ Location header contains `state=` if state was provided

**Note:** Testing this flow fully requires a client application or Postman OAuth flow tester.

---

### Test 3.4: Token Exchange ✅

This test requires a valid authorization code from Test 3.3.

**Endpoint:** `POST /connect/token`

**Request:**
```http
POST http://localhost:5294/connect/token
Content-Type: application/x-www-form-urlencoded

grant_type=authorization_code
&code={authorization-code-from-3.3}
&redirect_uri=http://localhost:5173/callback
&client_id={your-client-id}
&code_verifier={pkce-verifier}
```

**Expected Response (200 OK):**
```json
{
  "access_token": "eyJhbGci...",
  "token_type": "Bearer",
  "expires_in": 900,
  "refresh_token": "refresh-token-value",
  "scope": "openid profile email"
}
```

**What to Verify:**
- ✅ Returns access token
- ✅ Returns refresh token
- ✅ Token type is "Bearer"
- ✅ Expiration time is 900 seconds (15 minutes)

---

### Test 3.5: UserInfo Endpoint ✅

**Endpoint:** `GET /connect/userinfo`

**Request:**
```http
GET http://localhost:5294/connect/userinfo
Authorization: Bearer {access-token-from-oauth}
```

**Expected Response (200 OK):**
```json
{
  "sub": "user-guid",
  "email": "alice@example.com",
  "given_name": "Alice",
  "family_name": "Johnson",
  "name": "Alice Johnson"
}
```

**What to Verify:**
- ✅ Returns user claims
- ✅ Requires valid OAuth access token (not JWT token)
- ✅ Returns 401 without token

---

## Test Suite 4: Security & Error Handling

### Test 4.1: Token Expiration ⏱️

**Test Procedure:**
1. Login and get token
2. Wait 61 minutes (or modify JWT expiration to 1 minute for testing)
3. Try to access protected endpoint

**Expected:**
- Returns 401 Unauthorized after expiration

---

### Test 4.2: Invalid Token Format ✅

**Request:**
```http
GET http://localhost:5294/api/clients
Authorization: Bearer invalid-token-here
```

**Expected:**
- Status: 401 Unauthorized

---

### Test 4.3: Missing Authorization Header ✅

**Request:**
```http
GET http://localhost:5294/api/clients
```

**Expected:**
- Status: 401 Unauthorized

---

### Test 4.4: SQL Injection Attempt ✅

**Request:**
```json
POST http://localhost:5294/api/auth/login
Content-Type: application/json

{
  "email": "' OR '1'='1",
  "password": "anything"
}
```

**Expected:**
- Status: 401 Unauthorized (not 500 error)
- System should handle safely

---

### Test 4.5: CORS Preflight ✅

**Request:**
```http
OPTIONS http://localhost:5294/api/clients
Origin: http://localhost:5173
Access-Control-Request-Method: GET
Access-Control-Request-Headers: authorization
```

**Expected:**
- Status: 204 No Content
- Headers include: Access-Control-Allow-Origin: http://localhost:5173

---

## Test Suite 5: Database & Data Persistence

### Test 5.1: Database Seeding ✅

**Test Procedure:**
1. Stop the server
2. Delete the database (or use a fresh LocalDB instance)
3. Start the server
4. Check that migrations run automatically
5. Try to login with seeded user (if seeding is implemented)

**What to Verify:**
- ✅ Database is created automatically
- ✅ All tables exist (Users, Clients, OpenIddict tables)
- ✅ Seed data is present (if implemented)

---

### Test 5.2: Data Persistence ✅

**Test Procedure:**
1. Register a user
2. Create an OAuth client
3. Stop the server
4. Start the server
5. Login with the same user
6. List clients

**What to Verify:**
- ✅ User can still login after restart
- ✅ Client still exists after restart
- ✅ Data is persisted to database

---

## 🔧 Quick Test Script (PowerShell)

```powershell
# Set base URL
$baseUrl = "http://localhost:5294"

# 1. Register user
$registerBody = @{
    email = "test@example.com"
    password = "SecurePass123!"
    firstName = "Test"
    lastName = "User"
} | ConvertTo-Json

$register = Invoke-RestMethod -Uri "$baseUrl/api/auth/register" -Method Post -Body $registerBody -ContentType "application/json"
Write-Host "✅ User registered: $($register.id)"

# 2. Login
$loginBody = @{
    email = "test@example.com"
    password = "SecurePass123!"
} | ConvertTo-Json

$login = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method Post -Body $loginBody -ContentType "application/json"
$token = $login.token
Write-Host "✅ Login successful, token received"

# 3. Get profile
$headers = @{
    Authorization = "Bearer $token"
}

$profile = Invoke-RestMethod -Uri "$baseUrl/api/auth/profile/$($login.user.id)" -Method Get -Headers $headers
Write-Host "✅ Profile retrieved: $($profile.email)"

# 4. Create OAuth client
$clientBody = @{
    displayName = "Test Client"
    type = "Public"
    redirectUris = @("http://localhost:5173/callback")
    allowedScopes = @("openid", "profile", "email")
} | ConvertTo-Json

$client = Invoke-RestMethod -Uri "$baseUrl/api/clients" -Method Post -Body $clientBody -ContentType "application/json" -Headers $headers
Write-Host "✅ Client created: $($client.clientId)"

# 5. List clients
$clients = Invoke-RestMethod -Uri "$baseUrl/api/clients" -Method Get -Headers $headers
Write-Host "✅ Total clients: $($clients.Count)"

Write-Host "`n🎉 All tests passed!"
```

---

## 🐛 Common Issues & Troubleshooting

### Issue: "Cannot connect to database"
**Solution:** 
- Ensure SQL Server LocalDB is running
- Check connection string in appsettings.json
- Run: `sqllocaldb start ProjectsV13`

### Issue: "JWT Secret not configured"
**Solution:**
- Verify `Jwt:Secret` exists in appsettings.json
- Ensure it's at least 32 characters long

### Issue: "CORS policy blocked"
**Solution:**
- Verify frontend URL is in CORS policy (localhost:5173)
- Check that `app.UseCors()` is called before `app.UseAuthorization()`

### Issue: "401 on protected endpoints even with token"
**Solution:**
- Verify token is in Authorization header as: `Bearer {token}`
- Check token hasn't expired (1 hour lifetime)
- Ensure JWT configuration matches between generation and validation

### Issue: "OpenIddict tables not created"
**Solution:**
- Run migrations: `dotnet ef database update`
- Check that OpenIddict is configured before migrations

---

## ✅ Testing Checklist

### Authentication
- [ ] User registration works
- [ ] User login returns JWT token
- [ ] Token includes correct claims
- [ ] Profile endpoint requires authentication
- [ ] Invalid credentials return 401
- [ ] Duplicate email registration fails

### OAuth Clients
- [ ] List clients requires authentication
- [ ] Create client works with token
- [ ] Public client doesn't return secret
- [ ] Confidential client returns secret
- [ ] Get client by ID works
- [ ] Update client works
- [ ] Delete client works
- [ ] Invalid redirect URIs are rejected

### OAuth Flow
- [ ] Discovery document is public
- [ ] Authorization requires authentication
- [ ] Token exchange works with valid code
- [ ] UserInfo requires OAuth access token
- [ ] PKCE is enforced

### Security
- [ ] Protected endpoints return 401 without token
- [ ] Invalid tokens are rejected
- [ ] Expired tokens are rejected
- [ ] CORS is properly configured
- [ ] Exception handling doesn't leak sensitive data

---

## 📊 Test Results Template

```
Date: _______________
Tester: _______________

| Test Case | Status | Notes |
|-----------|--------|-------|
| 1.1 Register User | ⬜ | |
| 1.2 Login | ⬜ | |
| 1.3 Protected Endpoint | ⬜ | |
| 2.1 List Clients | ⬜ | |
| 2.2 Register Client | ⬜ | |
| 2.3 Get Client | ⬜ | |
| 2.4 Update Client | ⬜ | |
| 2.5 Delete Client | ⬜ | |
| 3.1 Discovery | ⬜ | |
| 3.2 Auth (no token) | ⬜ | |
| 3.3 Auth (with token) | ⬜ | |
| 3.4 Token Exchange | ⬜ | |
| 3.5 UserInfo | ⬜ | |

Overall Status: _______________
```

---

## 🎯 Next Steps

### For Complete MVP
1. **Implement React Frontend** (Phase 5) - See PHASE5_IMPLEMENTATION.md
2. **End-to-End OAuth Testing** with real client application
3. **Production Deployment** considerations

### Post-MVP Enhancements
- Rate limiting on authentication endpoints
- Token refresh implementation
- Admin dashboard
- Logging and monitoring
- Performance testing
- Security audit

---

**Happy Testing! 🚀**
