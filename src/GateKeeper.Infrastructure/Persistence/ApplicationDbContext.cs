using GateKeeper.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GateKeeper.Infrastructure.Persistence;

/// <summary>
/// Entity Framework Core database context for GateKeeper.
/// Manages User and Client aggregates with proper configurations.
/// Includes OpenIddict entities for OAuth2/OIDC functionality.
/// </summary>
public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Domain entities
    public DbSet<User> Users => Set<User>();
    public DbSet<Client> Clients => Set<Client>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        
        // Register OpenIddict entities (required for EF Core migrations)
        modelBuilder.UseOpenIddict();
    }
}
