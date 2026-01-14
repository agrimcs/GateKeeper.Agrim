using FluentAssertions;
using GateKeeper.Domain.ValueObjects;

namespace GateKeeper.Domain.Tests.ValueObjects;

/// <summary>
/// Tests for ClientSecret value object generation and hashing.
/// </summary>
public class ClientSecretTests
{
    [Fact]
    public void Generate_ShouldCreateNonEmptySecret()
    {
        // Act
        var secret = ClientSecret.Generate();

        // Assert
        secret.Should().NotBeNull();
        secret.HashedValue.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void Generate_ShouldCreateDifferentSecretsEachTime()
    {
        // Act
        var secret1 = ClientSecret.Generate();
        var secret2 = ClientSecret.Generate();

        // Assert
        secret1.HashedValue.Should().NotBe(secret2.HashedValue);
    }

    [Fact]
    public void Generate_ShouldCreateBase64EncodedSecret()
    {
        // Act
        var secret = ClientSecret.Generate();

        // Assert - Base64 strings should be convertible back to bytes without exception
        Action act = () => Convert.FromBase64String(secret.HashedValue);
        act.Should().NotThrow();
    }

    [Fact]
    public void Generate_ShouldCreateSecretWithMinimumLength()
    {
        // Act
        var secret = ClientSecret.Generate();

        // Assert - 32 bytes encoded in base64 = 44 characters (with padding)
        secret.HashedValue.Length.Should().BeGreaterThanOrEqualTo(40);
    }

    [Fact]
    public void FromHashed_ShouldCreateSecretWithProvidedHash()
    {
        // Arrange
        var hashedValue = "someprehashed-secret-value";

        // Act
        var secret = ClientSecret.FromHashed(hashedValue);

        // Assert
        secret.Should().NotBeNull();
        secret.HashedValue.Should().Be(hashedValue);
    }

    [Fact]
    public void ClientSecret_WithSameHashedValue_ShouldBeEqual()
    {
        // Arrange
        var hashedValue = "test-hash";
        var secret1 = ClientSecret.FromHashed(hashedValue);
        var secret2 = ClientSecret.FromHashed(hashedValue);

        // Assert
        secret1.Should().Be(secret2);
    }

    [Fact]
    public void ClientSecret_WithDifferentHashedValue_ShouldNotBeEqual()
    {
        // Arrange
        var secret1 = ClientSecret.FromHashed("hash1");
        var secret2 = ClientSecret.FromHashed("hash2");

        // Assert
        secret1.Should().NotBe(secret2);
    }
}
