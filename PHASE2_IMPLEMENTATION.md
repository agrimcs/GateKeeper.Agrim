# Phase 2: Application Layer - Implementation Guide

**Estimated Time:** 2-3 hours  
**Goal:** Create application services, DTOs, and validators for business use cases  
**Prerequisites:** Phase 1 (Domain Layer) completed

---

## Objectives

By the end of Phase 2, you will have:
- ✅ GateKeeper.Application project created (Class Library)
- ✅ User registration and authentication services
- ✅ Client registration and management services
- ✅ DTOs for request/response models
- ✅ FluentValidation validators for input validation
- ✅ Common interfaces and exceptions
- ✅ Comprehensive unit tests
- ✅ **Depends only on Domain layer**

---

## Task 1: Create Application Project

### Create new Class Library project:

```bash
cd GateKeeper
dotnet new classlib -n GateKeeper.Application
dotnet sln add GateKeeper.Application/GateKeeper.Application.csproj
```

### Add project reference to Domain:

```bash
cd GateKeeper.Application
dotnet add reference ../GateKeeper.Domain/GateKeeper.Domain.csproj
```

### Update GateKeeper.Application.csproj:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\GateKeeper.Domain\GateKeeper.Domain.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="FluentValidation" Version="11.9.0" />
  </ItemGroup>
</Project>
```

**Note:** Application layer depends on Domain and FluentValidation only.

---

## Task 2: Create Common Application Infrastructure

### Create folder structure:

```
GateKeeper.Application/
├── Common/
│   ├── Exceptions/
│   │   ├── ApplicationException.cs
│   │   ├── ValidationException.cs
│   │   └── UnauthorizedException.cs
│   ├── Interfaces/
│   │   └── ICurrentUserService.cs
│   └── Models/
│       └── Result.cs
├── Users/
│   ├── DTOs/
│   ├── Services/
│   └── Validators/
└── Clients/
    ├── DTOs/
    ├── Services/
    └── Validators/
```

### Common Exceptions

**File:** `GateKeeper.Application/Common/Exceptions/ApplicationException.cs`

```csharp
namespace GateKeeper.Application.Common.Exceptions;

/// <summary>
/// Base exception for application layer errors.
/// </summary>
public class ApplicationException : Exception
{
    public ApplicationException(string message) : base(message) { }
    
    public ApplicationException(string message, Exception innerException) 
        : base(message, innerException) { }
}
```

**File:** `GateKeeper.Application/Common/Exceptions/ValidationException.cs`

```csharp
using FluentValidation.Results;

namespace GateKeeper.Application.Common.Exceptions;

/// <summary>
/// Exception thrown when validation fails.
/// Contains detailed validation errors from FluentValidation.
/// </summary>
public class ValidationException : ApplicationException
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException() 
        : base("One or more validation failures have occurred.")
    {
        Errors = new Dictionary<string, string[]>();
    }

    public ValidationException(IEnumerable<ValidationFailure> failures)
        : this()
    {
        Errors = failures
            .GroupBy(e => e.PropertyName, e => e.ErrorMessage)
            .ToDictionary(failureGroup => failureGroup.Key, failureGroup => failureGroup.ToArray());
    }
}
```

**File:** `GateKeeper.Application/Common/Exceptions/UnauthorizedException.cs`

```csharp
namespace GateKeeper.Application.Common.Exceptions;

/// <summary>
/// Exception thrown when authentication fails.
/// </summary>
public class UnauthorizedException : ApplicationException
{
    public UnauthorizedException(string message) : base(message) { }
}
```

### Common Result Pattern (Optional but Recommended)

**File:** `GateKeeper.Application/Common/Models/Result.cs`

```csharp
namespace GateKeeper.Application.Common.Models;

/// <summary>
/// Represents the result of an operation with success/failure state.
/// Useful for operations that may fail without exceptions.
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public string? Error { get; }
    
    protected Result(bool isSuccess, string? error)
    {
        IsSuccess = isSuccess;
        Error = error;
    }
    
    public static Result Success() => new(true, null);
    public static Result Failure(string error) => new(false, error);
}

