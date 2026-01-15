# Phase 7: Demo OAuth Client Application

**Goal:** Create standalone client app to validate OAuth 2.0 integration with GateKeeper

**Estimated Time:** 1-2 hours

---

## Overview

Build minimal OAuth client that demonstrates:
- Authorization Code Flow with PKCE
- Token exchange
- Calling protected resources
- Token refresh

**Approach:** Simple HTML/JavaScript single page (no build tools needed)

---

## Implementation Steps

### 1. Register Client in GateKeeper

Use admin UI to create:
```
Client Name: Demo Client App
Client Type: Public (PKCE required)
Redirect URIs: http://localhost:8080/callback
Scopes: openid, profile, email, offline_access
```

Save the **Client ID** for next steps.

---

### 2. Create Demo Client Structure

```
demo-client/
├── index.html          # Main page with login button
├── callback.html       # OAuth callback handler
└── README.md          # Usage instructions
```

---

### 3. index.html - Main Page

**Features:**
- "Login with GateKeeper" button
- Display user info after login
- Show access token (truncated)
- "Refresh Token" button
- "Call UserInfo" button
- "Logout" button

**OAuth Flow:**
```javascript
// 1. Generate PKCE code_verifier and code_challenge
const codeVerifier = generateRandomString(128);
const codeChallenge = await sha256(codeVerifier);

// 2. Store code_verifier in sessionStorage
sessionStorage.setItem('code_verifier', codeVerifier);

// 3. Build authorization URL
const authUrl = `https://localhost:7001/connect/authorize?` +
  `client_id=${CLIENT_ID}` +
  `&redirect_uri=${REDIRECT_URI}` +
  `&response_type=code` +
  `&scope=openid profile email offline_access` +
  `&code_challenge=${codeChallenge}` +
  `&code_challenge_method=S256` +
  `&state=${generateRandomString(32)}`;

// 4. Redirect to GateKeeper
window.location.href = authUrl;
```

---

### 4. callback.html - Handle OAuth Redirect

**Process:**
```javascript
// 1. Extract authorization code from URL
const urlParams = new URLSearchParams(window.location.search);
const code = urlParams.get('code');
const state = urlParams.get('state');

// 2. Retrieve code_verifier
const codeVerifier = sessionStorage.getItem('code_verifier');

// 3. Exchange code for tokens
const tokenResponse = await fetch('https://localhost:7001/connect/token', {
  method: 'POST',
  headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
  body: new URLSearchParams({
    grant_type: 'authorization_code',
    client_id: CLIENT_ID,
    code: code,
    redirect_uri: REDIRECT_URI,
    code_verifier: codeVerifier
  })
});

const tokens = await tokenResponse.json();
// tokens = { access_token, refresh_token, expires_in, token_type }

// 4. Store tokens securely (sessionStorage for demo)
sessionStorage.setItem('access_token', tokens.access_token);
sessionStorage.setItem('refresh_token', tokens.refresh_token);

// 5. Fetch user info
const userInfo = await fetch('https://localhost:7001/connect/userinfo', {
  headers: { 'Authorization': `Bearer ${tokens.access_token}` }
});

// 6. Redirect back to main page
window.location.href = '/';
```

---

### 5. Key Functions to Implement

**PKCE Helpers:**
```javascript
// Generate random string for code_verifier/state
function generateRandomString(length) {
  const chars = 'ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-._~';
  let result = '';
  const randomValues = crypto.getRandomValues(new Uint8Array(length));
  randomValues.forEach(v => result += chars[v % chars.length]);
  return result;
}

// SHA-256 hash for code_challenge
async function sha256(plain) {
  const encoder = new TextEncoder();
  const data = encoder.encode(plain);
  const hash = await crypto.subtle.digest('SHA-256', data);
  return base64UrlEncode(hash);
}

