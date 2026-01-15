using GateKeeper.Domain.Entities;
using GateKeeper.Domain.Enums;
using GateKeeper.Domain.Interfaces;
using GateKeeper.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

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
        if (await context.Users.AnyAsync() || await context.Clients.AnyAsync())
        {
            return;
        }

        // Seed default organization
        var defaultOrg = new Organization
        {
            Id = Guid.NewGuid(),
            Name = "Default Organization",
            Subdomain = "default",
            SettingsJson = System.Text.Json.JsonSerializer.Serialize(new OrganizationSettings { AllowSelfSignup = true })
        };

        await context.Organizations.AddAsync(defaultOrg);

        // Seed admin user
        var adminEmail = Email.Create("admin@gatekeeper.local");
        var adminPassword = passwordHasher.HashPassword("Admin123!@#");
        
        var adminUser = User.Register(
            adminEmail,
            adminPassword,
            "Admin",
            "User",
            defaultOrg.Id
        );

        await context.Users.AddAsync(adminUser);

        // Seed test user
        var testEmail = Email.Create("test@example.com");
        var testPassword = passwordHasher.HashPassword("Test123!@#");
        
        var testUser = User.Register(
            testEmail,
            testPassword,
            "Test",
            "User",
            defaultOrg.Id
        );

        await context.Users.AddAsync(testUser);

        // Seed demo public client (for testing OAuth flow) - owned by admin
        var demoPublicClient = Client.CreatePublic(
            "Demo Public App",
            "demo-public-app",
            adminUser.Id,
            defaultOrg.Id,
            new List<RedirectUri>
            {
                RedirectUri.Create("https://oauth.pstmn.io/v1/callback"), // Postman OAuth
                RedirectUri.Create("http://localhost:5173/callback") // Local React dev
            },
            new List<string> { "openid", "profile", "email" }
        );

        await context.Clients.AddAsync(demoPublicClient);

        // Seed demo confidential client - owned by admin
        var secret = ClientSecret.Generate();
        var hashedSecret = ClientSecret.FromHashed(
            passwordHasher.HashPassword(secret.HashedValue)
        );

        var demoConfidentialClient = Client.CreateConfidential(
            "Demo Confidential App",
            "demo-confidential-app",
            hashedSecret,
            adminUser.Id,
            defaultOrg.Id,
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
