using FluentAssertions;
using GateKeeper.Domain.Exceptions;
using GateKeeper.Domain.ValueObjects;

namespace GateKeeper.Domain.Tests.ValueObjects;

/// <summary>
/// Tests for Email value object validation and normalization.
/// </summary>
public class EmailTests
{
    [Fact]
    public void Create_WithValidEmail_ShouldReturnEmail()
    {
        // Arrange
        var emailString = "user@example.com";

        // Act
        var email = Email.Create(emailString);

        // Assert
        email.Should().NotBeNull();
        email.Value.Should().Be("user@example.com");
    }

    [Fact]
    public void Create_ShouldNormalizeEmail_ToLowerCase()
    {
        // Arrange
        var emailString = "User@Example.COM";

        // Act
        var email = Email.Create(emailString);

        // Assert
        email.Value.Should().Be("user@example.com");
    }

    [Fact]
    public void Create_ShouldTrimWhitespace()
    {
        // Arrange
        var emailString = "  user@example.com  ";

        // Act
        var email = Email.Create(emailString);

        // Assert
        email.Value.Should().Be("user@example.com");
    }

    [Theory]
    [InlineData(null!)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithNullOrEmptyEmail_ShouldThrowDomainException(string? invalidEmail)
    {
        // Act
        Action act = () => Email.Create(invalidEmail!);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("Email cannot be empty");
    }

    [Theory]
    [InlineData("notanemail")]
    [InlineData("@nodomain.com")]
    [InlineData("multiple@@at.com")]
    [InlineData("missing@domain")]          // No TLD
    [InlineData("user@localhost")]          // No TLD - should be rejected for OAuth provider
    [InlineData("no@symbol")]               // Invalid format
    [InlineData("spaces in@email.com")]     // Contains spaces
    [InlineData("user@.com")]               // Missing domain
    [InlineData("user@domain.")]            // Missing TLD
    [InlineData("user@domain..com")]        // Double dot
    public void Create_WithInvalidEmailFormat_ShouldThrowDomainException(string invalidEmail)
    {
        // Act
        Action act = () => Email.Create(invalidEmail);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*Invalid email format*");
    }

    [Theory]
    [InlineData("")]                        // Empty
    [InlineData("   ")]                     // Whitespace only
    public void Create_WithEmptyEmail_ShouldThrowDomainException(string invalidEmail)
    {
        // Act
        Action act = () => Email.Create(invalidEmail);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*Email cannot be empty*");
    }

    [Fact]
    public void Create_WithTooLongEmail_ShouldThrowDomainException()
    {
        // Arrange - 255 characters total
        var localPart = new string('a', 64);
        var domainPart = new string('b', 186) + ".com"; // 64 + 1 (@) + 190 = 255
        var tooLongEmail = $"{localPart}@{domainPart}";

        // Act
        Action act = () => Email.Create(tooLongEmail);

        // Assert
        act.Should().Throw<DomainException>()
            .WithMessage("*too long*");
    }

    [Theory]
    [InlineData("user@example.com")]
    [InlineData("user.name@example.com")]
    [InlineData("user+tag@example.co.uk")]
    [InlineData("first.last@subdomain.example.com")]
    [InlineData("user123@test-domain.org")]
    [InlineData("a@example.io")]
    public void Create_WithValidEmails_ShouldSucceed(string validEmail)
    {
        // Act
        var email = Email.Create(validEmail);

        // Assert
        email.Should().NotBeNull();
        email.Value.Should().Be(validEmail.ToLowerInvariant());
    }

    [Fact]
    public void Email_WithSameValue_ShouldBeEqual()
    {
        // Arrange
        var email1 = Email.Create("user@example.com");
        var email2 = Email.Create("user@example.com");

        // Assert
        email1.Should().Be(email2);
    }

    [Fact]
    public void Email_WithDifferentValue_ShouldNotBeEqual()
    {
        // Arrange
        var email1 = Email.Create("user1@example.com");
        var email2 = Email.Create("user2@example.com");

        // Assert
        email1.Should().NotBe(email2);
    }
}
