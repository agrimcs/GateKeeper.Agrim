# Phase 1: Domain Layer - Implementation Guide

**Estimated Time:** 1-2 hours  
**Goal:** Create pure domain model with business logic and zero dependencies  
**Prerequisites:** .NET 9 SDK installed

---

## Objectives

By the end of Phase 1, you will have:
- ✅ GateKeeper.Domain project created (Class Library)
- ✅ Core domain entities (User, Client aggregates)
- ✅ Value objects (Email, RedirectUri, ClientSecret)
- ✅ Repository interfaces defined
- ✅ Domain events created
- ✅ Domain exceptions defined
- ✅ **Zero external dependencies** - pure C# domain logic

---

## Task 1: Create Domain Project

### Create new Class Library project:

```bash
cd GateKeeper
dotnet new classlib -n GateKeeper.Domain
dotnet sln add GateKeeper.Domain/GateKeeper.Domain.csproj
```

### Update GateKeeper.Domain.csproj:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
```

**Important:** Domain project should have **ZERO NuGet dependencies**. It's pure C# business logic.

---

## Task 2: Create Base Domain Classes

### Create Common folder structure:

```
GateKeeper.Domain/
├── Common/
│   ├── AggregateRoot.cs
│   ├── Entity.cs
│   ├── ValueObject.cs
│   └── IDomainEvent.cs
├── Entities/
├── ValueObjects/
├── Enums/
├── Events/
├── Exceptions/
└── Interfaces/
```

**File:** `GateKeeper.Domain/Common/Entity.cs`

```csharp
namespace GateKeeper.Domain.Common;

public abstract class Entity
{
    public Guid Id { get; protected set; }
    
    protected Entity() { }
    
    protected Entity(Guid id)
    {
        Id = id;
    }
    
    public override bool Equals(object? obj)
    {
        if (obj is not Entity entity)
            return false;
            
        return Id == entity.Id;
    }
    
    public override int GetHashCode() => Id.GetHashCode();
}
```

**File:** `GateKeeper.Domain/Common/AggregateRoot.cs`

```csharp
namespace GateKeeper.Domain.Common;

public abstract class AggregateRoot : Entity
{
    private readonly List<IDomainEvent> _domainEvents = new();
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    
    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }
    
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
```

**File:** `GateKeeper.Domain/Common/IDomainEvent.cs`

```csharp
namespace GateKeeper.Domain.Common;

public interface IDomainEvent
{
    DateTime OccurredOn => DateTime.UtcNow;
}
```

**File:** `GateKeeper.Domain/Common/ValueObject.cs`

```csharp
namespace GateKeeper.Domain.Common;

public abstract record ValueObject;
```

---

## Task 3: Create Domain Enums and Exceptions

**File:** `GateKeeper.Domain/Enums/ClientType.cs`

```csharp
namespace GateKeeper.Domain.Enums;

public enum ClientType
{
    Public = 0,      // JavaScript/Mobile apps (no secret)
    Confidential = 1  // Server-side apps (has secret)
}
```

**File:** `GateKeeper.Domain/Enums/GrantType.cs`

```csharp
namespace GateKeeper.Domain.Enums;

public enum GrantType
{
    AuthorizationCode,
    RefreshToken,
    ClientCredentials
}
```

**File:** `GateKeeper.Domain/Exceptions/DomainException.cs`

```csharp
namespace GateKeeper.Domain.Exceptions;

public class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
    
    public DomainException(string message, Exception innerException) 
        : base(message, innerException) { }
}
```

**File:** `GateKeeper.Domain/Exceptions/InvalidCredentialsException.cs`

```csharp
namespace GateKeeper.Domain.Exceptions;

public class InvalidCredentialsException : DomainException
{
    public InvalidCredentialsException() 
        : base("Invalid email or password.") { }
}
```

**File:** `GateKeeper.Domain/Exceptions/InvalidRedirectUriException.cs`

```csharp
namespace GateKeeper.Domain.Exceptions;

public class InvalidRedirectUriException : DomainException
{
    public InvalidRedirectUriException(string uri) 
        : base($"Invalid redirect URI: {uri}") { }
}
```

**File:** `GateKeeper.Domain/Exceptions/ClientNotFoundException.cs`

```csharp
namespace GateKeeper.Domain.Exceptions;

public class ClientNotFoundException : DomainException
{
    public ClientNotFoundException(string clientId) 
        : base($"Client with ID '{clientId}' was not found.") { }
}
```

---

## Task 4: Create Value Objects

**File:** `GateKeeper.Domain/ValueObjects/Email.cs`

```csharp
using GateKeeper.Domain.Common;
using GateKeeper.Domain.Exceptions;
using System.Text.RegularExpressions;