/// <summary>
/// Generic result with return value.
/// </summary>
public class Result<T> : Result
{
    public T? Value { get; }
    
    private Result(bool isSuccess, T? value, string? error) 
        : base(isSuccess, error)
    {
        Value = value;
    }
    
    public static Result<T> Success(T value) => new(true, value, null);
    public new static Result<T> Failure(string error) => new(false, default, error);
}
```

### Common Interface

**File:** `GateKeeper.Application/Common/Interfaces/ICurrentUserService.cs`

```csharp
namespace GateKeeper.Application.Common.Interfaces;

/// <summary>
/// Service to access current authenticated user information.
/// Will be implemented in Presentation layer (Server).
/// </summary>
public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? Email { get; }
    bool IsAuthenticated { get; }
}
```

---

## Task 3: User Application Services

### User DTOs

**File:** `GateKeeper.Application/Users/DTOs/RegisterUserDto.cs`

```csharp
namespace GateKeeper.Application.Users.DTOs;

/// <summary>
/// Request DTO for user registration.
/// </summary>
public record RegisterUserDto
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string ConfirmPassword { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
}
```

**File:** `GateKeeper.Application/Users/DTOs/LoginUserDto.cs`

```csharp
namespace GateKeeper.Application.Users.DTOs;

/// <summary>
/// Request DTO for user login.
/// </summary>
public record LoginUserDto
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}
```

**File:** `GateKeeper.Application/Users/DTOs/UserProfileDto.cs`

```csharp
namespace GateKeeper.Application.Users.DTOs;

/// <summary>
/// Response DTO for user profile information.
/// </summary>
public record UserProfileDto
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? LastLoginAt { get; init; }
}
```

**File:** `GateKeeper.Application/Users/DTOs/UpdateUserProfileDto.cs`

```csharp
namespace GateKeeper.Application.Users.DTOs;

/// <summary>
/// Request DTO for updating user profile.
/// </summary>
public record UpdateUserProfileDto
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
}
```

### User Service

**File:** `GateKeeper.Application/Users/Services/UserService.cs`

```csharp
using GateKeeper.Application.Common.Exceptions;
using GateKeeper.Application.Users.DTOs;
using GateKeeper.Domain.Entities;
using GateKeeper.Domain.Exceptions;
using GateKeeper.Domain.Interfaces;
using GateKeeper.Domain.ValueObjects;

namespace GateKeeper.Application.Users.Services;

/// <summary>
/// Application service for user-related operations.
/// Orchestrates domain logic, repositories, and external services.
/// </summary>
public class UserService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;

    public UserService(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Registers a new user in the system.
    /// </summary>
    public async Task<UserProfileDto> RegisterAsync(
        RegisterUserDto dto, 
        CancellationToken cancellationToken = default)
    {
        // Create email value object (validates format)
        var email = Email.Create(dto.Email);

        // Check if user already exists
        if (await _userRepository.ExistsAsync(email, cancellationToken))
        {
            throw new DuplicateEmailException(dto.Email);
        }

        // Hash password using infrastructure service
        var passwordHash = _passwordHasher.HashPassword(dto.Password);

        // Create user aggregate using domain factory method
        var user = User.Register(email, passwordHash, dto.FirstName, dto.LastName);

        // Persist to database
        await _userRepository.AddAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Map to response DTO
        return MapToProfileDto(user);
    }

    /// <summary>
    /// Authenticates a user with email and password.
    /// </summary>
    public async Task<UserProfileDto> LoginAsync(
        LoginUserDto dto, 
        CancellationToken cancellationToken = default)
    {
        var email = Email.Create(dto.Email);

        // Get user from repository
        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);
        if (user == null)
        {
            throw new UnauthorizedException("Invalid email or password");
        }

        // Verify password
        if (!_passwordHasher.VerifyPassword(user.PasswordHash, dto.Password))
        {
            throw new UnauthorizedException("Invalid email or password");
        }

        // Record login in domain (raises domain event)
        user.RecordLogin();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToProfileDto(user);
    }

    /// <summary>
    /// Gets user profile by ID.
    /// </summary>
    public async Task<UserProfileDto> GetProfileAsync(
        Guid userId, 
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            throw new UserNotFoundException(userId);
        }

        return MapToProfileDto(user);
    }

    /// <summary>
    /// Updates user profile information.
    /// </summary>
    public async Task<UserProfileDto> UpdateProfileAsync(
        Guid userId,
        UpdateUserProfileDto dto,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            throw new UserNotFoundException(userId);
        }

        // Use domain method to update (validates business rules)
        user.UpdateProfile(dto.FirstName, dto.LastName);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToProfileDto(user);
    }

    /// <summary>
    /// Gets all users with pagination.
    /// </summary>
    public async Task<List<UserProfileDto>> GetAllUsersAsync(
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        var users = await _userRepository.GetAllAsync(skip, take, cancellationToken);
        return users.Select(MapToProfileDto).ToList();
    }

    private static UserProfileDto MapToProfileDto(User user)
    {
        return new UserProfileDto
        {
            Id = user.Id,
            Email = user.Email.Value,
            FirstName = user.FirstName,
            LastName = user.LastName,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
        };
    }
}
```

### User Validators

**File:** `GateKeeper.Application/Users/Validators/RegisterUserDtoValidator.cs`

```csharp
using FluentValidation;
using GateKeeper.Application.Users.DTOs;

