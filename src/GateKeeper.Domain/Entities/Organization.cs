using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using GateKeeper.Domain.Common;

namespace GateKeeper.Domain.Entities;

/// <summary>
/// Represents a tenant organization in the multi-tenant system
/// </summary>
public class Organization : Entity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
    public string? CustomDomain { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string BillingPlan { get; set; } = "Free";
    
    // Persisted JSON settings column
    public string? SettingsJson { get; set; }

    // Non-mapped convenience property to work with typed settings
    [NotMapped]
    public OrganizationSettings Settings
    {
        get => string.IsNullOrEmpty(SettingsJson)
            ? new OrganizationSettings()
            : JsonSerializer.Deserialize<OrganizationSettings>(SettingsJson) ?? new OrganizationSettings();
        set => SettingsJson = JsonSerializer.Serialize(value);
    }

    // Navigation properties
    public virtual ICollection<User> Users { get; set; } = new List<User>();
    public virtual ICollection<Client> Clients { get; set; } = new List<Client>();
}

/// <summary>
/// Organization-specific configuration settings
/// </summary>
public class OrganizationSettings
{
    public bool AllowSelfSignup { get; set; } = false;
    public int MaxUsers { get; set; } = 100;
    public int MaxClients { get; set; } = 10;
    public string[] AllowedEmailDomains { get; set; } = Array.Empty<string>();
    public bool RequireEmailVerification { get; set; } = true;
    public int SessionTimeoutMinutes { get; set; } = 60;
    public string? LogoUrl { get; set; }
    public string? PrimaryColor { get; set; }
}
