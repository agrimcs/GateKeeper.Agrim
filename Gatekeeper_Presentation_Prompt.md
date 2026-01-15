# Gatekeeper: AI-First OAuth Platform

## Presentation (What to Display)

### 1. Project Overview
- Gatekeeper is a multi-tenant OAuth 2.0 / OpenID Connect authentication platform, inspired by Auth0/Okta.
- Built with ASP.NET Core (backend), React (frontend), and a demo OAuth client (HTML/JS).
- All core logic, flows, and security are implemented in the codebase (see below).

### 2. Architecture & Stack
- **Backend:** ASP.NET Core Web API
  - Controllers: Authentication, Authorization, Clients, Users
  - Domain: User, Organization, Client entities
  - Uses OpenIddict for OAuth2/OIDC protocol
  - Entity Framework Core for data access
- **Frontend:** React SPA (Vite, Tailwind CSS)
  - User registration, login, and client app management
  - AuthContext for session state
- **Demo Client:** HTML/JS app for OAuth login (PKCE, token management)

### 3. Core Features (from code)
- **User Registration & Login**
  - Users register with org name/subdomain ([RegisterPage.jsx](gatekeeper.client/src/features/auth/RegisterPage.jsx))
  - Secure password handling, JWT issuance ([AuthenticationController.cs](src/GateKeeper.Server/Controllers/AuthenticationController.cs))
- **OAuth 2.0 Authorization Code Flow**
  - /connect/authorize endpoint ([AuthorizationController.cs](src/GateKeeper.Server/Controllers/AuthorizationController.cs))
  - PKCE, cookie session, JWT tokens
- **Client App Registration**
  - Organizations register OAuth clients ([ClientsController.cs](src/GateKeeper.Server/Controllers/ClientsController.cs))
  - Each client is bound to an organization ([Client.cs](src/GateKeeper.Domain/Entities/Client.cs))
- **Multi-Tenant Security**
  - Users can only log in to client apps registered to their org ([AuthenticationController.cs](src/GateKeeper.Server/Controllers/AuthenticationController.cs))
- **Demo Client**
  - Demonstrates OAuth login, token handling ([demo-client/config.js](demo-client/config.js))

### 4. Security Highlights
- Passwords hashed (User.cs)
- JWTs signed/validated (OpenIddict)
- Return URL validation (AuthenticationController.cs)
- Org/client binding enforced (AuthenticationController.cs)
- PKCE and CSRF protection (demo-client)

### 5. Libraries Used & Significance
- **ASP.NET Core Identity:** User management, password security
- **OpenIddict:** OAuth2/OIDC protocol, token issuance
- **Entity Framework Core:** Data modeling, persistence
- **React, React Router:** SPA navigation
- **Tailwind CSS:** UI styling
- **Vite:** Fast React build

### 6. Demo Flow
- Register org & user (React UI)
- Register client app (React UI)
- Login via demo client (success for correct org, error for others)
- Direct login (no org check)

---

## Notes (Speaker Notes)

- **Project Goal:**
  - Build a secure, multi-tenant OAuth platform from scratch, using AI tools for speed and quality.

- **Tech Stack Rationale:**
  - ASP.NET Core: Secure, scalable, familiar to team
  - OpenIddict: Handles OAuth2/OIDC protocol, reduces custom code
  - React + Vite: Fast, modern SPA
  - Tailwind: Rapid UI styling

- **Library Significance:**
  - ASP.NET Core Identity: Handles user auth, password security
  - OpenIddict: Implements OAuth2/OIDC flows
  - EF Core: Data modeling and migrations
  - React Router: SPA flows

- **AI Usage:**
  - Copilot generated controller, DTO, and React component boilerplate
  - AI suggested security patterns (org/client binding, JWT best practices)
  - Used AI for code review, test generation, and documentation

- **Security Focus:**
  - All sensitive flows use best practices
  - Org/client binding prevents cross-tenant leaks
  - Return URL validation blocks open redirect

- **Demo Emphasis:**
  - Show org registration, client registration, and login flows
  - Highlight error when cross-org login is attempted from a client app
  - Show direct login (no org check)

- **Lessons Learned:**
  - AI tools accelerated development, especially for boilerplate
  - Human review essential for business logic and security
  - AI-first workflow is viable for rapid prototyping of secure systems

---

**End of Presentation Prompt**
