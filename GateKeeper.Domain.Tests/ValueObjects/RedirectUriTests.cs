using FluentAssertions;
using GateKeeper.Domain.Exceptions;
using GateKeeper.Domain.ValueObjects;

namespace GateKeeper.Domain.Tests.ValueObjects;

/// <summary>
/// Tests for RedirectUri value object validation.
/// Critical for OAuth security - ensures only valid, secure URIs are registered.
/// </summary>
public class RedirectUriTests
{
    [Fact]
    public void Create_WithValidHttpsUri_ShouldReturnRedirectUri()
    {
        // Arrange
        var uriString = "https://app.example.com/callback";

        // Act
        var redirectUri = RedirectUri.Create(uriString);

        // Assert
        redirectUri.Should().NotBeNull();
        redirectUri.Value.Should().Be(uriString);
    }

    [Fact]
    public void Create_WithLocalhostHttp_ShouldBeAllowed()
    {
        // Arrange
        var uriString = "http://localhost:3000/callback";

        // Act
        var redirectUri = RedirectUri.Create(uriString);

        // Assert
        redirectUri.Should().NotBeNull();
        redirectUri.Value.Should().Be(uriString);
    }

    [Theory]
    [InlineData("http://localhost/callback")]
    [InlineData("http://localhost:5000/auth")]
    [InlineData("http://LOCALHOST:8080/")]
    public void Create_WithLocalhostVariations_ShouldBeAllowed(string uriString)
    {
        // Act
        var redirectUri = RedirectUri.Create(uriString);

        // Assert
        redirectUri.Should().NotBeNull();
        redirectUri.Value.Should().Be(uriString);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrEmptyUri_ShouldThrowDomainException(string invalidUri)
    {
        // Act
        Action act = () => RedirectUri.Create(invalidUri);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Redirect URI cannot be empty");
    }

    [Theory]
    [InlineData("not-a-uri")]
    [InlineData("relative/path")]
    [InlineData("//no-scheme.com")]
    public void Create_WithInvalidUri_ShouldThrowInvalidRedirectUriException(string invalidUri)
    {
        // Act
        Action act = () => RedirectUri.Create(invalidUri);

        // Assert
        act.Should().Throw<InvalidRedirectUriException>();
    }

    [Theory]
    [InlineData("http://example.com/callback")]
    [InlineData("http://192.168.1.1/auth")]
    [InlineData("http://app.mycompany.com/redirect")]
    public void Create_WithHttpNonLocalhost_ShouldThrowInvalidRedirectUriException(string insecureUri)
    {
        // Act
        Action act = () => RedirectUri.Create(insecureUri);

        // Assert
        act.Should().Throw<InvalidRedirectUriException>()
            .WithMessage("*HTTPS required for non-localhost URIs*");
    }

    [Theory]
    [InlineData("https://app.example.com/callback")]
    [InlineData("https://oauth.myapp.com/redirect")]
    [InlineData("https://example.com:8443/auth")]
    public void Create_WithValidHttpsUris_ShouldSucceed(string httpsUri)
    {
        // Act
        var redirectUri = RedirectUri.Create(httpsUri);

        // Assert
        redirectUri.Should().NotBeNull();
        redirectUri.Value.Should().Be(httpsUri);
    }

    [Fact]
    public void RedirectUri_WithSameValue_ShouldBeEqual()
    {
        // Arrange
        var uri1 = RedirectUri.Create("https://example.com/callback");
        var uri2 = RedirectUri.Create("https://example.com/callback");

        // Assert
        uri1.Should().Be(uri2);
    }

    [Fact]
    public void RedirectUri_WithDifferentValue_ShouldNotBeEqual()
    {
        // Arrange
        var uri1 = RedirectUri.Create("https://example.com/callback1");
        var uri2 = RedirectUri.Create("https://example.com/callback2");

        // Assert
        uri1.Should().NotBe(uri2);
    }
}
