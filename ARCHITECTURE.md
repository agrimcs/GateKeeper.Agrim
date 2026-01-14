# GateKeeper - OAuth2/OIDC Identity Provider
## Architectural Specification

**Last Updated:** January 14, 2026  
**Target:** One-day hackathon MVP  
**Stack:** .NET 9 + React + SQL Server + OpenIddict

---

## Executive Summary

Self-hosted OAuth2/OIDC identity provider supporting Authorization Code Flow with PKCE. Users can register/login, developers can register client applications, and clients can authenticate users through standard OAuth2 flows.

**Scope:** Core OAuth server + Admin portal for client management in a monolithic React app.

---

## Key Architectural Decisions

| Decision | Choice | Rationale |
|----------|--------|-----------|
| OAuth Library | **OpenIddict** | Battle-tested, handles protocol complexity, focus on business logic |
| Database | **SQL Server** | Already set up (SSMS), relational model perfect for OAuth entities, EF Core support |
| Frontend Architecture | **Monolithic React SPA** | Single deployment, shared components, faster development |
| Architecture Pattern | **DDD + Clean Architecture** | Rich domain models, testable, maintainable, clear separation of concerns |
| Use Case Pattern | **Application Services** | Direct service calls, simple orchestration, no MediatR overhead |
| Password Hashing | **BCrypt.Net-Next** | OWASP-compliant, 50M+ downloads, proven security, open-source (MIT) |
| Token Strategy | **JWT Access + Refresh Tokens** | Standard OAuth2 pattern, stateless access tokens |

---

## Solution Structure

### Clean Architecture Layers

```
GateKeeper.sln
│
├── GateKeeper.Domain/                    # Core Domain (no dependencies)
│   ├── Entities/
│   │   ├── User.cs                       # Aggregate root
│   │   ├── Client.cs                     # Aggregate root
│   │   ├── Authorization.cs
│   │   └── RefreshToken.cs
│   ├── ValueObjects/
│   │   ├── Email.cs
│   │   ├── RedirectUri.cs
│   │   └── ClientSecret.cs
│   ├── Enums/
│   │   ├── ClientType.cs
│   │   └── GrantType.cs
│   ├── Events/
│   │   ├── UserRegisteredEvent.cs
│   │   └── ClientRegisteredEvent.cs
│   ├── Exceptions/
│   │   ├── DomainException.cs
│   │   └── InvalidRedirectUriException.cs
│   └── Interfaces/
│       ├── IUserRepository.cs
│       └── IClientRepository.cs
│
├── GateKeeper.Application/               # Use Cases
│   ├── Common/
│   │   ├── Interfaces/
│   │   │   ├── IApplicationDbContext.cs
│   │   │   ├── ITokenService.cs
│   │   │   └── IPasswordHasher.cs
│   │   ├── Exceptions/
│   │   │   └── ApplicationException.cs
│   │   └── Validation/
│   │       └── ValidationExtensions.cs
│   ├── Users/
│   │   ├── Services/
│   │   │   └── UserService.cs
│   │   ├── DTOs/
│   │   │   ├── RegisterUserDto.cs
│   │   │   ├── LoginUserDto.cs
│   │   │   └── UserProfileDto.cs
│   │   └── Validators/
│   │       ├── RegisterUserDtoValidator.cs
│   │       └── LoginUserDtoValidator.cs
│   └── Clients/
│       ├── Services/
│       │   └── ClientService.cs
│       ├── DTOs/
│       │   ├── RegisterClientDto.cs
│       │   ├── UpdateClientDto.cs
│       │   └── ClientResponseDto.cs
│       └── Validators/
│           ├── RegisterClientDtoValidator.cs
│           └── UpdateClientDtoValidator.cs
│
├── GateKeeper.Infrastructure/            # External Concerns
│   ├── Persistence/
│   │   ├── ApplicationDbContext.cs
│   │   ├── Configurations/
│   │   │   ├── UserConfiguration.cs
│   │   │   └── ClientConfiguration.cs
│   │   ├── Repositories/
│   │   │   ├── UserRepository.cs
│   │   │   └── ClientRepository.cs
│   │   └── Migrations/
│   ├── Security/
│   │   ├── BcryptPasswordHasher.cs
│   │   └── TokenService.cs
│   ├── OpenIddict/
│   │   └── OpenIddictConfiguration.cs
│   └── DependencyInjection.cs
│
├── GateKeeper.Server/                    # Presentation (API)
│   ├── Controllers/
│   │   ├── AuthenticationController.cs
│   │   ├── ClientsController.cs
│   │   └── UsersController.cs
│   ├── Endpoints/
│   │   ├── AuthorizationEndpoint.cs
│   │   └── TokenEndpoint.cs
│   ├── Middleware/
│   │   └── ExceptionHandlingMiddleware.cs
│   ├── Program.cs
│   └── appsettings.json
│
└── gatekeeper.client/                    # React SPA
    ├── src/
    │   ├── features/
    │   │   ├── auth/
    │   │   ├── oauth/
    │   │   └── admin/
    │   ├── components/
    │   ├── services/
    │   └── App.jsx
```