namespace GateKeeper.Application.Users.Validators;

/// <summary>
/// Validator for user registration requests.
/// Applies input validation rules before domain logic.
/// </summary>
public class RegisterUserDtoValidator : AbstractValidator<RegisterUserDto>
{
    public RegisterUserDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required")
            .EmailAddress()
            .WithMessage("Invalid email format")
            .MaximumLength(254)
            .WithMessage("Email must not exceed 254 characters");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password is required")
            .MinimumLength(8)
            .WithMessage("Password must be at least 8 characters")
            .MaximumLength(100)
            .WithMessage("Password must not exceed 100 characters")
            .Matches(@"[A-Z]")
            .WithMessage("Password must contain at least one uppercase letter")
            .Matches(@"[a-z]")
            .WithMessage("Password must contain at least one lowercase letter")
            .Matches(@"[0-9]")
            .WithMessage("Password must contain at least one number")
            .Matches(@"[\W_]")
            .WithMessage("Password must contain at least one special character");

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty()
            .WithMessage("Password confirmation is required")
            .Equal(x => x.Password)
            .WithMessage("Passwords do not match");

        RuleFor(x => x.FirstName)
            .NotEmpty()
            .WithMessage("First name is required")
            .MaximumLength(100)
            .WithMessage("First name must not exceed 100 characters");

        RuleFor(x => x.LastName)
            .NotEmpty()
            .WithMessage("Last name is required")
            .MaximumLength(100)
            .WithMessage("Last name must not exceed 100 characters");
    }
}
```

**File:** `GateKeeper.Application/Users/Validators/LoginUserDtoValidator.cs`

```csharp
using FluentValidation;
using GateKeeper.Application.Users.DTOs;

namespace GateKeeper.Application.Users.Validators;

/// <summary>
/// Validator for user login requests.
/// </summary>
public class LoginUserDtoValidator : AbstractValidator<LoginUserDto>
{
    public LoginUserDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required")
            .EmailAddress()
            .WithMessage("Invalid email format");

        RuleFor(x => x.Password)
            .NotEmpty()
            .WithMessage("Password is required");
    }
}
```

**File:** `GateKeeper.Application/Users/Validators/UpdateUserProfileDtoValidator.cs`

```csharp
using FluentValidation;
using GateKeeper.Application.Users.DTOs;

namespace GateKeeper.Application.Users.Validators;

/// <summary>
/// Validator for updating user profile.
/// </summary>
public class UpdateUserProfileDtoValidator : AbstractValidator<UpdateUserProfileDto>
{
    public UpdateUserProfileDtoValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty()
            .WithMessage("First name is required")
            .MaximumLength(100)
            .WithMessage("First name must not exceed 100 characters");