namespace GateKeeper.Domain.ValueObjects;

public sealed record Email : ValueObject
{
    public string Value { get; init; }
    
    private Email(string value)
    {
        Value = value;
    }
    
    public static Email Create(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Email cannot be empty");
            
        email = email.Trim().ToLowerInvariant();
        
        // Simple email validation
        var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
        if (!emailRegex.IsMatch(email))
            throw new DomainException("Invalid email format");
            
        return new Email(email);
    }
}
```

**File:** `GateKeeper.Domain/ValueObjects/RedirectUri.cs`

```csharp
using GateKeeper.Domain.Common;
using GateKeeper.Domain.Exceptions;

namespace GateKeeper.Domain.ValueObjects;

public sealed record RedirectUri : ValueObject
{
    public string Value { get; init; }
    
    private RedirectUri(string value)
    {
        Value = value;
    }
    
    public static RedirectUri Create(string uri)
    {
        if (string.IsNullOrWhiteSpace(uri))
            throw new DomainException("Redirect URI cannot be empty");
            
        if (!Uri.TryCreate(uri, UriKind.Absolute, out var parsedUri))
            throw new InvalidRedirectUriException(uri);
            
        // For OAuth security, we typically require HTTPS in production
        // For development, we can allow http://localhost
        if (parsedUri.Scheme != "https" && 
            !parsedUri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidRedirectUriException($"{uri} - HTTPS required for non-localhost URIs");
        }
            
        return new RedirectUri(uri);
    }
}
```

**File:** `GateKeeper.Domain/ValueObjects/ClientSecret.cs`

```csharp
using GateKeeper.Domain.Common;
using System.Security.Cryptography;

namespace GateKeeper.Domain.ValueObjects;

public sealed record ClientSecret : ValueObject
{
    public string HashedValue { get; init; }
    
    private ClientSecret(string hashedValue)
    {
        HashedValue = hashedValue;
    }
    
    public static ClientSecret Generate()
    {
        // Generate a cryptographically secure random secret
        var randomBytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        var plainSecret = Convert.ToBase64String(randomBytes);
        
        // For now, store plain (will hash in Infrastructure layer with real hasher)
        return new ClientSecret(plainSecret);
    }
    
    public static ClientSecret FromHashed(string hashedValue)
    {
        return new ClientSecret(hashedValue);
    }
}
```

---

## Task 5: Create Domain Events

**File:** `GateKeeper.Domain/Events/UserRegisteredEvent.cs`

```csharp
using GateKeeper.Domain.Common;

namespace GateKeeper.Domain.Events;

public record UserRegisteredEvent(Guid UserId, string Email) : IDomainEvent;
```

**File:** `GateKeeper.Domain/Events/ClientRegisteredEvent.cs`

```csharp
using GateKeeper.Domain.Common;

namespace GateKeeper.Domain.Events;

public record ClientRegisteredEvent(Guid ClientId, string ClientId) : IDomainEvent;
```

**File:** `GateKeeper.Domain/Events/UserAuthenticatedEvent.cs`

```csharp
using GateKeeper.Domain.Common;

namespace GateKeeper.Domain.Events;

public record UserAuthenticatedEvent(Guid UserId, DateTime AuthenticatedAt) : IDomainEvent;
```
---

## Task 6: Create User Entity (Aggregate Root)

**File:** `GateKeeper.Domain/Entities/User.cs`

```csharp
using GateKeeper.Domain.Common;
using GateKeeper.Domain.Events;
using GateKeeper.Domain.Exceptions;
using GateKeeper.Domain.ValueObjects;

namespace GateKeeper.Domain.Entities;