### Dependency Flow

```
Presentation (Server) → Application → Domain
       ↓
Infrastructure → Application → Domain

- Domain: Zero dependencies (pure C#)
- Application: Depends only on Domain
- Infrastructure: Implements Application interfaces
- Presentation: Depends on Application (direct service calls)
```

---

## Technology Stack

### Backend
- **.NET 9 Web API**
- **OpenIddict 5.x** - OAuth2/OIDC server
- **BCrypt.Net-Next** - Password hashing (OWASP-compliant, open-source)
- **Entity Framework Core 9** - ORM
- **SQL Server** - Database
- **FluentValidation** - DTO validation
- **System.IdentityModel.Tokens.Jwt** - JWT handling

### Frontend
- **React 18** + **Vite**
- **React Router 6** - Routing
- **React Hook Form** - Form handling
- **Axios** - HTTP client
- **Tailwind CSS** - Styling (or your preference)
- **Zustand/Context** - State management

---

## Domain Model Design

### Aggregates

#### 1. User (Aggregate Root)
```csharp
public class User : AggregateRoot
{
    public Guid Id { get; private set; }
    public Email Email { get; private set; }
    public string PasswordHash { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public DateTime CreatedAt { get; private set; }
    
    public static User Register(Email email, string password, string firstName, string lastName);
    public bool ValidatePassword(string password);
    public void UpdateProfile(string firstName, string lastName);
}
```

#### 2. Client (Aggregate Root)
```csharp
public class Client : AggregateRoot
{
    public Guid Id { get; private set; }
    public string ClientId { get; private set; }
    public ClientSecret Secret { get; private set; }
    public string DisplayName { get; private set; }
    public ClientType Type { get; private set; }
    private List<RedirectUri> _redirectUris;
    public IReadOnlyCollection<RedirectUri> RedirectUris => _redirectUris.AsReadOnly();
    
    public static Client CreateConfidential(string displayName, string clientId, ...);
    public static Client CreatePublic(string displayName, string clientId, ...);
    public void AddRedirectUri(RedirectUri uri);
    public bool ValidateRedirectUri(string uri);
}
```

### Value Objects

```csharp
public record Email
{
    public string Value { get; init; }
    public static Email Create(string email); // Validates format
}

public record RedirectUri
{
    public string Value { get; init; }
    public static RedirectUri Create(string uri); // Validates HTTPS, format
}

public record ClientSecret
{
    public string HashedValue { get; init; }
    public static ClientSecret Generate();
    public bool Verify(string plainSecret);
}
```

### Domain Events

```csharp
public record UserRegisteredEvent(Guid UserId, string Email);
public record ClientRegisteredEvent(Guid ClientId, string ClientId);
public record UserAuthenticatedEvent(Guid UserId, DateTime AuthenticatedAt);
```

### Database Schema (Persistence Layer)

Domain entities are mapped to database tables in Infrastructure:

1. **Users** - Maps to User aggregate
2. **Clients** - Maps to Client aggregate
3. **OpenIddictApplications** - OpenIddict integration
4. **OpenIddictAuthorizations** - OAuth consent records
5. **OpenIddictTokens** - Token storage

**Note:** We maintain a clean domain model separate from persistence concerns. EF Core configurations map domain entities to database schema.

---

## API Contract (High-Level)

### Authentication Endpoints
```
POST   /api/auth/register          # Create new user account
POST   /api/auth/login             # User login, returns cookie/token
POST   /api/auth/logout            # End session
GET    /api/auth/profile           # Get current user info
```

### OAuth2/OIDC Endpoints (OpenIddict)
```
GET    /connect/authorize          # Authorization endpoint (OAuth flow start)
POST   /connect/token              # Token endpoint (exchange code for tokens)
GET    /connect/userinfo           # UserInfo endpoint (get user claims)
GET    /.well-known/openid-configuration  # Discovery document
```

### Client Management (Admin APIs)
```
GET    /api/clients                # List OAuth clients
POST   /api/clients                # Register new OAuth client
GET    /api/clients/{id}           # Get client details
PUT    /api/clients/{id}           # Update client
DELETE /api/clients/{id}           # Delete client
```

### User Management (Admin APIs)
```
GET    /api/users                  # List users (admin)
GET    /api/users/{id}             # Get user details
```

---

## React Application Structure

### Routes
```
Public Routes:
  /login                  → Login page
  /register               → Registration page
  /oauth/authorize        → Consent screen (user sees during OAuth flow)

Protected Routes (Admin):
  /admin/clients          → Manage OAuth clients
  /admin/clients/new      → Register new client
  /admin/clients/:id      → Edit client
  /admin/users            → View users list

Optional:
  /demo                   → Demo OAuth client integration
```

### Key Components
- **LoginForm** - User authentication
- **RegisterForm** - New user signup
- **ConsentScreen** - OAuth authorization approval UI
- **ClientList** - Display registered OAuth clients
- **ClientForm** - Create/edit OAuth client applications

---

## Security Considerations

### Password Security
- Use BCrypt.Net-Next (bcrypt algorithm, OWASP-approved)
- Adaptive work factor (configurable cost parameter)
- Built-in salt generation (unique per password)
- Enforce strong password policy (minimum length, complexity requirements)
- Rate limit login attempts to prevent brute force attacks

### OAuth Security
- **PKCE Required** - All public clients must use PKCE
- **State Parameter** - CSRF protection in OAuth flow
- **Redirect URI Validation** - Strict whitelist matching
- **Token Security** - Short-lived access tokens (15 min), refresh tokens stored securely

### API Security
- CORS configuration for React frontend only
- HTTPS required in production
- Client secrets hashed in database
- Authorization codes single-use, short-lived (5 min)

### React Security
- Never store refresh tokens in localStorage (use httpOnly cookies or memory)
- Sanitize all user inputs to prevent XSS attacks
- Configure Content Security Policy (CSP) headers

---

## Development Sequence (DDD Approach)

### Phase 1: Domain Layer (1-2 hours)
1. Create GateKeeper.Domain project
2. Define core entities (User, Client as aggregate roots)
3. Create value objects (Email, RedirectUri, ClientSecret)
4. Define repository interfaces
5. Add domain events and exceptions
6. **No dependencies** - pure C# logic

### Phase 2: Application Layer (2-3 hours)
1. Create GateKeeper.Application project
2. Setup FluentValidation for DTO validation
3. Implement UserService (Register, Authenticate, GetProfile, etc.)
4. Implement ClientService (Register, Update, Delete, List)
5. Create DTOs with FluentValidation validators
6. Define application interfaces (ITokenService, IPasswordHasher, IApplicationDbContext)
7. Add exception handling and validation logic

### Phase 3: Infrastructure Layer (3-4 hours)
1. Create GateKeeper.Infrastructure project
2. Add BCrypt.Net-Next NuGet package
3. Setup ApplicationDbContext with EF Core
4. Configure entity mappings (Fluent API)
5. Implement repositories (UserRepository, ClientRepository)
6. Implement BcryptPasswordHasher (IPasswordHasher)
7. Implement TokenService (JWT generation)
8. Setup OpenIddict configuration
9. Create and run database migrations
10. Register services in DependencyInjection.cs