        RuleFor(x => x.LastName)
            .NotEmpty()
            .WithMessage("Last name is required")
            .MaximumLength(100)
            .WithMessage("Last name must not exceed 100 characters");
    }
}
```

---

## Task 4: Client Application Services

### Client DTOs

**File:** `GateKeeper.Application/Clients/DTOs/RegisterClientDto.cs`

```csharp
using GateKeeper.Domain.Enums;

namespace GateKeeper.Application.Clients.DTOs;

/// <summary>
/// Request DTO for registering a new OAuth client.
/// </summary>
public record RegisterClientDto
{
    public string DisplayName { get; init; } = string.Empty;
    public ClientType Type { get; init; }
    public List<string> RedirectUris { get; init; } = new();
    public List<string> AllowedScopes { get; init; } = new();
}
```

**File:** `GateKeeper.Application/Clients/DTOs/UpdateClientDto.cs`

```csharp
namespace GateKeeper.Application.Clients.DTOs;

/// <summary>
/// Request DTO for updating an OAuth client.
/// </summary>
public record UpdateClientDto
{
    public string DisplayName { get; init; } = string.Empty;
    public List<string> RedirectUris { get; init; } = new();
}
```

**File:** `GateKeeper.Application/Clients/DTOs/ClientResponseDto.cs`

```csharp
using GateKeeper.Domain.Enums;

namespace GateKeeper.Application.Clients.DTOs;

/// <summary>
/// Response DTO for OAuth client information.
/// Includes secret only on creation for confidential clients.
/// </summary>
public record ClientResponseDto
{
    public Guid Id { get; init; }
    public string ClientId { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public ClientType Type { get; init; }
    public List<string> RedirectUris { get; init; } = new();
    public List<string> AllowedScopes { get; init; } = new();
    public DateTime CreatedAt { get; init; }
    
    /// <summary>
    /// Plain-text client secret. Only populated on creation for confidential clients.
    /// WARNING: This should be displayed to user only once and never stored in plain text.
    /// </summary>
    public string? PlainTextSecret { get; init; }
}
```

### Client Service

**File:** `GateKeeper.Application/Clients/Services/ClientService.cs`

```csharp
using GateKeeper.Application.Clients.DTOs;
using GateKeeper.Domain.Entities;
using GateKeeper.Domain.Enums;
using GateKeeper.Domain.Exceptions;
using GateKeeper.Domain.Interfaces;
using GateKeeper.Domain.ValueObjects;

namespace GateKeeper.Application.Clients.Services;

/// <summary>
/// Application service for OAuth client management.
/// Handles client registration, configuration, and secret management.
/// </summary>
public class ClientService
{
    private readonly IClientRepository _clientRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;

