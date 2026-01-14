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
