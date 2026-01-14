using FluentAssertions;
using GateKeeper.Domain.Entities;
using GateKeeper.Domain.Enums;
using GateKeeper.Domain.Events;
using GateKeeper.Domain.Exceptions;
using GateKeeper.Domain.ValueObjects;

namespace GateKeeper.Domain.Tests.Entities;

/// <summary>
/// Tests for Client aggregate root entity.
/// Validates OAuth client registration, redirect URI management, and security rules.
/// </summary>
public class ClientTests
{
    [Fact]
    public void CreateConfidential_WithValidData_ShouldCreateClient()
    {
        // Arrange
        var displayName = "My Backend App";
        var clientId = "backend-app-123";
        var secret = ClientSecret.Generate();
        var redirectUris = new[] { RedirectUri.Create("https://app.example.com/callback") };
        var scopes = new[] { "openid", "profile", "email" };

        // Act
        var client = Client.CreateConfidential(displayName, clientId, secret, redirectUris, scopes);

        // Assert
        client.Should().NotBeNull();
        client.Id.Should().NotBeEmpty();
        client.ClientId.Should().Be(clientId);
        client.DisplayName.Should().Be(displayName);
        client.Type.Should().Be(ClientType.Confidential);
        client.Secret.Should().Be(secret);
        client.RedirectUris.Should().HaveCount(1);
        client.AllowedScopes.Should().HaveCount(3);
        client.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void CreatePublic_WithValidData_ShouldCreateClient()
    {
        // Arrange
        var displayName = "My SPA App";
        var clientId = "spa-app-456";
        var redirectUris = new[] { RedirectUri.Create("https://spa.example.com/callback") };
        var scopes = new[] { "openid", "profile" };

        // Act
        var client = Client.CreatePublic(displayName, clientId, redirectUris, scopes);

        // Assert
        client.Should().NotBeNull();
        client.Id.Should().NotBeEmpty();
        client.ClientId.Should().Be(clientId);
        client.DisplayName.Should().Be(displayName);
        client.Type.Should().Be(ClientType.Public);
        client.Secret.Should().BeNull();
        client.RedirectUris.Should().HaveCount(1);
        client.AllowedScopes.Should().HaveCount(2);
    }

    [Fact]
    public void CreateConfidential_ShouldRaiseClientRegisteredEvent()
    {
        // Arrange
        var displayName = "Test App";
        var clientId = "test-123";
        var secret = ClientSecret.Generate();
        var redirectUris = new[] { RedirectUri.Create("https://test.com/callback") };
        var scopes = new[] { "openid" };

        // Act
        var client = Client.CreateConfidential(displayName, clientId, secret, redirectUris, scopes);

        // Assert
        client.DomainEvents.Should().ContainSingle();
        var domainEvent = client.DomainEvents.First();
        domainEvent.Should().BeOfType<ClientRegisteredEvent>();
        
        var clientRegisteredEvent = (ClientRegisteredEvent)domainEvent;
        clientRegisteredEvent.Id.Should().Be(client.Id);
        clientRegisteredEvent.ClientId.Should().Be(clientId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateConfidential_WithInvalidDisplayName_ShouldThrowDomainException(string invalidDisplayName)
    {
        // Arrange
        var secret = ClientSecret.Generate();
        var redirectUris = new[] { RedirectUri.Create("https://test.com/callback") };
        var scopes = new[] { "openid" };

        // Act
        Action act = () => Client.CreateConfidential(invalidDisplayName, "client-123", secret, redirectUris, scopes);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Client display name is required");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateConfidential_WithInvalidClientId_ShouldThrowDomainException(string invalidClientId)
    {
        // Arrange
        var secret = ClientSecret.Generate();
        var redirectUris = new[] { RedirectUri.Create("https://test.com/callback") };
        var scopes = new[] { "openid" };

        // Act
        Action act = () => Client.CreateConfidential("Test App", invalidClientId, secret, redirectUris, scopes);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Client ID is required");
    }

    [Fact]
    public void AddRedirectUri_WithNewUri_ShouldAddToCollection()
    {
        // Arrange
        var client = Client.CreatePublic(
            "Test App",
            "test-123",
            new[] { RedirectUri.Create("https://test.com/callback1") },
            new[] { "openid" });

        var newUri = RedirectUri.Create("https://test.com/callback2");

        // Act
        client.AddRedirectUri(newUri);

        // Assert
        client.RedirectUris.Should().HaveCount(2);
        client.RedirectUris.Should().Contain(newUri);
    }

    [Fact]
    public void AddRedirectUri_WithDuplicateUri_ShouldThrowDomainException()
    {
        // Arrange
        var uri = RedirectUri.Create("https://test.com/callback");
        var client = Client.CreatePublic(
            "Test App",
            "test-123",
            new[] { uri },
            new[] { "openid" });

        // Act
        Action act = () => client.AddRedirectUri(uri);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*already exists*");
    }

    [Fact]
    public void RemoveRedirectUri_WithExistingUri_ShouldRemoveFromCollection()
    {
        // Arrange
        var uri1 = RedirectUri.Create("https://test.com/callback1");
        var uri2 = RedirectUri.Create("https://test.com/callback2");
        var client = Client.CreatePublic(
            "Test App",
            "test-123",
            new[] { uri1, uri2 },
            new[] { "openid" });

        // Act
        client.RemoveRedirectUri(uri1);

        // Assert
        client.RedirectUris.Should().HaveCount(1);
        client.RedirectUris.Should().NotContain(uri1);
        client.RedirectUris.Should().Contain(uri2);
    }

    [Fact]
    public void RemoveRedirectUri_WithNonExistingUri_ShouldThrowDomainException()
    {
        // Arrange
        var client = Client.CreatePublic(
            "Test App",
            "test-123",
            new[] { RedirectUri.Create("https://test.com/callback1") },
            new[] { "openid" });

        var nonExistingUri = RedirectUri.Create("https://test.com/callback2");

        // Act
        Action act = () => client.RemoveRedirectUri(nonExistingUri);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*not found*");
    }

    [Fact]
    public void ValidateRedirectUri_WithRegisteredUri_ShouldReturnTrue()
    {
        // Arrange
        var uriString = "https://test.com/callback";
        var client = Client.CreatePublic(
            "Test App",
            "test-123",
            new[] { RedirectUri.Create(uriString) },
            new[] { "openid" });

        // Act
        var isValid = client.ValidateRedirectUri(uriString);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void ValidateRedirectUri_WithUnregisteredUri_ShouldReturnFalse()
    {
        // Arrange
        var client = Client.CreatePublic(
            "Test App",
            "test-123",
            new[] { RedirectUri.Create("https://test.com/callback1") },
            new[] { "openid" });

        // Act
        var isValid = client.ValidateRedirectUri("https://test.com/callback2");

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void ValidateRedirectUri_IsCaseSensitive()
    {
        // Arrange
        var client = Client.CreatePublic(
            "Test App",
            "test-123",
            new[] { RedirectUri.Create("https://test.com/callback") },
            new[] { "openid" });

        // Act
        var isValid = client.ValidateRedirectUri("https://TEST.com/callback");

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void UpdateDisplayName_WithValidName_ShouldUpdateDisplayName()
    {
        // Arrange
        var client = Client.CreatePublic(
            "Old Name",
            "test-123",
            new[] { RedirectUri.Create("https://test.com/callback") },
            new[] { "openid" });

        // Act
        client.UpdateDisplayName("New Name");

        // Assert
        client.DisplayName.Should().Be("New Name");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateDisplayName_WithInvalidName_ShouldThrowDomainException(string invalidName)
    {
        // Arrange
        var client = Client.CreatePublic(
            "Test App",
            "test-123",
            new[] { RedirectUri.Create("https://test.com/callback") },
            new[] { "openid" });

        // Act
        Action act = () => client.UpdateDisplayName(invalidName);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Display name cannot be empty");
    }

    [Fact]
    public void CreateConfidential_WithMultipleRedirectUris_ShouldAddAllUris()
    {
        // Arrange
        var redirectUris = new[]
        {
            RedirectUri.Create("https://test.com/callback1"),
            RedirectUri.Create("https://test.com/callback2"),
            RedirectUri.Create("https://test.com/callback3")
        };

        // Act
        var client = Client.CreateConfidential(
            "Test App",
            "test-123",
            ClientSecret.Generate(),
            redirectUris,
            new[] { "openid" });

        // Assert
        client.RedirectUris.Should().HaveCount(3);
    }

    [Fact]
    public void CreatePublic_WithMultipleScopes_ShouldAddAllScopes()
    {
        // Arrange
        var scopes = new[] { "openid", "profile", "email", "phone", "address" };

        // Act
        var client = Client.CreatePublic(
            "Test App",
            "test-123",
            new[] { RedirectUri.Create("https://test.com/callback") },
            scopes);

        // Assert
        client.AllowedScopes.Should().HaveCount(5);
        client.AllowedScopes.Should().Contain(scopes);
    }
}