    public ClientService(
        IClientRepository clientRepository,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork)
    {
        _clientRepository = clientRepository;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Registers a new OAuth client.
    /// For confidential clients, returns plain-text secret that must be saved by user.
    /// </summary>
    public async Task<ClientResponseDto> RegisterClientAsync(
        RegisterClientDto dto,
        CancellationToken cancellationToken = default)
    {
        // Generate unique client ID
        var clientId = GenerateClientId(dto.DisplayName);

        // Check if client ID already exists
        if (await _clientRepository.ExistsAsync(clientId, cancellationToken))
        {
            clientId = $"{clientId}-{Guid.NewGuid().ToString()[..8]}";
        }

        // Parse redirect URIs to value objects (validates HTTPS requirement)
        var redirectUris = dto.RedirectUris
            .Select(uri => RedirectUri.Create(uri))
            .ToList();

        Client client;
        string? plainTextSecret = null;

        if (dto.Type == ClientType.Confidential)
        {
            // Generate secret for confidential clients
            var secret = ClientSecret.Generate();
            plainTextSecret = secret.HashedValue; // Store plain text to return to user
            
            // Hash the secret before storing
            var hashedSecret = ClientSecret.FromHashed(_passwordHasher.HashPassword(plainTextSecret));

            client = Client.CreateConfidential(
                dto.DisplayName,
                clientId,
                hashedSecret,
                redirectUris,
                dto.AllowedScopes);
        }
        else
        {
            // Public clients don't have secrets
            client = Client.CreatePublic(
                dto.DisplayName,
                clientId,
                redirectUris,
                dto.AllowedScopes);
        }

        await _clientRepository.AddAsync(client, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToResponseDto(client, plainTextSecret);
    }

    /// <summary>
    /// Gets a client by ID.
    /// </summary>
    public async Task<ClientResponseDto> GetClientByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var client = await _clientRepository.GetByIdAsync(id, cancellationToken);
        if (client == null)
        {
            throw new ClientNotFoundException(id);
        }

        return MapToResponseDto(client);
    }

    /// <summary>
    /// Gets a client by client ID string.
    /// </summary>
    public async Task<ClientResponseDto> GetClientByClientIdAsync(
        string clientId,
        CancellationToken cancellationToken = default)
    {
        var client = await _clientRepository.GetByClientIdAsync(clientId, cancellationToken);
        if (client == null)
        {
            throw new ClientNotFoundException(clientId);
        }

        return MapToResponseDto(client);
    }

    /// <summary>
    /// Gets all registered clients with pagination.
    /// </summary>
    public async Task<List<ClientResponseDto>> GetAllClientsAsync(
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        var clients = await _clientRepository.GetAllAsync(skip, take, cancellationToken);
        return clients.Select(c => MapToResponseDto(c)).ToList();
    }

    /// <summary>
    /// Updates client configuration.
    /// </summary>
    public async Task<ClientResponseDto> UpdateClientAsync(
        Guid id,
        UpdateClientDto dto,
        CancellationToken cancellationToken = default)
    {
        var client = await _clientRepository.GetByIdAsync(id, cancellationToken);
        if (client == null)
        {
            throw new ClientNotFoundException(id);
        }

        // Update display name
        client.UpdateDisplayName(dto.DisplayName);

        // Update redirect URIs - remove old ones and add new ones
        var currentUris = client.RedirectUris.ToList();
        var newUris = dto.RedirectUris.Select(uri => RedirectUri.Create(uri)).ToList();

        // Remove URIs that are no longer in the list
        foreach (var uri in currentUris)
        {
            if (!newUris.Any(u => u.Value == uri.Value))
            {
                client.RemoveRedirectUri(uri);
            }
        }

        // Add new URIs that aren't already present
        foreach (var uri in newUris)
        {
            if (!currentUris.Any(u => u.Value == uri.Value))
            {
                client.AddRedirectUri(uri);
            }
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return MapToResponseDto(client);
    }

    /// <summary>
    /// Deletes a client.
    /// </summary>
    public async Task DeleteClientAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var client = await _clientRepository.GetByIdAsync(id, cancellationToken);
        if (client == null)
        {
            throw new ClientNotFoundException(id);
        }

        await _clientRepository.DeleteAsync(client, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private static string GenerateClientId(string displayName)
    {
        // Create client ID from display name: "My App" -> "my-app"
        var clientId = displayName
            .ToLowerInvariant()
            .Replace(" ", "-")
            .Replace("_", "-");

        // Remove any non-alphanumeric characters except hyphens
        clientId = new string(clientId.Where(c => char.IsLetterOrDigit(c) || c == '-').ToArray());

        return clientId;
    }

    private static ClientResponseDto MapToResponseDto(Client client, string? plainTextSecret = null)
    {
        return new ClientResponseDto
        {
            Id = client.Id,
            ClientId = client.ClientId,
            DisplayName = client.DisplayName,
            Type = client.Type,
            RedirectUris = client.RedirectUris.Select(u => u.Value).ToList(),
            AllowedScopes = client.AllowedScopes.ToList(),
            CreatedAt = client.CreatedAt,
            PlainTextSecret = plainTextSecret // Only set on creation
        };
    }
}
```

### Client Validators

**File:** `GateKeeper.Application/Clients/Validators/RegisterClientDtoValidator.cs`

```csharp
using FluentValidation;
using GateKeeper.Application.Clients.DTOs;
using GateKeeper.Domain.Enums;

namespace GateKeeper.Application.Clients.Validators;

/// <summary>
/// Validator for client registration requests.
/// </summary>
public class RegisterClientDtoValidator : AbstractValidator<RegisterClientDto>
{
    public RegisterClientDtoValidator()
    {
        RuleFor(x => x.DisplayName)
            .NotEmpty()
            .WithMessage("Display name is required")
            .MaximumLength(200)
            .WithMessage("Display name must not exceed 200 characters");

        RuleFor(x => x.Type)
            .IsInEnum()
            .WithMessage("Invalid client type");

        RuleFor(x => x.RedirectUris)
            .NotEmpty()
            .WithMessage("At least one redirect URI is required")
            .Must(uris => uris.Count <= 10)
            .WithMessage("Maximum 10 redirect URIs allowed");

        RuleForEach(x => x.RedirectUris)
            .NotEmpty()
            .WithMessage("Redirect URI cannot be empty")
            .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _))
            .WithMessage("Redirect URI must be a valid absolute URL");

        RuleFor(x => x.AllowedScopes)
            .NotEmpty()
            .WithMessage("At least one scope is required")
            .Must(scopes => scopes.Count <= 20)
            .WithMessage("Maximum 20 scopes allowed");

        RuleForEach(x => x.AllowedScopes)
            .NotEmpty()
            .WithMessage("Scope cannot be empty")
            .MaximumLength(100)
            .WithMessage("Scope must not exceed 100 characters");
    }
}
```

**File:** `GateKeeper.Application/Clients/Validators/UpdateClientDtoValidator.cs`

```csharp
using FluentValidation;
using GateKeeper.Application.Clients.DTOs;

namespace GateKeeper.Application.Clients.Validators;

/// <summary>
/// Validator for client update requests.
/// </summary>
public class UpdateClientDtoValidator : AbstractValidator<UpdateClientDto>
{
    public UpdateClientDtoValidator()
    {
        RuleFor(x => x.DisplayName)
            .NotEmpty()
            .WithMessage("Display name is required")
            .MaximumLength(200)
            .WithMessage("Display name must not exceed 200 characters");

        RuleFor(x => x.RedirectUris)
            .NotEmpty()
            .WithMessage("At least one redirect URI is required")
            .Must(uris => uris.Count <= 10)
            .WithMessage("Maximum 10 redirect URIs allowed");

        RuleForEach(x => x.RedirectUris)
            .NotEmpty()
            .WithMessage("Redirect URI cannot be empty")
            .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _))
            .WithMessage("Redirect URI must be a valid absolute URL");
    }
}
```

---

## Task 5: Create Unit Tests Project

### Create test project:

```bash
cd GateKeeper
dotnet new xunit -n GateKeeper.Application.Tests
dotnet sln add GateKeeper.Application.Tests/GateKeeper.Application.Tests.csproj
```

### Add project references:

```bash
cd GateKeeper.Application.Tests
dotnet add reference ../GateKeeper.Application/GateKeeper.Application.csproj
dotnet add reference ../GateKeeper.Domain/GateKeeper.Domain.csproj
```

### Update GateKeeper.Application.Tests.csproj:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="coverlet.collector" Version="6.0.0" />
    <PackageReference Include="FluentAssertions" Version="7.0.0" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.0" />
    <PackageReference Include="Moq" Version="4.20.70" />
    <PackageReference Include="xunit" Version="2.9.0" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\GateKeeper.Application\GateKeeper.Application.csproj" />
    <ProjectReference Include="..\GateKeeper.Domain\GateKeeper.Domain.csproj" />
  </ItemGroup>
</Project>
```

