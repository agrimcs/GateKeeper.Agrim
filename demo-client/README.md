# GateKeeper Demo OAuth Client

A standalone demonstration client that validates OAuth 2.0 integration with GateKeeper using the Authorization Code Flow with PKCE.

## Features

✅ **OAuth 2.0 Authorization Code Flow with PKCE**
- Secure authorization without client secrets
- PKCE (Proof Key for Code Exchange) implementation
- State parameter for CSRF protection

✅ **Complete Token Management**
- Access token retrieval and display
- Refresh token functionality
- Token expiration countdown
- Automatic token storage

✅ **User Information**
- Fetch and display user profile data
- Shows name, email, and user ID
- Real-time UserInfo endpoint testing

✅ **Interactive UI**
- Clean, modern interface
- Real-time console logging
- Error handling and display
- Visual feedback for all operations

## Prerequisites

1. **GateKeeper Server Running**
   - Backend API must be running at `https://localhost:7001`
   - OAuth endpoints must be accessible

2. **HTTP Server**
   - Python 3, Node.js, or PHP for serving static files
   - Required to run on `http://localhost:8080`

3. **Registered OAuth Client**
   - Client must be registered in GateKeeper admin UI
   - See "Setup Instructions" below

## Setup Instructions

### Step 1: Register Client in GateKeeper

1. Start GateKeeper server and open admin UI
2. Navigate to **Clients** section
3. Click **"Create New Client"**
4. Configure the client:
   ```
   Client Name: Demo Client App
   Client ID: demo-client
   Client Type: Public (PKCE required)
   Grant Types: Authorization Code, Refresh Token
   Redirect URIs: http://localhost:8080/callback.html
   Scopes: openid, profile, email, offline_access
   Require PKCE: Yes
   ```
5. Save the client configuration

### Step 2: Update Configuration

Edit [config.js](config.js) with your client details:

```javascript
const CONFIG = {
    CLIENT_ID: 'demo-client',  // Your client ID from GateKeeper
    REDIRECT_URI: 'http://localhost:8080/callback.html',
    AUTHORITY: 'https://localhost:7001',
    SCOPES: 'openid profile email offline_access'
};
```

### Step 3: Start HTTP Server

Run the demo client using npx:

```bash
cd demo-client
npx http-server -p 8080
```

Or use Python if you prefer:
```bash
cd demo-client
python -m http.server 8080
```

### Step 4: Access the Application

1. Open browser to `http://localhost:8080`
2. You should see the GateKeeper Demo Client interface

## Usage Guide

### Complete OAuth Flow

1. **Click "Login with GateKeeper"**
   - Generates PKCE code_verifier and code_challenge
   - Stores code_verifier in sessionStorage
   - Redirects to GateKeeper authorization endpoint

2. **Login (if not authenticated)**
   - Enter your GateKeeper credentials
   - Complete any required MFA

3. **Grant Consent**
   - Review requested scopes (openid, profile, email, offline_access)
   - Click "Allow" to approve

4. **Automatic Token Exchange**
   - Returns to callback.html with authorization code
   - Validates state parameter (CSRF protection)
   - Exchanges code + code_verifier for tokens
   - Fetches user information
   - Redirects to main page

5. **View User Information**
   - See your name, email, and user ID
   - View truncated access token
   - See token expiration countdown

### Testing Token Operations

**Call UserInfo Endpoint:**
- Click "Call UserInfo" button
- Makes authenticated request to `/connect/userinfo`
- Displays response in console log
- Updates user information on screen

**Refresh Access Token:**
- Click "Refresh Token" button
- Uses refresh token to get new access token
- Updates token display with new value
- Resets expiration countdown

**Logout:**
- Click "Logout" button
- Clears all tokens from sessionStorage
- Returns to logged-out state

## Testing Checklist

