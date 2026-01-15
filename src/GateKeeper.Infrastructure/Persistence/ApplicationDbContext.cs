using GateKeeper.Application.Common;
using GateKeeper.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace GateKeeper.Infrastructure.Persistence;

/// <summary>
/// Entity Framework Core database context for GateKeeper.
/// Manages User and Client aggregates with proper configurations.
/// Includes OpenIddict entities for OAuth2/OIDC functionality.
/// Adds tenant-awareness via global query filters when a tenant is present.
/// </summary>
public class ApplicationDbContext : DbContext
{
    private readonly ITenantService? _tenantService;

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // Constructor used when DI provides tenant service (runtime)
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ITenantService tenantService)
        : base(options)
    {
        _tenantService = tenantService;
    }

    // Domain entities
    public DbSet<User> Users => Set<User>();
    public DbSet<Client> Clients => Set<Client>();
    public DbSet<Organization> Organizations => Set<Organization>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply all configurations from assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        // Register OpenIddict entities (required for EF Core migrations)
        modelBuilder.UseOpenIddict();

        // Tenant-aware global query filters:
        // If a tenant is resolved at runtime, automatically scope Users and Clients to that tenant.
        // When running design-time migrations (tenantService null), do not apply filters.
        if (_tenantService != null && _tenantService.GetCurrentTenantId() is Guid tenantId)
        {
            modelBuilder.Entity<User>().HasQueryFilter(u => u.OrganizationId == tenantId);
            modelBuilder.Entity<Client>().HasQueryFilter(c => c.OrganizationId == tenantId);
        }
    }
}