### Create test folder structure:

```
GateKeeper.Application.Tests/
├── Users/
│   ├── Services/
│   │   └── UserServiceTests.cs
│   └── Validators/
│       ├── RegisterUserDtoValidatorTests.cs
│       └── LoginUserDtoValidatorTests.cs
└── Clients/
    ├── Services/
    │   └── ClientServiceTests.cs
    └── Validators/
        └── RegisterClientDtoValidatorTests.cs
```

### Sample Test: UserServiceTests

**File:** `GateKeeper.Application.Tests/Users/Services/UserServiceTests.cs`

```csharp
using FluentAssertions;
using GateKeeper.Application.Common.Exceptions;
using GateKeeper.Application.Users.DTOs;
using GateKeeper.Application.Users.Services;
using GateKeeper.Domain.Entities;
using GateKeeper.Domain.Exceptions;
using GateKeeper.Domain.Interfaces;
using GateKeeper.Domain.ValueObjects;
using Moq;

namespace GateKeeper.Application.Tests.Users.Services;

/// <summary>
/// Tests for UserService application logic.
/// Uses Moq for mocking dependencies (repositories, infrastructure services).
/// </summary>
public class UserServiceTests
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _userService = new UserService(
            _userRepositoryMock.Object,
            _passwordHasherMock.Object,
            _unitOfWorkMock.Object);
    }

    [Fact]
    public async Task RegisterAsync_WithValidData_ShouldCreateUser()
    {
        // Arrange
        var dto = new RegisterUserDto
        {
            Email = "newuser@example.com",
            Password = "SecurePass123!",
            ConfirmPassword = "SecurePass123!",
            FirstName = "John",
            LastName = "Doe"
        };

        _userRepositoryMock
            .Setup(x => x.ExistsAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _passwordHasherMock
            .Setup(x => x.HashPassword(dto.Password))
            .Returns("hashed-password");

        _userRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _userService.RegisterAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Email.Should().Be(dto.Email.ToLowerInvariant());
        result.FirstName.Should().Be(dto.FirstName);
        result.LastName.Should().Be(dto.LastName);

        _userRepositoryMock.Verify(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ShouldThrowDuplicateEmailException()
    {
        // Arrange
        var dto = new RegisterUserDto
        {
            Email = "existing@example.com",
            Password = "SecurePass123!",
            ConfirmPassword = "SecurePass123!",
            FirstName = "John",
            LastName = "Doe"
        };

        _userRepositoryMock
            .Setup(x => x.ExistsAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        Func<Task> act = async () => await _userService.RegisterAsync(dto);

        // Assert
        await act.Should().ThrowAsync<DuplicateEmailException>()
            .WithMessage("*existing@example.com*");

        _userRepositoryMock.Verify(x => x.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ShouldReturnUserProfile()
    {
        // Arrange
        var dto = new LoginUserDto
        {
            Email = "user@example.com",
            Password = "CorrectPassword123!"
        };

        var email = Email.Create(dto.Email);
        var user = User.Register(email, "hashed-password", "John", "Doe");

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordHasherMock
            .Setup(x => x.VerifyPassword(user.PasswordHash, dto.Password))
            .Returns(true);

        // Act
        var result = await _userService.LoginAsync(dto);

        // Assert
        result.Should().NotBeNull();
        result.Email.Should().Be(dto.Email.ToLowerInvariant());
        result.Id.Should().Be(user.Id);

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_WithInvalidEmail_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var dto = new LoginUserDto
        {
            Email = "nonexistent@example.com",
            Password = "Password123!"
        };

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        Func<Task> act = async () => await _userService.LoginAsync(dto);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("*Invalid email or password*");
    }

    [Fact]
    public async Task LoginAsync_WithInvalidPassword_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var dto = new LoginUserDto
        {
            Email = "user@example.com",
            Password = "WrongPassword123!"
        };

        var email = Email.Create(dto.Email);
        var user = User.Register(email, "hashed-password", "John", "Doe");

        _userRepositoryMock
            .Setup(x => x.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _passwordHasherMock
            .Setup(x => x.VerifyPassword(user.PasswordHash, dto.Password))
            .Returns(false);

        // Act
        Func<Task> act = async () => await _userService.LoginAsync(dto);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("*Invalid email or password*");

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
```

