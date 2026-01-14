# Architecture Decision Records (ADRs)

**Project:** GateKeeper - OAuth2/OIDC Identity Provider  
**Last Updated:** January 14, 2026

This document records significant architectural decisions made during the development of GateKeeper. Each ADR captures the context, decision, rationale, and consequences of key architectural choices.

---

## Format

Each ADR follows this structure:
- **Status:** Accepted | Proposed | Deprecated | Superseded
- **Context:** What is the issue we're facing?
- **Decision:** What decision did we make?
- **Rationale:** Why did we make this decision?
- **Consequences:** What are the implications (positive and negative)?
- **Related ADRs:** Links to related decisions

---

## Table of Contents

### Phase 1: Domain Layer
- [ADR-001: Domain-Driven Design with Clean Architecture](#adr-001-domain-driven-design-with-clean-architecture)
- [ADR-002: BCrypt for Password Hashing](#adr-002-bcrypt-for-password-hashing)
- [ADR-003: Value Objects for Domain Primitives](#adr-003-value-objects-for-domain-primitives)

### Phase 2: Application Layer
- [ADR-004: Direct Exception Throwing vs Result Pattern](#adr-004-direct-exception-throwing-vs-result-pattern)
- [ADR-005: Service Methods Return DTOs, Not Domain Entities](#adr-005-service-methods-return-dtos-not-domain-entities)
- [ADR-006: FluentValidation for Input Validation](#adr-006-fluentvalidation-for-input-validation)
- [ADR-007: Application Layer Depends Only on Domain](#adr-007-application-layer-depends-only-on-domain)

### Phase 3: Infrastructure Layer
- [Pending implementation]

### Phase 4: API Layer
- [Pending implementation]

---

## Phase 1: Domain Layer

### ADR-001: Domain-Driven Design with Clean Architecture

**Status:** ✅ Accepted

**Date:** Phase 1 Implementation

**Context:**
Building an OAuth2/OIDC identity provider requires managing complex domain concepts (users, clients, tokens, authorizations) with strict business rules and security requirements. We need an architecture that:
- Isolates business logic from infrastructure concerns
- Makes the system testable and maintainable
- Supports evolution over time
- Prevents accidental complexity leaks

**Decision:**
Adopt **Domain-Driven Design (DDD)** principles with **Clean Architecture** layers:
1. **Domain Layer** - Pure business logic, zero dependencies
2. **Application Layer** - Use cases and orchestration
3. **Infrastructure Layer** - Database, external services
4. **Presentation Layer** - API controllers, endpoints

**Rationale:**
- OAuth/OIDC has well-defined domain concepts (User, Client, Token)
- DDD aggregates (User, Client) naturally map to OAuth entities
- Clean Architecture ensures domain remains the core without infrastructure pollution
- Testability: Domain logic can be tested without database or external dependencies
- OpenIddict (our OAuth library) works well with DDD patterns

**Consequences:**

*Positive:*
- Domain layer remains pure and testable
- Business rules are centralized in domain entities
- Changes to infrastructure don't affect business logic
- Clear dependency flow prevents architectural erosion

*Negative:*
- More initial setup (multiple projects/layers)
- Learning curve for developers unfamiliar with DDD
- More abstraction layers to navigate

**Related ADRs:** ADR-003, ADR-007

---

### ADR-002: BCrypt for Password Hashing

**Status:** ✅ Accepted

**Date:** Phase 1 Implementation

**Context:**
GateKeeper stores user passwords and OAuth client secrets. We need a secure, industry-standard hashing algorithm that:
- Meets OWASP recommendations
- Provides configurable work factor (computational cost)
- Is battle-tested and widely adopted
- Has quality .NET implementation

**Decision:**
Use **BCrypt.Net-Next** library for all password/secret hashing with work factor of 12.

**Rationale:**
- OWASP-recommended algorithm specifically for password hashing
- Automatic salt generation
- Configurable work factor allows tuning security vs performance
- 50M+ NuGet downloads, actively maintained
- Open-source (MIT license) - no vendor lock-in
- Better than PBKDF2 or plain SHA-256 for password storage

**Alternatives Considered:**
- **Argon2:** More modern but less widely adopted, fewer .NET libraries
- **PBKDF2:** OWASP-approved but requires more manual configuration
- **ASP.NET Core Identity PasswordHasher:** Couples us to Identity framework we don't need

**Consequences:**

*Positive:*
- Security meets industry standards
- Work factor can be increased over time as hardware improves
- Interface abstraction (`IPasswordHasher`) allows swapping if needed

*Negative:*
- BCrypt is slower than plain hashing (this is intentional for security)
- Work factor of 12 takes ~250-350ms per hash (acceptable for login operations)

**Implementation:**
```csharp
public interface IPasswordHasher
{
    string HashPassword(string password);
    bool VerifyPassword(string hashedPassword, string providedPassword);
}
```

**Related ADRs:** ADR-007

---

### ADR-003: Value Objects for Domain Primitives

**Status:** ✅ Accepted

**Date:** Phase 1 Implementation

**Context:**
Domain entities have properties with validation rules:
- Email addresses must be valid format and max 254 characters
- Redirect URIs must be HTTPS (with localhost exception)
- Client secrets must meet complexity requirements

Primitive strings don't enforce these rules, leading to:
- Validation scattered across layers
- Easy to create invalid domain states
- No compile-time safety

**Decision:**
Use **Value Objects** for all domain primitives with validation:
- `Email` - Validates email format
- `RedirectUri` - Validates HTTPS requirement
- `ClientSecret` - Handles secret generation and hashing

**Rationale:**
- **Domain-Driven Design best practice:** Value objects encapsulate validation
- **Compile-time safety:** Can't pass plain string where Email expected
- **Single responsibility:** Validation lives with the concept
- **Immutability:** Value objects are immutable records
- **Reusability:** Validation logic centralized, not duplicated

**Example:**
```csharp
public record Email
{
    public string Value { get; init; }
    
    public static Email Create(string email)
    {
        // Validation logic here
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Email cannot be empty");
        // ... more validation
        return new Email { Value = email };
    }
}
```

**Consequences:**

*Positive:*
- Invalid domain states become impossible
- Validation errors occur at domain boundary (clear error location)
- Type system helps prevent bugs (`Email` vs `string`)
- DRY - validation code not duplicated

*Negative:*
- Slightly more verbose (must call `Email.Create()` instead of using string)
- Entity Framework mapping requires configuration
- Team must learn value object pattern

**Related ADRs:** ADR-001

---

## Phase 2: Application Layer

### ADR-004: Direct Exception Throwing vs Result Pattern

**Status:** ✅ Accepted

**Date:** Phase 2 Implementation

**Context:**
Application services need to handle error cases:
- User not found
- Invalid credentials
- Duplicate email registration
- Validation failures

Two primary patterns:
1. **Exception-based:** Throw exceptions for error cases
2. **Result-based:** Return `Result<T>` with success/failure state

**Decision:**
Use **exceptions for error cases** as primary pattern, with optional `Result<T>` available for scenarios where failure is expected business flow.

**Rationale:**
- OAuth flows have exceptional error cases (invalid credentials is an error, not expected flow)
- ASP.NET Core middleware naturally catches exceptions and converts to HTTP responses
- Stack traces help debugging in development
- Result pattern adds ceremony for common cases
- Exception messages can be logged for security monitoring

**When to Use Each:**
- **Exceptions:** Authentication failures, not found errors, validation errors
- **Result:** Optional lookups, batch operations where some failures are expected

**Consequences:**

*Positive:*
- Natural C# idiom (most developers familiar)
- Works seamlessly with ASP.NET Core error handling
- Clear failure path in code
- Stack traces aid debugging

*Negative:*
- Exceptions have performance cost (acceptable for error cases)
- Must be caught at API boundary
- Can be overused for control flow (mitigated by guidelines)

**Implementation:**
```csharp
// Exception approach (used)
public async Task<UserProfileDto> LoginAsync(LoginUserDto dto)
{
    if (!passwordValid)
        throw new UnauthorizedException("Invalid credentials");
}

// Result approach (available but optional)
public async Task<Result<UserProfileDto>> TryGetUserAsync(Guid id)
{
    var user = await _repository.GetByIdAsync(id);
    return user == null 
        ? Result<UserProfileDto>.Failure("User not found")
        : Result<UserProfileDto>.Success(MapToDto(user));
}
```

**Related ADRs:** ADR-005

---

### ADR-005: Service Methods Return DTOs, Not Domain Entities

**Status:** ✅ Accepted

**Date:** Phase 2 Implementation

**Context:**
Application services orchestrate use cases and must return data to the API layer. Two options:
1. Return domain entities directly
2. Return DTOs (Data Transfer Objects)

Returning domain entities would:
- Expose domain internals to API layer
- Risk serialization of sensitive data
- Violate Clean Architecture boundaries
- Make breaking changes harder

**Decision:**
All public Application service methods **return DTOs**, never domain entities.

**Rationale:**
- **Boundary protection:** API layer doesn't see domain implementation details
- **Security:** Control exactly what data is exposed (e.g., never return password hashes)
- **Versioning:** DTOs can evolve independently of domain
- **Serialization control:** DTOs designed for JSON serialization
- **Clean Architecture:** Prevents coupling between layers

**Examples:**
```csharp
// ✅ Correct - Returns DTO
public async Task<UserProfileDto> RegisterAsync(RegisterUserDto dto)
{
    var user = User.Register(...);
    return MapToProfileDto(user);
}

// ❌ Wrong - Returns domain entity
public async Task<User> RegisterAsync(RegisterUserDto dto)
{
    return User.Register(...);
}
```

**Consequences:**

*Positive:*
- Clear API contracts independent of domain implementation
- Can expose different views of same domain entity
- Prevents accidental exposure of sensitive data
- Makes versioning easier (add DTOv2 without changing domain)

*Negative:*
- Requires mapping code (entity → DTO)
- More classes to maintain
- Mapping can be tedious for large entities

**Mitigation:**
- Use simple mapping methods in services (no AutoMapper needed for MVP)
- DTOs are simple records with no logic

**Related ADRs:** ADR-004, ADR-007

---

### ADR-006: FluentValidation for Input Validation

**Status:** ✅ Accepted

**Date:** Phase 2 Implementation

**Context:**
Application layer receives DTOs from API layer that need validation:
- Email format and length
- Password complexity (OWASP requirements)
- Required fields
- Business constraints (e.g., max 10 redirect URIs)

Need validation before calling domain logic to:
- Fail fast with clear error messages
- Prevent invalid data from reaching domain
- Provide user-friendly feedback

**Decision:**
Use **FluentValidation** library for all DTO validation in Application layer.

**Rationale:**
- Industry standard for .NET validation
- Fluent, readable syntax: `RuleFor(x => x.Email).NotEmpty().EmailAddress()`
- Separates validation from DTOs (Single Responsibility Principle)
- Integrates with ASP.NET Core for automatic validation
- Extensible with custom validators
- Better than Data Annotations for complex rules

**Example:**
```csharp
public class RegisterUserDtoValidator : AbstractValidator<RegisterUserDto>
{
    public RegisterUserDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Invalid email format")
            .MaximumLength(254).WithMessage("Email too long");
            
        RuleFor(x => x.Password)
            .MinimumLength(8)
            .Matches(@"[A-Z]").WithMessage("Must contain uppercase")
            .Matches(@"[0-9]").WithMessage("Must contain digit");
    }
}
```

**Alternatives Considered:**
- **Data Annotations:** Too limited for complex rules, couples validation to DTOs
- **Manual validation:** Verbose, error-prone, inconsistent

**Consequences:**

*Positive:*
- Validators are testable in isolation
- Consistent validation approach across all DTOs
- Clear, user-friendly error messages
- Can be reused in different contexts
- Async validation support for database checks

*Negative:*
- Additional NuGet dependency
- Learning curve for developers unfamiliar with FluentValidation
- Must register validators in DI container

**Related ADRs:** ADR-007

---

### ADR-007: Application Layer Depends Only on Domain

**Status:** ✅ Accepted

**Date:** Phase 2 Implementation

**Context:**
Clean Architecture requires dependency flow: Presentation → Application → Domain

Application layer needs external services (database, password hashing, token generation) but should not depend on Infrastructure layer implementations.

**Decision:**
Application layer:
1. **Depends on:** Domain layer + FluentValidation only
2. **Defines interfaces** for infrastructure services it needs
3. **Never references** Infrastructure or Presentation projects

Infrastructure services needed:
- `IPasswordHasher` - Password hashing
- `IUserRepository`, `IClientRepository` - Data access
- `IUnitOfWork` - Transaction management
- `ICurrentUserService` - Current user context (from API layer)

**Rationale:**
- **Dependency Inversion Principle:** Depend on abstractions, not concretions
- **Testability:** Can mock interfaces in unit tests
- **Flexibility:** Swap implementations without changing Application code
- **Clean Architecture:** Domain and Application should be infrastructure-agnostic

**Dependency Flow:**
```
Presentation (Server) ──depends on──→ Application ──depends on──→ Domain
        ↑                                                              ↑
        │                                                              │
        └──────────── Infrastructure ─────implements─────────────────┘
```

**Consequences:**

*Positive:*
- Application services can be unit tested with mocks
- Can swap database (SQL Server → PostgreSQL) without touching Application
- Domain and Application form the "core" that never changes
- Clear architectural boundaries

*Negative:*
- Must define interfaces for everything
- More indirection (call through interface, not concrete class)
- Team must understand dependency inversion

**Implementation Check:**
```xml
<!-- GateKeeper.Application.csproj -->
<ItemGroup>
  <ProjectReference Include="..\GateKeeper.Domain\GateKeeper.Domain.csproj" />
  <PackageReference Include="FluentValidation" Version="11.9.0" />
  <!-- NO reference to Infrastructure or Server -->
</ItemGroup>
```

**Related ADRs:** ADR-001, ADR-002, ADR-005, ADR-006

---

## Future ADRs (To Be Documented)

### Phase 3: Infrastructure Layer
- ADR-008: Entity Framework Core for Data Access
- ADR-009: OpenIddict Integration Strategy
- ADR-010: SQL Server Database Choice

### Phase 4: API Layer
- ADR-011: Exception Handling Middleware Pattern
- ADR-012: JWT Token Configuration
- ADR-013: CORS Policy for React SPA

### Phase 5: Frontend
- ADR-014: React with Vite for SPA
- ADR-015: State Management Approach

---

## ADR Template

Use this template for new ADRs:

```markdown
### ADR-XXX: [Decision Title]

**Status:** Proposed | Accepted | Deprecated | Superseded

**Date:** [Date]

**Context:**
[What is the issue/problem we're facing?]

**Decision:**
[What decision did we make?]

**Rationale:**
[Why did we make this decision?]

**Alternatives Considered:**
- Option 1: [Why not chosen]
- Option 2: [Why not chosen]

**Consequences:**

*Positive:*
- [Benefit 1]
- [Benefit 2]

*Negative:*
- [Drawback 1]
- [Drawback 2]

**Implementation:**
[Code examples or configuration]

**Related ADRs:** [Links to related decisions]
```

---

## Revision History

| Date | Changes | Author |
|------|---------|--------|
| 2026-01-14 | Initial ADRs for Phase 1 & 2 | MrArchitect |

---

**Note:** This document should be updated as new architectural decisions are made. ADRs are immutable once accepted - if a decision changes, create a new ADR that supersedes the old one.
