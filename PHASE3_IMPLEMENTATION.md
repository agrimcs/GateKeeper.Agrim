# Phase 3: Infrastructure Layer - Implementation Guide

**Estimated Time:** 3-4 hours  
**Goal:** Implement persistence, security services, and infrastructure concerns  
**Prerequisites:** Phase 1 (Domain) and Phase 2 (Application) completed

---

## Objectives

By the end of Phase 3, you will have:
- ✅ GateKeeper.Infrastructure project created (Class Library)
- ✅ Entity Framework Core DbContext with configurations
- ✅ Repository implementations for User and Client
- ✅ BCrypt password hasher implementation
- ✅ Unit of Work pattern for transaction management
- ✅ Database migrations created
- ✅ SQL Server database schema generated
- ✅ Comprehensive integration tests
- ✅ **Implements all Application layer interfaces**

---

## Task 1: Create Infrastructure Project

### Create new Class Library project:

```bash
cd GateKeeper
dotnet new classlib -n GateKeeper.Infrastructure
dotnet sln add GateKeeper.Infrastructure/GateKeeper.Infrastructure.csproj
```

### Add project references:

```bash
cd GateKeeper.Infrastructure
dotnet add reference ../GateKeeper.Domain/GateKeeper.Domain.csproj
dotnet add reference ../GateKeeper.Application/GateKeeper.Application.csproj
```

### Update GateKeeper.Infrastructure.csproj:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <ProjectReference Include="..\GateKeeper.Domain\GateKeeper.Domain.csproj" />
    <ProjectReference Include="..\GateKeeper.Application\GateKeeper.Application.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.EntityFrameworkCore" Version="9.0.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="9.0.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="9.0.0">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="Microsoft.EntityFrameworkCore.Tools" Version="9.0.0">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="BCrypt.Net-Next" Version="4.0.3" />
  </ItemGroup>
</Project>
```

**Note:** Infrastructure depends on Domain + Application and includes EF Core + BCrypt.

---

## Task 2: Create Folder Structure

Create the following directory structure:

```
GateKeeper.Infrastructure/
├── Persistence/
│   ├── ApplicationDbContext.cs
│   ├── Configurations/
│   │   ├── UserConfiguration.cs
│   │   └── ClientConfiguration.cs
│   ├── Repositories/
│   │   ├── UserRepository.cs
│   │   └── ClientRepository.cs
│   ├── Interceptors/
│   │   └── DomainEventDispatcherInterceptor.cs
│   └── Migrations/
├── Security/
│   └── BcryptPasswordHasher.cs
└── DependencyInjection.cs
```

---

## Task 3: Implement Password Hasher

**File:** `GateKeeper.Infrastructure/Security/BcryptPasswordHasher.cs`

```csharp
using BCrypt.Net;
using GateKeeper.Domain.Interfaces;

namespace GateKeeper.Infrastructure.Security;

/// <summary>
/// BCrypt implementation of password hashing.
/// Uses work factor of 12 (OWASP recommended).
/// </summary>
public class BcryptPasswordHasher : IPasswordHasher
{
    private const int WorkFactor = 12;