### Sample Test: Validator Tests

**File:** `GateKeeper.Application.Tests/Users/Validators/RegisterUserDtoValidatorTests.cs`

```csharp
using FluentAssertions;
using FluentValidation.TestHelper;
using GateKeeper.Application.Users.DTOs;
using GateKeeper.Application.Users.Validators;

namespace GateKeeper.Application.Tests.Users.Validators;

/// <summary>
/// Tests for RegisterUserDtoValidator.
/// Uses FluentValidation.TestHelper for concise test syntax.
/// </summary>
public class RegisterUserDtoValidatorTests
{
    private readonly RegisterUserDtoValidator _validator;

    public RegisterUserDtoValidatorTests()
    {
        _validator = new RegisterUserDtoValidator();
    }

    [Fact]
    public void Validate_WithValidData_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var dto = new RegisterUserDto
        {
            Email = "valid@example.com",
            Password = "SecurePass123!",
            ConfirmPassword = "SecurePass123!",
            FirstName = "John",
            LastName = "Doe"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("invalid-email")]
    [InlineData("@example.com")]
    public void Validate_WithInvalidEmail_ShouldHaveValidationError(string email)
    {
        // Arrange
        var dto = new RegisterUserDto
        {
            Email = email,
            Password = "SecurePass123!",
            ConfirmPassword = "SecurePass123!",
            FirstName = "John",
            LastName = "Doe"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData("")]
    [InlineData("short")]
    [InlineData("nouppercase123!")]
    [InlineData("NOLOWERCASE123!")]
    [InlineData("NoDigits!")]
    [InlineData("NoSpecialChar123")]
    public void Validate_WithInvalidPassword_ShouldHaveValidationError(string password)
    {
        // Arrange
        var dto = new RegisterUserDto
        {
            Email = "valid@example.com",
            Password = password,
            ConfirmPassword = password,
            FirstName = "John",
            LastName = "Doe"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Fact]
    public void Validate_WithMismatchedPasswords_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new RegisterUserDto
        {
            Email = "valid@example.com",
            Password = "SecurePass123!",
            ConfirmPassword = "DifferentPass123!",
            FirstName = "John",
            LastName = "Doe"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ConfirmPassword)
            .WithErrorMessage("Passwords do not match");
    }
}
```

