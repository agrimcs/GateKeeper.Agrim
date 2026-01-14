using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GateKeeper.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for ApplicationDbContext.
/// Used by EF Core tools for migrations.
/// </summary>
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        
        // Use a default connection string for design-time operations
        // This will be overridden at runtime by appsettings.json
        optionsBuilder.UseSqlServer(
            "Server=localhost;Database=GateKeeperDb_Dev;Integrated Security=true;TrustServerCertificate=true;MultipleActiveResultSets=true"
        );

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