public class User : AggregateRoot
{
    public Email Email { get; private set; } = null!;
    public string PasswordHash { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    
    // EF Core constructor
    private User() { }
    
    private User(Guid id, Email email, string passwordHash, string firstName, string lastName)
    {
        Id = id;
        Email = email;
        PasswordHash = passwordHash;
        FirstName = firstName;
        LastName = lastName;
        CreatedAt = DateTime.UtcNow;
    }
    
    public static User Register(
        Email email, 
        string passwordHash, 
        string firstName, 
        string lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new DomainException("First name is required");
            
        if (string.IsNullOrWhiteSpace(lastName))
            throw new DomainException("Last name is required");
        
        var user = new User(Guid.NewGuid(), email, passwordHash, firstName, lastName);
        
        user.AddDomainEvent(new UserRegisteredEvent(user.Id, email.Value));
        
        return user;
    }
    
    public void UpdateProfile(string firstName, string lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new DomainException("First name is required");
            
        if (string.IsNullOrWhiteSpace(lastName))
            throw new DomainException("Last name is required");
        
        FirstName = firstName;
        LastName = lastName;
    }
    
    public void RecordLogin()
    {
        LastLoginAt = DateTime.UtcNow;
        AddDomainEvent(new UserAuthenticatedEvent(Id, DateTime.UtcNow));
    }
}
```

---

## Task 7: Create Client Entity (Aggregate Root)

**File:** `GateKeeper.Domain/Entities/Client.cs`

```csharp
using GateKeeper.Domain.Common;
using GateKeeper.Domain.Enums;
using GateKeeper.Domain.Events;
using GateKeeper.Domain.Exceptions;
using GateKeeper.Domain.ValueObjects;

namespace GateKeeper.Domain.Entities;

public class Client : AggregateRoot
{
    public string ClientId { get; private set; } = string.Empty;
    public ClientSecret? Secret { get; private set; }
    public string DisplayName { get; private set; } = string.Empty;
    public ClientType Type { get; private set; }
    public DateTime CreatedAt { get; private set; }
    
    private readonly List<RedirectUri> _redirectUris = new();
    public IReadOnlyCollection<RedirectUri> RedirectUris => _redirectUris.AsReadOnly();
    
    private readonly List<string> _allowedScopes = new();
    public IReadOnlyCollection<string> AllowedScopes => _allowedScopes.AsReadOnly();
    
    // EF Core constructor
    private Client() { }
    
    private Client(
        Guid id,
        string clientId,
        string displayName,
        ClientType type,
        ClientSecret? secret = null)
    {
        Id = id;
        ClientId = clientId;
        DisplayName = displayName;
        Type = type;
        Secret = secret;
        CreatedAt = DateTime.UtcNow;
    }
    
    public static Client CreateConfidential(
        string displayName,
        string clientId,
        ClientSecret secret,
        IEnumerable<RedirectUri> redirectUris,
        IEnumerable<string> scopes)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            throw new DomainException("Client display name is required");
            
        if (string.IsNullOrWhiteSpace(clientId))
            throw new DomainException("Client ID is required");
        
        var client = new Client(Guid.NewGuid(), clientId, displayName, ClientType.Confidential, secret);
        
        foreach (var uri in redirectUris)
            client._redirectUris.Add(uri);
            
        foreach (var scope in scopes)
            client._allowedScopes.Add(scope);
        
        client.AddDomainEvent(new ClientRegisteredEvent(client.Id, client.ClientId));
        
        return client;
    }
    
    public static Client CreatePublic(
        string displayName,
        string clientId,
        IEnumerable<RedirectUri> redirectUris,
        IEnumerable<string> scopes)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            throw new DomainException("Client display name is required");
            
        if (string.IsNullOrWhiteSpace(clientId))
            throw new DomainException("Client ID is required");
        
        var client = new Client(Guid.NewGuid(), clientId, displayName, ClientType.Public);
        
        foreach (var uri in redirectUris)
            client._redirectUris.Add(uri);
            
        foreach (var scope in scopes)
            client._allowedScopes.Add(scope);
        
        client.AddDomainEvent(new ClientRegisteredEvent(client.Id, client.ClientId));
        
        return client;
    }
    
    public void AddRedirectUri(RedirectUri uri)
    {
        if (_redirectUris.Any(u => u.Value.Equals(uri.Value, StringComparison.Ordinal)))
            throw new DomainException($"Redirect URI {uri.Value} already exists for this client");
        
        _redirectUris.Add(uri);
    }
    
    public void RemoveRedirectUri(RedirectUri uri)
    {
        var existing = _redirectUris.FirstOrDefault(u => u.Value == uri.Value);
        if (existing == null)
            throw new DomainException($"Redirect URI {uri.Value} not found");
        
        _redirectUris.Remove(existing);
    }
    
    public bool ValidateRedirectUri(string uri)
    {
        return _redirectUris.Any(u => u.Value.Equals(uri, StringComparison.Ordinal));
    }
    
    public void UpdateDisplayName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            throw new DomainException("Display name cannot be empty");
        
        DisplayName = displayName;
    }
}
```
```

---

## Task 8: Create Repository Interfaces

**File:** `GateKeeper.Domain/Interfaces/IUserRepository.cs`