### Phase 4: Presentation Layer (2-3 hours)
1. Restructure GateKeeper.Server as presentation layer
2. Create API controllers calling application services directly
3. Implement AuthenticationController (register, login, profile)
4. Implement ClientsController (CRUD operations)
5. Create OAuth endpoints (AuthorizationEndpoint, TokenEndpoint)
6. Add exception handling middleware
7. Configure dependency injection in Program.cs
8. Add CORS configuration
9. Test endpoints with Postman

### Phase 5: React Frontend (4-5 hours)
1. Setup routing (React Router)
2. Build login/register pages
3. Build OAuth consent screen
4. Build admin client management UI
5. Integrate with backend APIs

### Phase 6: Integration & Testing (2 hours)
1. End-to-end OAuth flow testing
2. Validate domain rules enforcement
3. Test error scenarios and validation
4. Fix bugs and edge cases

### Phase 7: Polish (If Time)
- Unit tests for domain logic
- Discovery document endpoint
- Demo client application
- Basic styling improvements
- README with architecture overview

**Total Estimate:** 14-19 hours (includes debugging time)

---

## OpenIddict Configuration Notes

### Flows to Enable
- Authorization Code Flow with PKCE
- Refresh Token Flow

### Token Lifetimes
- Authorization Code: 5 minutes
- Access Token: 15 minutes
- Refresh Token: 30 days

### Scopes to Support
- `openid` (required for OIDC)
- `profile` (user profile info)
- `email` (email address)
- `offline_access` (refresh tokens)

### Grant Types
- `authorization_code`
- `refresh_token`

---

## Success Criteria

### MVP Must-Haves
✅ User registration and login  
✅ OAuth2 Authorization Code Flow with PKCE  
✅ JWT access + refresh tokens  
✅ Client application registration (admin UI)  
✅ OAuth consent screen  
✅ Working end-to-end OAuth flow  

### Nice-to-Haves
⭐ Demo client showing integration  
⭐ Discovery document endpoint  
⭐ User management admin UI  
⭐ Token introspection endpoint  

### Out of Scope (Post-Hackathon)
❌ Social login (Google, GitHub)  
❌ Multi-factor authentication  
❌ Role-based access control  
❌ Token revocation API  
❌ Session management  
❌ Admin dashboard with analytics  

---

## Deployment Notes

