using GateKeeper.Application.Common.Interfaces;
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
            var enableSensitiveDataLogging = configuration["Logging:EnableSensitiveDataLogging"];
            if (enableSensitiveDataLogging == "true")
            {
                options.EnableSensitiveDataLogging();
            }

            // Suppress pending model changes warning (for development)
            options.ConfigureWarnings(warnings =>
            {
                warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning);
            });

            // OpenIddict requires DbContext to be configured
            options.UseOpenIddict();
        });

        // OpenIddict configuration
        services.AddOpenIddict()
            .AddCore(options =>
            {
                options.UseEntityFrameworkCore()
                    .UseDbContext<ApplicationDbContext>();
            })
            .AddServer(options =>
            {
                // Enable the authorization and token endpoints
                options.SetAuthorizationEndpointUris("/connect/authorize")
                       .SetTokenEndpointUris("/connect/token")
                       .SetUserinfoEndpointUris("/connect/userinfo");

                // Enable flows
                options.AllowAuthorizationCodeFlow()
                       .AllowRefreshTokenFlow();

                // Require PKCE for public clients
                options.RequireProofKeyForCodeExchange();

                // Register scopes (permissions)
                options.RegisterScopes("openid", "profile", "email", "offline_access");

                // Configure token lifetimes
                options.SetAccessTokenLifetime(TimeSpan.FromMinutes(15))
                       .SetAuthorizationCodeLifetime(TimeSpan.FromMinutes(5))
                       .SetRefreshTokenLifetime(TimeSpan.FromDays(30));

                // Register signing and encryption credentials
                options.AddDevelopmentEncryptionCertificate()
                       .AddDevelopmentSigningCertificate();

                // Register ASP.NET Core host
                options.UseAspNetCore()
                       .EnableAuthorizationEndpointPassthrough()
                       .EnableTokenEndpointPassthrough()
                       .EnableUserinfoEndpointPassthrough();
            })
            .AddValidation(options =>
            {
                options.UseLocalServer();
                options.UseAspNetCore();
            });

        // Register repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IClientRepository, ClientRepository>();

        // Register Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Register security services
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<IJwtTokenGenerator, JwtTokenGenerator>();

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