```csharp
using GateKeeper.Domain.Entities;
using GateKeeper.Domain.ValueObjects;

namespace GateKeeper.Domain.Interfaces;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default);
    Task<List<User>> GetAllAsync(int skip = 0, int take = 50, CancellationToken cancellationToken = default);
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Email email, CancellationToken cancellationToken = default);
}
```

**File:** `GateKeeper.Domain/Interfaces/IClientRepository.cs`

```csharp
using GateKeeper.Domain.Entities;

namespace GateKeeper.Domain.Interfaces;

public interface IClientRepository
{
    Task<Client?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Client?> GetByClientIdAsync(string clientId, CancellationToken cancellationToken = default);
    Task<List<Client>> GetAllAsync(int skip = 0, int take = 50, CancellationToken cancellationToken = default);
    Task AddAsync(Client client, CancellationToken cancellationToken = default);
    Task UpdateAsync(Client client, CancellationToken cancellationToken = default);
    Task DeleteAsync(Client client, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string clientId, CancellationToken cancellationToken = default);
}
```

**File:** `GateKeeper.Domain/Interfaces/IUnitOfWork.cs`

```csharp
namespace GateKeeper.Domain.Interfaces;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

**File:** `GateKeeper.Domain/Interfaces/IPasswordHasher.cs`

```csharp
namespace GateKeeper.Domain.Interfaces;

/// <summary>
/// Interface for password hashing operations.
/// Implementation will use BCrypt.Net-Next in the Infrastructure layer.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hashes a plain-text password using the bcrypt algorithm.
    /// </summary>
    /// <param name="password">Plain-text password to hash</param>
    /// <returns>Bcrypt hashed password with embedded salt</returns>
    string HashPassword(string password);
    
    /// <summary>
    /// Verifies a plain-text password against a bcrypt hash.
    /// </summary>
    /// <param name="hashedPassword">The bcrypt hash to verify against</param>
    /// <param name="providedPassword">The plain-text password to verify</param>
    /// <returns>True if password matches, false otherwise</returns>
    bool VerifyPassword(string hashedPassword, string providedPassword);
}
```

---

## Verification

### Check Your Domain Project Structure:

```
GateKeeper.Domain/
├── Common/
│   ├── AggregateRoot.cs
│   ├── Entity.cs
│   ├── ValueObject.cs
│   └── IDomainEvent.cs
├── Entities/
│   ├── User.cs
│   └── Client.cs
├── ValueObjects/
│   ├── Email.cs
│   ├── RedirectUri.cs
│   └── ClientSecret.cs
├── Enums/
│   ├── ClientType.cs
│   └── GrantType.cs
├── Events/
│   ├── UserRegisteredEvent.cs
│   ├── ClientRegisteredEvent.cs
│   └── UserAuthenticatedEvent.cs
├── Exceptions/
│   ├── DomainException.cs
│   ├── InvalidCredentialsException.cs
│   ├── InvalidRedirectUriException.cs
│   └── ClientNotFoundException.cs
└── Interfaces/
    ├── IUserRepository.cs
    ├── IClientRepository.cs
    ├── IUnitOfWork.cs
    └── IPasswordHasher.cs
```

### Build and Verify:

```bash
cd GateKeeper.Domain
dotnet build
```

You should see:
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

---

## Phase 1 Complete! ✅

**What you've built:**
- ✅ Pure domain model with **zero dependencies**
- ✅ Rich entities with business logic (User, Client)
- ✅ Validated value objects (Email, RedirectUri, ClientSecret)
- ✅ Domain events for loosely coupled communication
- ✅ Repository interfaces defining data access contracts
- ✅ IPasswordHasher interface (will use BCrypt in Infrastructure)
- ✅ Custom domain exceptions for business rule violations

**Domain-Driven Design Principles Applied:**
- **Aggregate Roots:** User and Client control consistency boundaries
- **Value Objects:** Immutable, self-validating objects
- **Domain Events:** Capture significant business occurrences
- **Ubiquitous Language:** Domain concepts match business terminology
- **Encapsulation:** Business rules enforced within entities

---

## Next Phase

**Phase 2: Application Layer** will build on this domain foundation by:
- Creating application services (UserService, ClientService)
- Implementing use cases (RegisterUser, AuthenticateUser, RegisterClient, etc.)
- Defining DTOs for API contracts
- Adding FluentValidation for DTO validation
- Orchestrating domain logic through application services
- Implementing IPasswordHasher will happen in Phase 3 (Infrastructure) using BCrypt.Net-Next

The domain layer you've built is **framework-agnostic** and can be tested independently. All framework-specific code (EF Core, ASP.NET, OpenIddict, BCrypt) will be in Infrastructure and Presentation layers.