    /// <summary>
    /// Hashes a plain text password using BCrypt with automatic salt generation.
    /// </summary>
    /// <param name="password">Plain text password to hash</param>
    /// <returns>BCrypt hashed password</returns>
    public string HashPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Password cannot be null or empty", nameof(password));
        }

        return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
    }

    /// <summary>
    /// Verifies a plain text password against a BCrypt hash.
    /// </summary>
    /// <param name="hashedPassword">BCrypt hashed password from database</param>
    /// <param name="providedPassword">Plain text password to verify</param>
    /// <returns>True if password matches, false otherwise</returns>
    public bool VerifyPassword(string hashedPassword, string providedPassword)
    {
        if (string.IsNullOrWhiteSpace(hashedPassword))
        {
            throw new ArgumentException("Hashed password cannot be null or empty", nameof(hashedPassword));
        }

        if (string.IsNullOrWhiteSpace(providedPassword))
        {
            return false;
        }

        try
        {
            return BCrypt.Net.BCrypt.Verify(providedPassword, hashedPassword);
        }
        catch (SaltParseException)
        {
            // Invalid hash format
            return false;
        }
    }
}
```

**Why BCrypt?**
- OWASP-recommended for password storage
- Automatic salt generation
- Adaptive work factor (can increase security over time)
- Resistant to rainbow table attacks
- Work factor 12 = ~250-350ms (good balance of security vs UX)

---

## Task 4: Create EF Core DbContext

**File:** `GateKeeper.Infrastructure/Persistence/ApplicationDbContext.cs`

```csharp
using GateKeeper.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GateKeeper.Infrastructure.Persistence;

/// <summary>
/// Entity Framework Core database context for GateKeeper.
/// Manages User and Client aggregates with proper configurations.
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Client> Clients => Set<Client>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
```

**Key Points:**
- Exposes DbSet for each aggregate root (User, Client)
- Uses fluent configuration (separate files, not data annotations)
- Configurations applied via assembly scanning

---

## Task 5: Configure Entity Mappings

### User Configuration

**File:** `GateKeeper.Infrastructure/Persistence/Configurations/UserConfiguration.cs`

```csharp
using GateKeeper.Domain.Entities;
using GateKeeper.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GateKeeper.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for User aggregate.
/// Maps domain entity to database schema.
/// </summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // Table mapping
        builder.ToTable("Users");

        // Primary key
        builder.HasKey(u => u.Id);

        // Email value object mapping
        builder.OwnsOne(u => u.Email, email =>
        {
            email.Property(e => e.Value)
                .HasColumnName("Email")
                .IsRequired()
                .HasMaxLength(254);

            // Create unique index on email for fast lookups and uniqueness
            email.HasIndex(e => e.Value)
                .IsUnique()
                .HasDatabaseName("IX_Users_Email");
        });

        // Password hash
        builder.Property(u => u.PasswordHash)
            .IsRequired()
            .HasMaxLength(100); // BCrypt hashes are ~60 chars, buffer for future algorithms

        // Profile information
        builder.Property(u => u.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.LastName)
            .IsRequired()
            .HasMaxLength(100);

        // Timestamps
        builder.Property(u => u.CreatedAt)
            .IsRequired();

        builder.Property(u => u.LastLoginAt)
            .IsRequired(false);

        // Indexes for common queries
        builder.HasIndex(u => u.CreatedAt)
            .HasDatabaseName("IX_Users_CreatedAt");
    }
}
```

**Why This Approach?**
- **Value object mapping:** Email is owned entity with proper column naming
- **Indexes:** Email is unique and indexed for fast authentication lookups
- **Max lengths:** Prevent unbounded VARCHAR issues
- **Separation:** Configuration separate from domain entities (Clean Architecture)

---

### Client Configuration

**File:** `GateKeeper.Infrastructure/Persistence/Configurations/ClientConfiguration.cs`

```csharp
using GateKeeper.Domain.Entities;
using GateKeeper.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GateKeeper.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF Core configuration for Client aggregate.
/// Maps domain entity to database schema including collections.
/// </summary>
public class ClientConfiguration : IEntityTypeConfiguration<Client>
{
    public void Configure(EntityTypeBuilder<Client> builder)
    {
        // Table mapping
        builder.ToTable("Clients");

        // Primary key
        builder.HasKey(c => c.Id);

        // ClientId (OAuth client_id string)
        builder.Property(c => c.ClientId)
            .IsRequired()
            .HasMaxLength(100);

        // Unique index on ClientId for OAuth lookups
        builder.HasIndex(c => c.ClientId)
            .IsUnique()
            .HasDatabaseName("IX_Clients_ClientId");

        // Display name
        builder.Property(c => c.DisplayName)
            .IsRequired()
            .HasMaxLength(200);

        // Client type enum
        builder.Property(c => c.Type)
            .IsRequired()
            .HasConversion<string>() // Store as string in DB for readability
            .HasMaxLength(20);

        // Client secret value object
        builder.OwnsOne(c => c.Secret, secret =>
        {
            secret.Property(s => s.HashedValue)
                .HasColumnName("SecretHash")
                .IsRequired(false) // Nullable for public clients
                .HasMaxLength(100);
        });

        // Redirect URIs collection (owned entities)
        builder.OwnsMany(c => c.RedirectUris, redirectUri =>
        {
            redirectUri.ToTable("ClientRedirectUris");
            
            redirectUri.WithOwner()
                .HasForeignKey("ClientId");

            redirectUri.Property<int>("Id")
                .ValueGeneratedOnAdd();

            redirectUri.HasKey("Id");

            redirectUri.Property(r => r.Value)
                .HasColumnName("Uri")
                .IsRequired()
                .HasMaxLength(500);

            // Index for URI validation queries
            redirectUri.HasIndex("ClientId", "Value")
                .HasDatabaseName("IX_ClientRedirectUris_ClientId_Uri");
        });

        // Allowed scopes as JSON array or separate table
        // Simple approach: store as comma-separated string for MVP
        builder.Property(c => c.AllowedScopes)
            .HasConversion(
                v => string.Join(',', v),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
            )
            .HasColumnName("AllowedScopes")
            .HasMaxLength(1000);

        // Timestamps
        builder.Property(c => c.CreatedAt)
            .IsRequired();

        // Indexes
        builder.HasIndex(c => c.CreatedAt)
            .HasDatabaseName("IX_Clients_CreatedAt");

        builder.HasIndex(c => c.Type)
            .HasDatabaseName("IX_Clients_Type");
    }
}
```

**Key Design Decisions:**
- **RedirectUris:** Separate table (owned entities) for proper querying
- **AllowedScopes:** Comma-separated for MVP (can refactor to separate table later)
- **ClientType:** Stored as string for human readability in database
- **ClientSecret:** Nullable for public clients (they don't have secrets)

---

## Task 6: Implement Unit of Work

**File:** `GateKeeper.Infrastructure/Persistence/UnitOfWork.cs`

```csharp
using GateKeeper.Domain.Interfaces;