---

## Task 6: Build and Test

### Build the solution:

```bash
dotnet build
```

### Run Application tests:

```bash
dotnet test GateKeeper.Application.Tests/GateKeeper.Application.Tests.csproj
```

**Expected:** All tests should pass.

---

## Verification Checklist

Before moving to Phase 3, verify:

- ✅ GateKeeper.Application project compiles successfully
- ✅ All DTOs created for User and Client operations
- ✅ UserService and ClientService implemented with full CRUD operations
- ✅ FluentValidation validators created for all input DTOs
- ✅ Common exceptions defined (ApplicationException, ValidationException, UnauthorizedException)
- ✅ Result pattern implemented (optional but recommended)
- ✅ Application.Tests project created with comprehensive tests
- ✅ Tests use Moq for mocking dependencies
- ✅ All tests passing
- ✅ No direct dependencies on Infrastructure or Presentation layers

---

## Key Architectural Points

### ✅ Separation of Concerns
- **DTOs** for data transfer (not domain entities)
- **Services** for orchestration (not business logic)
- **Validators** for input validation (not business rules)
- **Domain entities** for business logic

### ✅ Dependency Direction
- Application → Domain (correct)
- Application ← Infrastructure (will be implemented in Phase 3)
- Never: Domain → Application

### ✅ Password Security
- Plain-text passwords only exist in DTOs (in memory)
- Immediately hashed using `IPasswordHasher`
- Never stored in plain text
- Client secrets generated securely, returned once

### ✅ Testing Strategy
- **Unit tests** for Application layer using mocks
- **Domain logic** tested in Domain.Tests (Phase 1)
- **Integration tests** will be in Infrastructure.Tests (Phase 3)

---

## What's Next?

**Phase 3:** Infrastructure Layer
- Implement EF Core DbContext
- Create repositories
- Implement BCrypt password hasher
- Set up SQL Server database
- Create migrations
- Integration tests

---

## Notes

- **Validators vs Domain Rules:** Validators check input format/syntax. Domain entities enforce business rules.
- **Password Requirements:** OWASP recommends minimum 8 characters with complexity. Adjust based on security requirements.
- **Client Secret:** Returned only once on creation. User must save it securely.
- **Unit of Work:** Encapsulates SaveChanges to manage transactions.

---

**Phase 2 Complete! Ready for Phase 3: Infrastructure Layer** 🚀