// Base64 URL encoding
function base64UrlEncode(arrayBuffer) {
  const base64 = btoa(String.fromCharCode(...new Uint8Array(arrayBuffer)));
  return base64.replace(/\+/g, '-').replace(/\//g, '_').replace(/=/g, '');
}
```

**Token Operations:**
```javascript
// Refresh access token
async function refreshAccessToken() {
  const refreshToken = sessionStorage.getItem('refresh_token');
  const response = await fetch('https://localhost:7001/connect/token', {
    method: 'POST',
    headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
    body: new URLSearchParams({
      grant_type: 'refresh_token',
      client_id: CLIENT_ID,
      refresh_token: refreshToken
    })
  });
  const tokens = await response.json();
  sessionStorage.setItem('access_token', tokens.access_token);
  return tokens.access_token;
}

// Call UserInfo endpoint
async function getUserInfo() {
  const accessToken = sessionStorage.getItem('access_token');
  const response = await fetch('https://localhost:7001/connect/userinfo', {
    headers: { 'Authorization': `Bearer ${accessToken}` }
  });
  return await response.json();
}
```

---

### 6. Serve Demo Client

**Simple HTTP Server:**
```bash
# Python 3
python -m http.server 8080

# Node.js (npx)
npx http-server -p 8080

# PHP
php -S localhost:8080
```

Access at: `http://localhost:8080`

---

## Testing Checklist

### Complete OAuth Flow
- [ ] Click "Login with GateKeeper"
- [ ] Redirected to GateKeeper login (if not logged in)
- [ ] See consent screen with requested scopes
- [ ] Approve consent
- [ ] Redirected back to demo app with code
- [ ] Code exchanged for tokens automatically
- [ ] User info displayed (name, email, sub)

### Token Operations
- [ ] Access token works immediately
- [ ] Token displayed (first 20 chars)
- [ ] Click "Call UserInfo" - successful
- [ ] Wait 15+ minutes for token expiry
- [ ] Click "Refresh Token" - new token received
- [ ] New token works for UserInfo

### Error Scenarios
- [ ] Deny consent - error returned
- [ ] Manipulate code in URL - token exchange fails
- [ ] Wrong code_verifier - PKCE validation fails
- [ ] Expired code - exchange fails

---

## Demo UI Features

**Logged Out State:**
```
┌─────────────────────────────────┐
│   GateKeeper Demo Client        │
│                                  │
│   Test OAuth 2.0 Integration    │
│                                  │
│   [Login with GateKeeper]       │
└─────────────────────────────────┘
```

**Logged In State:**
```
┌─────────────────────────────────┐
│   Welcome, John Doe!            │
│   Email: john@example.com       │
│   User ID: abc123...            │
│                                  │
│   Access Token: eyJhbGc...      │
│   Expires: 14 minutes           │
│                                  │
│   [Call UserInfo]               │
│   [Refresh Token]               │
│   [Logout]                      │
└─────────────────────────────────┘
```

---

## Configuration

**Constants to define:**
```javascript
const CLIENT_ID = 'demo-client';  // From GateKeeper registration
const REDIRECT_URI = 'http://localhost:8080/callback.html';
const AUTHORITY = 'https://localhost:7001';
const SCOPES = 'openid profile email offline_access';
```

---

## Success Criteria

✅ Complete OAuth flow works without errors  
✅ Tokens received and validated  
✅ UserInfo endpoint returns correct claims  
✅ Token refresh works  
✅ PKCE validation enforced  
✅ Error handling displays meaningful messages  

---

## Optional Enhancements

**If Time Permits:**
- Show token expiration countdown
- Parse and display JWT claims (decode access token)
- Token revocation demo
- Multiple client support (switch client IDs)
- Copy token to clipboard button
- Network request logs panel

---

## Expected Results

**Console Output:**
```
[OAuth] Starting authorization flow...
[OAuth] Code verifier generated: dBjftJ...
[OAuth] Code challenge: E9Melhoa...
[OAuth] Redirecting to authorization endpoint...
[OAuth] Authorization code received: SplxlO...
[OAuth] Exchanging code for tokens...
[OAuth] Tokens received successfully
[OAuth] Access token expires in: 900 seconds
[OAuth] Fetching user info...
[OAuth] User: { sub: "abc123", email: "john@example.com", name: "John Doe" }
```

---

## Troubleshooting

**Issue:** CORS errors
- **Fix:** Add `http://localhost:8080` to GateKeeper CORS policy

**Issue:** Redirect URI mismatch
- **Fix:** Ensure registered URI exactly matches (including trailing slash)

**Issue:** Invalid code_verifier
- **Fix:** Check PKCE implementation, verify base64url encoding

**Issue:** Token expired
- **Fix:** Use refresh token to get new access token

---

## Deliverables

1. Working demo client at `demo-client/`
2. README with setup instructions
3. Screenshot of successful login
4. Verified end-to-end OAuth flow

**Result:** Proof that external applications can successfully integrate with GateKeeper OAuth server.