namespace GateKeeper.Infrastructure.Persistence;

/// <summary>
/// Unit of Work pattern implementation using EF Core DbContext.
/// Provides transaction boundary for repository operations.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Saves all changes to the database in a single transaction.
    /// </summary>
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Begins a new database transaction.
    /// Useful for multi-step operations that need rollback capability.
    /// </summary>
    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    /// <summary>
    /// Commits the current transaction.
    /// </summary>
    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_context.Database.CurrentTransaction != null)
        {
            await _context.Database.CommitTransactionAsync(cancellationToken);
        }
    }

    /// <summary>
    /// Rolls back the current transaction.
    /// </summary>
    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_context.Database.CurrentTransaction != null)
        {
            await _context.Database.RollbackTransactionAsync(cancellationToken);
        }
    }
}
```

---

## Task 7: Implement Repositories

### User Repository

**File:** `GateKeeper.Infrastructure/Persistence/Repositories/UserRepository.cs`

```csharp
using GateKeeper.Domain.Entities;
using GateKeeper.Domain.Interfaces;
using GateKeeper.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace GateKeeper.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for User aggregate.
/// Provides data access operations for users.
/// </summary>
public class UserRepository : IUserRepository
{
    private readonly ApplicationDbContext _context;

    public UserRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);
    }

    public async Task<User?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .FirstOrDefaultAsync(u => u.Email.Value == email.Value, cancellationToken);
    }

    public async Task<bool> ExistsAsync(Email email, CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AnyAsync(u => u.Email.Value == email.Value, cancellationToken);
    }

    public async Task<List<User>> GetAllAsync(
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .OrderByDescending(u => u.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        await _context.Users.AddAsync(user, cancellationToken);
    }

    public Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        _context.Users.Update(user);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(User user, CancellationToken cancellationToken = default)
    {
        _context.Users.Remove(user);
        return Task.CompletedTask;
    }
}
```

**Important Notes:**
- **No SaveChanges:** Repositories don't save directly (Unit of Work pattern)
- **Value object queries:** Access `Email.Value` for database queries
- **Pagination:** Default skip/take for list operations
- **Async all the way:** All operations support cancellation tokens

---

### Client Repository

**File:** `GateKeeper.Infrastructure/Persistence/Repositories/ClientRepository.cs`

```csharp
using GateKeeper.Domain.Entities;
using GateKeeper.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GateKeeper.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for Client aggregate.
/// Provides data access operations for OAuth clients.
/// </summary>
public class ClientRepository : IClientRepository
{
    private readonly ApplicationDbContext _context;

