# Phase 6: Integration Testing & Validation

**Goal:** Validate end-to-end OAuth flows, domain rules, security, and fix bugs

**Estimated Time:** 1-2 hours

---

## Testing Checklist

### 1. OAuth Authorization Code Flow (Core)

**Test:** Complete OAuth flow from external client
- [ ] Start authorization request with PKCE parameters
- [ ] User redirected to login (if not authenticated)
- [ ] User sees consent screen with requested scopes
- [ ] User approves consent
- [ ] Authorization code returned to redirect_uri
- [ ] Exchange code for access + refresh tokens
- [ ] Access token works for `/connect/userinfo`
- [ ] Refresh token can get new access token

**Validation:**
- PKCE code_challenge validated correctly
- Redirect URI matches registered client
- Authorization code single-use only
- Tokens contain correct claims (sub, email, name)

**Tools:** Postman OAuth 2.0 flow or demo client app

---

### 2. Domain Invariants Enforcement

**User Aggregate:**
- [ ] Cannot register with duplicate email
- [ ] Email format validated (via Value Object)
- [ ] Password validation rules enforced
- [ ] BCrypt hash generated correctly

**Client Aggregate:**
- [ ] Cannot create client with invalid redirect URI
- [ ] Redirect URI must be HTTPS (localhost allowed)
- [ ] Client secret generated and hashed
- [ ] Client type (public/confidential) enforced

**Test:** Try invalid operations, verify exceptions thrown

---

### 3. Security Validations

**Authentication:**
- [ ] Login with wrong password fails
- [ ] Expired JWT rejected
- [ ] Protected endpoints require authentication
- [ ] CORS only allows frontend origin

**OAuth Security:**
- [ ] Invalid client_id rejected
- [ ] Mismatched redirect_uri rejected
- [ ] Invalid PKCE code_verifier rejected
- [ ] Expired authorization code rejected
- [ ] Client secret validation works

**Test:** Send invalid requests, verify 401/400 responses

---

### 4. React Frontend Integration

**Admin UI:**
- [ ] Login redirects to admin after success
- [ ] Register creates account and logs in
- [ ] Client list loads from API
- [ ] Create client saves and displays
- [ ] Edit client updates correctly
- [ ] Delete client removes from list
- [ ] Logout clears session

**OAuth Consent:**
- [ ] Consent screen shows correct client info
- [ ] Scope checkboxes work
- [ ] Approve redirects with code
- [ ] Deny redirects with error

**Test:** Click through all UI flows

---

### 5. Error Handling

**API Layer:**
- [ ] Validation errors return 400 with details
- [ ] Domain exceptions return 400
- [ ] Not found returns 404
- [ ] Unauthorized returns 401
- [ ] Server errors return 500 (logged)

**Frontend:**
- [ ] API errors display to user
- [ ] Network errors handled gracefully
- [ ] Loading states shown
- [ ] Form validation feedback

---

### 6. Data Persistence

**Database:**
- [ ] Users saved with correct schema
- [ ] Clients saved with redirect URIs
- [ ] OpenIddict tables populated
- [ ] Tokens stored correctly
- [ ] Entity relationships maintained

**Test:** Query database after operations

---

## Test Scenarios

### Scenario 1: New User OAuth Flow
1. Register new user
2. Register OAuth client
3. Start OAuth flow from external app
4. Login as new user
5. Approve consent
6. Receive tokens
7. Call userinfo endpoint

**Expected:** Full flow succeeds, correct claims returned

---

### Scenario 2: Existing User Consent
1. Login as existing user
2. Start OAuth flow for new client
3. User already authenticated, goes straight to consent
4. Approve consent
5. Complete token exchange

**Expected:** No re-login required, consent saved

---

### Scenario 3: Invalid Requests
1. Try OAuth with wrong redirect_uri
2. Try token exchange with wrong code_verifier
3. Try userinfo with expired token
4. Try accessing admin without auth

**Expected:** All fail with appropriate errors

---

## Bug Fix Priority

**Critical (Must Fix):**
- OAuth flow breaks
- Cannot login/register
- Security vulnerabilities
- Data not persisting

**High (Should Fix):**
- UI errors/crashes
- Poor error messages
- Missing validations

**Low (Can Defer):**
- Styling issues
- Performance optimizations
- Nice-to-have features

---

## Tools & Commands

**Backend:**
```bash
# Run server
dotnet run --project src/GateKeeper.Server

# Check logs for errors
```

**Frontend:**
```bash
# Run client
npm run dev

# Check browser console
```

**Database:**
```sql
-- Verify data
SELECT * FROM Users;
SELECT * FROM Clients;
SELECT * FROM OpenIddictApplications;
SELECT * FROM OpenIddictAuthorizations;
SELECT * FROM OpenIddictTokens;
```

**Postman:**
- Import GateKeeper.postman_collection.json
- Use OAuth 2.0 Authorization Code with PKCE flow
- Test all endpoints

---

## Success Criteria

✅ Complete OAuth flow works end-to-end  
✅ All domain rules enforced  
✅ Security validations pass  
✅ Admin UI fully functional  
✅ Errors handled gracefully  
✅ No critical bugs  

---

## Next Steps

If all tests pass → **Phase 7: Polish** (optional)
- Add unit tests
- Improve UI styling
- Create demo client
- Documentation updates

If issues found → Fix and re-test systematically
