using FluentAssertions;
using FluentValidation.TestHelper;
using GateKeeper.Application.Users.DTOs;
using GateKeeper.Application.Users.Validators;

namespace GateKeeper.Application.Tests.Users.Validators;

/// <summary>
/// Tests for RegisterUserDtoValidator.
/// Uses FluentValidation.TestHelper for concise test syntax.
/// </summary>
public class RegisterUserDtoValidatorTests
{
    private readonly RegisterUserDtoValidator _validator;

    public RegisterUserDtoValidatorTests()
    {
        _validator = new RegisterUserDtoValidator();
    }

    #region Valid Data Tests

    [Fact]
    public void Validate_WithValidData_ShouldNotHaveValidationErrors()
    {
        // Arrange
        var dto = new RegisterUserDto
        {
            Email = "valid@example.com",
            Password = "SecurePass123!",
            ConfirmPassword = "SecurePass123!",
            FirstName = "John",
            LastName = "Doe"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region Email Validation Tests

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyEmail_ShouldHaveValidationError(string email)
    {
        // Arrange
        var dto = new RegisterUserDto
        {
            Email = email,
            Password = "SecurePass123!",
            ConfirmPassword = "SecurePass123!",
            FirstName = "John",
            LastName = "Doe"
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
    [InlineData("user")]
    public void Validate_WithInvalidEmailFormat_ShouldHaveValidationError(string email)
    {
        // Arrange
        var dto = new RegisterUserDto
        {
            Email = email,
            Password = "SecurePass123!",
            ConfirmPassword = "SecurePass123!",
            FirstName = "John",
            LastName = "Doe"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Validate_WithTooLongEmail_ShouldHaveValidationError()
    {
        // Arrange - Email longer than 254 characters
        var longEmail = new string('a', 250) + "@test.com"; // 259 characters total
        var dto = new RegisterUserDto
        {
            Email = longEmail,
            Password = "SecurePass123!",
            ConfirmPassword = "SecurePass123!",
            FirstName = "John",
            LastName = "Doe"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email)
            .WithErrorMessage("Email must not exceed 254 characters");
    }

    #endregion

    #region Password Validation Tests

    [Fact]
    public void Validate_WithEmptyPassword_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new RegisterUserDto
        {
            Email = "valid@example.com",
            Password = "",
            ConfirmPassword = "",
            FirstName = "John",
            LastName = "Doe"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password is required");
    }

    [Theory]
    [InlineData("short")]
    [InlineData("Pass1!")]
    public void Validate_WithTooShortPassword_ShouldHaveValidationError(string password)
    {
        // Arrange
        var dto = new RegisterUserDto
        {
            Email = "valid@example.com",
            Password = password,
            ConfirmPassword = password,
            FirstName = "John",
            LastName = "Doe"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password must be at least 8 characters");
    }

    [Theory]
    [InlineData("nouppercase123!")]
    public void Validate_WithNoUppercaseLetter_ShouldHaveValidationError(string password)
    {
        // Arrange
        var dto = new RegisterUserDto
        {
            Email = "valid@example.com",
            Password = password,
            ConfirmPassword = password,
            FirstName = "John",
            LastName = "Doe"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password must contain at least one uppercase letter");
    }

    [Theory]
    [InlineData("NOLOWERCASE123!")]
    public void Validate_WithNoLowercaseLetter_ShouldHaveValidationError(string password)
    {
        // Arrange
        var dto = new RegisterUserDto
        {
            Email = "valid@example.com",
            Password = password,
            ConfirmPassword = password,
            FirstName = "John",
            LastName = "Doe"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password must contain at least one lowercase letter");
    }

    [Theory]
    [InlineData("NoDigits!")]
    [InlineData("Password!")]
    public void Validate_WithNoDigit_ShouldHaveValidationError(string password)
    {
        // Arrange
        var dto = new RegisterUserDto
        {
            Email = "valid@example.com",
            Password = password,
            ConfirmPassword = password,
            FirstName = "John",
            LastName = "Doe"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password must contain at least one number");
    }

    [Theory]
    [InlineData("NoSpecialChar123")]
    [InlineData("Password123")]
    public void Validate_WithNoSpecialCharacter_ShouldHaveValidationError(string password)
    {
        // Arrange
        var dto = new RegisterUserDto
        {
            Email = "valid@example.com",
            Password = password,
            ConfirmPassword = password,
            FirstName = "John",
            LastName = "Doe"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password must contain at least one special character");
    }

    [Fact]
    public void Validate_WithTooLongPassword_ShouldHaveValidationError()
    {
        // Arrange - Password longer than 100 characters (needs 101+ chars)
        // Create 101 character password: 96 lowercase + uppercase + digit + special + 2 more chars
        var longPassword = new string('a', 96) + "A1!XX"; // 96 + 5 = 101 characters
        var dto = new RegisterUserDto
        {
            Email = "valid@example.com",
            Password = longPassword,
            ConfirmPassword = longPassword,
            FirstName = "John",
            LastName = "Doe"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Password)
            .WithErrorMessage("Password must not exceed 100 characters");
    }

    #endregion

    #region ConfirmPassword Validation Tests

    [Fact]
    public void Validate_WithEmptyConfirmPassword_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new RegisterUserDto
        {
            Email = "valid@example.com",
            Password = "SecurePass123!",
            ConfirmPassword = "",
            FirstName = "John",
            LastName = "Doe"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ConfirmPassword)
            .WithErrorMessage("Password confirmation is required");
    }

    [Fact]
    public void Validate_WithMismatchedPasswords_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new RegisterUserDto
        {
            Email = "valid@example.com",
            Password = "SecurePass123!",
            ConfirmPassword = "DifferentPass123!",
            FirstName = "John",
            LastName = "Doe"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ConfirmPassword)
            .WithErrorMessage("Passwords do not match");
    }

    #endregion

    #region Name Validation Tests

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyFirstName_ShouldHaveValidationError(string firstName)
    {
        // Arrange
        var dto = new RegisterUserDto
        {
            Email = "valid@example.com",
            Password = "SecurePass123!",
            ConfirmPassword = "SecurePass123!",
            FirstName = firstName,
            LastName = "Doe"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FirstName)
            .WithErrorMessage("First name is required");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyLastName_ShouldHaveValidationError(string lastName)
    {
        // Arrange
        var dto = new RegisterUserDto
        {
            Email = "valid@example.com",
            Password = "SecurePass123!",
            ConfirmPassword = "SecurePass123!",
            FirstName = "John",
            LastName = lastName
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.LastName)
            .WithErrorMessage("Last name is required");
    }

    [Fact]
    public void Validate_WithTooLongFirstName_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new RegisterUserDto
        {
            Email = "valid@example.com",
            Password = "SecurePass123!",
            ConfirmPassword = "SecurePass123!",
            FirstName = new string('a', 101),
            LastName = "Doe"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.FirstName)
            .WithErrorMessage("First name must not exceed 100 characters");
    }

    [Fact]
    public void Validate_WithTooLongLastName_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new RegisterUserDto
        {
            Email = "valid@example.com",
            Password = "SecurePass123!",
            ConfirmPassword = "SecurePass123!",
            FirstName = "John",
            LastName = new string('a', 101)
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.LastName)
            .WithErrorMessage("Last name must not exceed 100 characters");
    }

    #endregion
}