    public ClientRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Client?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Clients
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<Client?> GetByClientIdAsync(
        string clientId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Clients
            .FirstOrDefaultAsync(c => c.ClientId == clientId, cancellationToken);
    }

    public async Task<bool> ExistsAsync(string clientId, CancellationToken cancellationToken = default)
    {
        return await _context.Clients
            .AnyAsync(c => c.ClientId == clientId, cancellationToken);
    }

    public async Task<List<Client>> GetAllAsync(
        int skip = 0,
        int take = 50,
        CancellationToken cancellationToken = default)
    {
        return await _context.Clients
            .OrderByDescending(c => c.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Client client, CancellationToken cancellationToken = default)
    {
        await _context.Clients.AddAsync(client, cancellationToken);
    }

    public Task UpdateAsync(Client client, CancellationToken cancellationToken = default)
    {
        _context.Clients.Update(client);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Client client, CancellationToken cancellationToken = default)
    {
        _context.Clients.Remove(client);
        return Task.CompletedTask;
    }
}
```

---

## Task 8: Dependency Injection Configuration

**File:** `GateKeeper.Infrastructure/DependencyInjection.cs`

```csharp
using GateKeeper.Domain.Interfaces;
using GateKeeper.Infrastructure.Persistence;
using GateKeeper.Infrastructure.Persistence.Repositories;
using GateKeeper.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace GateKeeper.Infrastructure;

/// <summary>
/// Extension methods for configuring Infrastructure layer services.
/// Called from Program.cs in the API project.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database configuration
        services.AddDbContext<ApplicationDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            
            options.UseSqlServer(connectionString, sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(5),
                    errorNumbersToAdd: null);
                
                sqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
            });

            // Enable sensitive data logging in development only
            if (configuration.GetValue<bool>("Logging:EnableSensitiveDataLogging"))
            {
                options.EnableSensitiveDataLogging();
            }
        });

        // Register repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IClientRepository, ClientRepository>();

        // Register Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Register security services
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();

        return services;
    }

    /// <summary>
    /// Applies pending migrations to the database.
    /// Call this in Program.cs during startup for automatic migrations.
    /// </summary>
    public static async Task ApplyMigrationsAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        
        if ((await context.Database.GetPendingMigrationsAsync()).Any())
        {
            await context.Database.MigrateAsync();
        }
    }
}
```

**Key Features:**
- **SQL Server configuration:** Connection string from appsettings.json
- **Retry logic:** Automatic retry on transient failures
- **Migration helper:** Easy database initialization
- **Scoped repositories:** New instance per HTTP request
- **Singleton hasher:** Stateless, can be shared

---

## Task 9: Create Database Migrations

### Update appsettings.json in Server Project

First, add connection string to `GateKeeper.Server/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=GateKeeperDb;Integrated Security=true;TrustServerCertificate=true;MultipleActiveResultSets=true"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.EntityFrameworkCore": "Warning"
    },
    "EnableSensitiveDataLogging": false
  }
}
```

**Development only** (`appsettings.Development.json`):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=GateKeeperDb_Dev;Integrated Security=true;TrustServerCertificate=true;MultipleActiveResultSets=true"
  },
  "Logging": {
    "LogLevel": {
      "Microsoft.EntityFrameworkCore": "Information"
    },
    "EnableSensitiveDataLogging": true
  }
}
```