### ✅ Complete OAuth Flow
- [ ] Click "Login with GateKeeper"
- [ ] Redirected to GateKeeper login page
- [ ] Successfully authenticate
- [ ] See consent screen with requested scopes
- [ ] Approve consent
- [ ] Redirected back to demo app
- [ ] Authorization code exchanged automatically
- [ ] User info displayed (name, email, user ID)
- [ ] Access token shown (truncated)

### ✅ Token Operations
- [ ] Access token works immediately after login
- [ ] Token expiration countdown is accurate
- [ ] "Call UserInfo" returns correct data
- [ ] "Refresh Token" gets new access token
- [ ] New token works for subsequent requests
- [ ] Token expiry updates after refresh

### ✅ Error Scenarios
- [ ] Deny consent → Error message shown
- [ ] Manipulate code in URL → Token exchange fails
- [ ] Wrong redirect URI → Error from GateKeeper
- [ ] Expired authorization code → Appropriate error
- [ ] Invalid PKCE code_verifier → PKCE validation fails

### ✅ Security Features
- [ ] PKCE code_challenge generated correctly (SHA-256)
- [ ] State parameter validated (CSRF protection)
- [ ] Tokens stored only in sessionStorage (not localStorage)
- [ ] Code_verifier cleared after token exchange
- [ ] All parameters properly URL-encoded

## File Structure

```
demo-client/
├── index.html          # Main page with login/user interface
├── callback.html       # OAuth callback handler
├── config.js           # OAuth configuration (edit this!)
└── README.md          # This file
```

## Browser Console Output

Successful flow produces logs like:
```
[System] Demo client initialized
[OAuth] Starting authorization flow...
[OAuth] Code verifier generated: dBjftJevsLQUn...
[OAuth] Code challenge: E9Melhoa2OwvFr...
[OAuth] Redirecting to authorization endpoint...
[OAuth] Authorization code received: SplxlO...
[OAuth] Code verifier retrieved: dBjftJevsLQUn...
[OAuth] Tokens received successfully
[OAuth] Access token: eyJhbGciOiJSUzI1Ni...
[OAuth] Token expires in: 900 seconds
[OAuth] User info retrieved: { sub: "abc123", ... }
```

## Troubleshooting

### CORS Errors

**Problem:** Browser blocks requests due to CORS policy

**Solution:** Add `http://localhost:8080` to GateKeeper CORS settings in [appsettings.Development.json](../src/GateKeeper.Server/appsettings.Development.json):
```json
"AllowedOrigins": [
  "https://localhost:5173",
  "http://localhost:8080"
]
```

### Redirect URI Mismatch

**Problem:** "redirect_uri_mismatch" error

**Solution:** Ensure registered URI exactly matches, including:
- Protocol (http vs https)
- Port number
- Path (callback.html)
- No trailing slash differences

### Invalid Code Verifier

**Problem:** "invalid_grant" or PKCE validation fails

**Solution:**
- Verify SHA-256 hashing is correct
- Check base64url encoding (no +/= characters)
- Ensure code_verifier is exactly 128 characters
- Confirm code_verifier stored before redirect

### Token Expired

**Problem:** 401 Unauthorized when calling UserInfo

**Solution:**
- Click "Refresh Token" to get new access token
- Check token expiration countdown
- Verify refresh token is present

### Client Not Found

**Problem:** "invalid_client" error

**Solution:**
- Verify client is registered in GateKeeper
- Check CLIENT_ID matches exactly
- Ensure client is enabled
- Confirm client type is "Public"

### HTTPS Certificate Errors

**Problem:** "NET::ERR_CERT_AUTHORITY_INVALID"

**Solution:**
- Accept self-signed certificate in browser
- Visit `https://localhost:7001` directly first
- Add certificate exception
- Or configure proper SSL certificate in GateKeeper

## Security Notes

⚠️ **This is a demo application for testing purposes only**

