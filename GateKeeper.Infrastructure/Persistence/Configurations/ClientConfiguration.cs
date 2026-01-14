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