### Create Initial Migration

```bash
# From solution root directory
cd GateKeeper.Infrastructure

# Create initial migration
dotnet ef migrations add InitialCreate --startup-project ../GateKeeper.Server/GateKeeper.Server.csproj --output-dir Persistence/Migrations

# Apply migration to database
dotnet ef database update --startup-project ../GateKeeper.Server/GateKeeper.Server.csproj
```

**What This Creates:**
- Migration files in `Persistence/Migrations/` folder
- `Users` table with Email value object
- `Clients` table with Type enum
- `ClientRedirectUris` table for redirect URI collection
- All indexes and constraints

### Verify Migration

Connect to SQL Server using SSMS or Azure Data Studio and verify:

```sql
-- Check tables were created
SELECT TABLE_NAME 
FROM INFORMATION_SCHEMA.TABLES 
WHERE TABLE_TYPE = 'BASE TABLE'
ORDER BY TABLE_NAME;

-- Should see:
-- - Users
-- - Clients
-- - ClientRedirectUris
-- - __EFMigrationsHistory

-- Check Users table structure
EXEC sp_help 'Users';

-- Check indexes
SELECT 
    i.name AS IndexName,
    t.name AS TableName,
    c.name AS ColumnName
FROM sys.indexes i
INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
INNER JOIN sys.tables t ON i.object_id = t.object_id
WHERE t.name IN ('Users', 'Clients')
ORDER BY t.name, i.name;
```

---

## Task 10: Seed Initial Data (Optional but Recommended)

**File:** `GateKeeper.Infrastructure/Persistence/ApplicationDbContextSeed.cs`

```csharp
using GateKeeper.Domain.Entities;
using GateKeeper.Domain.Enums;
using GateKeeper.Domain.Interfaces;
using GateKeeper.Domain.ValueObjects;

namespace GateKeeper.Infrastructure.Persistence;

/// <summary>
/// Seeds initial data for development and testing.
/// Creates sample users and OAuth clients.
/// </summary>
public static class ApplicationDbContextSeed
{
    public static async Task SeedAsync(
        ApplicationDbContext context,
        IPasswordHasher passwordHasher)
    {
        // Only seed if database is empty
        if (context.Users.Any() || context.Clients.Any())
        {
            return;
        }

        // Seed admin user
        var adminEmail = Email.Create("admin@gatekeeper.local");
        var adminPassword = passwordHasher.HashPassword("Admin123!@#");
        
        var adminUser = User.Register(
            adminEmail,
            adminPassword,
            "Admin",
            "User"
        );

        await context.Users.AddAsync(adminUser);

        // Seed test user
        var testEmail = Email.Create("test@example.com");
        var testPassword = passwordHasher.HashPassword("Test123!@#");
        
        var testUser = User.Register(
            testEmail,
            testPassword,
            "Test",
            "User"
        );

        await context.Users.AddAsync(testUser);

        // Seed demo public client (for testing OAuth flow)
        var demoPublicClient = Client.CreatePublic(
            "Demo Public App",
            "demo-public-app",
            new List<RedirectUri>
            {
                RedirectUri.Create("https://oauth.pstmn.io/v1/callback"), // Postman OAuth
                RedirectUri.Create("http://localhost:5173/callback") // Local React dev
            },
            new List<string> { "openid", "profile", "email" }
        );

        await context.Clients.AddAsync(demoPublicClient);

        // Seed demo confidential client
        var secret = ClientSecret.Generate();
        var hashedSecret = ClientSecret.FromHashed(
            passwordHasher.HashPassword(secret.HashedValue)
        );

        var demoConfidentialClient = Client.CreateConfidential(
            "Demo Confidential App",
            "demo-confidential-app",
            hashedSecret,
            new List<RedirectUri>
            {
                RedirectUri.Create("https://localhost:5001/callback")
            },
            new List<string> { "openid", "profile", "email", "api" }
        );

        await context.Clients.AddAsync(demoConfidentialClient);

        // Save all seed data
        await context.SaveChangesAsync();

        Console.WriteLine("=== Seed Data Created ===");
        Console.WriteLine($"Admin User: {adminEmail.Value} / Admin123!@#");
        Console.WriteLine($"Test User: {testEmail.Value} / Test123!@#");
        Console.WriteLine($"Demo Public Client ID: demo-public-app (no secret)");
        Console.WriteLine($"Demo Confidential Client ID: demo-confidential-app");
        Console.WriteLine($"Demo Confidential Client Secret: {secret.HashedValue}");
        Console.WriteLine("=========================");
    }
}
```