**For Production Use, Consider:**
- Store tokens securely (not in sessionStorage)
- Implement proper token encryption
- Use HTTPS for demo client (not HTTP)
- Add token revocation on logout
- Implement proper error handling
- Add logging and monitoring
- Use secure cookie storage
- Implement Content Security Policy
- Add rate limiting
- Use production-ready HTTP server

## Architecture

### OAuth Flow Sequence

```
┌─────────┐                ┌──────────┐                ┌───────────┐
│ Browser │                │  Demo    │                │GateKeeper │
│         │                │  Client  │                │           │
└────┬────┘                └─────┬────┘                └─────┬─────┘
     │                           │                           │
     │  1. Click Login           │                           │
     ├──────────────────────────>│                           │
     │                           │                           │
     │  2. Generate PKCE         │                           │
     │     code_verifier/challenge│                          │
     │<──────────────────────────┤                           │
     │                           │                           │
     │  3. Redirect to /authorize│                           │
     ├───────────────────────────┴──────────────────────────>│
     │                                                        │
     │  4. Login + Consent                                   │
     │<───────────────────────────────────────────────────────┤
     │                                                        │
     │  5. Redirect to callback with code                    │
     │<───────────────────────────────────────────────────────┤
     │                           │                           │
     │  6. Exchange code for tokens (+ code_verifier)       │
     ├───────────────────────────┴──────────────────────────>│
     │                                                        │
     │  7. Return tokens                                     │
     │<───────────────────────────────────────────────────────┤
     │                           │                           │
     │  8. Get UserInfo with access_token                   │
     ├───────────────────────────┴──────────────────────────>│
     │                                                        │
     │  9. Return user claims                                │
     │<───────────────────────────────────────────────────────┤
     │                           │                           │
```

## PKCE Implementation Details

**Code Verifier:**
- Random string, 43-128 characters
- Characters: [A-Z, a-z, 0-9, -, ., _, ~]
- Stored in sessionStorage before redirect

**Code Challenge:**
- SHA-256 hash of code_verifier
- Base64url encoded (no padding)
- Sent in authorization request

**Verification:**
- Server stores code_challenge
- Client sends code_verifier in token request
- Server hashes code_verifier and compares

## API Endpoints Used

### Authorization Endpoint
```
GET https://localhost:7001/connect/authorize
Parameters:
  - client_id
  - redirect_uri
  - response_type=code
  - scope
  - code_challenge
  - code_challenge_method=S256
  - state
```

### Token Endpoint
```
POST https://localhost:7001/connect/token
Body (application/x-www-form-urlencoded):
  - grant_type=authorization_code
  - client_id
  - code
  - redirect_uri
  - code_verifier

Response:
  - access_token
  - refresh_token
  - expires_in
  - token_type
```

### UserInfo Endpoint
```
GET https://localhost:7001/connect/userinfo
Headers:
  - Authorization: Bearer <access_token>

Response:
  - sub (user ID)
  - name
  - email
  - other claims based on scopes
```

### Token Refresh
```
POST https://localhost:7001/connect/token
Body:
  - grant_type=refresh_token
  - client_id
  - refresh_token

Response:
  - access_token (new)
  - refresh_token (possibly new)
  - expires_in
  - token_type
```

## Success Criteria

✅ Complete OAuth flow works without errors  
✅ Tokens received and validated  
✅ UserInfo endpoint returns correct claims  
✅ Token refresh works properly  
✅ PKCE validation enforced  
✅ Error handling displays meaningful messages  
✅ State validation prevents CSRF attacks  
✅ Console logging provides visibility  

## Additional Resources

- [OAuth 2.0 Authorization Code Flow](https://oauth.net/2/grant-types/authorization-code/)
- [PKCE Specification (RFC 7636)](https://datatracker.ietf.org/doc/html/rfc7636)
- [OpenID Connect Core](https://openid.net/specs/openid-connect-core-1_0.html)
- [OpenIDDict Documentation](https://documentation.openiddict.com/)

## License

This demo client is part of the GateKeeper project and follows the same license.