### Development
- Backend: `dotnet run` (https://localhost:7001)
- Frontend: `npm run dev` (http://localhost:5173)
- Database: SQL Server LocalDB or Express

### Production Considerations
- Use Azure SQL or SQL Server
- Configure HTTPS with valid certificates
- Set proper CORS origins
- Use environment variables for secrets
- Consider Azure App Service or Docker containers

---

## Risk Mitigation

| Risk | Impact | Mitigation |
|------|--------|------------|
| OpenIddict complexity | High | Follow official samples, start with minimal config |
| OAuth protocol bugs | High | Use Postman OAuth flow testing, validate PKCE properly |
| Time overrun | Medium | Stick to MVP, cut demo client if needed |
| React-Backend integration | Medium | Mock APIs first, implement incrementally |
| Security vulnerabilities | High | Use established libraries, follow OWASP guidelines |

---

## DDD Patterns & Design Principles

### Core Patterns Used

1. **Aggregate Pattern**
   - User and Client are aggregate roots
   - Aggregates enforce invariants and maintain consistency
   - All changes go through aggregate root methods

2. **Repository Pattern**
   - Abstract data access from domain
   - Repositories work with aggregate roots
   - Defined as interfaces in Domain, implemented in Infrastructure

3. **Value Objects**
   - Immutable, validated objects (Email, RedirectUri)
   - No identity, equality based on value
   - Encapsulate validation logic

4. **Domain Events**
   - Decouple domain logic from side effects
   - UserRegisteredEvent, ClientRegisteredEvent
   - Can trigger notifications, logging, etc.

5. **Application Services Pattern**
   - Orchestrate domain logic and coordinate workflows
   - UserService, ClientService encapsulate use cases
   - Controllers call services directly (thin controllers)
   - Services work with repositories and domain entities

### Clean Architecture Principles

1. **Dependency Rule**
   - Dependencies point inward toward Domain
   - Domain has zero dependencies
   - Infrastructure depends on Application interfaces

2. **Separation of Concerns**
   - Domain: Business logic only
   - Application: Use case orchestration
   - Infrastructure: Technical implementation
   - Presentation: HTTP/API concerns

3. **Testability**
   - Domain logic testable without database
   - Application services testable with mocked repositories
   - Controllers testable with mocked services

4. **Flexibility**
   - Swap EF Core for Dapper without touching domain
   - Change API framework without touching use cases
   - Replace infrastructure with minimal impact

### Architecture Decision Records

**ADR-001: DDD over Feature Slices**
- **Context:** Need maintainable, testable architecture beyond MVP
- **Decision:** Use Domain-Driven Design with Clean Architecture
- **Rationale:** Complex domain (OAuth flows), long-term maintainability, testability
- **Consequences:** More upfront structure, better quality, easier to extend

**ADR-002: Application Services over CQRS**
- **Context:** Need organized use case orchestration
- **Decision:** Use Application Services pattern without MediatR
- **Rationale:** Simpler architecture, no additional dependencies, direct service calls
- **Consequences:** Less abstraction, but more straightforward for hackathon timeline

**ADR-003: Repository Pattern**
- **Context:** Abstract data access from domain
- **Decision:** Use repository pattern with interfaces in Domain layer
- **Rationale:** Testable without database, swap persistence mechanisms
- **Consequences:** More interfaces, but domain stays pure

**ADR-004: Rich Domain Models**
- **Context:** Where to put business logic
- **Decision:** Use rich domain models with behavior, not anemic entities
- **Rationale:** Encapsulate business rules, enforce invariants at domain level
- **Consequences:** Domain entities more complex, but logic is centralized

**ADR-005: BCrypt.Net-Next over ASP.NET Identity**
- **Context:** Password hashing strategy for user authentication
- **Decision:** Use BCrypt.Net-Next library instead of ASP.NET Core Identity
- **Rationale:** 
  - OWASP-approved bcrypt algorithm (ranked among top secure hashing algorithms)
  - 50M+ NuGet downloads (proven, battle-tested)
  - Open-source (MIT license)
  - Clean architecture - no framework coupling in domain
  - Simpler than full Identity system for OAuth-focused app
- **Consequences:** 
  - Manual user management (no Identity tables/managers)
  - Must implement password validation manually
  - More control over security parameters
  - Easier testing and domain purity

---

## Next Steps for Implementation Agent

**Phase-by-Phase Implementation:**

1. **Phase 1: Domain Layer** (1-2 hours)
   - Create pure domain entities (User, Client)
   - Value objects with validation
   - Repository and IPasswordHasher interfaces
   
2. **Phase 2: Application Layer** (2-3 hours)
   - Application services (UserService, ClientService)
   - DTOs with FluentValidation
   - Use case orchestration

3. **Phase 3: Infrastructure Layer** (3-4 hours)
   - EF Core + SQL Server setup
   - BCrypt.Net-Next implementation
   - Repository implementations
   - OpenIddict configuration
   - Database migrations

4. **Phase 4: API Layer** (2-3 hours)
   - Controllers calling services
   - JWT authentication
   - OAuth endpoints
   - Exception middleware

5. **Phase 5: React Frontend** (4-5 hours)
   - Login/register UI
   - OAuth consent screen
   - Admin client management

6. **Phase 6: Testing & Polish** (1-2 hours)
   - End-to-end OAuth flows
   - Security validation
   - Bug fixes

**Connection String:** `Server=(localdb)\\ProjectsV13;Database=GateKeeperDb;Trusted_Connection=True;MultipleActiveResultSets=true`

**Reference:** This document for all architectural decisions. Ask for clarification on any ambiguous requirements before implementing.