**Usage in Program.cs:**

```csharp
// After app.Build(), before app.Run()
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    
    await ApplicationDbContextSeed.SeedAsync(context, passwordHasher);
}
```

---

## Task 11: Update Domain Interfaces (If Needed)

Ensure `IUnitOfWork` includes transaction methods. Update if necessary:

**File:** `GateKeeper.Domain/Interfaces/IUnitOfWork.cs`

```csharp
namespace GateKeeper.Domain.Interfaces;

/// <summary>
/// Unit of Work pattern interface.
/// Manages transactions and coordinates repository changes.
/// </summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Saves all changes to the database.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Begins a new transaction.
    /// </summary>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Commits the current transaction.
    /// </summary>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Rolls back the current transaction.
    /// </summary>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
```

---

## Task 12: Integration Testing Setup

### Create Test Project

```bash
cd GateKeeper
dotnet new xunit -n GateKeeper.Infrastructure.Tests
dotnet sln add GateKeeper.Infrastructure.Tests/GateKeeper.Infrastructure.Tests.csproj

cd GateKeeper.Infrastructure.Tests
dotnet add reference ../GateKeeper.Domain/GateKeeper.Domain.csproj
dotnet add reference ../GateKeeper.Application/GateKeeper.Application.csproj
dotnet add reference ../GateKeeper.Infrastructure/GateKeeper.Infrastructure.csproj
```

### Add Test Packages

**File:** `GateKeeper.Infrastructure.Tests/GateKeeper.Infrastructure.Tests.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.11.0" />
    <PackageReference Include="xunit" Version="2.9.0" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2">
      <PrivateAssets>all</PrivateAssets>
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
    <PackageReference Include="FluentAssertions" Version="6.12.0" />
    <PackageReference Include="Microsoft.EntityFrameworkCore.InMemory" Version="9.0.0" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\GateKeeper.Domain\GateKeeper.Domain.csproj" />
    <ProjectReference Include="..\GateKeeper.Application\GateKeeper.Application.csproj" />
    <ProjectReference Include="..\GateKeeper.Infrastructure\GateKeeper.Infrastructure.csproj" />
  </ItemGroup>
</Project>
```

### Sample Repository Test

**File:** `GateKeeper.Infrastructure.Tests/Repositories/UserRepositoryTests.cs`

