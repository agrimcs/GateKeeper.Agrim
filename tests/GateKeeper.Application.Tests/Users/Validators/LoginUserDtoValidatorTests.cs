using FluentAssertions;
using FluentValidation.TestHelper;
using GateKeeper.Application.Users.DTOs;
using GateKeeper.Application.Users.Validators;

namespace GateKeeper.Application.Tests.Users.Validators;

/// <summary>
/// Tests for LoginUserDtoValidator.
/// </summary>
public class LoginUserDtoValidatorTests
{
    private readonly LoginUserDtoValidator _validator;

    public LoginUserDtoValidatorTests()
    {
        _validator = new LoginUserDtoValidator();
    }

    [Fact]
    public void Validate_WithValidData_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var dto = new LoginUserDto
        {
            Email = "user@example.com",
            Password = "AnyPassword"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyEmail_ShouldHaveValidationError(string email)
    {
        // Arrange
        var dto = new LoginUserDto
        {
            Email = email,
            Password = "AnyPassword"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Email is required");
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("@example.com")]
    [InlineData("user@")]
    public void Validate_WithInvalidEmailFormat_ShouldHaveValidationError(string email)
    {
        // Arrange
        var dto = new LoginUserDto
        {
            Email = email,
            Password = "AnyPassword"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Invalid email format");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyPassword_ShouldHaveValidationError(string password)
    {
        // Arrange
        var dto = new LoginUserDto
        {
            Email = "user@example.com",
            Password = password
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password is required");
    }
}
