using FluentAssertions;
using GateKeeper.Infrastructure.Security;

namespace GateKeeper.Infrastructure.Tests.Security;

public class BcryptPasswordHasherTests
{
    private readonly BcryptPasswordHasher _hasher;

    public BcryptPasswordHasherTests()
    {
        _hasher = new BcryptPasswordHasher();
    }

    [Fact]
    public void HashPassword_ShouldReturnHashedPassword()
    {
        // Arrange
        var password = "Test123!@#";

        // Act
        var hashedPassword = _hasher.HashPassword(password);

        // Assert
        hashedPassword.Should().NotBeNullOrEmpty();
        hashedPassword.Should().NotBe(password);
        hashedPassword.Should().StartWith("$2");
    }

    [Fact]
    public void HashPassword_ShouldThrowException_WhenPasswordIsNull()
    {
        // Act & Assert
        var act = () => _hasher.HashPassword(null!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void VerifyPassword_ShouldReturnTrue_WhenPasswordMatches()
    {
        // Arrange
        var password = "Test123!@#";
        var hashedPassword = _hasher.HashPassword(password);

        // Act
        var result = _hasher.VerifyPassword(hashedPassword, password);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_ShouldReturnFalse_WhenPasswordDoesNotMatch()
    {
        // Arrange
        var password = "Test123!@#";
        var hashedPassword = _hasher.HashPassword(password);

        // Act
        var result = _hasher.VerifyPassword(hashedPassword, "WrongPassword");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void VerifyPassword_ShouldReturnFalse_WhenProvidedPasswordIsNull()
    {
        // Arrange
        var hashedPassword = _hasher.HashPassword("Test123");

        // Act
        var result = _hasher.VerifyPassword(hashedPassword, null!);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void HashPassword_ShouldGenerateDifferentHashesForSamePassword()
    {
        // Arrange
        var password = "Test123!@#";

        // Act
        var hash1 = _hasher.HashPassword(password);
        var hash2 = _hasher.HashPassword(password);

        // Assert
        hash1.Should().NotBe(hash2); // BCrypt uses random salts
        _hasher.VerifyPassword(hash1, password).Should().BeTrue();
        _hasher.VerifyPassword(hash2, password).Should().BeTrue();
    }
}