```csharp
using FluentAssertions;
using GateKeeper.Domain.Entities;
using GateKeeper.Domain.ValueObjects;
using GateKeeper.Infrastructure.Persistence;
using GateKeeper.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GateKeeper.Infrastructure.Tests.Repositories;

public class UserRepositoryTests : IDisposable
{
    private readonly ApplicationDbContext _context;
    private readonly UserRepository _repository;

    public UserRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new ApplicationDbContext(options);
        _repository = new UserRepository(_context);
    }

    [Fact]
    public async Task AddAsync_ShouldAddUserToDatabase()
    {
        // Arrange
        var email = Email.Create("test@example.com");
        var user = User.Register(email, "hashedPassword", "John", "Doe");

        // Act
        await _repository.AddAsync(user);
        await _context.SaveChangesAsync();

        // Assert
        var savedUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == user.Id);
        savedUser.Should().NotBeNull();
        savedUser!.Email.Value.Should().Be("test@example.com");
        savedUser.FirstName.Should().Be("John");
    }

    [Fact]
    public async Task GetByEmailAsync_ShouldReturnUser_WhenUserExists()
    {
        // Arrange
        var email = Email.Create("test@example.com");
        var user = User.Register(email, "hashedPassword", "Jane", "Smith");
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByEmailAsync(email);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(user.Id);
        result.FirstName.Should().Be("Jane");
    }

    [Fact]
    public async Task GetByEmailAsync_ShouldReturnNull_WhenUserDoesNotExist()
    {
        // Arrange
        var email = Email.Create("nonexistent@example.com");

        // Act
        var result = await _repository.GetByEmailAsync(email);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnTrue_WhenUserExists()
    {
        // Arrange
        var email = Email.Create("existing@example.com");
        var user = User.Register(email, "hashedPassword", "Bob", "Johnson");
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        // Act
        var exists = await _repository.ExistsAsync(email);

        // Assert
        exists.Should().BeTrue();
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnFalse_WhenUserDoesNotExist()
    {
        // Arrange
        var email = Email.Create("nonexistent@example.com");

        // Act
        var exists = await _repository.ExistsAsync(email);

        // Assert
        exists.Should().BeFalse();
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
```

### Run Tests

```bash
dotnet test GateKeeper.Infrastructure.Tests/GateKeeper.Infrastructure.Tests.csproj
```

---

## Task 13: Verify Everything Works

### 1. Build Solution

```bash
dotnet build
```

Should have **zero errors**.

### 2. Run All Tests

```bash
dotnet test
```

All tests should pass (Domain tests + Infrastructure tests).

### 3. Verify Database Schema

Use SQL Server Management Studio or Azure Data Studio:

```sql
-- Connect to database
USE GateKeeperDb_Dev;

-- Check table structure
SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Users';
SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Clients';
SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ClientRedirectUris';

-- Verify indexes
EXEC sp_helpindex 'Users';
EXEC sp_helpindex 'Clients';
```

### 4. Test Password Hashing

Create a simple console test in `Program.cs` (temporary):

```csharp
using GateKeeper.Infrastructure.Security;

var hasher = new BcryptPasswordHasher();

var password = "Test123!@#";
var hash = hasher.HashPassword(password);

Console.WriteLine($"Password: {password}");
Console.WriteLine($"Hash: {hash}");
Console.WriteLine($"Verify (correct): {hasher.VerifyPassword(hash, password)}");
Console.WriteLine($"Verify (wrong): {hasher.VerifyPassword(hash, "WrongPassword")}");
```

Expected output:
```
Password: Test123!@#
Hash: $2a$12$[60-character hash]
Verify (correct): True
Verify (wrong): False
```

---

## Phase 3 Checklist

Before moving to Phase 4, verify:

- ✅ Infrastructure project created with correct dependencies
- ✅ BCrypt password hasher implemented and tested
- ✅ EF Core DbContext configured with proper mappings
- ✅ User and Client entity configurations complete
- ✅ Value objects mapped correctly (Email, RedirectUri, ClientSecret)
- ✅ Repositories implement all interface methods
- ✅ Unit of Work handles transactions
- ✅ Database migrations created and applied
- ✅ Connection string configured in appsettings.json
- ✅ SQL Server database created with correct schema
- ✅ Indexes created for performance (Email, ClientId)
- ✅ Seed data script ready (optional)
- ✅ Integration tests passing
- ✅ No compilation errors
- ✅ DependencyInjection.cs registers all services

---

## Common Issues & Solutions

### Issue 1: Migration Fails with "No DbContext Found"

**Solution:** Ensure startup project is set correctly:

```bash
dotnet ef migrations add InitialCreate \
  --project GateKeeper.Infrastructure \
  --startup-project GateKeeper.Server
```

### Issue 2: Value Object Mapping Error

**Error:** `The property 'Email' is of type 'Email' which is not supported`

**Solution:** Use `.OwnsOne()` in entity configuration:

```csharp
builder.OwnsOne(u => u.Email, email => {
    email.Property(e => e.Value).HasColumnName("Email");
});
```

### Issue 3: ClientSecret Null for Public Clients

**Expected behavior:** Public clients don't have secrets, so `Secret` can be null.

**Solution:** Make sure configuration marks it nullable:

```csharp
secret.Property(s => s.HashedValue)
    .IsRequired(false); // Allow null for public clients
```

### Issue 4: Connection String Errors

**Error:** `A network-related or instance-specific error occurred`

**Solutions:**
- Verify SQL Server is running
- Check server name (usually `localhost` or `(localdb)\MSSQLLocalDB`)
- Ensure TCP/IP is enabled in SQL Server Configuration Manager
- Try `Server=.;Database=GateKeeperDb;...` (dot means local server)

### Issue 5: InMemory Database Issues in Tests

**Error:** Tests interfere with each other

**Solution:** Use unique database name per test:

```csharp
.UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
```

---

## Next Steps

**Phase 4: API Layer**
- Create ASP.NET Core controllers
- Implement exception handling middleware
- Configure authentication & authorization
- Add Swagger/OpenAPI documentation
- Integrate OpenIddict for OAuth2/OIDC

**Phase 5: Frontend**
- Build React application
- Implement login/registration forms
- Create OAuth consent screen
- Build client management UI

---

## Performance Considerations

### Indexes Created
- `IX_Users_Email` (unique) - Fast authentication lookups
- `IX_Clients_ClientId` (unique) - OAuth client identification
- `IX_ClientRedirectUris_ClientId_Uri` - URI validation
- `IX_Users_CreatedAt` - Sorting user lists
- `IX_Clients_CreatedAt` - Sorting client lists

### Connection Pooling
EF Core automatically uses connection pooling. Default pool size is 100.

### Query Optimization
- Repositories use `FirstOrDefaultAsync` (stops at first match)
- Pagination with `Skip/Take` for large result sets
- Indexed columns for WHERE clauses

---

## Security Notes

✅ **Password hashing:** BCrypt with work factor 12  
✅ **SQL injection:** EF Core parameterizes queries automatically  
✅ **Connection strings:** Stored in appsettings (use User Secrets or Azure Key Vault for production)  
✅ **Client secrets:** Hashed before storage, never returned after creation  

⚠️ **Important:** Never log connection strings or hashed passwords  
⚠️ **Important:** Use HTTPS in production (TrustServerCertificate=true is dev-only)

---

## Estimated Completion Time

- Setup & NuGet packages: 15 min
- Password hasher: 15 min
- DbContext & configurations: 45 min
- Repositories & Unit of Work: 30 min
- Dependency injection: 15 min
- Migrations & database: 30 min
- Seed data: 20 min
- Integration tests: 45 min
- Testing & verification: 30 min

**Total: ~3.5-4 hours**

---

## Summary

Phase 3 completes the Infrastructure layer:

✅ **Persistence:** EF Core with SQL Server  
✅ **Security:** BCrypt password hashing  
✅ **Repositories:** User and Client data access  
✅ **Unit of Work:** Transaction management  
✅ **Migrations:** Database schema version control  
✅ **Testing:** Integration tests with in-memory database

Your application now has a **complete backend foundation** ready for the API layer.

---

**Phase 3 Status:** 🎯 Ready for Implementation  
**Next Phase:** [Phase 4 - API Layer & OpenIddict](PHASE4_IMPLEMENTATION.md)
